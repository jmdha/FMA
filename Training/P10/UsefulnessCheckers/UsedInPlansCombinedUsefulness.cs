using PDDLSharp.CodeGenerators.PDDL;
using PDDLSharp.ErrorListeners;
using PDDLSharp.Models.PDDL.Domain;
using PDDLSharp.Models.PDDL.Problem;
using PDDLSharp.Parsers.FastDownward.Plans;
using Tools;

namespace P10.UsefulnessCheckers
{
    public class UsedInPlansCombinedUsefulness : UsedInPlansUsefulness
    {
        public UsedInPlansCombinedUsefulness(string workingDir, int timeLimitS) : base(workingDir, timeLimitS)
        {
        }

        public override List<ActionDecl> GetUsefulCandidates(DomainDecl domain, List<ProblemDecl> problems, List<ActionDecl> candidates)
        {
            if (candidates.Count == 0)
                return new List<ActionDecl>();
            var usefulCandidates = new List<ActionDecl>();
            var errorListener = new ErrorListener();
            var codeGenerator = new PDDLCodeGenerator(errorListener);
            var planParser = new FDPlanParser(errorListener);

            var testDomain = domain.Copy();
            var domainFile = new FileInfo(Path.Combine(WorkingDir, "usefulCheckDomain.pddl"));
            testDomain.Actions.AddRange(candidates);
            codeGenerator.Generate(testDomain, domainFile.FullName);

            int count = 1;
            foreach (var problem in problems)
            {
                ConsoleHelper.WriteLineColor($"\tChecking problem {count++} out of {problems.Count}", ConsoleColor.Magenta);
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
                        var allUsed = plan.Plan.Where(x => candidates.Any(y => y.Name == x.ActionName));
                        foreach (var used in allUsed)
                        {
                            if (!usefulCandidates.Any(x => x.Name == used.ActionName))
                            {
                                var target = candidates.First(x => x.Name == used.ActionName);
                                if (target != null)
                                    usefulCandidates.Add(target);
                            }
                        }
                    }
                    else if (!File.Exists(Path.Combine(WorkingDir, "plan.plan")))
                        ConsoleHelper.WriteLineColor($"\t\tPlanner timed out! Consider using easier usefulness problems...", ConsoleColor.Yellow);
                }
            }

            return usefulCandidates;
        }
    }
}
