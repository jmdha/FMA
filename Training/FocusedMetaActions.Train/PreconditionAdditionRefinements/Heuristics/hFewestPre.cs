namespace FocusedMetaActions.Train.PreconditionAdditionRefinements.Heuristics
{
    public class hFewestPre : IHeuristic
    {
        public int GetValue(PreconditionState preconditions)
        {
            return preconditions.Precondition.Count;
        }
    }
}
