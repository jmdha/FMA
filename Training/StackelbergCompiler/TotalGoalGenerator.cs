using PDDLSharp.Models.PDDL;
using PDDLSharp.Models.PDDL.Domain;
using PDDLSharp.Models.PDDL.Expressions;
using PDDLSharp.Models.PDDL.Problem;
using PDDLSharp.Translators.Grounders;

namespace StackelbergCompiler
{
    public static class TotalGoalGenerator
    {
        public static List<PredicateExp> TotalGoal = new List<PredicateExp>();
        public static List<PredicateExp> CopyTotalGoal()
        {
            var retList = new List<PredicateExp>();
            foreach (var pred in TotalGoal)
                retList.Add(pred.Copy());
            return retList;
        }
        public static List<PredicateExp> GenerateTotalGoal(ProblemDecl problem, DomainDecl domain)
        {
            var grounder = new PredicateGrounder(new PDDLDecl(domain, problem));
            if (domain.Predicates != null)
            {
                var newGoals = new List<PredicateExp>();
                foreach (var predicate in domain.Predicates.Predicates)
                    if (!StaticPredicateDetector.StaticPredicates.Contains(predicate.Name))
                        newGoals.AddRange(grounder.Ground(predicate));
                foreach (var predicate in newGoals)
                    predicate.Name = $"{ReservedNames.IsGoalPrefix}{predicate.Name}";
                TotalGoal = newGoals;
            }
            return TotalGoal;
        }
        public static void Clear() => TotalGoal.Clear();
    }
}
