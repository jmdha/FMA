using PDDLSharp.Models.PDDL;
using PDDLSharp.Models.PDDL.Domain;
using PDDLSharp.Models.PDDL.Problem;
using PDDLSharp.Translators.StaticPredicateDetectors;

namespace StackelbergCompiler
{
    public static class StaticPredicateDetector
    {
        private static List<string> _staticPredicates = new List<string>();
        public static List<string> StaticPredicates => _staticPredicates;
        public static List<string> GenerateStaticPredicates(DomainDecl domain)
        {
            _staticPredicates = new List<string>();
            var statics = SimpleStaticPredicateDetector.FindStaticPredicates(new PDDLDecl(domain, new ProblemDecl()));
            foreach (var staticPred in statics)
                _staticPredicates.Add(staticPred.Name);
            if (!_staticPredicates.Contains("="))
                _staticPredicates.Add("=");
            return _staticPredicates;
        }
        public static void Clear() => _staticPredicates.Clear();
    }
}
