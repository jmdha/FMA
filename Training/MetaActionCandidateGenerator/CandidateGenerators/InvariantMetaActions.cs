using PDDLSharp.Models.PDDL;
using PDDLSharp.Models.PDDL.Domain;
using PDDLSharp.Models.PDDL.Expressions;
using PDDLSharp.Models.PDDL.Overloads;

namespace MetaActionCandidateGenerator.CandidateGenerators
{
    /// <summary>
    /// Generates meta action by the assumption that any predicate can be an invariant
    /// </summary>
    public class InvariantMetaActions : BaseCandidateGenerator
    {
        internal override List<ActionDecl> GenerateCandidatesInner(PDDLDecl pddlDecl)
        {
            if (pddlDecl.Domain.Predicates == null)
                throw new Exception("No predicates defined in domain!");
            Initialize(pddlDecl);
            ContextualizeIfNotAlready(pddlDecl);

            var candidates = new List<ActionDecl>();
            int count = 0;
            foreach (var pred in pddlDecl.Domain.Predicates.Predicates)
            {
                if (Statics.Any(x => x.Name == pred.Name))
                    continue;
                if (pred.Arguments.Count == 0)
                    continue;
                if (pred.CanOnlyBeSetToTrue(pddlDecl.Domain) || pred.CanOnlyBeSetToFalse(pddlDecl.Domain))
                    continue;

                var permutations = GeneratePermutations(pred.Arguments.Count);
                if (permutations.Count > 0 && permutations[0].Length > 1)
                {
                    permutations.RemoveAll(x => x.All(y => y == true));
                    permutations.RemoveAll(x => x.All(y => y == false));
                }
                foreach (var permutation in permutations)
                {
                    foreach (var action in pddlDecl.Domain.Actions)
                    {
                        if (action.FindNames(pred.Name).Count == 0)
                            continue;
                        var mutated = GetMutatedPredicate(pred, permutation.ToList());
                        if (!mutated.Equals(pred))
                            candidates.Add(GenerateMetaAction(
                                $"meta_{pred.Name}_{action.Name}_{count++}",
                                new List<IExp>() { pred },
                                new List<IExp>() { GetMutatedPredicate(pred, permutation.ToList()), new NotExp(pred) },
                                action));
                    }
                }
            }

            return candidates.Distinct(pddlDecl.Domain.Actions);
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
    }
}