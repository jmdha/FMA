namespace P10.Models
{
    public class hSum<T> : IHeuristic<T>
    {
        public List<IHeuristic<T>> Heuristics { get; set; }

        public hSum(List<IHeuristic<T>> heuristics)
        {
            Heuristics = heuristics;
        }

        public int GetValue(T metaAction)
        {
            var value = 0;
            foreach (var heuristic in Heuristics)
                value += heuristic.GetValue(metaAction);
            return value;
        }
    }
}
