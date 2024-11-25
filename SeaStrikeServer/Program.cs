using System.Net.Sockets;
using System.Net;
using System.Text;

internal class Program
{
    private static void Main(string[] args)
    {
        int port = 12345; // A port, amelyen a szerver figyel
        TcpListener server = null;

        try
        {
            // Szerver inicializálása
            server = new TcpListener(IPAddress.Any, port);
            server.Start();
            Console.WriteLine($"Szerver elindult és figyel a {port} porton...");

            while (true)
            {
                Console.WriteLine("Várakozás kapcsolatra...");
                TcpClient client = server.AcceptTcpClient();
                Console.WriteLine("Kapcsolat létrejött!");

                try
                {
                    // Üzenet fogadása
                    NetworkStream stream = client.GetStream();
                    byte[] buffer = new byte[1024];
                    int bytesRead = stream.Read(buffer, 0, buffer.Length);
                    string clientMessage = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                    Console.WriteLine($"Üzenet a klienstől: {clientMessage}");

                    // Válasz küldése a kliensnek
                    string response = "Üdv, kliens! A szerver fogadta az üzenetedet.";
                    byte[] responseData = Encoding.UTF8.GetBytes(response);
                    stream.Write(responseData, 0, responseData.Length);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Hiba történt a kliens kezelésében: {ex.Message}");
                }
                finally
                {
                    client.Close();
                    Console.WriteLine("Kapcsolat lezárva.");
                }
            }
        }
        catch (SocketException ex)
        {
            Console.WriteLine($"Socket hiba történt: {ex.Message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Hiba történt: {ex.Message}");
        }
        finally
        {
            server?.Stop();
            Console.WriteLine("Szerver leállt.");
        }
    }
}