using FocusedMetaActions.Train.Helpers;
using PDDLSharp.CodeGenerators.PDDL;
using PDDLSharp.ErrorListeners;
using PDDLSharp.Models.PDDL;
using PDDLSharp.Models.PDDL.Domain;
using PDDLSharp.Models.PDDL.Problem;
using System.Diagnostics;
using System.Text;

namespace FocusedMetaActions.Train.MacroExtractor
{
    public class CacheGenerator
    {
        public static bool ShowSTDOut { get; set; } = false;
        public static int WaitDelay { get; set; } = 1000;

        private static readonly string _replacementsPath = "replacements";
        private static readonly string _cacheFolder = "cache";
        private static readonly string _searchString = "--search \"sym_stackelberg(optimal_engine=symbolic(plan_reuse_minimal_task_upper_bound=false, plan_reuse_upper_bound=true), upper_bound_pruning=false)\"";

        private Process? _currentProcess;
        internal string _log = "";

        public void GenerateCache(DomainDecl domain, List<ProblemDecl> problems, ActionDecl metaAction, string tempFolder, string outFolder, int timeLimitS)
        {
            var tmpFolder = Path.Combine(tempFolder, _cacheFolder);
            PathHelper.RecratePath(tmpFolder);

            var listener = new ErrorListener();
            var codeGenerator = new PDDLCodeGenerator(listener);
            int count = 1;
            foreach (var problem in problems)
            {
                ConsoleHelper.WriteLineColor($"\t\tProblem {count++} out of {problems.Count}", ConsoleColor.Magenta);
                var decl = StackelbergHelper.CompileToStackelberg(new PDDLDecl(domain, problem), metaAction.Copy());
                codeGenerator.Generate(decl.Domain, Path.Combine(tmpFolder, "tempDomain.pddl"));
                codeGenerator.Generate(decl.Problem, Path.Combine(tmpFolder, "tempProblem.pddl"));
                ExecutePlanner(ExternalPaths.StackelbergPath,
                    Path.Combine(tmpFolder, "tempDomain.pddl"),
                    Path.Combine(tmpFolder, "tempProblem.pddl"),
                    tmpFolder,
                    timeLimitS);
            }

            if (!Directory.Exists(Path.Combine(tmpFolder, _replacementsPath)))
            {
                ConsoleHelper.WriteLineColor("Replacement folder was not found or is empty! This most likely means an error with the Training process! Check logs and make sure everything works correct!", ConsoleColor.Red);
                return;
            }
            if (Directory.GetFiles(Path.Combine(tmpFolder, _replacementsPath)).Length == 0)
            {
                ConsoleHelper.WriteLineColor("Replacement folder was not found or is empty! This most likely means an error with the Training process! Check logs and make sure everything works correct!", ConsoleColor.Red);
                return;
            }

            var extractor = new Extractor();
            extractor.ExtractMacros(domain, Directory.GetFiles(Path.Combine(tmpFolder, _replacementsPath)).ToList(), outFolder);
        }

        private void ExecutePlanner(string stackelbergPath, string domainPath, string problemPath, string outputPath, int timeLimitS)
        {
            var task = new Task(() => RunPlanner(stackelbergPath, domainPath, problemPath, outputPath));
            task.Start();
            if (timeLimitS != -1)
            {
                var watch = new Stopwatch();
                watch.Start();
                while (!task.IsCompleted)
                {
                    Thread.Sleep(1000);
                    if (_currentProcess != null && watch.ElapsedMilliseconds / 1000 > timeLimitS)
                    {
                        ConsoleHelper.WriteLineColor("\tPlanner times out! Killing...", ConsoleColor.DarkYellow);
                        _currentProcess.Kill(true);
                    }
                }
            }
            else
                task.Wait();
        }

        private void RunPlanner(string stackelbergPath, string domainPath, string problemPath, string tempPath)
        {
            _log = "";
            StringBuilder sb = new StringBuilder("");
            sb.Append($"{stackelbergPath} ");
            sb.Append($"\"{domainPath}\" ");
            sb.Append($"\"{problemPath}\" ");
            sb.Append($"{_searchString} ");

            _currentProcess = new Process
            {
                StartInfo = new ProcessStartInfo()
                {
                    FileName = "python3",
                    Arguments = sb.ToString(),
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    WorkingDirectory = tempPath
                }
            };
            _currentProcess.OutputDataReceived += (s, e) =>
            {
                _log += $"{e.Data}{Environment.NewLine}";
                if (ShowSTDOut)
                    Console.WriteLine(e.Data);
            };
            _currentProcess.ErrorDataReceived += (s, e) =>
            {
                _log += $"ERROR: {e.Data}{Environment.NewLine}";
                if (ShowSTDOut)
                    Console.WriteLine($"ERROR: {e.Data}");
            };

            _currentProcess.Start();
            _currentProcess.BeginOutputReadLine();
            _currentProcess.BeginErrorReadLine();
            _currentProcess.WaitForExit();

            if (Directory.Exists(Path.Combine(tempPath, _replacementsPath)))
            {
                int count = 0;
                int preCount = -1;
                while (count != preCount)
                {
                    preCount = count;
                    Thread.Sleep(WaitDelay);
                    count = Directory.GetFiles(Path.Combine(tempPath, _replacementsPath)).Count();
                    ConsoleHelper.WriteLineColor($"\t\t\tWaiting for planner to finish outputting files [was {preCount} is now {count}]...", ConsoleColor.Magenta);
                }
            }
        }
    }
}
