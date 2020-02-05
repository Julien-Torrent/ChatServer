using CommandLine;

namespace Server
{
    class Options
    {
        [Option('a', "address", Required = true, HelpText = "The address the server will listen to")]
        public string IPAddress { get; set; }

        [Option('p', "port", Default = 4000, Required = false, HelpText = "The port the server will listen to")]
        public int Port { get; set; }

        [Option('s', "size", Default = 10, Required = false, HelpText = "The maximum number of clients simultaneously connected")]
        public int MaxClients { get; set; }
    }
}
