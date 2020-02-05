using CommandLine;

namespace Server
{
    class Options
    {
        [Option('s', "size", Default = 10, Required = false, HelpText = "The maximum number of clients simultaneously connected")]
        public int MaxClients { get; set; }
    }
}
