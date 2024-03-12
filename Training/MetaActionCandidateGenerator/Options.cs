using CommandLine;

namespace MetaActionCandidateGenerator
{
    public class Options
    {
        [Flags]
        public enum GeneratorStrategies
        {
            None = 1,
            PredicateMetaActions = 2,
            StrippedMetaActions = 3,
            AgressiveStrippedMetaActions = 4,
            MutexedMetaActions = 5,
            InvariantMetaActions = 6
        }

        [Option("output", Required = true, HelpText = "Where to output the meta actions")]
        public string OutputPath { get; set; } = "";
        [Option("domain", Required = true, HelpText = "Path to the domain file")]
        public string DomainPath { get; set; } = "";
        [Option("problem", Required = true, HelpText = "Path to the problem file")]
        public string ProblemPath { get; set; } = "";
        [Option("generation-strategy", Required = true, HelpText = "The generator strategy")]
        public GeneratorStrategies GeneratorStrategy { get; set; }
    }
}
