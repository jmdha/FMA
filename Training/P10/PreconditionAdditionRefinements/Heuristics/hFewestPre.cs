using P10.Models;

namespace P10.PreconditionAdditionRefinements.Heuristics
{
    public class hFewestPre : IHeuristic<PreconditionState>
    {
        public int GetValue(PreconditionState preconditions)
        {
            return preconditions.Precondition.Count;
        }
    }
}
