using PDDLSharp.Models.PDDL;
using PDDLSharp.Models.PDDL.Domain;

namespace MetaActionCandidateGenerator
{
    public interface ICandidateGenerator
    {
        public List<ActionDecl> GenerateCandidates(PDDLDecl pddl);
    }
}
