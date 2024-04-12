using P10.Helpers;
using P10.PreconditionAdditionRefinements.Heuristics;
using P10.Verifiers;
using PDDLSharp.CodeGenerators.PDDL;
using PDDLSharp.ErrorListeners;
using PDDLSharp.Models.PDDL;
using PDDLSharp.Models.PDDL.Domain;
using PDDLSharp.Models.PDDL.Problem;
using System.Diagnostics;
using System.Text;
using Tools;
using static P10.PreconditionAdditionRefinements.StateExploreVerifier;

namespace P10.PreconditionAdditionRefinements
{
    public class PreconditionAdditionRefinement
    {
        public int TimeLimitS { get; }
        public string TempDir { get; }
        public string OutputDir { get; }
        public IHeuristic Heuristic { get; set; }
        public IVerifier Verifier { get; } = new FrontierVerifier();
        public ActionDecl MetaAction { get; }

        private readonly PriorityQueue<PreconditionState, int> _openList = new PriorityQueue<PreconditionState, int>();
        private readonly List<PreconditionState> _closedList = new List<PreconditionState>();
        private int _initialPossibilities = 0;
        private readonly Stopwatch _watch = new Stopwatch();
        private readonly string _tempValidationFolder = "";
        private RefinementResult _result = new RefinementResult();
        private readonly int _maxPreconditionCombinations;
        private readonly int _maxAddedParameters;
        private readonly string _learningCache = ".cache";

        public PreconditionAdditionRefinement(int timeLimitS, ActionDecl metaAction, string tempDir, string outputDir, int maxPreconditionCombinations, int maxAddedParameters, string learningCache)
        {
            Heuristic = new hSum(new List<IHeuristic>() {
                new hMustBeApplicable(),
                //new hMustBeValid(),
                new hWeighted(new hMostValid(), 100000),
                new hWeighted(new hFewestParameters(), 10000),
                new hWeighted(new hFewestPre(), 1000),
                new hWeighted(new hMostApplicable(), 100)
            });
            MetaAction = metaAction;
            TimeLimitS = timeLimitS;
            TempDir = tempDir;
            OutputDir = outputDir;
            _tempValidationFolder = Path.Combine(tempDir, "validation");
            PathHelper.RecratePath(_tempValidationFolder);
            _maxPreconditionCombinations = maxPreconditionCombinations;
            _maxAddedParameters = maxAddedParameters;
            _learningCache = learningCache;
            if (learningCache != "" && !Directory.Exists(learningCache))
                Directory.CreateDirectory(learningCache);
        }

        public RefinementResult Refine(DomainDecl domain, List<ProblemDecl> problems)
        {
            _result = new RefinementResult()
            {
                Domain = domain.Name!.Name,
                MetaAction = MetaAction.Name
            };
            _watch.Start();
            _result.RefinedMetaActions = Run(domain, problems);
            _watch.Stop();
            _result.RefinementTime = _watch.ElapsedMilliseconds;
            _result.ValidRefinements = _result.RefinedMetaActions.Count;
            return _result;
        }

        private List<ActionDecl> Run(DomainDecl domain, List<ProblemDecl> problems)
        {
            var returnList = new List<ActionDecl>();
            ConsoleHelper.WriteLineColor($"\t\tValidating...", ConsoleColor.Magenta);
            if (VerificationHelper.IsValid(domain, problems, MetaAction, _tempValidationFolder, TimeLimitS, _learningCache))
            {
                _result.AlreadyValid = true;
                ConsoleHelper.WriteLineColor($"\tOriginal meta action is valid!", ConsoleColor.Green);
                returnList.Add(MetaAction);
                return returnList;
            }

            var refined = false;
            int offset = 0;
            while (!refined)
            {
                ConsoleHelper.WriteLineColor($"\t\tInitial state space exploration started...", ConsoleColor.Magenta);
                var refineProblem = InitializeStateSearch(domain, problems, offset);
                if (refineProblem == -1)
                {
                    ConsoleHelper.WriteLineColor($"\t\tExploration failed", ConsoleColor.Magenta);
                    _result.Succeded = false;
                    return new List<ActionDecl>();
                }
                offset = refineProblem + 1;
                var refinedCandidates = RefineIterate(domain, problems);
                if (refinedCandidates.Count != 0)
                {
                    returnList = refinedCandidates;
                    refined = true;
                }
                else
                {
                    ConsoleHelper.WriteLineColor($"\t\tNo valid refinements for state explored problem! Trying next problem.", ConsoleColor.Magenta);
                    if (offset >= problems.Count)
                        break;
                }
            }

            return returnList;
        }

        private List<ActionDecl> RefineIterate(DomainDecl domain, List<ProblemDecl> problems)
        {
            int iteration = 0;
            var returnList = new List<ActionDecl>();
            ConsoleHelper.WriteLineColor($"\tRefining iteration {iteration++}...", ConsoleColor.Magenta);
            var refined = GetNextRefined(domain, problems);
            while (refined != null)
            {
                ConsoleHelper.WriteLineColor($"\t\tValidating...", ConsoleColor.Magenta);
                if (VerificationHelper.IsValid(domain, problems, refined, _tempValidationFolder, TimeLimitS, _learningCache))
                {
                    ConsoleHelper.WriteLineColor($"\tRefined meta action is valid!", ConsoleColor.Green);
                    returnList.Add(refined);
                }
                ConsoleHelper.WriteLineColor($"\tRefining iteration {iteration++}...", ConsoleColor.Magenta);
                refined = GetNextRefined(domain, problems);
            }
            return returnList;
        }

        private int InitializeStateSearch(DomainDecl domain, List<ProblemDecl> problems, int offset)
        {
            var searchWorkingDir = Path.Combine(TempDir, "state-search");
            PathHelper.RecratePath(searchWorkingDir);

            var searchWatch = new Stopwatch();
            searchWatch.Start();
            bool invalidInSome = false;
            var count = offset;
            foreach (var problem in problems.Skip(offset))
            {
                var pddlDecl = new PDDLDecl(domain, problem);
                var compiled = StackelbergHelper.CompileToStackelberg(pddlDecl, MetaAction.Copy());

                var verifier = new StateExploreVerifier(_maxPreconditionCombinations, _maxAddedParameters);
                if (File.Exists(Path.Combine(searchWorkingDir, StateInfoFile)))
                    File.Delete(Path.Combine(searchWorkingDir, StateInfoFile));
                verifier.UpdateSearchString(compiled);
                var result = verifier.VerifyCode(compiled.Domain, compiled.Problem, Path.Combine(TempDir, "state-search"), TimeLimitS);
                if (result == StateExploreResult.UnknownError)
                {
                    GenerateErrorLogFile(verifier._log, compiled.Domain, compiled.Problem);
                    ConsoleHelper.WriteLineColor($"\t\t\tUnknown error! Trying next problem...", ConsoleColor.Yellow);
                    invalidInSome = true;
                }
                else if (result == StateExploreResult.MetaActionValid)
                    ConsoleHelper.WriteLineColor($"\t\t\tMeta action valid in problem. Trying next problem...", ConsoleColor.Yellow);
                else if (result == StateExploreResult.InvariantError)
                {
                    ConsoleHelper.WriteLineColor($"\t\t\tInvariant error! Trying next problem...", ConsoleColor.Yellow);
                    invalidInSome = true;
                }
                else if (result == StateExploreResult.Success)
                {
                    var parseWatch = new Stopwatch();
                    parseWatch.Start();
                    UpdateOpenList(MetaAction, searchWorkingDir);
                    parseWatch.Stop();
                    _result.StackelbergOutputParsingTime += parseWatch.ElapsedMilliseconds;
                    invalidInSome = true;

                    if (_openList.Count != 0)
                    {
                        ConsoleHelper.WriteLineColor($"\t\t\tExploration successful!", ConsoleColor.Green);
                        break;
                    }
                    else
                        ConsoleHelper.WriteLineColor($"\t\t\tExploration resulted in no candidates! Trying next problem!", ConsoleColor.Green);
                }
                count++;
            }
            searchWatch.Stop();
            _result.StateSpaceSearchTime += searchWatch.ElapsedMilliseconds;

            if (!invalidInSome)
                throw new Exception("Meta Action was valid in all problems??? This should not be possible");

            if (_openList.Count == 0)
                return -1;

            return count;
        }

        private void GenerateErrorLogFile(string log, DomainDecl domain, ProblemDecl problem)
        {
            var file = Path.Combine(TempDir, $"{MetaAction.Name}_verification-log_{problem.Name}_{DateTime.Now.TimeOfDay}.txt");
            var sb = new StringBuilder();
            var codeGenerator = new PDDLCodeGenerator(new ErrorListener());

            sb.AppendLine(" == Log ==");
            sb.AppendLine(log);
            sb.AppendLine();
            sb.AppendLine();
            sb.AppendLine(" == Domain ==");
            sb.AppendLine(codeGenerator.Generate(domain));
            sb.AppendLine();
            sb.AppendLine();
            sb.AppendLine(" == Problem ==");
            sb.AppendLine(codeGenerator.Generate(problem));

            File.WriteAllText(file, sb.ToString());
        }

        private ActionDecl? GetNextRefined(DomainDecl domain, List<ProblemDecl> problems)
        {
            if (_openList.Count == 0)
                return null;

            ConsoleHelper.WriteLineColor($"\t\t{_openList.Count} possibilities left [Est. {TimeSpan.FromMilliseconds((double)_openList.Count * ((double)(_watch.ElapsedMilliseconds + 1) / (double)(1 + (_initialPossibilities - _openList.Count)))).ToString("hh\\:mm\\:ss")} until finished]", ConsoleColor.Magenta);
            var state = _openList.Dequeue();
            _closedList.Add(state);
#if DEBUG
            ConsoleHelper.WriteLineColor($"\t\tBest Validity: {Math.Round((1 - ((double)state.InvalidStates / (double)state.TotalInvalidStates)) * 100, 2)}%", ConsoleColor.Magenta);
            ConsoleHelper.WriteLineColor($"\t\tBest Applicability: {Math.Round(((double)state.Applicability / (double)(state.TotalValidStates + state.TotalInvalidStates)) * 100, 2)}%", ConsoleColor.Magenta);
            ConsoleHelper.WriteLineColor($"\t\tPrecondition: {state}", ConsoleColor.Magenta);
#endif
            return state.MetaAction;
        }

        private void UpdateOpenList(ActionDecl currentMetaAction, string workingDir)
        {
            ConsoleHelper.WriteLineColor($"\t\t\tParsing stackelberg output", ConsoleColor.Magenta);
            var toCheck = StackelbergOutputParser.ParseOutput(currentMetaAction, workingDir, _closedList);
            _result.InitialRefinementPossibilities += toCheck.Count;

            foreach (var check in toCheck)
                check.hValue = Heuristic.GetValue(check);
            toCheck.RemoveAll(x => x.hValue == int.MaxValue);

            ConsoleHelper.WriteLineColor($"\t\t\tChecks for covered meta actions", ConsoleColor.Magenta);
            ConsoleHelper.WriteLineColor($"\t\t\tTotal to check: {toCheck.Count}", ConsoleColor.Magenta);
            foreach (var state in toCheck)
                if (!IsCovered(state, toCheck))
                    _openList.Enqueue(state, state.hValue);
            ConsoleHelper.WriteLineColor($"\t\t\tTotal not covered: {_openList.Count}", ConsoleColor.Magenta);

            _result.FinalRefinementPossibilities += _openList.Count;
            _initialPossibilities = _openList.Count;
        }

        private bool IsCovered(PreconditionState check, List<PreconditionState> others)
        {
            if (check.ValidStates == check.TotalValidStates)
                return false;
            foreach (var state in others)
            {
                if (state != check &&
                    state.Precondition.Count < check.Precondition.Count &&
                    state.ValidStates == check.ValidStates &&
                    state.Applicability == check.Applicability)
                {
                    if (state.Precondition.All(x => check.Precondition.Any(y => y.Equals(x))))
                        return true;
                }
            }
            return false;
        }
    }
}
