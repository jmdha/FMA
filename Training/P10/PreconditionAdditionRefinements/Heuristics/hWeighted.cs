namespace P10.PreconditionAdditionRefinements.Heuristics
{
    public class hWeighted : IHeuristic
    {
        public IHeuristic Heuristic { get; set; }
        public double Weight { get; set; }

        public hWeighted(IHeuristic heuristic, double weight)
        {
            Heuristic = heuristic;
            Weight = weight;
        }

        public int GetValue(PreconditionState metaAction) => (int)(Heuristic.GetValue(metaAction) * Weight);
    }
}
