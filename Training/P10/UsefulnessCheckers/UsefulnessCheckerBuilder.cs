using static P10.Options;

namespace P10.UsefulnessCheckers
{
    public static class UsefulnessCheckerBuilder
    {
        private static readonly Dictionary<UsefulnessStrategies, Func<string, IUsefulnessChecker>> _strategies = new Dictionary<UsefulnessStrategies, Func<string, IUsefulnessChecker>>()
        {
            { UsefulnessStrategies.UsedInPlans, (w) => new UsedInPlansUsefulness(w) },
            { UsefulnessStrategies.UsedInPlansCombined, (w) => new UsedInPlansCombinedUsefulness(w) },
            { UsefulnessStrategies.ReducesMetaSearchTime, (w) => new UsedInPlansCombinedUsefulness(w) },
        };

        public static IUsefulnessChecker GetUsefulnessChecker(UsefulnessStrategies strategy, string workingDir) => _strategies[strategy](workingDir);
    }
}
