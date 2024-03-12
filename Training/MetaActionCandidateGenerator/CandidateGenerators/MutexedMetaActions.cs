using PDDLSharp.Contextualisers.PDDL;
using PDDLSharp.ErrorListeners;
using PDDLSharp.Models.PDDL.Domain;
using PDDLSharp.Models.PDDL.Expressions;
using PDDLSharp.Models.PDDL;
using PDDLSharp.Tools;
using PDDLSharp.Translators.StaticPredicateDetectors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PDDLSharp.Toolkit.MutexDetectors;
using System.Threading;

namespace MetaActionCandidateGenerator.CandidateGenerators
{
    /// <summary>
    /// Assumed every predicate can be a mutex, and constructs meta actions out of them
    /// </summary>
    public class MutexedMetaActions : ICandidateGenerator
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

            if (pddlDecl.Domain.Predicates != null)
            {
                var statics = SimpleStaticPredicateDetector.FindStaticPredicates(pddlDecl);
                statics.Add(new PredicateExp("="));
                foreach (var pred in pddlDecl.Domain.Predicates.Predicates)
                {
                    if (statics.Any(x => x.Name == pred.Name))
                        continue;

                    var count = 0;
                    foreach (var action in pddlDecl.Domain.Actions)
                    {
                        var findTarget = action.FindNames(pred.Name).Cast<PredicateExp>().ToList();
                        findTarget = findTarget.DistinctBy(x => GetArgString(x.Arguments)).ToList();
                        if (findTarget.Count > 0)
                        {
                            foreach (var target in findTarget)
                            {
                                var newAction = action.Copy();
                                newAction.Name = $"meta_{pred.Name}_{count++}";
                                newAction.Parameters = new ParameterExp(newAction);
                                newAction.Preconditions = new AndExp(newAction, new List<IExp>(GetRequiredStatics(action, target, statics)) { new NotExp(target) });
                                newAction.Effects = new AndExp(newAction, new List<IExp>() { target });

                                var all = newAction.FindTypes<PredicateExp>();
                                foreach (var item in all)
                                    foreach (var arg in item.Arguments)
                                        if (!newAction.Parameters.Values.Contains(arg))
                                            newAction.Parameters.Values.Add(arg);

                                candidates.Add(newAction);

                                var newActionNegated = action.Copy();
                                newActionNegated.Name = $"meta_{pred.Name}_{count++}";
                                newActionNegated.Parameters = new ParameterExp(newActionNegated);
                                newActionNegated.Preconditions = new AndExp(newActionNegated, new List<IExp>(GetRequiredStatics(action, target, statics)) { target });
                                newActionNegated.Effects = new AndExp(newActionNegated, new List<IExp>() { new NotExp(target) });

                                var all2 = newActionNegated.FindTypes<PredicateExp>();
                                foreach (var item in all2)
                                    foreach (var arg in item.Arguments)
                                        if (!newActionNegated.Parameters.Values.Contains(arg))
                                            newActionNegated.Parameters.Values.Add(arg);

                                candidates.Add(newActionNegated);
                            }
                        }
                    }
                }
            }


            return candidates;
        }

        private string GetArgString(List<NameExp> list)
        {
            var retStr = "";
            foreach (var item in list)
                retStr += item.Name;
            return retStr;
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
