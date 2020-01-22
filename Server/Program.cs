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
            var srv = new ChatServer("127.0.0.1", 4000);
            srv.Start();

            // Wait until the quit command is executed
            while (Console.ReadLine() != "quit")
            {
                Task.Delay(100).Wait();
            }

            srv.Stop();
        }
    }
}
