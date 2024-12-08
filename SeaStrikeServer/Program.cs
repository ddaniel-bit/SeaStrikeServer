using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SeaStrikeServer
{
    class Program
    {
        static List<TcpClient> clients = new List<TcpClient>();
        static Dictionary<TcpClient, string> clientIds = new Dictionary<TcpClient, string>();
        static TcpListener server;
        static List<int[,]> matrixlista = new List<int[,]>();
        static int clientCounter = 1; // Kezdjük el a klienseket 1-től számozni
        static SemaphoreSlim semaphore = new SemaphoreSlim(1, 1); // Szinkronizációhoz

        static async Task Main(string[] args)
        {
            const int port = 8080;
            server = new TcpListener(IPAddress.Any, port);
            server.Start();

            Console.WriteLine($"Szerver elindult a {port} porton...");

            while (true)
            {
                // Új kliens elfogadása
                TcpClient client = await server.AcceptTcpClientAsync();

                await semaphore.WaitAsync();
                try
                {
                    clients.Add(client);
                }
                finally
                {
                    semaphore.Release();
                }

                // Kliens azonosító hozzárendelése
                string clientId = $"Kliens{clientCounter++}";
                clientIds[client] = clientId;
                Console.WriteLine($"Új kliens csatlakozott ({clientId}). Jelenlegi kliensszám: {clients.Count}");

                _ = HandleClientAsync(client, clientId);
            }
        }

        static async Task HandleClientAsync(TcpClient client, string clientId)
        {
            NetworkStream stream = client.GetStream();
            byte[] buffer = new byte[1024];

            try
            {
                while (true)
                {
                    int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                    if (bytesRead == 0)
                    {
                        Console.WriteLine($"{clientId} lecsatlakozott.");
                        break;
                    }

                    string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    Console.WriteLine($"Üzenet a {clientId} ({client.Client.RemoteEndPoint}) címről: {message}");

                    if (message == "join")
                    {
                        // "ok" válasz küldése
                        await SendToClientAsync(client, "ok");

                        // Ha elég kliens csatlakozott, küldje ki a "start" üzenetet
                        await semaphore.WaitAsync();
                        try
                        {
                            if (clients.Count >= 2)
                            {
                                foreach (var c in clients)
                                {
                                    await SendToClientAsync(c, "start");
                                }
                            }
                        }
                        finally
                        {
                            semaphore.Release();
                        }
                    }
                    else if (message.StartsWith("{"))
                    {
                        // Mátrix feldolgozása
                        int[,] matrix = ConvertStringToMatrix(message);
                        Console.WriteLine($"{clientId} által küldött mátrix:");
                        PrintMatrix(matrix);

                        await semaphore.WaitAsync();
                        try
                        {
                            matrixlista.Add(matrix);

                            if (matrixlista.Count == 2)
                            {
                                // Mindkét kliens elküldte a mátrixot, játék indítása...
                                Console.WriteLine("Mindkét kliens küldött mátrixot, játék indítása...");

                                foreach (var c in clients)
                                {
                                    await SendToClientAsync(c, "gamestart");
                                }

                                // Késleltetés és üzenetküldés
                                var client1 = clients[0];
                                if (client1 != null)
                                {
                                    await Task.Delay(2000);
                                    await SendToClientAsync(client1, "yourturn");
                                    Console.WriteLine("yourturn elküldve az első kliensnek");
                                }


                                foreach (var c in clients)
                                {
                                    await SendToClientAsync(c, "test");
                                }
                            }
                        }
                        finally
                        {
                            semaphore.Release();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Hiba a kliens kezelésében: {ex.Message}");
            }
            finally
            {
                await semaphore.WaitAsync();
                try
                {
                    clients.Remove(client);
                    clientIds.Remove(client);
                }
                finally
                {
                    semaphore.Release();
                }
                stream.Close();
                client.Close();
            }
        }

        static async Task SendToClientAsync(TcpClient client, string message)
        {
            if (client.Connected)
            {
                NetworkStream stream = client.GetStream();
                byte[] messageBytes = Encoding.UTF8.GetBytes(message);
                await stream.WriteAsync(messageBytes, 0, messageBytes.Length);
            }
        }

        static void PrintMatrix(int[,] matrix)
        {
            int rows = matrix.GetLength(0);
            int cols = matrix.GetLength(1);

            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    Console.Write(matrix[i, j] + " ");
                }
                Console.WriteLine();
            }
        }

        static int[,] ConvertStringToMatrix(string matrixString)
        {
            // Például: "{1,0,0},{0,1,0},{0,0,1}"
            string[] rows = matrixString.Trim(new char[] { '{', '}' }).Split("},{");
            int rowCount = rows.Length;
            int colCount = rows[0].Split(',').Length;
            int[,] matrix = new int[rowCount, colCount];

            for (int i = 0; i < rowCount; i++)
            {
                string[] elements = rows[i].Split(',');
                for (int j = 0; j < colCount; j++)
                {
                    matrix[i, j] = int.Parse(elements[j]);
                }
            }

            return matrix;
        }
    }
}
