using PDDLSharp.Models.PDDL.Domain;
using PDDLSharp.Models.PDDL.Problem;

namespace P10.RefinementStrategies
{
    public interface IRefinementStrategy
    {
        public int TimeLimitS { get; }
        public string TempDir { get; }
        public string OutputDir { get; }
        public ActionDecl MetaAction { get; }

        public List<ActionDecl> Refine(DomainDecl domain, List<ProblemDecl> problems);
    }
}
