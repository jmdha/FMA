using CommandLine;
using System.Diagnostics;
using System.Text;
using Tools;

namespace StackelbergVerifier
{
    public class StackelbergVerifier : BaseCLI
    {
        private static string _replacementsPath = "replacements";
        private static string _stackelbergPath = PathHelper.RootPath("../Dependencies/stackelberg-planner/src/fast-downward.py");
        private static int _returnCode = int.MaxValue;
        private static Process? _activeProcess;

        static int Main(string[] args)
        {
            Parser.Default.ParseArguments<Options>(args)
              .WithNotParsed(HandleParseError)
              .WithParsed(RunStacklebergVerifier);
            return _returnCode;
        }

        public static void RunStacklebergVerifier(Options opts)
        {
            opts.OutputPath = PathHelper.RootPath(opts.OutputPath);
            opts.DomainFilePath = PathHelper.RootPath(opts.DomainFilePath);
            opts.ProblemFilePath = PathHelper.RootPath(opts.ProblemFilePath);
            _replacementsPath = Path.Combine(opts.OutputPath, _replacementsPath);

            ConsoleHelper.WriteLineColor("Verifying paths...");
            if (!Directory.Exists(opts.OutputPath))
                Directory.CreateDirectory(opts.OutputPath);
            if (!File.Exists(opts.DomainFilePath))
                throw new FileNotFoundException($"Domain file not found: {opts.DomainFilePath}");
            if (!File.Exists(opts.ProblemFilePath))
                throw new FileNotFoundException($"Problem file not found: {opts.ProblemFilePath}");
            if (!File.Exists(_stackelbergPath))
                throw new FileNotFoundException($"Stackelberg planner file not found: {_stackelbergPath}");
            if (File.Exists(Path.Combine(opts.OutputPath, "pareto_frontier.json")))
                File.Delete(Path.Combine(opts.OutputPath, "pareto_frontier.json"));
            ConsoleHelper.WriteLineColor("Done!", ConsoleColor.Green);

            ConsoleHelper.WriteLineColor("Executing Stackelberg Planner");
            ConsoleHelper.WriteLineColor("(Note, this may take a while)");
            var isValid = Validate(opts.DomainFilePath, opts.ProblemFilePath, opts.OutputPath);
            if (isValid)
            {
                _returnCode = 0;
                ConsoleHelper.WriteLineColor("== Frontier is valid ==", ConsoleColor.Green);
            }
            else
            {
                _returnCode = 1;
                ConsoleHelper.WriteLineColor("== Frontier is not valid ==", ConsoleColor.Red);
            }
        }

        private static bool IsFrontierValid(string file)
        {
            if (!File.Exists(file))
                return false;
            var text = File.ReadAllText(file);
            var index = text.LastIndexOf("\"attacker cost\": ") + "\"attacker cost\": ".Length;
            var endIndex = text.IndexOf(",", index);
            var numberStr = text.Substring(index, endIndex - index);
            var number = int.Parse(numberStr);
            if (number != int.MaxValue)
                return true;
            return false;
        }

        private static int ExecutePlanner(string domainPath, string problemPath, string outputPath)
        {
            StringBuilder sb = new StringBuilder("");
            sb.Append($"{_stackelbergPath} ");
            sb.Append($"\"{domainPath}\" ");
            sb.Append($"\"{problemPath}\" ");
            sb.Append($"--search \"sym_stackelberg(optimal_engine=symbolic(plan_reuse_minimal_task_upper_bound=true, plan_reuse_upper_bound=true, force_bw_search_minimum_task_seconds=30, time_limit_seconds_minimum_task=300), upper_bound_pruning=true)\" ");

            _activeProcess = new Process
            {
                StartInfo = new ProcessStartInfo()
                {
                    FileName = "python2",
                    Arguments = sb.ToString(),
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    WorkingDirectory = outputPath
                }
            };

            _activeProcess.Start();
            _activeProcess.WaitForExit();
            return _activeProcess.ExitCode;
        }

        public static bool Validate(string domainPath, string problemPath, string outputPath)
        {
            var exitCode = ExecutePlanner(domainPath, problemPath, outputPath);
            if (exitCode != 0)
                return false;
            else
            {
                if (Directory.Exists(_replacementsPath))
                {
                    int count = 0;
                    int preCount = -1;
                    while (count != preCount)
                    {
                        preCount = count;
                        Thread.Sleep(1000);
                        count = Directory.GetFiles(_replacementsPath).Count();
                        ConsoleHelper.WriteLineColor($"Waiting for planner to finish outputting files [was {preCount} is now {count}]...", ConsoleColor.Yellow);
                    }
                }
            }
            return IsFrontierValid(Path.Combine(outputPath, "pareto_frontier.json"));
        }
    }
}