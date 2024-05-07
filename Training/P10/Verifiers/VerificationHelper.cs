using P10.Helpers;
using PDDLSharp.CodeGenerators.PDDL;
using PDDLSharp.ErrorListeners;
using PDDLSharp.Models.PDDL;
using PDDLSharp.Models.PDDL.Domain;
using PDDLSharp.Models.PDDL.Problem;
using Tools;
using static P10.Verifiers.FrontierVerifier;

namespace P10.Verifiers
{
    public static class VerificationHelper
    {
        public static List<FrontierResult> GetValidity(DomainDecl domain, List<ProblemDecl> problems, ActionDecl metaAction, string workingDir, int timeLimitS)
        {
            var verifier = new FrontierVerifier();
            var validity = new List<FrontierResult>();
            for(int i = 0; i < problems.Count; i++)
            {
                ConsoleHelper.WriteLineColor($"\t\t\tValidating on problem {problems[i].Name} [{i + 1} of {problems.Count}] ", ConsoleColor.Yellow);
                var compiled = StackelbergHelper.CompileToStackelberg(new PDDLDecl(domain, problems[i]), metaAction.Copy());
                var result = verifier.Verify(compiled.Domain, compiled.Problem, workingDir, timeLimitS);
                if (verifier.TimedOut)
                {
                    ConsoleHelper.WriteLineColor($"\t\t\tMeta Action Verification timed out assuming its valid...", ConsoleColor.Yellow);
                    validity.Add(FrontierResult.Valid);
                }
                else if (result == FrontierResult.Invalid)
                {
                    ConsoleHelper.WriteLineColor($"\t\t\t\tInvalid", ConsoleColor.Red);
                    validity.Add(FrontierResult.Invalid);
                }
                else if (result == FrontierResult.Valid)
                {
                    ConsoleHelper.WriteLineColor($"\t\t\t\tValid", ConsoleColor.Green);
                    validity.Add(FrontierResult.Valid);
                }
                else if (result == FrontierResult.Valid)
                {
                    ConsoleHelper.WriteLineColor($"\t\t\t\tInapplicable", ConsoleColor.Yellow);
                    validity.Add(FrontierResult.Inapplicable);
                }
            }
            return validity;
        }

        public static bool IsAllValidOrInapplicable(DomainDecl domain, List<ProblemDecl> problems, ActionDecl metaAction, string workingDir, int timeLimitS)
        {
            var verifier = new FrontierVerifier();
            bool any = false;
            for (int i = 0; i < problems.Count; i++)
            {
                ConsoleHelper.WriteLineColor($"\t\t\tValidating on problem {problems[i].Name} [{i + 1} of {problems.Count}] ", ConsoleColor.Yellow);
                var compiled = StackelbergHelper.CompileToStackelberg(new PDDLDecl(domain, problems[i]), metaAction.Copy());
                var result = verifier.Verify(compiled.Domain, compiled.Problem, workingDir, timeLimitS);
                if (verifier.TimedOut)
                    ConsoleHelper.WriteLineColor($"\t\t\t\tMeta Action Verification timed out, trying next problem...", ConsoleColor.Yellow);
                else if (result == FrontierResult.Invalid)
                {
                    ConsoleHelper.WriteLineColor($"\t\t\t\tInvalid", ConsoleColor.Red);
                    return false;
                }
                else if (result == FrontierResult.Valid)
                {
                    ConsoleHelper.WriteLineColor($"\t\t\t\tValid", ConsoleColor.Green);
                    any = true;
                }
                else if (result == FrontierResult.Inapplicable)
                {
                    any = true;
                    ConsoleHelper.WriteLineColor($"\t\t\t\tInapplicable", ConsoleColor.Yellow);
                }
            }
            return any;
        }
    }
}
