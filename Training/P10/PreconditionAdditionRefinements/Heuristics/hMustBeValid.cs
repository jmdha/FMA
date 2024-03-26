using P10.Models;

namespace P10.PreconditionAdditionRefinements.Heuristics
{
    public class hMustBeValid : IHeuristic<PreconditionState>
    {
        public int GetValue(PreconditionState preconditions)
        {
            if (preconditions.InvalidStates == preconditions.TotalInvalidStates)
                return int.MaxValue;
            return 0;
        }
    }
}
