using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

class Program
{
    private static List<TcpClient> clients = new List<TcpClient>();
    private static readonly object clientLock = new object();
    private static int connectedClients = 0; // A csatlakozott kliensek száma

    static void Main(string[] args)
    {
        const int port = 8080;
        TcpListener listener = new TcpListener(IPAddress.Any, port);
        listener.Start();

        Console.WriteLine("Szerver fut...");

        while (true)
        {
            Console.WriteLine("Új kliens csatlakozására várakozás...");
            TcpClient newClient = listener.AcceptTcpClient();

            lock (clientLock)
            {
                clients.Add(newClient);
                connectedClients++;
            }

            Console.WriteLine($"Új kliens csatlakozott! Jelenlegi kliensek száma: {connectedClients}");

            // Új szál indítása a kliens kezelésére
            Thread clientThread = new Thread(() => HandleClient(newClient));
            clientThread.Start();

            // Ellenőrizzük, hogy mindkét kliens csatlakozott-e
            lock (clientLock)
            {
                if (connectedClients == 2)
                {
                    BroadcastMessage("start");
                    Console.WriteLine("Mindkét kliens csatlakozott! 'start' üzenet kiküldve.");
                }
            }
        }
    }

    private static void HandleClient(TcpClient client)
    {
        try
        {
            using NetworkStream stream = client.GetStream();
            byte[] buffer = new byte[1024];

            // Üzenetek kezelése a klienssel
            while (true)
            {
                int bytesRead = stream.Read(buffer, 0, buffer.Length);
                if (bytesRead == 0)
                {
                    Console.WriteLine("Kliens levált.");
                    break;
                }

                string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                Console.WriteLine($"Üzenet a klienstől: {message}");

                // Például: válasz visszaküldése
                if (message.Trim().ToLower() == "join")
                {
                    string response = "ok";
                    byte[] responseBytes = Encoding.UTF8.GetBytes(response);
                    stream.Write(responseBytes, 0, responseBytes.Length);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Hiba a kliens kezelésében: {ex.Message}");
        }
        finally
        {
            lock (clientLock)
            {
                clients.Remove(client);
                connectedClients--;
                Console.WriteLine($"Kliens eltávolítva. Jelenlegi kliensek száma: {connectedClients}");
            }
            client.Close();
            Console.WriteLine("Kliens kapcsolat bezárva.");
        }
    }

    private static void BroadcastMessage(string message)
    {
        lock (clientLock)
        {
            byte[] messageBytes = Encoding.UTF8.GetBytes(message);
            foreach (var client in clients)
            {
                try
                {
                    NetworkStream stream = client.GetStream();
                    stream.Write(messageBytes, 0, messageBytes.Length);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Hiba az üzenet küldésekor: {ex.Message}");
                }
            }
        }
    }
}
