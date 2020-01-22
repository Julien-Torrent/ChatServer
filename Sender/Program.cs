using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Sender
{
    class Program
    {
        static void Main(string[] args)
        {
            // Get the username of the user
            Console.WriteLine("Enter your username:");
            var usr = Console.ReadLine();

            try
            {
                // Tries to connect to the server
                var client = new TcpClient();
                client.Connect(IPAddress.Parse("127.0.0.1"), 4000);

                // Send the username to the server (must be send immediately after the connection)
                client.GetStream().Write(Encoding.UTF8.GetBytes(usr));

                // Start to read the incoming data from the server
                Task.Run(() => Read(client));

                // While the client is connected, read what the user want to write
                while (client.Connected)
                {
                    client.GetStream().Write(Encoding.UTF8.GetBytes(Console.ReadLine()));
                }
            }
            catch(Exception)
            {
                // Couldn't connect to the server
                Console.WriteLine("The server is either full or shutdown");
            }

            Console.ReadLine();
        }

        /// <summary>
        /// Reads the data coming in the TcpClient as UTF8 string
        /// and Write the to the console
        /// </summary>
        /// <param name="client">The TcpClient to listen for incoming messages</param>
        /// <returns>Awaitable Task</returns>
        static async Task Read(TcpClient client)
        {
            while (client.Connected)
            {
                while (client.GetStream().DataAvailable)
                {
                    // Read the string from the stream
                    byte[] buffer = new byte[client.Available];
                    var bytes = await client.GetStream().ReadAsync(buffer, 0, client.Available);
                    var text = Encoding.UTF8.GetString(buffer, 0, bytes);

                    // Print the username in blue
                    Console.ForegroundColor = ConsoleColor.Blue;
                    Console.Write(text.Split(':').First());

                    // Print the rest of the message in gray
                    Console.ForegroundColor = ConsoleColor.Gray;
                    Console.WriteLine(text[text.Split(':').First().Length..]);
                }

                await Task.Delay(50);
            }
        }
    }
}
