using PDDLSharp.Models.PDDL;
using PDDLSharp.Translators.Tools;

namespace P10.PreconditionAdditionRefinements.Heuristics
{
    public class hMostStatics : IHeuristic
    {
        private HashSet<string> _statics;
        public hMostStatics(PDDLDecl pddlDecl)
        {
            var staticPreds = SimpleStaticPredicateDetector.FindStaticPredicates(pddlDecl);
            _statics = new HashSet<string>();
            foreach (var statics in staticPreds)
                _statics.Add(statics.Name);
        }

        public int GetValue(PreconditionState preconditions)
        {
            var value = _statics.Count;
            var found = new HashSet<string>();
            foreach (var pre in preconditions.Precondition)
            {
                if (pre is INamedNode named && _statics.Contains(named.Name) && !found.Contains(named.Name))
                {
                    value--;
                    found.Add(named.Name);
                }
            }
            return value;
        }
    }
}
