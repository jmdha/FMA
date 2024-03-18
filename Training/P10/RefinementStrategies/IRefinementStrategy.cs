using PDDLSharp.Models.PDDL.Domain;
using PDDLSharp.Models.PDDL.Problem;

namespace P10.RefinementStrategies
{
    public interface IRefinementStrategy
    {
        public int TimeLimitS { get; }
        public int MetaActionIndex { get; }
        public string TempDir { get; }
        public string OutputDir { get; }

        public ActionDecl? Refine(DomainDecl domain, List<ProblemDecl> problems, ActionDecl currentMetaAction, ActionDecl originalMetaAction);
    }
}
