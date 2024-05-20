using static FocusedMetaActions.Train.Options;

namespace FocusedMetaActions.Train.UsefulnessCheckers
{
    public static class UsefulnessCheckerBuilder
    {
        private static readonly Dictionary<UsefulnessStrategies, Func<string, int, IUsefulnessChecker>> _strategies = new Dictionary<UsefulnessStrategies, Func<string, int, IUsefulnessChecker>>()
        {
            { UsefulnessStrategies.UsedInPlans, (w, t) => new UsedInPlansUsefulness(w, t) },
            { UsefulnessStrategies.UsedInPlansCombined, (w, t) => new UsedInPlansCombinedUsefulness(w, t) },
            { UsefulnessStrategies.ReducesMetaSearchTime, (w, t) => new ReducesMetaSearchTimeUsefulness(w, t) },
            { UsefulnessStrategies.ReducesMetaSearchTimeTop1, (w, t) => new TopNReducesMetaSearchTimeUsefulness(w, t, 1) },
            { UsefulnessStrategies.ReducesMetaSearchTimeTop2, (w, t) => new TopNReducesMetaSearchTimeUsefulness(w, t, 2) },
            { UsefulnessStrategies.ReducesMetaSearchTimeTop5, (w, t) => new TopNReducesMetaSearchTimeUsefulness(w, t, 5) },
            { UsefulnessStrategies.ReducesPlanLength, (w, t) => new ReducesPlanLengthUsefulness(w, t) },
            { UsefulnessStrategies.ReducesPlanLengthTop1, (w, t) => new TopNReducesPlanLengthUsefulness(w, t, 1) },
            { UsefulnessStrategies.ReducesPlanLengthTop2, (w, t) => new TopNReducesPlanLengthUsefulness(w, t, 2) },
            { UsefulnessStrategies.ReducesPlanLengthTop5, (w, t) => new TopNReducesPlanLengthUsefulness(w, t, 5) },
        };

        public static IUsefulnessChecker GetUsefulnessChecker(UsefulnessStrategies strategy, string workingDir, int timeLimitS)
        {
            if (timeLimitS == -1)
                timeLimitS = 9999999;
            return _strategies[strategy](workingDir, timeLimitS);
        }
    }
}
