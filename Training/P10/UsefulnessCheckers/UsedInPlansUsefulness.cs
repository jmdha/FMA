using PDDLSharp.CodeGenerators.PDDL;
using PDDLSharp.ErrorListeners;
using PDDLSharp.Models.PDDL.Domain;
using PDDLSharp.Models.PDDL.Problem;
using PDDLSharp.Parsers.FastDownward.Plans;
using Tools;

namespace P10.UsefulnessCheckers
{
    public class UsedInPlansUsefulness : IUsefulnessChecker
    {
        public string WorkingDir { get; } = "usefulness";
        public int TimeLimitS { get; }

        public UsedInPlansUsefulness(string workingDir, int timeLimitS)
        {
            WorkingDir = Path.Combine(workingDir, WorkingDir);
            PathHelper.RecratePath(WorkingDir);
            TimeLimitS = timeLimitS;
        }

        public virtual List<ActionDecl> GetUsefulCandidates(DomainDecl domain, List<ProblemDecl> problems, List<ActionDecl> candidates)
        {
            if (candidates.Count == 0)
                return new List<ActionDecl>();
            var usefulCandidates = new List<ActionDecl>();

            var count = 1;
            foreach (var candidate in candidates)
            {
                ConsoleHelper.WriteLineColor($"\tChecking candidate {count++} out of {candidates.Count}", ConsoleColor.Magenta);
                if (IsMetaActionUseful(domain, problems, candidate) != -1)
                    usefulCandidates.Add(candidate);
            }

            return usefulCandidates;
        }

        internal int IsMetaActionUseful(DomainDecl domain, List<ProblemDecl> problems, ActionDecl candidate)
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
                ConsoleHelper.WriteLineColor($"\t\tChecking if meta action occurs in plan for problem {count} out of {problems.Count}", ConsoleColor.Magenta);
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
                        if (plan.Plan.Any(y => y.ActionName == candidate.Name))
                        {
                            ConsoleHelper.WriteLineColor($"\t\tMeta action was used in problem {count}!", ConsoleColor.Green);
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
