using P10.Helpers;
using PDDLSharp.Models.PDDL;
using PDDLSharp.Models.PDDL.Domain;
using PDDLSharp.Models.PDDL.Problem;
using Tools;

namespace P10.Verifiers
{
    public static class VerificationHelper
    {
        public static bool IsValid(DomainDecl domain, List<ProblemDecl> problems, ActionDecl metaAction, string workingDir, int timeLimitS)
        {
            var verifier = new FrontierVerifier();
            bool any = false;
            var count = 1;
            foreach (var problem in problems)
            {
                ConsoleHelper.WriteLineColor($"\t\t\tValidating on problem {problem.Name} [{count++} of {problems.Count}] ", ConsoleColor.Yellow);
                var compiled = StackelbergHelper.CompileToStackelberg(new PDDLDecl(domain, problem), metaAction.Copy());
                var isValid = verifier.Verify(compiled.Domain, compiled.Problem, workingDir, timeLimitS);
                if (verifier.TimedOut)
                {
                    ConsoleHelper.WriteLineColor($"\t\t\tMeta Action Verification timed out, trying next problem...", ConsoleColor.Yellow);
                    continue;
                }
                if (!isValid)
                {
                    ConsoleHelper.WriteLineColor($"\t\t\t\tInvalid", ConsoleColor.Red);
                    ConsoleHelper.WriteLineColor($"\t\t\tMeta action invalid in problem {problem.Name}", ConsoleColor.Red);
                    any = false;
                    break;
                }
                else
                {
                    ConsoleHelper.WriteLineColor($"\t\t\t\tValid", ConsoleColor.Green);
                    any = true;
                }
            }
            return any;
        }
    }
}
