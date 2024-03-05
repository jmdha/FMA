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
                    newAct.Preconditions = new AndExp(newAct, new List<IExp>(GetRequiredStatics(pddlDecl, predicate)));
                    newAct.Effects = new AndExp(newAct, new List<IExp>() { predicate.Copy() });
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
                var staticsNames = GetStaticsForPredicate(pddlDecl, action, predicate);
                foreach (var argName in staticsNames.Keys)
                {
                    var staticsToAdd = staticsNames[argName];
                    foreach (var staticToAdd in staticsToAdd)
                    {
                        var newStatics = new PredicateExp(staticToAdd);
                        newStatics.Arguments.Add(new NameExp(argName));
                        if (!requiredStatics.Contains(newStatics))
                            requiredStatics.Add(newStatics);
                    }
                }
            }
            return requiredStatics;
        }

        private Dictionary<string, List<string>> GetStaticsForPredicate(PDDLDecl pddlDecl, ActionDecl act, PredicateExp pred)
        {
            var requiredStatics = new Dictionary<string, List<string>>();
            var statics = SimpleStaticPredicateDetector.FindStaticPredicates(pddlDecl);
            statics.RemoveAll(x => x.Arguments.Count != 1);
            var actionStatics = act.Preconditions.FindTypes<PredicateExp>().Where(x => statics.Any(y => y.Name == x.Name)).ToList();

            var instances = act.Effects.FindNames(pred.Name);
            foreach (var instance in instances)
            {
                if (instance is PredicateExp predicate && predicate.Arguments.Count == pred.Arguments.Count)
                {
                    for (int i = 0; i < predicate.Arguments.Count; i++)
                    {
                        var find = actionStatics.FirstOrDefault(x => x.Arguments[0].Name == predicate.Arguments[i].Name);
                        if (find != null)
                        {
                            if (!requiredStatics.ContainsKey(pred.Arguments[i].Name))
                                requiredStatics.Add(pred.Arguments[i].Name, new List<string>() { find.Name });
                            else
                                requiredStatics[pred.Arguments[i].Name].Add(find.Name);
                        }
                    }
                }
            }

            return requiredStatics;
        }
    }
}
