using PDDLSharp.Contextualisers.PDDL;
using PDDLSharp.ErrorListeners;
using PDDLSharp.Models.PDDL;
using PDDLSharp.Models.PDDL.Domain;
using PDDLSharp.Models.PDDL.Expressions;
using PDDLSharp.Translators.StaticPredicateDetectors;

namespace MetaActionCandidateGenerator.CandidateGenerators
{
    /// <summary>
    /// Generates meta actions by taking normal actions and removing all (non-static) preconditions from it. As well as for each action, make an action that is only a single effect predicate.
    /// </summary>
    public class AgressiveStrippedMetaActions : ICandidateGenerator
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
                EnsureAnd(action);
                var effects = new HashSet<IExp>();
                if (action.Effects is AndExp andEff)
                {
                    foreach (var child in andEff.Children)
                    {
                        if (child is NotExp not)
                        {
                            effects.Add(not);
                            effects.Add(not.Child);
                        }
                        else
                        {
                            effects.Add(child);
                            effects.Add(new NotExp(child));
                        }
                    }
                }

                int count = 0;
                foreach (var effect in effects)
                {
                    var newAction = action.Copy();
                    newAction.Name = $"meta_{newAction.Name}_{count++}";
                    if (newAction.Preconditions is AndExp andPre)
                    {
                        var toRemovePre = new List<IExp>();
                        foreach (var child in andPre.Children)
                            if (child is INamedNode named)
                                if (!statics.Any(x => x.Name == named.Name))
                                    toRemovePre.Add(child);
                        foreach (var remove in toRemovePre)
                            andPre.Remove(remove);
                    }
                    newAction.Effects = new AndExp(newAction, new List<IExp>() { effect });

                    var toRemove = new List<NameExp>();
                    foreach (var arg in newAction.Parameters.Values)
                    {
                        var find = newAction.FindNames(arg.Name);
                        if (find.Count == 1)
                            toRemove.Add(arg);
                    }
                    foreach (var remove in toRemove)
                        newAction.Parameters.Values.Remove(remove);

                    candidates.Add(newAction);
                }
            }

            return candidates;
        }

        private void EnsureAnd(ActionDecl action)
        {
            if (action.Preconditions is not AndExp)
                action.Preconditions = new AndExp(action, new List<IExp>() { action.Preconditions });
            if (action.Effects is not AndExp)
                action.Effects = new AndExp(action, new List<IExp>() { action.Effects });
        }
    }
}
