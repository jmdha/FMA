namespace P10.PreconditionAdditionRefinements.Heuristics
{
    public class hMostApplicable : IHeuristic
    {
        public int GetValue(PreconditionState preconditions)
        {
            return 100 - (int)(((double)preconditions.Applicability / (double)(preconditions.TotalValidStates + preconditions.TotalInvalidStates)) * 100);
        }
    }
}
