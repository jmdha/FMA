namespace FocusedMetaActions.Train.PreconditionAdditionRefinements.Heuristics
{
    public class hSum : IHeuristic
    {
        public List<IHeuristic> Heuristics { get; set; }

        public hSum(List<IHeuristic> heuristics)
        {
            Heuristics = heuristics;
        }

        public int GetValue(PreconditionState metaAction)
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
