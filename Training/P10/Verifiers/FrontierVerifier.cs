using PDDLSharp.CodeGenerators.PDDL;
using PDDLSharp.ErrorListeners;
using PDDLSharp.Models.PDDL.Domain;
using PDDLSharp.Models.PDDL.Problem;

namespace P10.Verifiers
{
    public class FrontierVerifier : BaseVerifier
    {
        public enum FrontierResult { None, Valid, Invalid, Inapplicable }
        private FrontierResult IsFrontierValid(string file)
        {
            if (!File.Exists(file))
                return FrontierResult.Invalid;
            var text = File.ReadAllText(file);
            if (text.Replace(" ", "").Trim().ToLower() == "[{\"attackercost\":0,\"defendercost\":0,\"sequences\":[[]],\"attackerplan\":[]}]")
                return FrontierResult.Inapplicable;
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
            if (exitCode != 0)
                return FrontierResult.Invalid;
            return IsFrontierValid(Path.Combine(workingDir, "pareto_frontier.json"));
        }
    }
}
