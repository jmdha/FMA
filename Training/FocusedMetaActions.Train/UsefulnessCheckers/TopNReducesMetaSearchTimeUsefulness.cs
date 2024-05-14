using FocusedMetaActions.Train.Helpers;
using PDDLSharp.Models.PDDL.Domain;
using PDDLSharp.Models.PDDL.Problem;
using System.Text.RegularExpressions;

namespace FocusedMetaActions.Train.UsefulnessCheckers
{
    public class TopNReducesMetaSearchTimeUsefulness : ReducesMetaSearchTimeUsefulness
    {
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
            var usefulCandidates = new List<CandidateAndSearch>();
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
                        usefulCandidates.Add(new CandidateAndSearch(candidate, metaSearchTimes.Sum()));
                }
            }
            return usefulCandidates.OrderBy(x => x.SearchTime).Take(N).Select(x => x.Candidate).ToList();
        }

        private class CandidateAndSearch
        {
            public ActionDecl Candidate { get; set; }
            public double SearchTime { get; set; }

            public CandidateAndSearch(ActionDecl candidate, double searchTime)
            {
                Candidate = candidate;
                SearchTime = searchTime;
            }
        }
    }
}
