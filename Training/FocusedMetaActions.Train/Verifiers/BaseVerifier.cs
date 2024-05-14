using PDDLSharp.CodeGenerators.PDDL;
using PDDLSharp.ErrorListeners;
using PDDLSharp.Models.PDDL.Domain;
using PDDLSharp.Models.PDDL.Problem;
using System.Diagnostics;
using System.Text;
using P10.Helpers;

namespace P10.Verifiers
{
    public abstract class BaseVerifier
    {
        public bool TimedOut { get; internal set; }
        public static bool ShowSTDOut { get; set; } = false;
        public string SearchString { get; set; } = "--search \"sym_stackelberg(optimal_engine=symbolic(plan_reuse_minimal_task_upper_bound=false, plan_reuse_upper_bound=true), upper_bound_pruning=false)\"";
        private Process? _currentProcess;
        internal string _log = "";
        internal DomainDecl? _domain;
        internal ProblemDecl? _problem;

        public void Stop()
        {
            if (_currentProcess != null)
                _currentProcess.Kill(true);
        }

        internal int ExecutePlanner(string stackelbergPath, string domainPath, string problemPath, string outputPath, int timeLimitS)
        {
            TimedOut = false;
            var task = new Task<int>(() => RunPlanner(stackelbergPath, domainPath, problemPath, outputPath));
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
                        TimedOut = true;
                        ConsoleHelper.WriteLineColor("\tPlanner times out! Killing...", ConsoleColor.DarkYellow);
                        _currentProcess.Kill(true);
                    }
                }
            }
            else
            {
                task.Wait();
            }

            return task.Result;
        }

        private int RunPlanner(string stackelbergPath, string domainPath, string problemPath, string outputPath)
        {
            _log = "";
            StringBuilder sb = new StringBuilder("");
            sb.Append($"{stackelbergPath} ");
            sb.Append($"\"{domainPath}\" ");
            sb.Append($"\"{problemPath}\" ");
            sb.Append($"{SearchString} ");

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
                    WorkingDirectory = outputPath
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
            return _currentProcess.ExitCode;
        }

        public virtual string GetLog()
        {
            var sb = new StringBuilder();
            var codeGenerator = new PDDLCodeGenerator(new ErrorListener());

            sb.AppendLine(" == Log ==");
            sb.AppendLine(_log);
            sb.AppendLine();
            sb.AppendLine();
            if (_domain != null)
            {
                sb.AppendLine(" == Domain ==");
                sb.AppendLine(codeGenerator.Generate(_domain));
                sb.AppendLine();
                sb.AppendLine();
            }
            if (_problem != null)
            {
                sb.AppendLine(" == Problem ==");
                sb.AppendLine(codeGenerator.Generate(_problem));
            }

            return sb.ToString();
        }
    }
}
