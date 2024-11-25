using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

class Program
{
    static List<TcpClient> clients = new List<TcpClient>(); // Lista a csatlakozott kliensek tárolására
    static bool gameStarted = false; // Játék kezdési állapot

    static void Main()
    {
        TcpListener server = new TcpListener(IPAddress.Parse("127.0.0.1"), 5000);
        server.Start();
        Console.WriteLine("Szerver indult...");

        while (true)
        {
            // Várakozás új kliens csatlakozására
            TcpClient client = server.AcceptTcpClient();
            clients.Add(client);
            Console.WriteLine("Új kliens csatlakozott!");

            Thread clientThread = new Thread(() => HandleClient(client));
            clientThread.Start();
        }
    }

    static void HandleClient(TcpClient client)
    {
        NetworkStream stream = client.GetStream();
        byte[] buffer = new byte[1024];
        int bytesRead;

        // Várakozás a "join" üzenetre
        bytesRead = stream.Read(buffer, 0, buffer.Length);
        string message = Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim();

        if (message == "join")
        {
            // Ha van elég hely, válaszolj "ok"-kal
            if (clients.Count == 1)
            {
                // Az első kliens csatlakozik, válaszolj "ok"-kal
                SendMessage(client, "ok");
            }
            else if (clients.Count == 2)
            {
                // A második kliens csatlakozik
                SendMessage(client, "ok");
                // Küldd el az "isactive" üzenetet az első kliensnek
                SendMessage(clients[0], "isactive");
                // Küldd el az "isactive" üzenetet a második kliensnek is
                SendMessage(client, "isactive");
            }
            else
            {
                // Ha a szerver tele van
                SendMessage(client, "Server full");
            }

            // Várakozás, amíg mindkét kliens aktív válasza megérkezik
            bool allActive = false;
            while (!allActive)
            {
                string response1 = ReceiveMessage(clients[0]);
                string response2 = ReceiveMessage(clients[1]);

                if (response1 == "active" && response2 == "active")
                {
                    allActive = true;
                    // Ha mindkét kliens aktív, indítsuk el a játékot
                    foreach (var c in clients)
                    {
                        SendMessage(c, "start");
                    }
                    Console.WriteLine("A játék elindult!");
                }
            }
        }
    }

    static string ReceiveMessage(TcpClient client)
    {
        NetworkStream stream = client.GetStream();
        byte[] buffer = new byte[1024];
        int bytesRead = stream.Read(buffer, 0, buffer.Length);
        return Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim();
    }

    static void SendMessage(TcpClient client, string message)
    {
        NetworkStream stream = client.GetStream();
        byte[] data = Encoding.UTF8.GetBytes(message);
        stream.Write(data, 0, data.Length);
    }
}
