using P10.Verifiers;
using PDDLSharp.Models.PDDL;
using PDDLSharp.Models.PDDL.Domain;

namespace P10.RefinementStrategies
{
    public interface IRefinementStrategy
    {
        public IVerifier Verifier { get; }
        public ActionDecl? Refine(PDDLDecl pddlDecl, ActionDecl currentMetaAction, ActionDecl originalMetaAction, string workingDir);
    }
}
