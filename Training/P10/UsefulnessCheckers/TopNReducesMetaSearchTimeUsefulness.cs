using PDDLSharp.CodeGenerators.PDDL;
using PDDLSharp.ErrorListeners;
using PDDLSharp.Models.PDDL.Domain;
using PDDLSharp.Models.PDDL.Problem;
using PDDLSharp.Parsers.FastDownward.Plans;
using System.Text.RegularExpressions;
using Tools;

namespace P10.UsefulnessCheckers
{
    public class TopNReducesMetaSearchTimeUsefulness : UsedInPlansUsefulness
    {
        public static int Rounds { get; set; } = 5;
        public int N { get; set; }

        private readonly Regex _searchTime = new Regex("Search time: ([0-9.]*)", RegexOptions.Compiled);

        public TopNReducesMetaSearchTimeUsefulness(string workingDir, int timeLimitS, int n) : base(workingDir, timeLimitS)
        {
            N = n;
        }

        public override List<ActionDecl> GetUsefulCandidates(DomainDecl domain, List<ProblemDecl> problems, List<ActionDecl> candidates)
        {
            if (candidates.Count == 0)
                return new List<ActionDecl>();
            var normalSearchTime = GetDefaultSearchTime(domain, problems);
            ConsoleHelper.WriteLineColor($"\tAverage base search time: {normalSearchTime}s", ConsoleColor.Magenta);
            var metaSearchTimes = GetMetaSearchTimes(domain, problems, candidates);

            var toRemove = new List<ActionDecl>();
            foreach (var key in metaSearchTimes.Keys)
                if (metaSearchTimes[key] > normalSearchTime)
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

        private double GetDefaultSearchTime(DomainDecl domain, List<ProblemDecl> problems)
        {
            double returnValue = 0;
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

                var times = new List<double>();
                for (int i = 0; i < Rounds; i++)
                {
                    using (ArgsCaller fdCaller = new ArgsCaller("python3"))
                    {
                        var log = "";
                        fdCaller.StdOut += (s, o) =>
                        {
                            log += o.Data;
                        };
                        fdCaller.StdErr += (s, o) => { };
                        fdCaller.Arguments.Add(ExternalPaths.FastDownwardPath, "");
                        fdCaller.Arguments.Add("--alias", "lama-first");
                        fdCaller.Arguments.Add("--overall-time-limit", $"{TimeLimitS}s");
                        fdCaller.Arguments.Add("--plan-file", "plan.plan");
                        fdCaller.Arguments.Add(domainFile.FullName, "");
                        fdCaller.Arguments.Add(problemFile.FullName, "");
                        fdCaller.Process.StartInfo.WorkingDirectory = WorkingDir;
                        fdCaller.Run();
                        var matches = _searchTime.Match(log);
                        if (matches == null)
                            throw new Exception("No search time for problem???");
                        if (matches.Groups[1].Value == "")
                            times.Add(TimeLimitS);
                        else
                            times.Add(double.Parse(matches.Groups[1].Value));
                    }
                }
                returnValue += times.Average();
                count++;
            }

            return returnValue;
        }

        private Dictionary<ActionDecl, double> GetMetaSearchTimes(DomainDecl domain, List<ProblemDecl> problems, List<ActionDecl> candidates)
        {
            var returnList = new Dictionary<ActionDecl, double>();
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
                double searchTime = 0;
                ConsoleHelper.WriteLineColor($"\t\tGetting meta search time for candidate {candidate.Name}", ConsoleColor.Magenta);
                foreach (var problem in problems)
                {
                    ConsoleHelper.WriteLineColor($"\t\tGetting meta search time in problem {count} out of {problems.Count}", ConsoleColor.Magenta);
                    var problemFile = new FileInfo(Path.Combine(WorkingDir, "usefulCheckProblem.pddl"));
                    codeGenerator.Generate(problem, problemFile.FullName);

                    var times = new List<double>();
                    for (int i = 0; i < Rounds; i++)
                    {
                        using (ArgsCaller fdCaller = new ArgsCaller("python3"))
                        {
                            var log = "";
                            fdCaller.StdOut += (s, o) =>
                            {
                                log += o.Data;
                            };
                            fdCaller.StdErr += (s, o) => { };
                            fdCaller.Arguments.Add(ExternalPaths.FastDownwardPath, "");
                            fdCaller.Arguments.Add("--alias", "lama-first");
                            fdCaller.Arguments.Add("--overall-time-limit", $"{TimeLimitS}s");
                            fdCaller.Arguments.Add("--plan-file", "plan.plan");
                            fdCaller.Arguments.Add(domainFile.FullName, "");
                            fdCaller.Arguments.Add(problemFile.FullName, "");
                            fdCaller.Process.StartInfo.WorkingDirectory = WorkingDir;
                            fdCaller.Run();
                            var matches = _searchTime.Match(log);
                            if (matches == null)
                                throw new Exception("No search time for problem???");
                            if (matches.Groups[1].Value == "")
                                times.Add(TimeLimitS);
                            else
                                times.Add(double.Parse(matches.Groups[1].Value));
                        }
                    }
                    searchTime += times.Average();
                    count++;
                }
                returnList.Add(candidate, searchTime);
            }

            foreach(var key in returnList.Keys)
                ConsoleHelper.WriteLineColor($"\t'{key.Name}' average search: {returnList[key]}s", ConsoleColor.Magenta);

            return returnList;
        }
    }
}
