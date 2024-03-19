using PDDLSharp.Models.PDDL.Domain;
using PDDLSharp.Models.PDDL.Problem;
using System.Diagnostics;
using System.Text;
using Tools;

namespace P10.Verifiers
{
    public abstract class BaseVerifier : IVerifier
    {
        public string SearchString { get; set; } = "--search \"sym_stackelberg(optimal_engine=symbolic(plan_reuse_minimal_task_upper_bound=false, plan_reuse_upper_bound=true), upper_bound_pruning=false)\"";
        private Process? _currentProcess;
        internal string _log = "";

        internal int ExecutePlanner(string domainPath, string problemPath, string outputPath, int timeLimitS)
        {
            var task = new Task<int>(() => RunPlanner(domainPath, problemPath, outputPath));
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
            {
                task.Wait();
            }

            return task.Result;
        }

        private int RunPlanner(string domainPath, string problemPath, string outputPath)
        {
            _log = "";
            StringBuilder sb = new StringBuilder("");
            sb.Append($"{ExternalPaths.StackelbergPath} ");
            sb.Append($"\"{domainPath}\" ");
            sb.Append($"\"{problemPath}\" ");
            sb.Append($"{SearchString} ");

            _currentProcess = new Process
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
            _currentProcess.OutputDataReceived += (s, e) => { 
                _log += e.Data;
#if DEBUG
                Console.WriteLine(e.Data);
#endif
            };
            _currentProcess.ErrorDataReceived += (s, e) => {
                _log += e.Data;
#if DEBUG
                Console.WriteLine($"ERROR: {e.Data}");
#endif
            };

            _currentProcess.Start();
            _currentProcess.BeginOutputReadLine();
            _currentProcess.BeginErrorReadLine();
            _currentProcess.WaitForExit();
            return _currentProcess.ExitCode;
        }

        public abstract bool Verify(DomainDecl domain, ProblemDecl problem, string workingDir, int timeLimitS);
    }
}
