using P10.Models;

namespace P10.RefinementStrategies.GroundedPredicateAdditions.Heuristics
{
    public class hMustBeApplicable : IHeuristic<PreconditionState>
    {
        public int GetValue(PreconditionState preconditions)
        {
            if (preconditions.Applicability == 0)
                return int.MaxValue;
            return 0;
        }
    }
}
