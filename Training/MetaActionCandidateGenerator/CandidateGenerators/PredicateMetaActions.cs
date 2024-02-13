using PDDLSharp.Contextualisers.PDDL;
using PDDLSharp.ErrorListeners;
using PDDLSharp.Models.PDDL;
using PDDLSharp.Models.PDDL.Domain;
using PDDLSharp.Models.PDDL.Expressions;
using PDDLSharp.Translators.StaticPredicateDetectors;

namespace MetaActionCandidateGenerator.CandidateGenerators
{
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
                    newAct.Effects = new AndExp(newAct, new List<IExp>() { predicate.Copy() });
                    candidates.Add(newAct);
                }
            }

            return candidates;
        }
    }
}
