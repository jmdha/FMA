using PDDLSharp.CodeGenerators.PDDL;
using PDDLSharp.ErrorListeners;
using PDDLSharp.Models.PDDL.Domain;
using PDDLSharp.Models.PDDL.Problem;
using PDDLSharp.Parsers.FastDownward.Plans;
using System.Diagnostics;
using Tools;

namespace P10.UsefulnessCheckers
{
    public class TopNReducesMetaSearchTimeUsefulness : UsedInPlansUsefulness
    {
        public static int Rounds { get; set; } = 5;
        public int N { get; set; }

        public TopNReducesMetaSearchTimeUsefulness(string workingDir, int n) : base(workingDir)
        {
            N = n;
        }

        public override List<ActionDecl> GetUsefulCandidates(DomainDecl domain, List<ProblemDecl> problems, List<ActionDecl> candidates)
        {
            var normalSearchTime = GetDefaultSearchTime(domain, problems);
            var metaSearchTimes = GetMetaSearchTimes(domain, problems, candidates);

            var toRemove = new List<ActionDecl>();
            foreach (var key in metaSearchTimes.Keys)
                if (metaSearchTimes[key] >= normalSearchTime)
                    toRemove.Add(key);
            foreach (var remove in toRemove)
                metaSearchTimes.Remove(remove);

            var ordered = metaSearchTimes.OrderBy(x => x.Value).ToList();

            var usefull = new List<ActionDecl>();
            for (int n = 0; n < N; n++)
            {
                if (ordered.Count == 0)
                    break;
                usefull.Add(ordered[0].Key);
                ordered.RemoveAt(0);
            }

            return usefull;
        }

        private long GetDefaultSearchTime(DomainDecl domain, List<ProblemDecl> problems)
        {
            long returnValue = 0;
            var errorListener = new ErrorListener();
            var codeGenerator = new PDDLCodeGenerator(errorListener);
            var planParser = new FDPlanParser(errorListener);

            var testDomain = domain.Copy();
            var domainFile = new FileInfo(Path.Combine(WorkingDir, "usefulCheckDomain.pddl"));
            codeGenerator.Generate(testDomain, domainFile.FullName);

            int count = 1;
            foreach (var problem in problems)
            {
                ConsoleHelper.WriteLineColor($"\t\tGetting base search time in problem {count} out of {problems.Count}", ConsoleColor.Magenta);
                var problemFile = new FileInfo(Path.Combine(WorkingDir, "usefulCheckProblem.pddl"));
                codeGenerator.Generate(problem, problemFile.FullName);

                var watch = new Stopwatch();
                watch.Start();
                for (int i = 0; i < Rounds; i++)
                {
                    using (ArgsCaller fdCaller = new ArgsCaller("python3"))
                    {
                        fdCaller.StdOut += (s, o) => { };
                        fdCaller.StdErr += (s, o) => { };
                        fdCaller.Arguments.Add(ExternalPaths.FastDownwardPath, "");
                        fdCaller.Arguments.Add("--alias", "lama-first");
                        fdCaller.Arguments.Add("--overall-time-limit", "5m");
                        fdCaller.Arguments.Add("--plan-file", "plan.plan");
                        fdCaller.Arguments.Add(domainFile.FullName, "");
                        fdCaller.Arguments.Add(problemFile.FullName, "");
                        fdCaller.Process.StartInfo.WorkingDirectory = WorkingDir;
                        fdCaller.Run();
                    }
                }
                watch.Stop();
                returnValue += watch.ElapsedMilliseconds;
                count++;
            }

            return returnValue;
        }

        private Dictionary<ActionDecl, long> GetMetaSearchTimes(DomainDecl domain, List<ProblemDecl> problems, List<ActionDecl> candidates)
        {
            var returnList = new Dictionary<ActionDecl, long>();
            foreach (var candidate in candidates)
            {
                var errorListener = new ErrorListener();
                var codeGenerator = new PDDLCodeGenerator(errorListener);
                var planParser = new FDPlanParser(errorListener);

                var testDomain = domain.Copy();
                testDomain.Actions.Add(candidate);
                var domainFile = new FileInfo(Path.Combine(WorkingDir, "usefulCheckDomain.pddl"));
                codeGenerator.Generate(testDomain, domainFile.FullName);

                int count = 1;
                long searchTime = 0;
                ConsoleHelper.WriteLineColor($"\t\tGetting meta search time for candidate {candidate.Name}", ConsoleColor.Magenta);
                foreach (var problem in problems)
                {
                    ConsoleHelper.WriteLineColor($"\t\tGetting meta search time in problem {count} out of {problems.Count}", ConsoleColor.Magenta);
                    var problemFile = new FileInfo(Path.Combine(WorkingDir, "usefulCheckProblem.pddl"));
                    codeGenerator.Generate(problem, problemFile.FullName);

                    var watch = new Stopwatch();
                    watch.Start();
                    for (int i = 0; i < Rounds; i++)
                    {
                        using (ArgsCaller fdCaller = new ArgsCaller("python3"))
                        {
                            fdCaller.StdOut += (s, o) => { };
                            fdCaller.StdErr += (s, o) => { };
                            fdCaller.Arguments.Add(ExternalPaths.FastDownwardPath, "");
                            fdCaller.Arguments.Add("--alias", "lama-first");
                            fdCaller.Arguments.Add("--overall-time-limit", "5m");
                            fdCaller.Arguments.Add("--plan-file", "plan.plan");
                            fdCaller.Arguments.Add(domainFile.FullName, "");
                            fdCaller.Arguments.Add(problemFile.FullName, "");
                            fdCaller.Process.StartInfo.WorkingDirectory = WorkingDir;
                            fdCaller.Run();
                        }
                    }
                    watch.Stop();
                    searchTime += watch.ElapsedMilliseconds;
                    count++;
                }
                returnList.Add(candidate, searchTime);
            }

            return returnList;
        }
    }
}
