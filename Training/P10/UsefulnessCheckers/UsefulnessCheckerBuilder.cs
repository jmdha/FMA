using static P10.Options;

namespace P10.UsefulnessCheckers
{
    public static class UsefulnessCheckerBuilder
    {
        private static readonly Dictionary<UsefulnessStrategies, Func<string, int, IUsefulnessChecker>> _strategies = new Dictionary<UsefulnessStrategies, Func<string, int, IUsefulnessChecker>>()
        {
            { UsefulnessStrategies.UsedInPlans, (w, t) => new UsedInPlansUsefulness(w, t) },
            { UsefulnessStrategies.UsedInPlansCombined, (w, t) => new UsedInPlansCombinedUsefulness(w, t) },
            { UsefulnessStrategies.ReducesMetaSearchTime, (w, t) => new UsedInPlansCombinedUsefulness(w, t) },
            { UsefulnessStrategies.ReducesMetaSearchTimeTop1, (w, t) => new TopNReducesMetaSearchTimeUsefulness(w, t, 1) },
            { UsefulnessStrategies.ReducesMetaSearchTimeTop2, (w, t) => new TopNReducesMetaSearchTimeUsefulness(w, t, 2) },
            { UsefulnessStrategies.ReducesMetaSearchTimeTop5, (w, t) => new TopNReducesMetaSearchTimeUsefulness(w, t, 5) },
        };

        public static IUsefulnessChecker GetUsefulnessChecker(UsefulnessStrategies strategy, string workingDir, int timeLimitS)
        {
            if (timeLimitS == -1)
                timeLimitS = 9999999;
            return _strategies[strategy](workingDir, timeLimitS);
        }
    }
}
