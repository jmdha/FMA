using PDDLSharp.CodeGenerators.PDDL;
using PDDLSharp.ErrorListeners;
using PDDLSharp.Models.PDDL;
using PDDLSharp.Models.PDDL.Domain;
using PDDLSharp.Models.PDDL.Problem;
using Tools;

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

            _tempFolder = PathHelper.RootPath(_tempFolder);
            _tempStatePath = PathHelper.RootPath(_tempStatePath);

            PathHelper.RecratePath(_tempFolder);
            PathHelper.RecratePath(_tempStatePath);
        }

        public bool Refine(PDDLDecl pddlDecl, DomainDecl domain, List<ProblemDecl> problems)
        {
            while (!IsValid(domain, problems))
            {
                ConsoleHelper.WriteLineColor($"\tRefining...", ConsoleColor.Magenta);
                var refined = Strategy.Refine(pddlDecl, RefinedMetaActionCandidate);
                if (refined == null)
                    return false;
                RefinedMetaActionCandidate = refined;
            }
            return true;
        }

        private bool IsValid(DomainDecl domain, List<ProblemDecl> problems)
        {
            var listener = new ErrorListener();
            var codeGenerator = new PDDLCodeGenerator(listener);
            bool isValid = true;
            foreach (var problem in problems)
            {
                var compiled = StackelbergCompiler.StackelbergCompiler.CompileToStackelberg(new PDDLDecl(domain, problem), RefinedMetaActionCandidate.Copy());
                codeGenerator.Generate(compiled.Domain, Path.Combine(_tempFolder, "tempDomain.pddl"));
                codeGenerator.Generate(compiled.Problem, Path.Combine(_tempFolder, "tempProblem.pddl"));
                if (!StackelbergVerifier.StackelbergVerifier.Validate(
                    Path.Combine(_tempFolder, "tempDomain.pddl"),
                    Path.Combine(_tempFolder, "tempProblem.pddl"),
                    _tempStatePath))
                    isValid = false;
            }
            return isValid;
        }
    }
}
