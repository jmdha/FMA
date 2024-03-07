﻿using PDDLSharp.CodeGenerators.PDDL;
using PDDLSharp.ErrorListeners;
using PDDLSharp.Models.PDDL.Domain;
using PDDLSharp.Models.PDDL.Problem;
using PDDLSharp.Parsers.FastDownward.Plans;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tools;

namespace P10
{
    public class UsefullnessChecker
    {
        private static readonly string _tempFolder = PathHelper.RootPath("temp/usefullness");

        public UsefullnessChecker()
        {
            PathHelper.RecratePath(_tempFolder);
        }

        public List<ActionDecl> GetUsefulCandidates(DomainDecl domain, List<ProblemDecl> problems, List<ActionDecl> candidates)
        {
            var usefulCandidates = new List<ActionDecl>();

            var count = 0;
            foreach (var candidate in candidates)
            {
                ConsoleHelper.WriteLineColor($"\tChecking candidate {count++} out of {candidates.Count}", ConsoleColor.Magenta);
                if (IsMetaActionUseful(domain, problems, candidate))
                    usefulCandidates.Add(candidate);
            }

            return usefulCandidates;
        }

        internal bool IsMetaActionUseful(DomainDecl domain, List<ProblemDecl> problems, ActionDecl candidate)
        {
            var errorListener = new ErrorListener();
            var codeGenerator = new PDDLCodeGenerator(errorListener);
            var planParser = new FDPlanParser(errorListener);

            var testDomain = domain.Copy();
            testDomain.Actions.Add(candidate);

            var domainFile = new FileInfo(Path.Combine(_tempFolder, "usefulCheckDomain.pddl"));
            codeGenerator.Generate(testDomain, domainFile.FullName);

            foreach (var problem in problems)
            {
                var problemFile = new FileInfo(Path.Combine(_tempFolder, "usefulCheckProblem.pddl"));
                codeGenerator.Generate(problem, problemFile.FullName);

                using (ArgsCaller fdCaller = new ArgsCaller("python3"))
                {
                    fdCaller.StdOut += (s, o) => { };
                    fdCaller.StdErr += (s, o) => { };
                    fdCaller.Arguments.Add(PathHelper.RootPath("../Dependencies/fast-downward/fast-downward.py"), "");
                    fdCaller.Arguments.Add("--alias", "lama-first");
                    fdCaller.Arguments.Add("--overall-time-limit", "5m");
                    fdCaller.Arguments.Add("--plan-file", "plan.plan");
                    fdCaller.Arguments.Add(domainFile.FullName, "");
                    fdCaller.Arguments.Add(problemFile.FullName, "");
                    fdCaller.Process.StartInfo.WorkingDirectory = _tempFolder;
                    if (fdCaller.Run() == 0)
                    {
                        var plan = planParser.Parse(new FileInfo(Path.Combine(_tempFolder, "plan.plan")));
                        if (plan.Plan.Any(y => y.ActionName == candidate.Name))
                            return true;
                    }
                }
            }

            return false;
        }
    }
}