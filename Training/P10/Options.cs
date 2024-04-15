using CommandLine;
using static MetaActionCandidateGenerator.Options;

namespace P10
{
    public class Options
    {
        [Flags]
        public enum UsefulnessStrategies
        {
            None = 1,
            UsedInPlans = 2,
            UsedInPlansCombined = 3,
            ReducesMetaSearchTime = 4,
            ReducesMetaSearchTimeTop1 = 5,
            ReducesMetaSearchTimeTop2 = 6,
            ReducesMetaSearchTimeTop5 = 7,
        }
        [Option("output", Required = false, HelpText = "Where to output the meta actions", Default = "output")]
        public string OutputPath { get; set; } = "output";
        [Option("temp", Required = false, HelpText = "Where to put temporary files", Default = "temp")]
        public string TempPath { get; set; } = "temp";
        [Option("domain", Required = true, HelpText = "Path to the domain file")]
        public string DomainPath { get; set; } = "";
        [Option("problems", Required = true, HelpText = "Path to the problem files")]
        public IEnumerable<string> ProblemsPath { get; set; } = new List<string>();
        [Option("remove-duplicates", Required = false, HelpText = "If duplicate meta actions should be removed from candidates and output.")]
        public bool RemoveDuplicates { get; set; } = false;
        [Option("generation-strategies", Required = true, HelpText = $"The generator strategies. Valid values: " +
            $"{nameof(MetaActionCandidateGenerator.Options.GeneratorStrategies.PredicateMetaActions)}," +
            $"{nameof(MetaActionCandidateGenerator.Options.GeneratorStrategies.StrippedMetaActions)}," +
            $"{nameof(MetaActionCandidateGenerator.Options.GeneratorStrategies.AgressiveStrippedMetaActions)}," +
            $"{nameof(MetaActionCandidateGenerator.Options.GeneratorStrategies.FlipMetaActions)}," +
            $"{nameof(MetaActionCandidateGenerator.Options.GeneratorStrategies.CPDDLInvariantMetaActions)}," +
            $"{nameof(MetaActionCandidateGenerator.Options.GeneratorStrategies.InvariantMetaActions)}")]
        public IEnumerable<GeneratorStrategies> GeneratorStrategies { get; set; } = new List<GeneratorStrategies>();
        [Option("pre-usefulness-strategy", Required = false, HelpText = "The usefulness strategy for the pre-usefulness check", Default = UsefulnessStrategies.None)]
        public UsefulnessStrategies PreUsefulnessStrategy { get; set; } = UsefulnessStrategies.None;
        [Option("post-usefulness-strategy", Required = false, HelpText = "The usefulness strategy for the post-usefulness check", Default = UsefulnessStrategies.None)]
        public UsefulnessStrategies PostUsefulnessStrategy { get; set; } = UsefulnessStrategies.None;

        [Option("fast-downward-path", Required = false, HelpText = "Path to Fast Downward")]
        public string FastDownwardPath { get; set; } = "";
        [Option("stackelberg-path", Required = false, HelpText = "Path to the Stackelberg Planner")]
        public string StackelbergPath { get; set; } = "";
        [Option("cpddl-path", Required = false, HelpText = "Path to the CPDDL executable")]
        public string CPDDLPath { get; set; } = "";

        [Option("validation-time-limit", Required = false, HelpText = "Time limit in seconds that each validation step is allowed to take. (-1 for no time limit)", Default = -1)]
        public int ValidationTimeLimitS { get; set; } = -1;
        [Option("exploration-time-limit", Required = false, HelpText = "Time limit in seconds that each state exploration step is allowed to take. (-1 for no time limit)", Default = -1)]
        public int ExplorationTimeLimitS { get; set; } = -1;
        [Option("refinement-time-limit", Required = false, HelpText = "Time limit in seconds that each refinement step is allowed to take. (-1 for no time limit)", Default = -1)]
        public int RefinementTimeLimitS { get; set; } = -1;

        [Option("stackelberg-debug", Required = false, HelpText = "Show the stdout of the Stackelberg Planner", Default = false)]
        public bool StackelbergDebug { get; set; } = false;

        [Option("max-precondition-combinations", Required = false, HelpText = "How many precondition combinations to try", Default = 3)]
        public int MaxPreconditionCombinations { get; set; } = 3;
        [Option("max-added-parameters", Required = false, HelpText = "How many additional parameters are allowed to add", Default = 0)]
        public int MaxAddedParameters { get; set; } = 0;

        [Option("learning-cache-path", Required = false, HelpText = "Path to the cross-run cache. If a path is given, the cache is enabled, otherwise the application will not use caching.")]
        public string LearningCache { get; set; } = "";
    }
}
