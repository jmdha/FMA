using FocusedMetaActions.Train.Helpers;
using PDDLSharp.CodeGenerators.PDDL;
using PDDLSharp.ErrorListeners;
using PDDLSharp.Models.PDDL.Domain;
using PDDLSharp.Models.PDDL.Problem;
using PDDLSharp.Parsers.FastDownward.Plans;

namespace FocusedMetaActions.Train.UsefulnessCheckers
{
    public class TopNReducesPlanLengthUsefulness : UsedInPlansUsefulness
    {
        public int N { get; set; }
        public TopNReducesPlanLengthUsefulness(string workingDir, int timeLimitS, int n) : base(workingDir, timeLimitS)
        {
            N = n;
        }

        public override List<ActionDecl> GetUsefulCandidates(DomainDecl domain, List<ProblemDecl> problems, List<ActionDecl> candidates)
        {
            if (candidates.Count == 0)
                return new List<ActionDecl>();
            var usefulCandidates = new List<CandidateAndPlanLength>();
            var planLengths = GetPlanLengths(domain, problems);

            var count = 1;
            foreach (var candidate in candidates)
            {
                ConsoleHelper.WriteLineColor($"\tChecking candidate {count++} out of {candidates.Count}", ConsoleColor.Magenta);
                var usedIn = IsMetaActionUseful(domain, problems, candidate);
                if (usedIn != -1)
                {
                    ConsoleHelper.WriteLineColor($"\t\tGetting meta plan lengths...", ConsoleColor.Magenta);
                    var metaPlanLengths = GetPlanLengths(domain, problems.Skip(usedIn).ToList(), candidate);
                    var metaAvg = metaPlanLengths.Average();
                    var planAvg = planLengths.GetRange(usedIn, planLengths.Count - usedIn).Average();
                    ConsoleHelper.WriteLineColor($"\t\t\tCandidate avg plan length was {metaAvg} steps vs. {planAvg} steps base", ConsoleColor.Magenta);
                    if (metaAvg < planAvg)
                        usefulCandidates.Add(new CandidateAndPlanLength(candidate, metaPlanLengths.Sum()));
                }
            }

            if (N == -1)
                return usefulCandidates.Select(x => x.Candidate).ToList();
            return usefulCandidates.OrderBy(x => x.PlanLength).Take(N).Select(x => x.Candidate).ToList();
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

        private class CandidateAndPlanLength
        {
            public ActionDecl Candidate { get; set; }
            public int PlanLength { get; set; }

            public CandidateAndPlanLength(ActionDecl candidate, int planLength)
            {
                Candidate = candidate;
                PlanLength = planLength;
            }
        }
    }
}
