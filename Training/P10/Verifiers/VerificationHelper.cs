using P10.Helpers;
using PDDLSharp.CodeGenerators.PDDL;
using PDDLSharp.ErrorListeners;
using PDDLSharp.Models.PDDL;
using PDDLSharp.Models.PDDL.Domain;
using PDDLSharp.Models.PDDL.Problem;
using Tools;

namespace P10.Verifiers
{
    public static class VerificationHelper
    {
        public static bool IsValid(DomainDecl domain, List<ProblemDecl> problems, ActionDecl metaAction, string workingDir, int timeLimitS)
        {
            var verifier = new FrontierVerifier();
            bool stop = false;
            var timer = new System.Timers.Timer();
            if (timeLimitS > -1)
                timer.Interval = timeLimitS * 1000;
            timer.AutoReset = false;
            timer.Elapsed += (s, e) =>
            {
                stop = true;
                verifier.Stop();
            };
            if (timeLimitS > -1)
                timer.Start();
            bool any = false;
            foreach (var problem in problems)
            {
                var compiled = StackelbergHelper.CompileToStackelberg(new PDDLDecl(domain, problem), metaAction.Copy());
                var isValid = verifier.Verify(compiled.Domain, compiled.Problem, workingDir, -1);
                if (stop)
                    break;
                if (verifier.TimedOut)
                {
                    ConsoleHelper.WriteLineColor($"\t\tMeta Action Verification timed out, assuming following problems are too hard...", ConsoleColor.Yellow);
                    break;
                }
                if (!isValid)
                {
                    ConsoleHelper.WriteLineColor($"\t\tMeta action invalid in problem {problem.Name}", ConsoleColor.Red);
                    any = false;
                    break;
                }
                else
                    any = true;
            }
            if (timeLimitS > -1)
                timer.Stop();
            return any;
        }

        private static readonly Dictionary<INode, string> _textCache = new Dictionary<INode, string>();
        private static string GetCached(INode node)
        {
            if (_textCache.ContainsKey(node))
                return _textCache[node];
            var codeGenerator = new PDDLCodeGenerator(new ErrorListener());
            _textCache.Add(node, codeGenerator.Generate(node));
            return _textCache[node];
        }
    }
}
