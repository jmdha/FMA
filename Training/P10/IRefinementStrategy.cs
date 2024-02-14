using PDDLSharp.Models.PDDL;
using PDDLSharp.Models.PDDL.Domain;

namespace P10
{
    public interface IRefinementStrategy
    {
        public ActionDecl? Refine(PDDLDecl pddlDecl, ActionDecl currentMetaAction);
    }
}
