using PDDLSharp.Models.PDDL.Domain;
using PDDLSharp.Models.PDDL.Problem;

namespace FocusedMetaActions.Train.UsefulnessCheckers
{
    public interface IUsefulnessChecker
    {
        public string WorkingDir { get; }
        public int TimeLimitS { get; }
        public List<ActionDecl> GetUsefulCandidates(DomainDecl domain, List<ProblemDecl> problems, List<ActionDecl> candidates);
    }
}
