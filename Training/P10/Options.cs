using CommandLine;
using System;
using System.Net.Sockets;
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

        [Flags]
        public enum UsefulnessStrategies
        {
            None = 1,
            UsedInPlans = 2,
            UsedInPlansCombined = 3,
            ReducesMetaSearchTime = 4
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
        [Option("remove-duplicates", Required = false, HelpText = "If duplicate meta actions should be removed from candidates and output.")]
        public bool RemoveDuplicates { get; set; } = false;
        [Option("problems", Required = true, HelpText = "Path to the problem file")]
        public IEnumerable<string> ProblemsPath { get; set; } = new List<string>();
        [Option("generation-strategies", Required = true, HelpText = "The generator strategies. Valid values: " +
            $"{nameof(MetaActionCandidateGenerator.Options.GeneratorStrategies.PredicateMetaActions)}," +
            $"{nameof(MetaActionCandidateGenerator.Options.GeneratorStrategies.StrippedMetaActions)}," +
            $"{nameof(MetaActionCandidateGenerator.Options.GeneratorStrategies.AgressiveStrippedMetaActions)}," +
            $"{nameof(MetaActionCandidateGenerator.Options.GeneratorStrategies.MutexedMetaActions)}," +
            $"{nameof(MetaActionCandidateGenerator.Options.GeneratorStrategies.InvariantMetaActions)}")]
        public IEnumerable<GeneratorStrategies> GeneratorStrategies { get; set; } = new List<GeneratorStrategies>();
        [Option("refinement-strategy", Required = true, HelpText = $"The refinement strategy")]
        public RefinementStrategies RefinementStrategy { get; set; }
        [Option("usefulness-strategy", Required = false, HelpText = "The usefulness strategy")]
        public UsefulnessStrategies UsefulnessStrategy { get; set; } = UsefulnessStrategies.None;

        [Option("fast-downward-path", Required = false, HelpText = "Path to Fast Downward")]
        public string FastDownwardPath { get; set; } = "";
        [Option("stackelberg-path", Required = false, HelpText = "Path to the Stackelberg Planner")]
        public string StackelbergPath { get; set; } = "";

        [Option("refinement-time-limit", Required = false, HelpText = "Time limit in seconds that each refinement step is allowed to take.")]
        public int TimeLimitS { get; set; } = -1;

        [Option("stackelberg-debug", Required = false, HelpText = "Show the stdout of the Stackelberg Planner")]
        public bool StackelbergDebug { get; set; } = false;
    }
}
