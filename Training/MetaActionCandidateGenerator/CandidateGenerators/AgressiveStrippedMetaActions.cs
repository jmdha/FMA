using PDDLSharp.Models.PDDL;
using PDDLSharp.Models.PDDL.Domain;
using PDDLSharp.Models.PDDL.Expressions;
using PDDLSharp.Models.PDDL.Overloads;

namespace MetaActionCandidateGenerator.CandidateGenerators
{
    /// <summary>
    /// Generates meta actions by taking normal actions and removing all (non-static) preconditions from it. As well as for each action, make an action that is only a single effect predicate.
    /// </summary>
    public class AgressiveStrippedMetaActions : StrippedMetaActions
    {
        internal override List<ActionDecl> GenerateCandidatesInner(PDDLDecl pddlDecl)
        {
            Initialize(pddlDecl);
            ContextualizeIfNotAlready(pddlDecl);

            var candidates = new List<ActionDecl>();
            foreach (var action in pddlDecl.Domain.Actions)
            {
                action.EnsureAnd();
                if (action.Effects is AndExp andEff)
                {
                    int count = 0;
                    foreach (var effect in andEff.Children)
                        candidates.Add(GenerateMetaAction(
                            $"meta_{action.Name}_{count++}",
                            new List<IExp>(),
                            new List<IExp>() { effect },
                            action));
                }
            }

            return candidates.Distinct(pddlDecl.Domain.Actions);
        }
    }
}
