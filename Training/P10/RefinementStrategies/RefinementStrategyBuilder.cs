using P10.RefinementStrategies.GroundedPredicateAdditions;
using PDDLSharp.Models.PDDL.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace P10.RefinementStrategies
{
    public static class RefinementStrategyBuilder
    {
        private static Dictionary<Options.RefinementStrategies, Func<int, ActionDecl, int, string, string, IRefinementStrategy>> _options = new Dictionary<Options.RefinementStrategies, Func<int, ActionDecl, int, string, string, IRefinementStrategy>>()
        {
            { Options.RefinementStrategies.GroundedPredicateAdditions, (t, m, i, w, o) => new GroundedPredicateAdditionsRefinement(t, m, i, w, o) }
        };

        public static IRefinementStrategy GetStrategy(Options.RefinementStrategies strategy, int timeLimitS, ActionDecl metaAction, int metaActionIndex, string tempDir, string outputDir) => _options[strategy](timeLimitS, metaAction, metaActionIndex, tempDir, outputDir);
    }
}
