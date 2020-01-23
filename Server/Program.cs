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
                var srv = new ChatServer(options.IPAddress, options.Port);
                srv.Start();

                // Wait until the quit command is executed
                Console.WriteLine("Type quit or press Ctrl^C to stop the server");
                while (Console.ReadLine() != "quit")
                {
                    Task.Delay(100).Wait();
                }

                srv.Stop();
            });
        }
    }
}
