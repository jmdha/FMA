using CommandLine;
using static MetaActionCandidateGenerator.Options;

namespace P10
{
    public class Options
    {
        [Flags]
        public enum RefinementStrategies
        {
            None = 1,
            ActionPrecondition = 2,
            GroundedPredicateAdditions = 3
        }

        [Option("output", Required = true, HelpText = "Where to output the meta actions")]
        public string OutputPath { get; set; } = "";
        [Option("domain", Required = true, HelpText = "Path to the domain file")]
        public string DomainPath { get; set; } = "";
        [Option("problems", Required = true, HelpText = "Path to the problem file")]
        public IEnumerable<string> ProblemsPath { get; set; } = new List<string>();
        [Option("generation-strategy", Required = true, HelpText = "The generator strategy")]
        public GeneratorStrategies GeneratorStrategy { get; set; }
        [Option("refinement-strategy", Required = true, HelpText = "The refinement strategy")]
        public RefinementStrategies RefinementStrategy { get; set; }
    }
}
