using P10.RefinementStrategies;
using P10.Verifiers;
using PDDLSharp.Models.PDDL;
using PDDLSharp.Models.PDDL.Domain;
using PDDLSharp.Models.PDDL.Problem;
using Tools;

namespace P10
{
    public class MetaActionRefiner
    {
        private static readonly string _tempFolder = PathHelper.RootPath("temp");

        public ActionDecl OriginalMetaActionCandidate { get; internal set; }
        public ActionDecl RefinedMetaActionCandidate { get; internal set; }
        public IRefinementStrategy Strategy { get; }
        public IVerifier Verifier { get; } = new FrontierVerifier();

        private int _iteration = 0;

        public MetaActionRefiner(ActionDecl metaActionCandidate, IRefinementStrategy strategy)
        {
            OriginalMetaActionCandidate = metaActionCandidate.Copy();
            RefinedMetaActionCandidate = metaActionCandidate.Copy();
            Strategy = strategy;

            PathHelper.RecratePath(_tempFolder);
        }

        public bool Refine(DomainDecl domain, List<ProblemDecl> problems)
        {
            _iteration = 0;
            while (!IsValid(domain, problems))
            {
                _iteration++;
                ConsoleHelper.WriteLineColor($"\tRefining iteration {_iteration}...", ConsoleColor.Magenta);
                var refined = Strategy.Refine(domain, problems, RefinedMetaActionCandidate, OriginalMetaActionCandidate, _tempFolder);
                if (refined == null)
                    return false;
                RefinedMetaActionCandidate = refined;
            }
            return true;
        }

        private bool IsValid(DomainDecl domain, List<ProblemDecl> problems)
        {
            ConsoleHelper.WriteLineColor($"\tValidating...", ConsoleColor.Magenta);
            bool isValid = true;
            foreach (var problem in problems)
            {
                var compiled = StackelbergCompiler.StackelbergCompiler.CompileToStackelberg(new PDDLDecl(domain, problem), RefinedMetaActionCandidate.Copy());
                if (!Verifier.Verify(compiled.Domain, compiled.Problem, Path.Combine(_tempFolder, "validation")))
                    isValid = false;
            }
            return isValid;
        }
    }
}
