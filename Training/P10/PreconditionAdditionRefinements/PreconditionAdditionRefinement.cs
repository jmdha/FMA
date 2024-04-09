using P10.Helpers;
using P10.Models;
using P10.PreconditionAdditionRefinements.Heuristics;
using P10.Verifiers;
using PDDLSharp.CodeGenerators.PDDL;
using PDDLSharp.ErrorListeners;
using PDDLSharp.Models.PDDL;
using PDDLSharp.Models.PDDL.Domain;
using PDDLSharp.Models.PDDL.Expressions;
using PDDLSharp.Models.PDDL.Overloads;
using PDDLSharp.Models.PDDL.Problem;
using PDDLSharp.Parsers.PDDL;
using System.Diagnostics;
using System.Numerics;
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
        public IHeuristic<PreconditionState> Heuristic { get; set; }
        public IVerifier Verifier { get; } = new FrontierVerifier();
        public ActionDecl MetaAction { get; }

        private readonly PriorityQueue<PreconditionState, int> _openList = new PriorityQueue<PreconditionState, int>();
        private int _initialPossibilities = 0;
        private readonly Stopwatch _watch = new Stopwatch();
        private readonly string _tempValidationFolder = "";
        private RefinementResult _result = new RefinementResult();
        private int _maxPreconditionCombinations;
        private int _maxAddedParameters;
        private string _learningCache = ".cache";

        public PreconditionAdditionRefinement(int timeLimitS, ActionDecl metaAction, string tempDir, string outputDir, int maxPreconditionCombinations, int maxAddedParameters, string learningCache)
        {
            Heuristic = new hSum<PreconditionState>(new List<IHeuristic<PreconditionState>>() {
                new hMustBeApplicable(),
                //new hMustBeValid(),
                new hWeighted<PreconditionState>(new hMostValid(), 100000),
                new hWeighted<PreconditionState>(new hFewestParameters(), 10000),
                new hWeighted<PreconditionState>(new hFewestPre(), 1000),
                new hWeighted<PreconditionState>(new hMostApplicable(), 100)
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
            int iteration = 0;
            var returnList = new List<ActionDecl>();
            ConsoleHelper.WriteLineColor($"\t\tValidating...", ConsoleColor.Magenta);
            if (VerificationHelper.IsValid(domain, problems, MetaAction, _tempValidationFolder, TimeLimitS, _learningCache))
            {
                _result.AlreadyValid = true;
                ConsoleHelper.WriteLineColor($"\tOriginal meta action is valid!", ConsoleColor.Green);
                returnList.Add(MetaAction);
                return returnList;
            }

            ConsoleHelper.WriteLineColor($"\t\tInitial state space exploration started...", ConsoleColor.Magenta);
            if (!InitializeStateSearch(domain, problems))
            {
                ConsoleHelper.WriteLineColor($"\t\tExploration failed", ConsoleColor.Magenta);
                _result.Succeded = false;
                return new List<ActionDecl>();
            }
            else
            {
                ConsoleHelper.WriteLineColor($"\t\tExploration finished", ConsoleColor.Magenta);
                _result.Succeded = true;
            }

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

        private bool InitializeStateSearch(DomainDecl domain, List<ProblemDecl> problems, int offset = 0)
        {
            if (problems.Count <= offset)
                return false;

            var searchWorkingDir = Path.Combine(TempDir, "state-search");
            PathHelper.RecratePath(searchWorkingDir);

            var searchWatch = new Stopwatch();
            searchWatch.Start();
            bool invalidInSome = false;
            foreach (var problem in problems)
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
            }
            searchWatch.Stop();
            _result.StateSpaceSearchTime += searchWatch.ElapsedMilliseconds;

            if (!invalidInSome)
                throw new Exception("Meta Action was valid in all problems??? This should not be possible");

            if (_openList.Count == 0)
                return false;

            return true;
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
#if DEBUG
            ConsoleHelper.WriteLineColor($"\t\tBest Validity: {Math.Round((1 - ((double)state.InvalidStates / (double)state.TotalInvalidStates)) * 100, 2)}%", ConsoleColor.Magenta);
            ConsoleHelper.WriteLineColor($"\t\tBest Applicability: {Math.Round(((double)state.Applicability / (double)(state.TotalValidStates + state.TotalInvalidStates)) * 100, 2)}%", ConsoleColor.Magenta);
            ConsoleHelper.WriteLineColor($"\t\tPrecondition: {state}", ConsoleColor.Magenta);
#endif
            return state.MetaAction;
        }

        private void UpdateOpenList(ActionDecl currentMetaAction, string workingDir)
        {
            currentMetaAction.EnsureAnd();
            ConsoleHelper.WriteLineColor($"\t\tUpdating open list...", ConsoleColor.Magenta);
            var targetFile = new FileInfo(Path.Combine(workingDir, StateExploreVerifier.StateInfoFile));

            ConsoleHelper.WriteLineColor($"\t\t\tParsing stackelberg output", ConsoleColor.Magenta);
            var listener = new ErrorListener();
            var parser = new PDDLParser(listener);
            var toCheck = new List<PreconditionState>();

            var text = File.ReadAllText(targetFile.FullName);
            var lines = text.Split('\n').ToList();
            var checkedMetaActions = new HashSet<ActionDecl>();
            var totalValidStates = Convert.ToInt32(lines[0]);
            var totalInvalidStates = Convert.ToInt32(lines[1]);
            for (int i = 2; i < lines.Count; i += 4)
            {
                if (i + 4 > lines.Count)
                    break;
                var metaAction = currentMetaAction.Copy();
                var preconditions = new List<IExp>();

                var typesStr = lines[i].Trim();
                var types = typesStr.Split(' ').ToList();
                types.RemoveAll(x => x == "");

                var facts = lines[i + 1].Split('|').ToList();
                facts.RemoveAll(x => x == "");
                foreach (var fact in facts)
                {
                    bool isNegative = fact.Contains("NegatedAtom");
                    var predText = fact.Replace("NegatedAtom", "").Replace("Atom", "").Trim();
                    var predName = predText.Substring(0, predText.IndexOf('[')).Trim();
                    var paramString = predText.Substring(predText.IndexOf('[')).Replace("[", "").Replace("]", "").Trim();
                    var paramStrings = paramString.Split(',').ToList();
                    paramStrings.RemoveAll(x => x == "");

                    var newPredicate = new PredicateExp(predName);
                    foreach (var item in paramStrings)
                    {
                        var index = Int32.Parse(item);
                        if (index >= metaAction.Parameters.Values.Count)
                        {
                            if (types.Count == 0)
                                throw new Exception("Added precondition is trying to reference a added parameter, but said parameter have not been added! (Stackelberg Output Malformed)");
                            var newNamed = new NameExp($"?{item}", new TypeExp(types[metaAction.Parameters.Values.Count - index]));
                            metaAction.Parameters.Values.Add(newNamed);
                            newPredicate.Arguments.Add(newNamed);
                        }
                        else
                        {
                            var param = metaAction.Parameters.Values[index];
                            newPredicate.Arguments.Add(new NameExp(param.Name));
                        }
                    }

                    if (isNegative)
                        preconditions.Add(new NotExp(newPredicate));
                    else
                        preconditions.Add(newPredicate);
                }
                var invalidStates = Convert.ToInt32(lines[i + 3]);
                var applicability = Convert.ToInt32(lines[i + 2]);

                if (metaAction.Preconditions is AndExp and)
                {
                    // Remove preconditions that have the same effect
                    if (metaAction.Effects is AndExp effAnd)
                    {
                        var cpy = effAnd.Copy();
                        cpy.RemoveContext();
                        cpy.RemoveTypes();
                        if (cpy.Children.All(x => preconditions.Any(y => y.Equals(x))))
                            continue;
                        if (IsSubset(preconditions, cpy.Children))
                            continue;
                    }

                    // Prune some nonsensical preconditions.
                    if (preconditions.Any(x => preconditions.Contains(new NotExp(x))))
                        continue;

                    foreach (var precon in preconditions)
                        if (!and.Children.Contains(precon))
                            and.Children.Add(precon);
                }

                if (!checkedMetaActions.Contains(metaAction))
                {
                    checkedMetaActions.Add(metaAction);
                    var newState = new PreconditionState(
                        totalValidStates,
                        totalInvalidStates,
                        totalValidStates - (totalInvalidStates - invalidStates),
                        invalidStates,
                        applicability,
                        metaAction,
                        preconditions,
                        0);
                    newState.hValue = Heuristic.GetValue(newState);
                    if (newState.hValue != int.MaxValue)
                        toCheck.Add(newState);
                }
            }

            _result.InitialRefinementPossibilities += toCheck.Count;

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

        private bool IsSubset(List<IExp> set1, List<IExp> set2)
        {
            foreach (var item in set1)
                if (!set2.Contains(item))
                    return false;
            return true;
        }
    }
}
