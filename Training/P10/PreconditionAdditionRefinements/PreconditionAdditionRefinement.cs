using P10.Helpers;
using P10.PreconditionAdditionRefinements.Heuristics;
using P10.Verifiers;
using PDDLSharp.Models.PDDL;
using PDDLSharp.Models.PDDL.Domain;
using PDDLSharp.Models.PDDL.Problem;
using System.Diagnostics;
using Tools;
using static P10.PreconditionAdditionRefinements.StateExploreVerifier;

namespace P10.PreconditionAdditionRefinements
{
    public class PreconditionAdditionRefinement
    {
        public int ValidationTimeLimitS { get; }
        public int ExplorationTimeLimitS { get; }
        public int RefinementTimeLimitS { get; }
        public string TempDir { get; }
        public IHeuristic Heuristic { get; set; }
        public ActionDecl MetaAction { get; }

        private readonly List<PreconditionState> _closedList = new List<PreconditionState>();
        private int _initialPossibilities = 0;
        private readonly Stopwatch _watch = new Stopwatch();
        private readonly string _tempValidationFolder = "";
        private RefinementResult _result = new RefinementResult();
        private readonly int _maxPreconditionCombinations;
        private readonly int _maxAddedParameters;
        private readonly string _searchWorkingDir;

        public PreconditionAdditionRefinement(int validationTimeLimitS, int explorationTimeLimitS, int refinementTimeLimitS, ActionDecl metaAction, string tempDir, int maxPreconditionCombinations, int maxAddedParameters, PDDLDecl pddlDecl)
        {
            Heuristic = new hSum(new List<IHeuristic>() {
                new hMustBeApplicable(),
                //new hMustBeValid(),
                new hWeighted(new hMostStatics(pddlDecl), 100000),
                //new hWeighted(new hMostValid(), 10000),
                new hWeighted(new hFewestParameters(), 1000),
                new hWeighted(new hFewestPre(), 100),
                new hWeighted(new hMostApplicable(), 10)
            });
            MetaAction = metaAction;
            ValidationTimeLimitS = validationTimeLimitS;
            ExplorationTimeLimitS = explorationTimeLimitS;
            RefinementTimeLimitS = refinementTimeLimitS;
            TempDir = tempDir;
            _tempValidationFolder = Path.Combine(tempDir, "validation");
            PathHelper.RecratePath(_tempValidationFolder);
            _searchWorkingDir = Path.Combine(tempDir, "state-search");
            PathHelper.RecratePath(_searchWorkingDir);
            _maxPreconditionCombinations = maxPreconditionCombinations;
            _maxAddedParameters = maxAddedParameters;
        }

        public RefinementResult Refine(DomainDecl domain, List<ProblemDecl> problems)
        {
            _result = new RefinementResult()
            {
                ID = P10.ID,
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

            // Check if initial meta action is valid
            ConsoleHelper.WriteLineColor($"\t\tValidating...", ConsoleColor.Magenta);
            var result = VerificationHelper.IsValid(domain, problems, MetaAction, _tempValidationFolder, ValidationTimeLimitS);
            if (result == FrontierVerifier.FrontierResult.Valid)
            {
                _result.AlreadyValid = true;
                _result.Succeded = true;
                ConsoleHelper.WriteLineColor($"\tOriginal meta action is valid!", ConsoleColor.Green);
                returnList.Add(MetaAction);
                return returnList;
            } else if (result == FrontierVerifier.FrontierResult.Inapplicable)
            {
                ConsoleHelper.WriteLineColor($"\tOriginal meta action is inapplicable...", ConsoleColor.Yellow);
                return returnList;
            }

            // Iterate through all problems, until some valid refinements are found
            int count = 1;
            foreach (var problem in problems)
            {
                ConsoleHelper.WriteLineColor($"\t\tStarting refinement on problem {problem.Name!.Name} ({count++} out of {problems.Count})", ConsoleColor.Magenta);

                // Explore state for problem
                ConsoleHelper.WriteLineColor($"\t\tInitial state space exploration started...", ConsoleColor.Magenta);
                var explored = ExploreState(domain, problem);
                if (explored == StateExploreResult.TimedOut)
                {
                    ConsoleHelper.WriteLineColor($"\t\tState exploration timed out. Trying next problem...", ConsoleColor.Yellow);
                    continue;
                }
                if (explored != StateExploreResult.Success && explored != StateExploreResult.TimedOutButSuccess)
                    continue;

                // Generate refinement list
                var openList = UpdateOpenList(MetaAction);
                if (openList.Count == 0)
                {
                    ConsoleHelper.WriteLineColor($"\t\t\tExploration yielded no candidates. Trying next problem...", ConsoleColor.Yellow);
                    continue;
                }

                // Check through each of the refinements and add valid ones to the return set.
                var timeoutWatch = new Stopwatch();
                timeoutWatch.Start();
                var nextRefined = GetNextRefined(openList);
                while (nextRefined != null)
                {
                    if (RefinementTimeLimitS > -1 && timeoutWatch.ElapsedMilliseconds / 1000 > RefinementTimeLimitS)
                    {
                        ConsoleHelper.WriteLineColor($"\t\tRefinement checking timed out, stopping...", ConsoleColor.Yellow);
                        break;
                    }
                    ConsoleHelper.WriteLineColor($"\t\tValidating...", ConsoleColor.Magenta);
                    if (VerificationHelper.IsValid(domain, problems, nextRefined, _tempValidationFolder, ValidationTimeLimitS) == FrontierVerifier.FrontierResult.Valid)
                    {
                        ConsoleHelper.WriteLineColor($"\tMeta action refinement is valid!", ConsoleColor.Green);
                        returnList.Add(nextRefined);
                    }
                    nextRefined = GetNextRefined(openList);
                }
                if (returnList.Count == 0)
                    ConsoleHelper.WriteLineColor($"\t\tNo valid refinements for state explored problem! Trying next problem...", ConsoleColor.Magenta);
                else
                    break;
            }

            return returnList;
        }

        private StateExploreResult ExploreState(DomainDecl domain, ProblemDecl problem)
        {
            var pddlDecl = new PDDLDecl(domain, problem);
            var compiled = StackelbergHelper.CompileToStackelberg(pddlDecl, MetaAction.Copy());

            var searchWatch = new Stopwatch();
            searchWatch.Start();
            var verifier = new StateExploreVerifier(_maxPreconditionCombinations, _maxAddedParameters, ExplorationTimeLimitS);
            if (File.Exists(Path.Combine(_searchWorkingDir, StateInfoFile)))
                File.Delete(Path.Combine(_searchWorkingDir, StateInfoFile));
            verifier.UpdateSearchString(compiled);
            var result = verifier.VerifyCode(compiled.Domain, compiled.Problem, _searchWorkingDir, ExplorationTimeLimitS);
            if (result == StateExploreResult.UnknownError)
            {
                var file = Path.Combine(TempDir, $"{MetaAction.Name}_verification-log_{pddlDecl.Problem.Name}_{DateTime.Now.TimeOfDay}.txt");
                File.WriteAllText(file, verifier.GetLog());
                ConsoleHelper.WriteLineColor($"\t\t\tUnknown error! Trying next problem...", ConsoleColor.Yellow);
            }
            else if (result == StateExploreResult.MetaActionValid)
                ConsoleHelper.WriteLineColor($"\t\t\tMeta action valid in problem. Trying next problem...", ConsoleColor.Yellow);
            else if (result == StateExploreResult.InvariantError)
                ConsoleHelper.WriteLineColor($"\t\t\tInvariant error! Trying next problem...", ConsoleColor.Yellow);
            else if (result == StateExploreResult.PDDLError)
                ConsoleHelper.WriteLineColor($"\t\t\tPDDL error! Trying next problem...", ConsoleColor.Yellow);
            else if (result == StateExploreResult.Success)
                ConsoleHelper.WriteLineColor($"\t\t\tState exploration succeeded!", ConsoleColor.Green);
            else if (result == StateExploreResult.TimedOut)
                ConsoleHelper.WriteLineColor($"\t\t\tState exploration timed out...", ConsoleColor.Yellow);
            else if (result == StateExploreResult.TimedOutButSuccess)
                ConsoleHelper.WriteLineColor($"\t\t\tState exploration timed out but some refinement options exist...", ConsoleColor.Yellow);
            searchWatch.Stop();
            _result.StateSpaceSearchTime += searchWatch.ElapsedMilliseconds;
            return result;
        }

        private ActionDecl? GetNextRefined(PriorityQueue<PreconditionState, int> openList)
        {
            if (openList.Count == 0)
                return null;

            _result.Succeded = true;

            ConsoleHelper.WriteLineColor($"\t\t{openList.Count} possibilities left [Est. {TimeSpan.FromMilliseconds((double)openList.Count * ((double)(_watch.ElapsedMilliseconds + 1) / (double)(1 + (_initialPossibilities - openList.Count)))).ToString("hh\\:mm\\:ss")} until finished]", ConsoleColor.Magenta);
            var state = openList.Dequeue();
            _closedList.Add(state);
            ConsoleHelper.WriteLineColor($"\t\tPrecondition: {state}", ConsoleColor.Magenta);
            return state.MetaAction;
        }

        private PriorityQueue<PreconditionState, int> UpdateOpenList(ActionDecl currentMetaAction)
        {
            var parseWatch = new Stopwatch();
            parseWatch.Start();

            ConsoleHelper.WriteLineColor($"\t\t\tParsing stackelberg output", ConsoleColor.Magenta);
            var openList = new PriorityQueue<PreconditionState, int>();
            var toCheck = StackelbergOutputParser.ParseOutput(currentMetaAction, _searchWorkingDir, _closedList);
            _result.InitialRefinementPossibilities += toCheck.Count;

            foreach (var check in toCheck)
                check.hValue = Heuristic.GetValue(check);
            toCheck.RemoveAll(x => x.hValue == int.MaxValue);

            ConsoleHelper.WriteLineColor($"\t\t\tChecks for covered meta actions", ConsoleColor.Magenta);
            ConsoleHelper.WriteLineColor($"\t\t\tTotal to check: {toCheck.Count}", ConsoleColor.Magenta);
            foreach (var state in toCheck)
                if (!IsCovered(state, toCheck))
                    openList.Enqueue(state, state.hValue);
            ConsoleHelper.WriteLineColor($"\t\t\tTotal not covered: {openList.Count}", ConsoleColor.Magenta);

            parseWatch.Stop();
            _result.StackelbergOutputParsingTime += parseWatch.ElapsedMilliseconds;
            _result.FinalRefinementPossibilities += openList.Count;
            _initialPossibilities = openList.Count;

            return openList;
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
