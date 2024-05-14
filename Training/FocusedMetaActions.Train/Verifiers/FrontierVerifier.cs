using PDDLSharp.CodeGenerators.PDDL;
using PDDLSharp.ErrorListeners;
using PDDLSharp.Models.PDDL.Domain;
using PDDLSharp.Models.PDDL.Problem;

namespace FocusedMetaActions.Train.Verifiers
{
    public class FrontierVerifier : BaseVerifier
    {
        public enum FrontierResult { None, Valid, Invalid, Inapplicable }

        /// <summary>
        /// If the frontier file does not exist, assume the planner failed and the meta action is invalid.
        /// If the frontier exists, but it has an attacker cost of infinite (<seealso cref="int.MaxValue"/>) it is invalid.
        /// Otherwise, the meta action is valid.
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        private FrontierResult IsFrontierValid(string file)
        {
            if (!File.Exists(file))
                return FrontierResult.Invalid;
            var text = File.ReadAllText(file);
            var index = text.LastIndexOf("\"attacker cost\": ") + "\"attacker cost\": ".Length;
            var endIndex = text.IndexOf(",", index);
            var numberStr = text.Substring(index, endIndex - index);
            var number = int.Parse(numberStr);
            if (number != int.MaxValue)
                return FrontierResult.Valid;
            return FrontierResult.Invalid;
        }

        public FrontierResult Verify(DomainDecl domain, ProblemDecl problem, string workingDir, int timeLimitS)
        {
            _domain = domain;
            _problem = problem;
            if (File.Exists(Path.Combine(workingDir, "pareto_frontier.json")))
                File.Delete(Path.Combine(workingDir, "pareto_frontier.json"));
            var listener = new ErrorListener();
            var codeGenerator = new PDDLCodeGenerator(listener);
            var domainFile = Path.Combine(workingDir, $"tempDomain.pddl");
            var problemFile = Path.Combine(workingDir, $"tempProblem.pddl");
            codeGenerator.Generate(domain, domainFile);
            codeGenerator.Generate(problem, problemFile);
            var exitCode = ExecutePlanner(ExternalPaths.StackelbergPath, domainFile, problemFile, workingDir, timeLimitS);
            // If the planner returned some other error code than zero, assume the meta action is invalid.
            if (exitCode != 0)
                return FrontierResult.Invalid;
            // This small warning from the Stackelberg Planner basically means the translator saw all the fix actions as unreachable, i.e. the meta action is completely inapplicable
            if (_log.Contains("Warning: running stackelberg search on a task without fix actions"))
                return FrontierResult.Inapplicable;
            return IsFrontierValid(Path.Combine(workingDir, "pareto_frontier.json"));
        }
    }
}
