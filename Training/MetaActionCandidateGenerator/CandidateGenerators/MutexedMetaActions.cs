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

namespace MetaActionCandidateGenerator.CandidateGenerators
{
    /// <summary>
    /// Generates meta actions by taking normal actions that contain mutexes and removing all (non-static) preconditions from it.
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

            var mutexFinder = new EffectBalanceMutexes();
            var mutexes = mutexFinder.FindMutexes(pddlDecl);
            var statics = SimpleStaticPredicateDetector.FindStaticPredicates(pddlDecl);
            statics.Add(new PredicateExp("="));
            foreach (var mutex in mutexes)
            {
                foreach (var action in pddlDecl.Domain.Actions)
                {
                    var mutexPre = action.Preconditions.FindNames(mutex.Name);
                    var mutexEff = action.Effects.FindNames(mutex.Name);
                    if (mutexPre.Count == 0 || mutexEff.Count == 0)
                        continue;

                    var newAction = action.Copy();
                    newAction.Name = $"meta_{mutex.Name}";
                    newAction.Parameters = new ParameterExp();
                    var preAnd = new AndExp(newAction);
                    newAction.Preconditions = preAnd;
                    foreach(var item in mutexPre)
                    {
                        if (item.Parent is NotExp not)
                            preAnd.Add(not);
                        else
                            preAnd.Add(item);
                    }
                    var effAnd = new AndExp(newAction);
                    newAction.Effects = effAnd;
                    foreach (var item in mutexEff)
                    {
                        if (item.Parent is NotExp not)
                            effAnd.Add(not);
                        else
                            effAnd.Add(item);
                    }

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

                    foreach(var requiredStatic in requiredStatics)
                        preAnd.Add(requiredStatic);
                    candidates.Add(newAction);
                }
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
