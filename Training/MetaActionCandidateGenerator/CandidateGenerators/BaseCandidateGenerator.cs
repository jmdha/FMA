using PDDLSharp.Contextualisers.PDDL;
using PDDLSharp.ErrorListeners;
using PDDLSharp.Models.PDDL;
using PDDLSharp.Models.PDDL.Domain;
using PDDLSharp.Models.PDDL.Expressions;
using PDDLSharp.Tools;
using PDDLSharp.Translators.Tools;

namespace MetaActionCandidateGenerator.CandidateGenerators
{
    public abstract class BaseCandidateGenerator : ICandidateGenerator
    {
        public List<PredicateExp> Statics = new List<PredicateExp>();
        public List<PredicateExp> SimpleStatics = new List<PredicateExp>();

        public void Initialize(PDDLDecl pddlDecl)
        {
            Statics = SimpleStaticPredicateDetector.FindStaticPredicates(pddlDecl);
            Statics.Add(new PredicateExp("="));
            SimpleStatics = new List<PredicateExp>(Statics.Count);
            foreach (var staticItem in Statics)
                if (staticItem.Arguments.Count <= 1)
                    SimpleStatics.Add(staticItem);
        }

        public List<ActionDecl> GenerateCandidates(PDDLDecl pddl)
        {
            var candidates = GenerateCandidatesInner(pddl);
            foreach (var candidate in candidates)
                if (!candidate.Name.Contains('$'))
                    candidate.Name = $"${candidate.Name}";
            return candidates;
        }

        internal abstract List<ActionDecl> GenerateCandidatesInner(PDDLDecl pddl);

        internal void ContextualizeIfNotAlready(PDDLDecl pddlDecl)
        {
            if (!pddlDecl.IsContextualised)
            {
                var listener = new ErrorListener();
                var contextualiser = new PDDLContextualiser(listener);
                contextualiser.Contexturalise(pddlDecl);
            }
        }

        internal ActionDecl GenerateMetaAction(string actionName, List<IExp> preconditions, List<IExp> effects, ActionDecl staticsReference)
        {
            var newAction = new ActionDecl(actionName);
            newAction.Parameters = new ParameterExp();
            newAction.Preconditions = new AndExp(newAction, preconditions);
            newAction.Effects = new AndExp(newAction, effects);

            var requiredStatics = new HashSet<PredicateExp>();

            // Find requires statics for preconditions
            var actionPreconditions = newAction.Preconditions.FindTypes<PredicateExp>();
            foreach (var precondition in actionPreconditions)
                requiredStatics.AddRange(GetUnaryStaticsForPredicate(staticsReference, precondition).ToHashSet());
            // Find requires statics for effects
            var actionEffects = newAction.Effects.FindTypes<PredicateExp>();
            foreach (var effect in actionEffects)
                requiredStatics.AddRange(GetUnaryStaticsForPredicate(staticsReference, effect).ToHashSet());

            // Add statics that the preconditions
            if (newAction.Preconditions is AndExp and)
                foreach (var reqStatic in requiredStatics)
                    and.Children.Add(reqStatic);

            // Stitch parameters together
            var all = newAction.FindTypes<PredicateExp>();
            foreach (var pred in all)
                foreach (var arg in pred.Arguments)
                    if (!newAction.Parameters.Values.Contains(arg))
                        newAction.Parameters.Values.Add(arg.Copy());

            return newAction;
        }

        private List<PredicateExp> GetUnaryStaticsForPredicate(ActionDecl act, PredicateExp pred)
        {
            var actionStatics = act.Preconditions.FindTypes<PredicateExp>().Where(x => SimpleStatics.Any(y => y.Name == x.Name)).ToList();

            var requiredStatics = new List<PredicateExp>();

            var instances = act.FindNames(pred.Name);
            foreach (var instance in instances)
            {
                if (instance is PredicateExp predicate && predicate.Arguments.Count == pred.Arguments.Count)
                {
                    var checkStatics = new List<PredicateExp>();
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

        internal PredicateExp GetEqualsPredicate(PredicateExp pred1, PredicateExp pred2)
        {
            var args = new List<NameExp>();
            for (int i = 0; i < pred1.Arguments.Count; i++)
            {
                if (pred1.Arguments[i].Name != pred2.Arguments[i].Name)
                {
                    args.Add(pred1.Arguments[i]);
                    args.Add(pred2.Arguments[i]);
                }
            }

            return new PredicateExp("=", args);
        }
    }
}
