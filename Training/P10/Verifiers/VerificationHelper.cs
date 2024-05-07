using P10.Helpers;
using PDDLSharp.CodeGenerators.PDDL;
using PDDLSharp.ErrorListeners;
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
            foreach (var problem in problems)
            {
                ConsoleHelper.WriteLineColor($"\t\tValidating on problem {problem.Name}: ", ConsoleColor.Yellow);
                var compiled = StackelbergHelper.CompileToStackelberg(new PDDLDecl(domain, problem), metaAction.Copy());
                var isValid = verifier.Verify(compiled.Domain, compiled.Problem, workingDir, timeLimitS);
                //if (verifier.TimedOut)
                //{
                //    ConsoleHelper.WriteLineColor($"\t\tMeta Action Verification timed out, assuming following problems are too hard...", ConsoleColor.Yellow);
                //    break;
                //}
                if (!isValid)
                {
                    ConsoleHelper.WriteLineColor($"\t\t\tInvalid", ConsoleColor.Red);
                    ConsoleHelper.WriteLineColor($"\t\tMeta action invalid in problem {problem.Name}", ConsoleColor.Red);
                    any = false;
                    break;
                }
                else
                {
                    ConsoleHelper.WriteLineColor($"\t\t\tValid", ConsoleColor.Green);
                    any = true;
                }
            }
            return any;
        }
    }
}
