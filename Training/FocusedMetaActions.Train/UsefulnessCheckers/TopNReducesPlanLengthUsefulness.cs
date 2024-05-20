using FocusedMetaActions.Train.Helpers;
using PDDLSharp.Models.PDDL.Domain;
using PDDLSharp.Models.PDDL.Problem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FocusedMetaActions.Train.UsefulnessCheckers
{
    public class TopNReducesPlanLengthUsefulness : ReducesPlanLengthUsefulness
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

            return usefulCandidates.OrderBy(x => x.PlanLength).Take(N).Select(x => x.Candidate).ToList();
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
