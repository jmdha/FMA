using PDDLSharp.CodeGenerators.PDDL;
using PDDLSharp.ErrorListeners;
using PDDLSharp.Models.PDDL.Domain;
using PDDLSharp.Models.PDDL.Problem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace P10.Verifiers
{
    public class StateExploreVerifier : BaseVerifier
    {
        public static readonly string StateInfoFile = "out";

        public StateExploreVerifier()
        {
            SearchString = "--search \"state_explore(optimal_engine=symbolic(plan_reuse_minimal_task_upper_bound=false, plan_reuse_upper_bound=true), upper_bound_pruning=false)\"";
        }

        public override bool Verify(DomainDecl domain, ProblemDecl problem, string workingDir)
        {
            var listener = new ErrorListener();
            var codeGenerator = new PDDLCodeGenerator(listener);
            var domainFile = Path.Combine(workingDir, $"tempDomain.pddl");
            var problemFile = Path.Combine(workingDir, $"tempProblem.pddl");
            codeGenerator.Generate(domain, domainFile);
            codeGenerator.Generate(problem, problemFile);
            var exitCode = ExecutePlanner(domainFile, problemFile, workingDir);
            if (exitCode != 0)
                return false;
            if (File.Exists(Path.Combine(workingDir, StateInfoFile)))
                return false;
            return true;
        }
    }
}
