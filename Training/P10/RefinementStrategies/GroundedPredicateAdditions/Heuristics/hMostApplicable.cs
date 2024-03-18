using P10.Models;

namespace P10.RefinementStrategies.GroundedPredicateAdditions.Heuristics
{
    public class hMostApplicable : IHeuristic<PreconditionState>
    {
        public int GetValue(PreconditionState preconditions)
        {
            return (int)(((double)preconditions.Applicability / (double)(preconditions.TotalValidStates + preconditions.TotalInvalidStates)) * 100);
        }
    }
}
