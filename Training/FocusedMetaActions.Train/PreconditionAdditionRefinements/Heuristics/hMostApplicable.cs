namespace FocusedMetaActions.Train.PreconditionAdditionRefinements.Heuristics
{
    public class hMostApplicable : IHeuristic
    {
        public int GetValue(PreconditionState preconditions)
        {
            return -preconditions.Applicability;
        }
    }
}
