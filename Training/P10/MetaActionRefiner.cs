using PDDLSharp.CodeGenerators.PDDL;
using PDDLSharp.ErrorListeners;
using PDDLSharp.Models.PDDL;
using PDDLSharp.Models.PDDL.Domain;
using PDDLSharp.Models.PDDL.Problem;

namespace P10
{
    public class MetaActionRefiner
    {
        private string _tempFolder = "temp/refiner";
        private string _tempStatePath = "temp/refiner/states";
        public ActionDecl OriginalMetaActionCandidate { get; internal set; }
        public ActionDecl RefinedMetaActionCandidate { get; internal set; }
        public IRefinementStrategy Strategy { get; }

        public MetaActionRefiner(ActionDecl metaActionCandidate, IRefinementStrategy strategy)
        {
            OriginalMetaActionCandidate = metaActionCandidate.Copy();
            RefinedMetaActionCandidate = metaActionCandidate.Copy();
            Strategy = strategy;
        }

        public void Refine(DomainDecl domain, List<ProblemDecl> problems)
        {
            while (!IsValid(domain, problems))
                RefinedMetaActionCandidate = Strategy.Refine();
        }

        private bool IsValid(DomainDecl domain, List<ProblemDecl> problems)
        {
            var listener = new ErrorListener();
            var codeGenerator = new PDDLCodeGenerator(listener);
            bool isValid = true;
            foreach (var problem in problems)
            {
                var compiled = StackelbergCompiler.StackelbergCompiler.CompileToStackelberg(new PDDLDecl(domain, problem), RefinedMetaActionCandidate);
                codeGenerator.Generate(compiled.Domain, Path.Combine(_tempFolder, "tempDomain.pddl"));
                codeGenerator.Generate(compiled.Problem, Path.Combine(_tempFolder, "tempProblem.pddl"));
                if (StackelbergVerifier.StackelbergVerifier.Validate(
                    Path.Combine(_tempFolder, "tempDomain.pddl"),
                    Path.Combine(_tempFolder, "tempProblem.pddl"),
                    _tempStatePath))
                    isValid = false;
            }
            return isValid;
        }
    }
}
