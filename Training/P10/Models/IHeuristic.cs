namespace P10.Models
{
    public interface IHeuristic<T>
    {
        public int GetValue(T metaAction);
    }
}
