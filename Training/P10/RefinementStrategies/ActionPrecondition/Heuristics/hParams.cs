using P10.Models;

namespace P10.RefinementStrategies.ActionPrecondition.Heuristics
{
    public class hParams : IHeuristic<MetaActionState>
    {
        public int GetValue(MetaActionState metaAction) => metaAction.MetaAction.Parameters.Values.Count;
    }
}
