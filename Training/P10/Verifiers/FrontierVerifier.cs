using PDDLSharp.CodeGenerators.PDDL;
using PDDLSharp.ErrorListeners;
using PDDLSharp.Models.PDDL.Domain;
using PDDLSharp.Models.PDDL.Problem;

namespace P10.Verifiers
{
    public class FrontierVerifier : BaseVerifier
    {
        private bool IsFrontierValid(string file)
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

        public override bool Verify(DomainDecl domain, ProblemDecl problem, string workingDir, int timeLimitS)
        {
            if (File.Exists(Path.Combine(workingDir, "pareto_frontier.json")))
                File.Delete(Path.Combine(workingDir, "pareto_frontier.json"));
            var listener = new ErrorListener();
            var codeGenerator = new PDDLCodeGenerator(listener);
            var domainFile = Path.Combine(workingDir, $"tempDomain.pddl");
            var problemFile = Path.Combine(workingDir, $"tempProblem.pddl");
            codeGenerator.Generate(domain, domainFile);
            codeGenerator.Generate(problem, problemFile);
            var exitCode = ExecutePlanner(domainFile, problemFile, workingDir, timeLimitS);
            if (exitCode != 0)
                return false;
            return IsFrontierValid(Path.Combine(workingDir, "pareto_frontier.json"));
        }
    }
}
