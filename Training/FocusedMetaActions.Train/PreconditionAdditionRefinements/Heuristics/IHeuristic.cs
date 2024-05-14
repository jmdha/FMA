namespace FocusedMetaActions.Train.PreconditionAdditionRefinements.Heuristics
{
    public interface IHeuristic
    {
        public int GetValue(PreconditionState metaAction);
    }
}
