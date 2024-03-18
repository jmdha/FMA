using PDDLSharp.Models.PDDL.Domain;
using PDDLSharp.Models.PDDL.Problem;
using PDDLSharp.Models.PDDL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tools;

namespace P10.Verifiers
{
    public static class VerificationHelper
    {
        public static bool IsValid(DomainDecl domain, List<ProblemDecl> problems, ActionDecl metaAction, string workingDir, int timeLimitS)
        {
            var verifier = new FrontierVerifier();
            foreach (var problem in problems)
            {
                var compiled = StackelbergCompiler.StackelbergCompiler.CompileToStackelberg(new PDDLDecl(domain, problem), metaAction.Copy());
                if (!verifier.Verify(compiled.Domain, compiled.Problem, workingDir, timeLimitS))
                    return false;
            }
            return true;
        }
    }
}
