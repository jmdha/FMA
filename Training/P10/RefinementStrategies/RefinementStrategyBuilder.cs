using P10.RefinementStrategies.GroundedPredicateAdditions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace P10.RefinementStrategies
{
    public static class RefinementStrategyBuilder
    {
        private static Dictionary<Options.RefinementStrategies, Func<int, int, string, string, IRefinementStrategy>> _options = new Dictionary<Options.RefinementStrategies, Func<int, int, string, string, IRefinementStrategy>>()
        {
            { Options.RefinementStrategies.GroundedPredicateAdditions, (t, i, w, o) => new GroundedPredicateAdditionsRefinement(t, i, w, o) }
        };

        public static IRefinementStrategy GetStrategy(Options.RefinementStrategies strategy, int timeLimitS, int metaActionIndex, string tempDir, string outputDir) => _options[strategy](timeLimitS, metaActionIndex, tempDir, outputDir);
    }
}
