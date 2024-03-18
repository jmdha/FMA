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
        private static Dictionary<Options.RefinementStrategies, Func<int, IRefinementStrategy>> _options = new Dictionary<Options.RefinementStrategies, Func<int, IRefinementStrategy>>()
        {
            { Options.RefinementStrategies.GroundedPredicateAdditions, (t) => new GroundedPredicateAdditionsRefinement(t) }
        };

        public static IRefinementStrategy GetStrategy(Options.RefinementStrategies strategy, int timeLimitS) => _options[strategy](timeLimitS);
    }
}
