using PDDLSharp.Models.PDDL;
using PDDLSharp.Models.PDDL.Domain;
using PDDLSharp.Models.PDDL.Expressions;

namespace P10.RefinementStrategies.GroundedPredicateAdditions
{
    public class PreconditionState
    {
        public int ValidStates { get; set; }
        public int InvalidStates { get; set; }
        public ActionDecl MetaAction { get; set; }
        public List<IExp> Precondition { get; set; }

        public PreconditionState(int validStates, int invalidStates, ActionDecl metaAction, List<IExp> precondition)
        {
            ValidStates = validStates;
            InvalidStates = invalidStates;
            MetaAction = metaAction;
            Precondition = precondition;
        }

        public override bool Equals(object? obj)
        {
            if (obj is PreconditionState other)
            {
                if (other.ValidStates != ValidStates) return false;
                if (other.InvalidStates != InvalidStates) return false;
                if (!other.MetaAction.Equals(MetaAction)) return false;
                if (other.Precondition.Count != Precondition.Count) return false;
                for (int i = 0; i < other.Precondition.Count; i++)
                    if (!other.Precondition[i].Equals(Precondition[i]))
                        return false;
                return true;
            }
            return false;
        }

        public override int GetHashCode()
        {
            var hash = HashCode.Combine(ValidStates, InvalidStates, MetaAction);
            foreach (var item in Precondition)
                hash ^= item.GetHashCode();
            return hash;
        }
    }
}
