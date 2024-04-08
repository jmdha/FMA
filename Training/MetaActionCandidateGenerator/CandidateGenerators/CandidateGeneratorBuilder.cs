using static MetaActionCandidateGenerator.Options;

namespace MetaActionCandidateGenerator.CandidateGenerators
{
    public static class CandidateGeneratorBuilder
    {
        private static readonly Dictionary<GeneratorStrategies, Func<ICandidateGenerator>> _generators = new Dictionary<GeneratorStrategies, Func<ICandidateGenerator>>()
        {
            { GeneratorStrategies.PredicateMetaActions, () => new PredicateMetaActions() },
            { GeneratorStrategies.StrippedMetaActions, () => new StrippedMetaActions() },
            { GeneratorStrategies.AgressiveStrippedMetaActions, () => new AgressiveStrippedMetaActions() },
            { GeneratorStrategies.FlipMetaActions, () => new FlipMetaActions() },
            { GeneratorStrategies.InvariantMetaActions, () => new InvariantMetaActions() },
        };

        public static ICandidateGenerator GetGenerator(GeneratorStrategies strategy) => _generators[strategy]();
    }
}
