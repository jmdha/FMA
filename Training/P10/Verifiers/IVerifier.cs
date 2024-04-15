using PDDLSharp.Models.PDDL.Domain;
using PDDLSharp.Models.PDDL.Problem;

namespace P10.Verifiers
{
    public interface IVerifier
    {
        public bool TimedOut { get; }
        public bool Verify(DomainDecl domain, ProblemDecl problem, string workingDir, int timeLimitS);
        public string GetLog();
    }
}
