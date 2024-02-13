using PDDLSharp.Models.PDDL.Domain;
using PDDLSharp.Models.PDDL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PDDLSharp.Translators.StaticPredicateDetectors;
using PDDLSharp.Models.PDDL.Expressions;

namespace MetaActionCandidateGenerator.CandidateGenerators
{
    public class PredicateMetaActions : ICandidateGenerator
    {
        public List<ActionDecl> GenerateCandidates(PDDLDecl pddl)
        {
            if (pddl.Domain.Predicates == null)
                throw new Exception("No predicates defined in domain!");

            var candidates = new List<ActionDecl>();

            var statics = SimpleStaticPredicateDetector.FindStaticPredicates(pddl);
            statics.Add(new PredicateExp("="));
            foreach (var predicate in pddl.Domain.Predicates.Predicates)
            {
                if (!statics.Any(x => x.Name.ToUpper() == predicate.Name.ToUpper()))
                {
                    var newAct = new ActionDecl($"meta_{predicate.Name}");
                    foreach(var arg in predicate.Arguments)
                        newAct.Parameters.Add(arg.Copy());
                    newAct.Effects = new AndExp(newAct, new List<IExp>() { predicate.Copy() });
                    candidates.Add(newAct);
                }
            }

            return candidates;
        }
    }
}
