using P10.Models;

namespace P10.RefinementStrategies.GroundedPredicateAdditions.Heuristics
{
    public class hMostApplicable : IHeuristic<PreconditionState>
    {
        public int GetValue(PreconditionState preconditions)
        {
            return (preconditions.TotalValidStates + preconditions.TotalInvalidStates) - preconditions.Applicability;
        }
    }
}
