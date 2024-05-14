namespace P10.PreconditionAdditionRefinements.Heuristics
{
    public class hMustBeApplicable : IHeuristic
    {
        public int GetValue(PreconditionState preconditions)
        {
            if (preconditions.Applicability == 0)
                return int.MaxValue;
            return 0;
        }
    }
}
