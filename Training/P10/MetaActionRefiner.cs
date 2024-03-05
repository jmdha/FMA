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
        private static readonly string _tempFolder = PathHelper.RootPath("temp/refiner");
        public static readonly string StackelbergOutputPath = PathHelper.RootPath("temp/stackelberg-output/");

        public ActionDecl OriginalMetaActionCandidate { get; internal set; }
        public ActionDecl RefinedMetaActionCandidate { get; internal set; }
        public IRefinementStrategy Strategy { get; }

        private int _iteration = 0;

        public MetaActionRefiner(ActionDecl metaActionCandidate, IRefinementStrategy strategy)
        {
            OriginalMetaActionCandidate = metaActionCandidate.Copy();
            RefinedMetaActionCandidate = metaActionCandidate.Copy();
            Strategy = strategy;

            PathHelper.RecratePath(_tempFolder);
            PathHelper.RecratePath(StackelbergOutputPath);
        }

        public bool Refine(PDDLDecl pddlDecl, DomainDecl domain, List<ProblemDecl> problems)
        {
            _iteration = 0;
            while (!IsValid(domain, problems))
            {
                _iteration++;
                ConsoleHelper.WriteLineColor($"\tRefining iteration {_iteration}...", ConsoleColor.Magenta);
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
                codeGenerator.Generate(compiled.Domain, Path.Combine(_tempFolder, $"{OriginalMetaActionCandidate.Name}_{_iteration}_tempDomain.pddl"));
                codeGenerator.Generate(compiled.Problem, Path.Combine(_tempFolder, $"{OriginalMetaActionCandidate.Name}_{_iteration}_tempProblem.pddl"));
                if (!StackelbergVerifier.StackelbergVerifier.Validate(
                    Path.Combine(_tempFolder, $"{OriginalMetaActionCandidate.Name}_{_iteration}_tempDomain.pddl"),
                    Path.Combine(_tempFolder, $"{OriginalMetaActionCandidate.Name}_{_iteration}_tempProblem.pddl"),
                    StackelbergOutputPath))
                    isValid = false;
            }
            return isValid;
        }
    }
}
