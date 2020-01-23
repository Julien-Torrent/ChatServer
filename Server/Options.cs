using CommandLine;

namespace Server
{
    class Options
    {
        [Option('a', "address", Required = true, HelpText = "The address the server will listen to")]
        public string IPAddress { get; set; }

        [Option('p', "port", Default = 4000, Required = false, HelpText = "The port the server will listen to")]
        public int Port { get; set; }
    }
}
