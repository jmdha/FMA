using P10.Models;

namespace P10.PreconditionAdditionRefinements.Heuristics
{
    public class hMostValid : IHeuristic<PreconditionState>
    {
        public int GetValue(PreconditionState preconditions)
        {
            return 100 - (int)((1 - ((double)preconditions.InvalidStates / (double)preconditions.TotalInvalidStates)) * 100);
        }
    }
}
