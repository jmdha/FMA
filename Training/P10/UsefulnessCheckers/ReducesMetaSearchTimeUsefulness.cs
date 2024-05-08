using PDDLSharp.CodeGenerators.PDDL;
using PDDLSharp.ErrorListeners;
using PDDLSharp.Models.PDDL.Domain;
using PDDLSharp.Models.PDDL.Problem;
using PDDLSharp.Parsers.FastDownward.Plans;
using System.Text.RegularExpressions;
using Tools;

namespace P10.UsefulnessCheckers
{
    public class ReducesMetaSearchTimeUsefulness : UsedInPlansUsefulness
    {
        public static int Rounds { get; set; } = 1;
        private readonly Regex _searchTime = new Regex("Search time: ([0-9.]*)", RegexOptions.Compiled);

        public ReducesMetaSearchTimeUsefulness(string workingDir, int timeLimitS) : base(workingDir, timeLimitS)
        {
        }

        public override List<ActionDecl> GetUsefulCandidates(DomainDecl domain, List<ProblemDecl> problems, List<ActionDecl> candidates)
        {
            if (candidates.Count == 0)
                return new List<ActionDecl>();
            var usefulCandidates = new List<ActionDecl>();
            ConsoleHelper.WriteLineColor($"\tGetting base search times...", ConsoleColor.Magenta);
            var searchTimes = GetSearchTimes(domain, problems);
            if (searchTimes.Average() <= 0.1)
                ConsoleHelper.WriteLineColor($"\tBase search time for usefulness problems is way too low! Consider using more difficult ones...", ConsoleColor.Yellow);

            var count = 1;
            foreach (var candidate in candidates)
            {
                ConsoleHelper.WriteLineColor($"\tChecking candidate {count++} out of {candidates.Count}", ConsoleColor.Magenta);
                var usedIn = IsMetaActionUseful(domain, problems, candidate);
                if (usedIn != -1)
                {
                    ConsoleHelper.WriteLineColor($"\t\tGetting meta search times...", ConsoleColor.Magenta);
                    var metaSearchTimes = GetSearchTimes(domain, problems.Skip(usedIn).ToList(), candidate);
                    var metaAvg = metaSearchTimes.Average();
                    var searchAvg = searchTimes.GetRange(usedIn, searchTimes.Count - usedIn).Average();
                    ConsoleHelper.WriteLineColor($"\t\t\tCandidate avg search time was {metaAvg}s vs. {searchAvg}s base", ConsoleColor.Magenta);
                    if (metaAvg < searchAvg)
                        usefulCandidates.Add(candidate);
                }
            }

            return usefulCandidates;
        }

        internal List<double> GetSearchTimes(DomainDecl domain, List<ProblemDecl> problems, ActionDecl metaAction)
        {
            var newDomain = domain.Copy();
            newDomain.Actions.Add(metaAction);
            return GetSearchTimes(newDomain, problems);
        }

        internal List<double> GetSearchTimes(DomainDecl domain, List<ProblemDecl> problems)
        {
            var returnList = new List<double>();
            var errorListener = new ErrorListener();
            var codeGenerator = new PDDLCodeGenerator(errorListener);
            var planParser = new FDPlanParser(errorListener);

            var testDomain = domain.Copy();
            var domainFile = new FileInfo(Path.Combine(WorkingDir, "usefulCheckDomain.pddl"));
            codeGenerator.Generate(testDomain, domainFile.FullName);

            int count = 1;
            foreach (var problem in problems)
            {
                ConsoleHelper.WriteLineColor($"\t\t\tGetting search time in problem {count} out of {problems.Count}", ConsoleColor.Magenta);
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
                returnList.Add(times.Average());
                count++;
            }

            return returnList;
        }
    }
}
