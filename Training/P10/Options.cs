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
            GroundedPredicateAdditions = 2
        }

        [Option("output", Required = false, HelpText = "Where to output the meta actions")]
        public string OutputPath { get; set; } = "output";
        [Option("temp", Required = false, HelpText = "Where to put temporary files")]
        public string TempPath { get; set; } = "temp";
        [Option("domain", Required = true, HelpText = "Path to the domain file")]
        public string DomainPath { get; set; } = "";
        [Option("pre-usefulness", Required = false, HelpText = "Check if meta action candidates seem to be useful, before refining them.")]
        public bool PreCheckUsefullness { get; set; } = false;
        [Option("post-usefulness", Required = false, HelpText = "Check if refined meta actions are useful before outputting them.")]
        public bool PostCheckUsefullness { get; set; } = false;
        [Option("problems", Required = true, HelpText = "Path to the problem file")]
        public IEnumerable<string> ProblemsPath { get; set; } = new List<string>();
        [Option("generation-strategies", Required = true, HelpText = "The generator strategies")]
        public IEnumerable<GeneratorStrategies> GeneratorStrategies { get; set; } = new List<GeneratorStrategies>();
        [Option("refinement-strategy", Required = true, HelpText = "The refinement strategy")]
        public RefinementStrategies RefinementStrategy { get; set; }

        [Option("fast-downward-path", Required = false, HelpText = "Path to Fast Downward")]
        public string FastDownwardPath { get; set; } = "";
        [Option("stackelberg-path", Required = false, HelpText = "Path to the Stackelberg Planner")]
        public string StackelbergPath { get; set; } = "";
    }
}
