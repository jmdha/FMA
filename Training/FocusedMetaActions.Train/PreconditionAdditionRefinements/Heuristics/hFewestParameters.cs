namespace P10.PreconditionAdditionRefinements.Heuristics
{
    public class hFewestParameters : IHeuristic
    {
        public int GetValue(PreconditionState preconditions)
        {
            return preconditions.MetaAction.Parameters.Values.Count;
        }
    }
}
