using PDDLSharp.Models.PDDL.Domain;
using PDDLSharp.Models.PDDL.Problem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace P10.Verifiers
{
    public interface IVerifier
    {
        public bool Verify(DomainDecl domain, ProblemDecl problem, string workingDir);
    }
}
