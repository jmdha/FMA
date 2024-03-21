using PDDLSharp.Models.PDDL;
using PDDLSharp.Models.PDDL.Domain;
using PDDLSharp.Models.PDDL.Expressions;
using PDDLSharp.Models.PDDL.Overloads;

namespace MetaActionCandidateGenerator.CandidateGenerators
{
    /// <summary>
    /// Assumed every predicate can be a mutex, and constructs meta actions out of them
    /// </summary>
    public class MutexedMetaActions : BaseCandidateGenerator
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
                        if (!predicate.CanOnlyBeSetToFalse(pddlDecl.Domain))
                            candidates.Add(GenerateMetaAction(
                                $"meta_{predicate.Name}_{counter++}",
                                new List<IExp>() { new NotExp(predicate) },
                                new List<IExp>() { predicate },
                                action));
                        if (!predicate.CanOnlyBeSetToTrue(pddlDecl.Domain))
                            candidates.Add(GenerateMetaAction(
                                $"meta_{predicate.Name}_{counter++}",
                                new List<IExp>() { predicate },
                                new List<IExp>() { new NotExp(predicate) },
                                action));
                    }
                }
            }

            return candidates.Distinct(pddlDecl.Domain.Actions);
        }
    }
}
