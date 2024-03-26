using P10.Models;

namespace P10.PreconditionAdditionRefinements.Heuristics
{
    public class hFewestParameters : IHeuristic<PreconditionState>
    {
        public int GetValue(PreconditionState preconditions)
        {
            return preconditions.MetaAction.Parameters.Values.Count;
        }
    }
}
