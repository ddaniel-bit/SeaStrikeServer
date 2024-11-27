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
        static TcpListener server;

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
                lock (clients)
                {
                    clients.Add(client);
                }
                Console.WriteLine($"Új kliens csatlakozott. Jelenlegi kliensszám: {clients.Count}");
                _ = HandleClientAsync(client);
            }
        }

        static async Task HandleClientAsync(TcpClient client)
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
                        Console.WriteLine("Kliens lecsatlakozott.");
                        break;
                    }

                    string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    Console.WriteLine($"Üzenet: {message}");

                    if (message == "join")
                    {
                        // "ok" válasz küldése
                        await SendToClientAsync(client, "ok");

                        // Ha elég kliens csatlakozott, küldje ki a "start" üzenetet
                        lock (clients)
                        {
                            if (clients.Count >= 2)
                            {
                                foreach (var c in clients)
                                {
                                    SendToClientAsync(c, "start").Wait();
                                }

                                // 10 másodperc késleltetés után üzenetek küldése specifikus klienseknek
                                /*_ = Task.Run(async () =>
                                {
                                    await Task.Delay(10000); // 10 másodperc késleltetés

                                    lock (clients)
                                    {
                                        if (clients.Count >= 2)
                                        {
                                            SendToClientAsync(clients[0], "1. kliens teszt üzenet").Wait();
                                            SendToClientAsync(clients[1], "2. kliens teszt üzenet").Wait();
                                        }
                                    }
                                });*/
                            }
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
                lock (clients)
                {
                    clients.Remove(client);
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
    }
}
