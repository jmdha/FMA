using PDDLSharp.Models.SAS;

namespace P10.Models
{
    public class hMax<T> : IHeuristic<T>
    {
        public List<IHeuristic<T>> Heuristics { get; set; }

        public hMax(List<IHeuristic<T>> heuristics)
        {
            Heuristics = heuristics;
        }

        public int GetValue(T metaAction)
        {
            int max = -1;
            foreach (var heuristic in Heuristics)
            {
                var hValue = heuristic.GetValue(metaAction);
                if (hValue > max)
                    max = hValue;
            }
            return max;
        }
    }
}
