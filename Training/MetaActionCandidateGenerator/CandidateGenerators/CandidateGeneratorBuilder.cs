using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static MetaActionCandidateGenerator.Options;

namespace MetaActionCandidateGenerator.CandidateGenerators
{
    public static class CandidateGeneratorBuilder
    {
        private static Dictionary<GeneratorStrategies, Func<ICandidateGenerator>> _generators = new Dictionary<GeneratorStrategies, Func<ICandidateGenerator>>()
        {
            { GeneratorStrategies.PredicateMetaActions, () => new PredicateMetaActions() },
            { GeneratorStrategies.StrippedMetaActions, () => new StrippedMetaActions() },
            { GeneratorStrategies.AgressiveStrippedMetaActions, () => new AgressiveStrippedMetaActions() },
            { GeneratorStrategies.MutexedMetaActions, () => new MutexedMetaActions() },
            { GeneratorStrategies.InvariantMetaActions, () => new InvariantMetaActions() },
        };

        public static ICandidateGenerator GetGenerator(GeneratorStrategies strategy) => _generators[strategy]();
    }
}
