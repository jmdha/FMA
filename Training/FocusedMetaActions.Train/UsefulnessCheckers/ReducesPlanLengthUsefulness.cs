using FocusedMetaActions.Train.Helpers;
using MetaActionGenerators.CandidateGenerators.CPDDLMutexMetaAction;
using PDDLSharp.CodeGenerators.PDDL;
using PDDLSharp.ErrorListeners;
using PDDLSharp.Models.PDDL.Domain;
using PDDLSharp.Models.PDDL.Problem;
using PDDLSharp.Parsers.FastDownward.Plans;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FocusedMetaActions.Train.UsefulnessCheckers
{
    public class ReducesPlanLengthUsefulness : UsedInPlansUsefulness
    {
        public ReducesPlanLengthUsefulness(string workingDir, int timeLimitS) : base(workingDir, timeLimitS)
        {
        }

        public override List<ActionDecl> GetUsefulCandidates(DomainDecl domain, List<ProblemDecl> problems, List<ActionDecl> candidates)
        {
            if (candidates.Count == 0)
                return new List<ActionDecl>();
            var usefulCandidates = new List<ActionDecl>();
            var basePlanLengths = GetPlanLengths(domain, problems);
            
            var count = 1;
            foreach (var candidate in candidates)
            {
                ConsoleHelper.WriteLineColor($"\tChecking candidate {count++} out of {candidates.Count}", ConsoleColor.Magenta);
                if (IsMetaPlanShorter(domain, problems, candidate, basePlanLengths) != -1)
                    usefulCandidates.Add(candidate);
            }

            return usefulCandidates;
        }

        internal List<int> GetPlanLengths(DomainDecl domain, List<ProblemDecl> problems, ActionDecl metaAction)
        {
            var newDomain = domain.Copy();
            newDomain.Actions.Add(metaAction);
            return GetPlanLengths(newDomain, problems);
        }

        internal List<int> GetPlanLengths(DomainDecl domain, List<ProblemDecl> problems)
        {
            var result = new List<int>();
            var errorListener = new ErrorListener();
            var codeGenerator = new PDDLCodeGenerator(errorListener);
            var planParser = new FDPlanParser(errorListener);

            var domainFile = new FileInfo(Path.Combine(WorkingDir, "usefulCheckDomain.pddl"));
            codeGenerator.Generate(domain, domainFile.FullName);

            int count = 1;
            foreach (var problem in problems)
            {
                ConsoleHelper.WriteLineColor($"\t\tGetting plan length for problem {count} out of {problems.Count}", ConsoleColor.Magenta);
                var problemFile = new FileInfo(Path.Combine(WorkingDir, "usefulCheckProblem.pddl"));
                codeGenerator.Generate(problem, problemFile.FullName);

                using (ArgsCaller fdCaller = new ArgsCaller("python3"))
                {
                    fdCaller.StdOut += (s, o) => { };
                    fdCaller.StdErr += (s, o) => { };
                    fdCaller.Arguments.Add(ExternalPaths.FastDownwardPath, "");
                    fdCaller.Arguments.Add("--alias", "lama-first");
                    fdCaller.Arguments.Add("--overall-time-limit", $"{TimeLimitS}s");
                    fdCaller.Arguments.Add("--plan-file", "plan.plan");
                    fdCaller.Arguments.Add(domainFile.FullName, "");
                    fdCaller.Arguments.Add(problemFile.FullName, "");
                    fdCaller.Process.StartInfo.WorkingDirectory = WorkingDir;
                    if (fdCaller.Run() == 0)
                    {
                        if (!File.Exists(Path.Combine(WorkingDir, "plan.plan")))
                            ConsoleHelper.WriteLineColor($"\t\tPlanner timed out! Consider using easier usefulness problems...", ConsoleColor.Yellow);
                        var plan = planParser.Parse(new FileInfo(Path.Combine(WorkingDir, "plan.plan")));
                        result.Add(plan.Plan.Count);
                    }
                    else if (!File.Exists(Path.Combine(WorkingDir, "plan.plan")))
                        ConsoleHelper.WriteLineColor($"\t\tPlanner timed out! Consider using easier usefulness problems...", ConsoleColor.Yellow);
                }
                count++;
            }

            return result;
        }

        private int IsMetaPlanShorter(DomainDecl domain, List<ProblemDecl> problems, ActionDecl candidate, List<int> basePlanLengths)
        {
            var errorListener = new ErrorListener();
            var codeGenerator = new PDDLCodeGenerator(errorListener);
            var planParser = new FDPlanParser(errorListener);

            var testDomain = domain.Copy();
            testDomain.Actions.Add(candidate);

            var domainFile = new FileInfo(Path.Combine(WorkingDir, "usefulCheckDomain.pddl"));
            codeGenerator.Generate(testDomain, domainFile.FullName);

            int count = 1;
            foreach (var problem in problems)
            {
                ConsoleHelper.WriteLineColor($"\t\tGetting meta plan lengths for problem {count} out of {problems.Count}", ConsoleColor.Magenta);
                var problemFile = new FileInfo(Path.Combine(WorkingDir, "usefulCheckProblem.pddl"));
                codeGenerator.Generate(problem, problemFile.FullName);

                using (ArgsCaller fdCaller = new ArgsCaller("python3"))
                {
                    fdCaller.StdOut += (s, o) => { };
                    fdCaller.StdErr += (s, o) => { };
                    fdCaller.Arguments.Add(ExternalPaths.FastDownwardPath, "");
                    fdCaller.Arguments.Add("--alias", "lama-first");
                    fdCaller.Arguments.Add("--overall-time-limit", $"{TimeLimitS}s");
                    fdCaller.Arguments.Add("--plan-file", "plan.plan");
                    fdCaller.Arguments.Add(domainFile.FullName, "");
                    fdCaller.Arguments.Add(problemFile.FullName, "");
                    fdCaller.Process.StartInfo.WorkingDirectory = WorkingDir;
                    if (fdCaller.Run() == 0)
                    {
                        if (!File.Exists(Path.Combine(WorkingDir, "plan.plan")))
                            ConsoleHelper.WriteLineColor($"\t\tPlanner timed out! Consider using easier usefulness problems...", ConsoleColor.Yellow);
                        var plan = planParser.Parse(new FileInfo(Path.Combine(WorkingDir, "plan.plan")));
                        if (plan.Plan.Any(y => y.ActionName == candidate.Name) && plan.Plan.Count < basePlanLengths[count - 1])
                        {
                            ConsoleHelper.WriteLineColor($"\t\tMeta plan shorter in in problem {count}!", ConsoleColor.Green);
                            return count - 1;
                        }
                    }
                    else if (!File.Exists(Path.Combine(WorkingDir, "plan.plan")))
                        ConsoleHelper.WriteLineColor($"\t\tPlanner timed out! Consider using easier usefulness problems...", ConsoleColor.Yellow);
                }
                count++;
            }

            ConsoleHelper.WriteLineColor($"\t\tMeta action does not appear useful...", ConsoleColor.Red);
            return -1;
        }
    }
}
