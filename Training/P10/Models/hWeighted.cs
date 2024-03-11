namespace P10.Models
{
    public class hWeighted<T> : IHeuristic<T>
    {
        public IHeuristic<T> Heuristic { get; set; }
        public double Weight { get; set; }

        public hWeighted(IHeuristic<T> heuristic, double weight)
        {
            Heuristic = heuristic;
            Weight = weight;
        }

        public int GetValue(T metaAction) => (int)(Heuristic.GetValue(metaAction) * Weight);
    }
}
