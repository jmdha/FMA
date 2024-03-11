using PDDLSharp.Contextualisers.PDDL;
using PDDLSharp.ErrorListeners;
using PDDLSharp.Models.PDDL;
using PDDLSharp.Models.PDDL.Domain;
using PDDLSharp.Models.PDDL.Expressions;
using PDDLSharp.Translators.StaticPredicateDetectors;

namespace MetaActionCandidateGenerator.CandidateGenerators
{
    /// <summary>
    /// Takes all non-static predicates and makes meta actions based on no preconditions and simply the predicate.
    /// Both a normal and a negated version is made for each predicate
    /// </summary>
    public class PredicateMetaActions : ICandidateGenerator
    {
        public List<ActionDecl> GenerateCandidates(PDDLDecl pddlDecl)
        {
            if (pddlDecl.Domain.Predicates == null)
                throw new Exception("No predicates defined in domain!");

            if (!pddlDecl.IsContextualised)
            {
                var listener = new ErrorListener();
                var contextualiser = new PDDLContextualiser(listener);
                contextualiser.Contexturalise(pddlDecl);
            }

            var candidates = new List<ActionDecl>();

            var statics = SimpleStaticPredicateDetector.FindStaticPredicates(pddlDecl);
            statics.Add(new PredicateExp("="));
            foreach (var predicate in pddlDecl.Domain.Predicates.Predicates)
            {
                if (!statics.Any(x => x.Name.ToUpper() == predicate.Name.ToUpper()))
                {
                    var newAct = new ActionDecl($"meta_{predicate.Name}");
                    foreach (var arg in predicate.Arguments)
                        newAct.Parameters.Add(arg.Copy());
                    var reqStatics = GetRequiredStatics(pddlDecl, predicate);
                    newAct.Preconditions = new AndExp(newAct, new List<IExp>(reqStatics));
                    newAct.Effects = new AndExp(newAct, new List<IExp>() { predicate.Copy() });
                    foreach(var reqStatic in reqStatics)
                        foreach(var args in reqStatic.Arguments)
                            if (!newAct.Parameters.Values.Contains(args))
                                newAct.Parameters.Add(args.Copy());

                    candidates.Add(newAct);
                }
            }

            return candidates;
        }

        private List<PredicateExp> GetRequiredStatics(PDDLDecl pddlDecl, PredicateExp predicate)
        {
            var requiredStatics = new List<PredicateExp>();
            foreach (var action in pddlDecl.Domain.Actions)
            {
                var staticsToAdd = GetStaticsForPredicate(pddlDecl, action, predicate);
                foreach(var toAdd in staticsToAdd)
                    if (!requiredStatics.Any(x => x.Name == toAdd.Name))
                        requiredStatics.Add(toAdd);
            }
            return requiredStatics;
        }

        private List<PredicateExp> GetStaticsForPredicate(PDDLDecl pddlDecl, ActionDecl act, PredicateExp pred)
        {
            var statics = SimpleStaticPredicateDetector.FindStaticPredicates(pddlDecl);
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
                    foreach(var check in checkStatics)
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
    }
}
