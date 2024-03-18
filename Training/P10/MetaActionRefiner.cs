using P10.RefinementStrategies;
using P10.Verifiers;
using PDDLSharp.Models.PDDL;
using PDDLSharp.Models.PDDL.Domain;
using PDDLSharp.Models.PDDL.Problem;
using System.Diagnostics;
using Tools;

namespace P10
{
    public class MetaActionRefiner
    {
        public ActionDecl OriginalMetaActionCandidate { get; internal set; }
        public IRefinementStrategy Strategy { get; }
        public IVerifier Verifier { get; } = new FrontierVerifier();
        public int TimeLimitS { get; } = -1;

        private int _iteration = 0;
        private string _tempPath = "";
        private string _tempValidationFolder = "";

        public MetaActionRefiner(ActionDecl metaActionCandidate, Options.RefinementStrategies strategy, string tempPath, string outPath, int timeLimitS, int metaActionIndex)
        {
            OriginalMetaActionCandidate = metaActionCandidate.Copy();
            Strategy = RefinementStrategyBuilder.GetStrategy(strategy, timeLimitS, metaActionIndex, tempPath, outPath);
            _tempPath = tempPath;
            TimeLimitS = timeLimitS;

            _tempValidationFolder = Path.Combine(_tempPath, "validation");
            PathHelper.RecratePath(_tempValidationFolder);
        }

        public List<ActionDecl> Refine(DomainDecl domain, List<ProblemDecl> problems)
        {
            _iteration = 0;
            var returnList = new List<ActionDecl>();
            if (IsValid(domain, problems, OriginalMetaActionCandidate))
            {
                ConsoleHelper.WriteLineColor($"\tOriginal meta action is valid!", ConsoleColor.Green);
                returnList.Add(OriginalMetaActionCandidate);
                return returnList;
            }
            var refined = Strategy.Refine(domain, problems, OriginalMetaActionCandidate, OriginalMetaActionCandidate);
            while (refined != null)
            {
                ConsoleHelper.WriteLineColor($"\tRefining iteration {_iteration++}...", ConsoleColor.Magenta);
                if (IsValid(domain, problems, refined))
                {
                    ConsoleHelper.WriteLineColor($"\tRefined meta action is valid!", ConsoleColor.Green);
                    returnList.Add(refined);
                }
                refined = Strategy.Refine(domain, problems, refined, OriginalMetaActionCandidate);
            }
            return returnList;
        }

        private bool IsValid(DomainDecl domain, List<ProblemDecl> problems, ActionDecl metaAction)
        {
            ConsoleHelper.WriteLineColor($"\t\tValidating...", ConsoleColor.Magenta);
            foreach (var problem in problems)
            {
                var compiled = StackelbergCompiler.StackelbergCompiler.CompileToStackelberg(new PDDLDecl(domain, problem), metaAction.Copy());
                if (!Verifier.Verify(compiled.Domain, compiled.Problem, _tempValidationFolder, TimeLimitS))
                    return false;
            }
            return true;
        }
    }
}
