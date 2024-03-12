using PDDLSharp.Contextualisers.PDDL;
using PDDLSharp.ErrorListeners;
using PDDLSharp.Models.PDDL.Domain;
using PDDLSharp.Models.PDDL.Expressions;
using PDDLSharp.Models.PDDL;
using PDDLSharp.Translators.StaticPredicateDetectors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MetaActionCandidateGenerator.CandidateGenerators
{
    /// <summary>
    /// 
    /// </summary>
    public class InvariantMetaActions : ICandidateGenerator
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
                int count = 0;
                foreach (var pred in pddlDecl.Domain.Predicates.Predicates)
                {
                    if (statics.Any(x => x.Name == pred.Name))
                        continue;
                    if (pred.Arguments.Count == 0)
                        continue;
                    if (!CanPredicateBeSetToFalse(pddlDecl, pred))
                        continue;
                    if (!CanPredicateBeSetToTrue(pddlDecl, pred))
                        continue;

                    var permutations = GeneratePermutations(pred.Arguments.Count);
                    if (permutations.Count > 0 && permutations[0].Length > 1)
                    {
                        permutations.RemoveAll(x => x.All(y => y == true));
                        permutations.RemoveAll(x => x.All(y => y == false));
                    }
                    foreach (var permutation in permutations)
                    {
                        var newAction = new ActionDecl($"meta_{pred.Name}_{count++}");
                        var preAnd = new AndExp(newAction);
                        newAction.Preconditions = preAnd;
                        var effAnd = new AndExp(newAction);
                        newAction.Effects = effAnd;
                        var currentPre = pred;
                        var currentEff = GetMutatedPredicate(pred, permutation.ToList());
                        if (currentPre.Equals(currentEff))
                            continue;
                        preAnd.Add(currentPre);
                        var preReq = GetRequiredStatics(pddlDecl, currentPre);
                        foreach (var pre in preReq)
                            if (!preAnd.Children.Contains(pre))
                                preAnd.Add(pre);
                        effAnd.Add(currentEff);
                        effAnd.Add(new NotExp(currentPre));
                        var effReq = GetRequiredStatics(pddlDecl, currentEff);
                        foreach (var pre in effReq)
                            if (!preAnd.Children.Contains(pre))
                                preAnd.Add(pre);

                        var all = newAction.FindTypes<PredicateExp>();
                        foreach (var item in all)
                            foreach (var arg in item.Arguments)
                                if (!newAction.Parameters.Values.Contains(arg))
                                    newAction.Parameters.Values.Add(arg);

                        candidates.Add(newAction);
                    }
                }
            }

            return candidates;
        }

        private List<bool[]> GeneratePermutations(int count)
        {
            var returnQueue = new List<bool[]>();
            GeneratePermutations(count, new bool[count], 0, returnQueue);
            return returnQueue;
        }


        private void GeneratePermutations(int count, bool[] source, int index, List<bool[]> returnQueue)
        {
            var trueSource = new bool[count];
            Array.Copy(source, trueSource, count);
            trueSource[index] = true;
            if (index < count - 1)
                GeneratePermutations(count, trueSource, index + 1, returnQueue);
            else
                returnQueue.Add(trueSource);

            var falseSource = new bool[count];
            Array.Copy(source, falseSource, count);
            falseSource[index] = false;
            if (index < count - 1)
                GeneratePermutations(count, falseSource, index + 1, returnQueue);
            else
                returnQueue.Add(falseSource);
        }

        private PredicateExp GetMutatedPredicate(PredicateExp from, List<bool> indexes)
        {
            var copy = from.Copy();
            for (int i = 0; i < copy.Arguments.Count; i++)
                if (indexes[i])
                    copy.Arguments[i].Name = $"{copy.Arguments[i].Name}2";
            return copy;
        }

        private List<PredicateExp> GetRequiredStatics(PDDLDecl pddlDecl, PredicateExp predicate)
        {
            var requiredStatics = new List<PredicateExp>();
            foreach (var action in pddlDecl.Domain.Actions)
            {
                var staticsToAdd = GetStaticsForPredicate(pddlDecl, action, predicate);
                foreach (var toAdd in staticsToAdd)
                    if (!requiredStatics.Any(x => x.Name == toAdd.Name))
                        requiredStatics.Add(toAdd);
            }
            return requiredStatics;
        }

        private List<PredicateExp> GetStaticsForPredicate(PDDLDecl pddlDecl, ActionDecl act, PredicateExp pred)
        {
            var statics = SimpleStaticPredicateDetector.FindStaticPredicates(pddlDecl);
            statics.RemoveAll(x => x.Arguments.Count > 1);
            var actionStatics = act.Preconditions.FindTypes<PredicateExp>().Where(x => statics.Any(y => y.Name == x.Name)).ToList();

            var requiredStatics = new List<PredicateExp>();
            var checkStatics = new List<PredicateExp>();

            var instances = act.Effects.FindNames(pred.Name);
            foreach (var instance in instances)
            {
                if (instance is PredicateExp predicate && predicate.Arguments.Count == pred.Arguments.Count)
                {
                    var nameMap = new Dictionary<string, string>();
                    for (int i = 0; i < predicate.Arguments.Count; i++)
                    {
                        var find = actionStatics.Where(x => x.Arguments.Any(y => y.Name == predicate.Arguments[i].Name));
                        if (find != null)
                        {
                            foreach (var actionStatic in find)
                            {
                                if (!requiredStatics.Any(x => x.Name == actionStatic.Name))
                                {
                                    var name = actionStatic.Arguments.First(x => x.Name == predicate.Arguments[i].Name);
                                    if (!nameMap.ContainsKey(name.Name))
                                        nameMap.Add(name.Name, pred.Arguments[i].Name);
                                    if (!checkStatics.Any(x => x.Name == actionStatic.Name))
                                        checkStatics.Add(actionStatic.Copy());
                                }
                            }
                        }
                    }
                    foreach (var check in checkStatics)
                    {
                        var newStatic = check.Copy();
                        foreach (var arg in newStatic.Arguments)
                            if (nameMap.ContainsKey(arg.Name))
                                arg.Name = nameMap[arg.Name];
                        requiredStatics.Add(newStatic);
                    }

                }
            }

            return requiredStatics;
        }

        private bool CanPredicateBeSetToFalse(PDDLDecl pddlDecl, PredicateExp pred)
        {
            var result = false;

            foreach(var action in pddlDecl.Domain.Actions)
            {
                var effects = action.FindNames(pred.Name);
                result = effects.Any(x => x.Parent is NotExp);
                if (result)
                    return true;
            }

            return result;
        }

        private bool CanPredicateBeSetToTrue(PDDLDecl pddlDecl, PredicateExp pred)
        {
            var result = false;

            foreach (var action in pddlDecl.Domain.Actions)
            {
                var effects = action.FindNames(pred.Name);
                result = effects.Any(x => x.Parent is not NotExp);
                if (result)
                    return true;
            }

            return result;
        }
    }
}