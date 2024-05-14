using FocusedMetaActions.Train.Helpers;
using PDDLSharp.Models.PDDL;
using PDDLSharp.Models.PDDL.Domain;
using PDDLSharp.Models.PDDL.Problem;

namespace FocusedMetaActions.Train.Verifiers
{
    public static class VerificationHelper
    {
        /// <summary>
        /// Verification can be a bit iffy with the Stackalberg Planner.
        /// 3 Cases are possible, either the meta action is valid, invalid, or its completely inapplicable.
        /// * A meta action is only valid, if it is valid in at least one problem and not invalid in any (it can however be inapplicable in all other problems than one)
        /// * A meta action is only inapplicable if it is inapplicable in all the problems.
        /// * A meta action is invalid if it is invalid in any single problem
        /// </summary>
        /// <param name="domain"></param>
        /// <param name="problems"></param>
        /// <param name="metaAction"></param>
        /// <param name="workingDir"></param>
        /// <param name="timeLimitS"></param>
        /// <returns></returns>
        public static FrontierVerifier.FrontierResult IsValid(DomainDecl domain, List<ProblemDecl> problems, ActionDecl metaAction, string workingDir, int timeLimitS)
        {
            var verifier = new FrontierVerifier();
            bool any = false;
            var count = 1;
            foreach (var problem in problems)
            {
                ConsoleHelper.WriteLineColor($"\t\t\tValidating on problem {problem.Name} [{count++} of {problems.Count}] ", ConsoleColor.Yellow);
                var compiled = StackelbergHelper.CompileToStackelberg(new PDDLDecl(domain, problem), metaAction.Copy());
                var result = verifier.Verify(compiled.Domain, compiled.Problem, workingDir, timeLimitS);
                if (verifier.TimedOut)
                {
                    ConsoleHelper.WriteLineColor($"\t\t\tMeta Action Verification timed out, assuming following problems are too hard.", ConsoleColor.Yellow);
                    break;
                }
                else if (result == FrontierVerifier.FrontierResult.Invalid)
                {
                    ConsoleHelper.WriteLineColor($"\t\t\t\tInvalid", ConsoleColor.Red);
                    return FrontierVerifier.FrontierResult.Invalid;
                }
                else if (result == FrontierVerifier.FrontierResult.Valid)
                {
                    ConsoleHelper.WriteLineColor($"\t\t\t\tValid", ConsoleColor.Green);
                    any = true;
                }
                else if (result == FrontierVerifier.FrontierResult.Inapplicable)
                    ConsoleHelper.WriteLineColor($"\t\t\t\tInapplicable", ConsoleColor.Yellow);
            }
            if (any)
                return FrontierVerifier.FrontierResult.Valid;
            return FrontierVerifier.FrontierResult.Inapplicable;
        }
    }
}
