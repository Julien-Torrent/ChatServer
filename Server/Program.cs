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
        private const int _port = 4000;
        private const string _address = "0.0.0.0";

        static void Main(string[] args)
        {
            Parser.Default.ParseArguments<Options>(args).WithParsed(options =>
            {
                var srv = new ChatServer(_address, _port, options.MaxClients);
                srv.Start();

                // Windows closed
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

                // User types quit
                srv.Stop();
            });
        }
    }
}
