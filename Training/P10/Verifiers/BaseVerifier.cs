using PDDLSharp.Models.PDDL.Domain;
using PDDLSharp.Models.PDDL.Problem;
using System.Diagnostics;
using System.Text;
using Tools;

namespace P10.Verifiers
{
    public abstract class BaseVerifier : IVerifier
    {
        public static readonly string StackelbergPath = PathHelper.RootPath("../Dependencies/stackelberg-planner/src/fast-downward.py");
        public string SearchString { get; set; } = "--search \"sym_stackelberg(optimal_engine=symbolic(plan_reuse_minimal_task_upper_bound=false, plan_reuse_upper_bound=true), upper_bound_pruning=false)\"";

        internal int ExecutePlanner(string domainPath, string problemPath, string outputPath)
        {
            StringBuilder sb = new StringBuilder("");
            sb.Append($"{StackelbergPath} ");
            sb.Append($"\"{domainPath}\" ");
            sb.Append($"\"{problemPath}\" ");
            sb.Append($"{SearchString} ");

            var process = new Process
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

            process.Start();
            process.WaitForExit();
            return process.ExitCode;
        }

        public abstract bool Verify(DomainDecl domain, ProblemDecl problem, string workingDir);
    }
}
