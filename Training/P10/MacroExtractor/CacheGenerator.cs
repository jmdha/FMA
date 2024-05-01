using MetaActionGenerators.Helpers;
using P10.Helpers;
using PDDLSharp.CodeGenerators.PDDL;
using PDDLSharp.ErrorListeners;
using PDDLSharp.Models.PDDL;
using PDDLSharp.Models.PDDL.Domain;
using PDDLSharp.Models.PDDL.Problem;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tools;

namespace P10.MacroExtractor
{
    public class CacheGenerator
    {
        private static string _replacementsPath = "replacements";
        private static string _cacheFolder = "cache";
        private static string _searchString = "--search \"sym_stackelberg(optimal_engine=symbolic(plan_reuse_minimal_task_upper_bound=false, plan_reuse_upper_bound=true), upper_bound_pruning=false)\"";
        public void GenerateCache(DomainDecl domain, List<ProblemDecl> problems, ActionDecl metaAction, string tempFolder, string outFolder)
        {
            var tmpFolder = Path.Combine(tempFolder, _cacheFolder);
            Tools.PathHelper.RecratePath(tmpFolder);

            var listener = new ErrorListener();
            var codeGenerator = new PDDLCodeGenerator(listener);
            int count = 1;
            foreach (var problem in problems)
            {
                ConsoleHelper.WriteLineColor($"\t\tProblem {count++} out of {problems.Count}", ConsoleColor.Magenta);
                var decl = StackelbergHelper.CompileToStackelberg(new PDDLDecl(domain, problem), metaAction.Copy());
                codeGenerator.Generate(decl.Domain, Path.Combine(tmpFolder, "tempDomain.pddl"));
                codeGenerator.Generate(decl.Problem, Path.Combine(tmpFolder, "tempProblem.pddl"));
                RunPlanner(ExternalPaths.OldModifiedStackelbergPath,
                    Path.Combine(tmpFolder, "tempDomain.pddl"),
                    Path.Combine(tmpFolder, "tempProblem.pddl"),
                    tmpFolder);
            }

            if (!Directory.Exists(Path.Combine(tmpFolder, _replacementsPath)))
                throw new DirectoryNotFoundException("The replacement folder was not found! This could mean the stackelberg verification failed.");
            if (Directory.GetFiles(Path.Combine(tmpFolder, _replacementsPath)).Length == 0)
                throw new DirectoryNotFoundException("The replacement folder has no replacements! This could mean the stackelberg verification failed.");

            var extractor = new Extractor();
            extractor.ExtractMacros(domain, Directory.GetFiles(Path.Combine(tmpFolder, _replacementsPath)).ToList(), outFolder);
        }

        private void RunPlanner(string stackelbergPath, string domainPath, string problemPath, string tempPath)
        {
            StringBuilder sb = new StringBuilder("");
            sb.Append($"{stackelbergPath} ");
            sb.Append($"\"{domainPath}\" ");
            sb.Append($"\"{problemPath}\" ");
            sb.Append($"{_searchString} ");

            var currentProcess = new Process
            {
                StartInfo = new ProcessStartInfo()
                {
                    FileName = "python2",
                    Arguments = sb.ToString(),
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    WorkingDirectory = tempPath
                }
            };
            //currentProcess.OutputDataReceived += (s, e) =>
            //{
            //    Console.WriteLine(e.Data);
            //};
            //currentProcess.ErrorDataReceived += (s, e) =>
            //{
            //    Console.WriteLine($"ERROR: {e.Data}");
            //};

            currentProcess.Start();
            //currentProcess.BeginOutputReadLine();
            //currentProcess.BeginErrorReadLine();
            currentProcess.WaitForExit();

            if (Directory.Exists(Path.Combine(tempPath, _replacementsPath)))
            {
                int count = 0;
                int preCount = -1;
                while (count != preCount)
                {
                    preCount = count;
                    Thread.Sleep(1000);
                    count = Directory.GetFiles(Path.Combine(tempPath, _replacementsPath)).Count();
                    ConsoleHelper.WriteLineColor($"\t\t\tWaiting for planner to finish outputting files [was {preCount} is now {count}]...", ConsoleColor.Magenta);
                }
            }
        }
    }
}
