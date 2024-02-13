using PDDLSharp.Models.PDDL;
using PDDLSharp.Models.PDDL.Domain;
using PDDLSharp.Models.PDDL.Expressions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MetaActionCandidateGenerator
{
    public interface ICandidateGenerator
    {
        public List<ActionDecl> GenerateCandidates(PDDLDecl pddl);
    }
}
