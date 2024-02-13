using CommandLine;

namespace StackelbergCompiler
{
    public class Options
    {
        [Option("domain", Required = true, HelpText = "Path to the domain file to use")]
        public string DomainFilePath { get; set; } = "";
        [Option("problem", Required = true, HelpText = "Path to the problem file to use")]
        public string ProblemFilePath { get; set; } = "";
        [Option("meta-action", Required = true, HelpText = "Path to the meta action file")]
        public string MetaActionFile { get; set; } = "";
        [Option("output", Required = false, HelpText = "Path to where to output the generated files.")]
        public string OutputPath { get; set; } = "";
    }
}
