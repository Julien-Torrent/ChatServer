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
        static void Main(string[] args)
        {
            Parser.Default.ParseArguments<Options>(args).WithParsed(options =>
            {
                var srv = new ChatServer(options.IPAddress, options.Port, options.MaxClients);
                srv.Start();

                // Windows closed
                AppDomain.CurrentDomain.ProcessExit += (sender, args) => srv.Stop();

                // Ctrl + C or Ctrl + Break
                Console.CancelKeyPress += new ConsoleCancelEventHandler((sender, args) => srv.Stop());


                // Wait until the quit command is executed
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
