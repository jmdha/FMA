using P10.Models;

namespace P10.PreconditionAdditionRefinements.Heuristics
{
    public class hMostApplicable : IHeuristic<PreconditionState>
    {
        public int GetValue(PreconditionState preconditions)
        {
            return 100 - (int)(((double)preconditions.Applicability / (double)(preconditions.TotalValidStates + preconditions.TotalInvalidStates)) * 100);
        }
    }
}
