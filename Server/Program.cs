using CommandLine;
using System;
using System.Threading.Tasks;

namespace Server
{
    /// <summary>
    /// Main class to launch the ChatServer
    /// </summary>
    class Program
    {
        /// <summary>
        /// We listen to the port 4000 of the container, the user will bind the desired port ( xxxx:4000 )
        /// </summary>
        private const int _port = 4000;
        
        /// <summary>
        /// The address the container is listening to. We listen to all the adresses of the container.
        /// </summary>
        private const string _address = "0.0.0.0";

        static void Main(string[] args)
        {
            Parser.Default.ParseArguments<Options>(args).WithParsed(options =>
            {
                var srv = new ChatServer(_address, _port, options.MaxClients);
                srv.Start();

                // Windows closed or user types quit
                AppDomain.CurrentDomain.ProcessExit += (sender, args) => srv.Stop();

                // Ctrl + C or Ctrl + Break
                Console.CancelKeyPress += new ConsoleCancelEventHandler((sender, args) => srv.Stop());

                // Wait until the quit command is executed
                Console.WriteLine($"Server is now listening on {_address}:{_port}");
                Console.WriteLine("Type 'quit' or press Ctrl^C to stop the server");
                while (Console.ReadLine() != "quit")
                {
                    Task.Delay(100).Wait();
                }
            });
        }
    }
}
