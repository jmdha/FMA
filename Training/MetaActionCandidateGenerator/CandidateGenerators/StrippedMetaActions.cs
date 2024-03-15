using PDDLSharp.Contextualisers.PDDL;
using PDDLSharp.ErrorListeners;
using PDDLSharp.Models.PDDL;
using PDDLSharp.Models.PDDL.Domain;
using PDDLSharp.Models.PDDL.Expressions;
using PDDLSharp.Tools;
using PDDLSharp.Translators.StaticPredicateDetectors;
using System;

namespace MetaActionCandidateGenerator.CandidateGenerators
{
    /// <summary>
    /// Generates meta actions by taking normal actions and removing all (non-static) preconditions from it.
    /// </summary>
    public class StrippedMetaActions : BaseCandidateGenerator
    {
        public override List<ActionDecl> GenerateCandidates(PDDLDecl pddlDecl)
        {
            Initialize(pddlDecl);
            ContextualizeIfNotAlready(pddlDecl);

            var candidates = new List<ActionDecl>();
            foreach (var action in pddlDecl.Domain.Actions)
            {
                EnsureAnd(action);
                if (action.Effects is AndExp and)
                    candidates.Add(GenerateMetaAction(
                        $"meta_{action.Name}", 
                        new List<IExp>(),
                        and.Children, 
                        action));
            }

            return candidates;
        }
    }
}
