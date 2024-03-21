using PDDLSharp.Models.PDDL;
using PDDLSharp.Models.PDDL.Domain;
using PDDLSharp.Models.PDDL.Expressions;

namespace MetaActionCandidateGenerator.CandidateGenerators
{
    /// <summary>
    /// Takes all non-static predicates and makes meta actions based on no preconditions and simply the predicate.
    /// Both a normal and a negated version is made for each predicate
    /// </summary>
    public class PredicateMetaActions : BaseCandidateGenerator
    {
        internal override List<ActionDecl> GenerateCandidatesInner(PDDLDecl pddlDecl)
        {
            if (pddlDecl.Domain.Predicates == null)
                throw new Exception("No predicates defined in domain!");
            Initialize(pddlDecl);
            ContextualizeIfNotAlready(pddlDecl);

            var candidates = new List<ActionDecl>();
            foreach (var predicate in pddlDecl.Domain.Predicates.Predicates)
            {
                if (!Statics.Any(x => x.Name.ToUpper() == predicate.Name.ToUpper()))
                {
                    int counter = 0;
                    foreach (var action in pddlDecl.Domain.Actions)
                    {
                        if (action.Effects.FindNames(predicate.Name).Count == 0)
                            continue;
                        if (CanPredicateBeSetToTrue(pddlDecl, predicate))
                            candidates.Add(GenerateMetaAction(
                                $"meta_{predicate.Name}_{counter++}",
                                new List<IExp>(),
                                new List<IExp>() { predicate },
                                action));
                        if (CanPredicateBeSetToFalse(pddlDecl, predicate))
                            candidates.Add(GenerateMetaAction(
                                $"meta_{predicate.Name}_{counter++}_false",
                                new List<IExp>(),
                                new List<IExp>() { new NotExp(predicate) },
                                action));
                    }
                }
            }

            return candidates;
        }
    }
}
