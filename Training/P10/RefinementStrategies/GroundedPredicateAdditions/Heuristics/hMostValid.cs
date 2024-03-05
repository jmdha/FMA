using P10.Models;

namespace P10.RefinementStrategies.GroundedPredicateAdditions.Heuristics
{
    public class hMostValid : IHeuristic<PreconditionState>
    {
        public int GetValue(PreconditionState preconditions)
        {
            return preconditions.InvalidStates;
        }
    }
}
