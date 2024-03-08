using PDDLSharp.Models.SAS;

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
            int sum = 0;
            foreach (var heuristic in Heuristics)
                if (sum < int.MaxValue)
                    sum = ClampSum(sum, heuristic.GetValue(metaAction));
            return sum;
        }

        private int ClampSum(int value1, int value2)
        {
            if (value1 == int.MaxValue || value2 == int.MaxValue)
                return int.MaxValue;
            return value1 + value2;
        }
    }
}
