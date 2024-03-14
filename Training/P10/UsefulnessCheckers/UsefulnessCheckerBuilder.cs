using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static P10.Options;

namespace P10.UsefulnessCheckers
{
    public static class UsefulnessCheckerBuilder
    {
        private static Dictionary<UsefulnessStrategies, Func<string,IUsefulnessChecker>> _strategies = new Dictionary<UsefulnessStrategies, Func<string, IUsefulnessChecker>>()
        {
            { UsefulnessStrategies.UsedInPlans, (w) => new UsedInPlansUsefulness(w) },
            { UsefulnessStrategies.ReducesMetaSearchTime, (w) => new ReducesMetaSearchTimeUsefulness(w) }
        };

        public static IUsefulnessChecker GetUsefulnessChecker(UsefulnessStrategies strategy, string workingDir) => _strategies[strategy](workingDir);
    }
}
