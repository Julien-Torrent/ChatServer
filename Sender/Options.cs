using CommandLine;

namespace Sender
{
    class Options
    {
        [Option('a', "address", Required = true, HelpText = "The address the client will try to connect")]
        public string IPAddress { get; set; }

        [Option('p', "port", Default = 4000, Required = false, HelpText = "The port the client will try to connect")]
        public int Port { get; set; }

        [Option('u', "username", Required = true, HelpText = "The name this client will have in the server")]
        public string Username { get; set; }
    }
}