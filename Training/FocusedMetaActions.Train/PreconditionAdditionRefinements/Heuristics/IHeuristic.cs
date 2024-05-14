namespace P10.PreconditionAdditionRefinements.Heuristics
{
    public interface IHeuristic
    {
        public int GetValue(PreconditionState metaAction);
    }
}
