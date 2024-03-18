using P10.Models;

namespace P10.RefinementStrategies.GroundedPredicateAdditions.Heuristics
{
    public class hFewestParameters : IHeuristic<PreconditionState>
    {
        public int GetValue(PreconditionState preconditions)
        {
            return preconditions.MetaAction.Parameters.Values.Count;
        }
    }
}
