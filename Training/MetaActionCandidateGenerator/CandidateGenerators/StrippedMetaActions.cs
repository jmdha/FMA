using PDDLSharp.Contextualisers.PDDL;
using PDDLSharp.ErrorListeners;
using PDDLSharp.Models.PDDL;
using PDDLSharp.Models.PDDL.Domain;
using PDDLSharp.Models.PDDL.Expressions;
using PDDLSharp.Tools;
using PDDLSharp.Translators.StaticPredicateDetectors;

namespace MetaActionCandidateGenerator.CandidateGenerators
{
    /// <summary>
    /// Generates meta actions by taking normal actions and removing all (non-static) preconditions from it.
    /// </summary>
    public class StrippedMetaActions : ICandidateGenerator
    {
        public List<ActionDecl> GenerateCandidates(PDDLDecl pddlDecl)
        {
            var candidates = new List<ActionDecl>();

            if (!pddlDecl.IsContextualised)
            {
                var listener = new ErrorListener();
                var contextualiser = new PDDLContextualiser(listener);
                contextualiser.Contexturalise(pddlDecl);
            }

            var statics = SimpleStaticPredicateDetector.FindStaticPredicates(pddlDecl);
            statics.RemoveAll(x => x.Arguments.Count > 1);
            foreach (var action in pddlDecl.Domain.Actions)
            {
                var newAction = action.Copy();
                newAction.Name = $"meta_{newAction.Name}";
                newAction.Parameters = new ParameterExp();
                newAction.Preconditions = new AndExp(newAction);

                var requiredStatics = new HashSet<PredicateExp>();
                var effects = newAction.Effects.FindTypes<PredicateExp>();
                foreach (var effect in effects)
                {
                    foreach (var arg in effect.Arguments)
                        if (!newAction.Parameters.Values.Contains(arg))
                            newAction.Parameters.Values.Add(arg.Copy());
                    requiredStatics.AddRange(GetRequiredStatics(action, effect, statics).ToHashSet());
                }

                foreach (var requiredStatic in requiredStatics)
                    foreach (var arg in requiredStatic.Arguments)
                        if (!newAction.Parameters.Values.Contains(arg))
                            newAction.Parameters.Values.Add(arg.Copy());
                newAction.Preconditions = new AndExp(newAction, new List<IExp>(requiredStatics));
                candidates.Add(newAction);
            }

            return candidates;
        }

        private List<PredicateExp> GetRequiredStatics(ActionDecl baseActionDecl, PredicateExp predicate, List<PredicateExp> statics)
        {
            var requiredStatics = new List<PredicateExp>();
            var precons = baseActionDecl.Preconditions.FindTypes<PredicateExp>();
            foreach (var precon in precons)
                if (statics.Any(x => x.Name == precon.Name))
                    if (precon.Arguments.Any(predicate.Arguments.Contains))
                        requiredStatics.Add(precon);
            return requiredStatics;
        }
    }
}
