using PDDLSharp.CodeGenerators.PDDL;
using PDDLSharp.ErrorListeners;
using PDDLSharp.Models.PDDL.Domain;
using PDDLSharp.Models.PDDL.Problem;
using PDDLSharp.Parsers.FastDownward.Plans;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tools;

namespace P10.UsefulnessCheckers
{
    public class ReducesMetaSearchTimeUsefulness : UsedInPlansUsefulness
    {
        public ReducesMetaSearchTimeUsefulness(string workingDir) : base(workingDir)
        {
        }

        public override List<ActionDecl> GetUsefulCandidates(DomainDecl domain, List<ProblemDecl> problems, List<ActionDecl> candidates)
        {
            var usefulCandidates = new List<ActionDecl>();
            var searchTimes = GetDefaultSearchTimes(domain, problems);

            var count = 1;
            foreach (var candidate in candidates)
            {
                ConsoleHelper.WriteLineColor($"\tChecking candidate {count++} out of {candidates.Count}", ConsoleColor.Magenta);
                if (IsMetaActionUseful(domain, problems, candidate) &&
                    DoesMetaActionReduceSearchTime(domain, problems, candidate, searchTimes))
                {
                    usefulCandidates.Add(candidate);
                }
            }

            return usefulCandidates;
        }

        private List<long> GetDefaultSearchTimes(DomainDecl domain, List<ProblemDecl> problems)
        {
            var returnList = new List<long>();
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
                    var watch = new Stopwatch();
                    watch.Start();
                    if (fdCaller.Run() == 0)
                    {
                        watch.Stop();
                        returnList.Add(watch.ElapsedMilliseconds);
                    }
                }
                count++;
            }

            return returnList;
        }

        private bool DoesMetaActionReduceSearchTime(DomainDecl domain, List<ProblemDecl> problems, ActionDecl candidate, List<long> searchTimes)
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
                ConsoleHelper.WriteLineColor($"\t\tChecking if meta action reduces search time in problem {count} out of {problems.Count}", ConsoleColor.Magenta);
                var problemFile = new FileInfo(Path.Combine(WorkingDir, "usefulCheckProblem.pddl"));
                codeGenerator.Generate(problem, problemFile.FullName);

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
                    var watch = new Stopwatch();
                    watch.Start();
                    if (fdCaller.Run() == 0)
                    {
                        watch.Stop();
                        if (watch.ElapsedMilliseconds < searchTimes[count - 1])
                        {
                            ConsoleHelper.WriteLineColor($"\t\tMeta action reduced search time in problem {count}!", ConsoleColor.Green);
                            return true;
                        }
                    }
                }
                count++;
            }

            ConsoleHelper.WriteLineColor($"\t\tMeta action does not appear useful...", ConsoleColor.Red);
            return false;
        }
    }
}
