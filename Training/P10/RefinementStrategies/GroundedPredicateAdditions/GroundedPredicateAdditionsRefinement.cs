using P10.Models;
using P10.RefinementStrategies.GroundedPredicateAdditions.Heuristics;
using P10.Verifiers;
using PDDLSharp.CodeGenerators.PDDL;
using PDDLSharp.ErrorListeners;
using PDDLSharp.Models.PDDL;
using PDDLSharp.Models.PDDL.Domain;
using PDDLSharp.Models.PDDL.Expressions;
using PDDLSharp.Models.PDDL.Problem;
using PDDLSharp.Parsers.PDDL;
using System.Diagnostics;
using Tools;

namespace P10.RefinementStrategies.GroundedPredicateAdditions
{
    public class GroundedPredicateAdditionsRefinement : IRefinementStrategy
    {
        public int TimeLimitS { get; }
        public int MetaActionIndex { get; }
        public string TempDir { get; }
        public string OutputDir { get; }
        public IHeuristic<PreconditionState> Heuristic { get; set; }

        private readonly PriorityQueue<PreconditionState, int> _openList = new PriorityQueue<PreconditionState, int>();
        private bool _isInitialized = false;
        private int _initialPossibilities = 0;
        private readonly Stopwatch _watch = new Stopwatch();
        private CSVWriter _csv;

        public GroundedPredicateAdditionsRefinement(int timeLimitS, int metaActionIndex, string tempDir, string outputDir)
        {
            Heuristic = new hSum<PreconditionState>(new List<IHeuristic<PreconditionState>>() {
                new hMustBeApplicable(),
                new hMustBeValid(),
                new hWeighted<PreconditionState>(new hMostValid(), 100000),
                new hWeighted<PreconditionState>(new hFewestParameters(), 10000),
                new hWeighted<PreconditionState>(new hFewestPre(), 1000),
                new hWeighted<PreconditionState>(new hMostApplicable(), 100)
            });
            TimeLimitS = timeLimitS;
            TempDir = tempDir;
            OutputDir = outputDir;
            _csv = new CSVWriter("refinement.csv", outputDir);
            MetaActionIndex = metaActionIndex;
        }

        public ActionDecl? Refine(DomainDecl domain, List<ProblemDecl> problems, ActionDecl currentMetaAction, ActionDecl originalMetaAction)
        {
            if (!_isInitialized)
            {
                _csv.Append("domain", domain.Name!.Name, MetaActionIndex);
                _csv.Append("meta-action", originalMetaAction.Name, MetaActionIndex);

                var searchWorkingDir = Path.Combine(TempDir, "state-search");
                PathHelper.RecratePath(searchWorkingDir);
                ConsoleHelper.WriteLineColor($"\t\tInitial state space exploration started...", ConsoleColor.Magenta);
                _isInitialized = true;
                var pddlDecl = new PDDLDecl(domain, problems[0]);
                var compiled = StackelbergCompiler.StackelbergCompiler.CompileToStackelberg(pddlDecl, originalMetaAction.Copy());

                var verifier = new StateExploreVerifier();
                if (File.Exists(Path.Combine(searchWorkingDir, StateExploreVerifier.StateInfoFile)))
                    File.Delete(Path.Combine(searchWorkingDir, StateExploreVerifier.StateInfoFile));
                verifier.UpdateSearchString(compiled);
                var searchWatch = new Stopwatch();
                searchWatch.Start();
                verifier.Verify(compiled.Domain, compiled.Problem, Path.Combine(TempDir, "state-search"), TimeLimitS);
                searchWatch.Stop();
                _csv.Append($"state_space_search_time", $"{searchWatch.ElapsedMilliseconds}", MetaActionIndex);
                searchWatch.Restart();
                if (!UpdateOpenList(originalMetaAction, searchWorkingDir))
                {
                    searchWatch.Stop();
                    _csv.Append($"stackelberg_output_parsing", $"{searchWatch.ElapsedMilliseconds}", MetaActionIndex);
                    return null;
                }
                searchWatch.Stop();
                _csv.Append($"stackelberg_output_parsing", $"{searchWatch.ElapsedMilliseconds}", MetaActionIndex);
                ConsoleHelper.WriteLineColor($"\t\tExploration finished", ConsoleColor.Magenta);

                _watch.Start();
            }
            if (_openList.Count == 0)
            {
                _csv.Append("total_refinement_time", $"{_watch.ElapsedMilliseconds}", MetaActionIndex);
                return null;
            }

            ConsoleHelper.WriteLineColor($"\t\t{_openList.Count} possibilities left [Est. {TimeSpan.FromMilliseconds((double)_openList.Count * ((double)(_watch.ElapsedMilliseconds + 1) / (double)(1 + (_initialPossibilities - _openList.Count)))).ToString("hh\\:mm\\:ss")} until finished]", ConsoleColor.Magenta);
            var state = _openList.Dequeue();
#if DEBUG
            ConsoleHelper.WriteLineColor($"\t\tBest Validity: {Math.Round((1 - ((double)state.InvalidStates / (double)state.TotalInvalidStates)) * 100, 2)}%", ConsoleColor.Magenta);
            ConsoleHelper.WriteLineColor($"\t\tBest Applicability: {Math.Round(((double)state.Applicability / (double)(state.TotalValidStates + state.TotalInvalidStates)) * 100, 2)}%", ConsoleColor.Magenta);
            ConsoleHelper.WriteLineColor($"\t\tPrecondition: {GetPreconText(state.Precondition)}", ConsoleColor.Magenta);
#endif
            return state.MetaAction;
        }

        private string GetPreconText(List<IExp> precons)
        {
            var listener = new ErrorListener();
            var codeGenerator = new PDDLCodeGenerator(listener);
            var preconStr = "";
            foreach (var precon in precons)
                preconStr += $"{codeGenerator.Generate(precon)}, ";
            return preconStr;
        }

        private bool UpdateOpenList(ActionDecl currentMetaAction, string workingDir)
        {
            ConsoleHelper.WriteLineColor($"\t\tUpdating open list...", ConsoleColor.Magenta);
            var targetFile = new FileInfo(Path.Combine(workingDir, StateExploreVerifier.StateInfoFile));

            if (!targetFile.Exists)
                return false;
            //throw new Exception("Stackelberg output does not exist!");

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
                    if (metaAction.Effects is AndExp effAnd && preconditions.Any(x => effAnd.Children.Any(y => y.Equals(x))))
                        continue;

                    foreach (var precon in preconditions)
                        and.Children.Add(precon);

                    //// Prune some nonsensical preconditions.
                    //if (andNode.Children.Any(x => andNode.Children.Contains(new NotExp(x))))
                    //    continue;
                }

                if (!checkedMetaActions.Contains(metaAction))
                {
                    checkedMetaActions.Add(metaAction);
                    var newState = new PreconditionState(
                        totalValidStates,
                        totalInvalidStates,
                        totalValidStates + totalInvalidStates - (totalInvalidStates - invalidStates),
                        invalidStates,
                        applicability,
                        metaAction,
                        preconditions);
                    toCheck.Add(newState);
                }
            }

            _csv.Append($"stackelberg_refinement_possibilities", $"{toCheck.Count}", MetaActionIndex);

            ConsoleHelper.WriteLineColor($"\t\t\tChecks for covered meta actions", ConsoleColor.Magenta);
            ConsoleHelper.WriteLineColor($"\t\t\tTotal to check: {toCheck.Count}", ConsoleColor.Magenta);
            foreach (var state in toCheck)
            {
                if (!IsCovered(state, toCheck))
                {
                    var hValue = Heuristic.GetValue(state);
                    if (hValue != int.MaxValue)
                        _openList.Enqueue(state, hValue);
                }
            }
            ConsoleHelper.WriteLineColor($"\t\t\tTotal not covered: {_openList.Count}", ConsoleColor.Magenta);

            _csv.Append($"final_refinement_possibilities", $"{_openList.Count}", MetaActionIndex);
            _initialPossibilities = _openList.Count;

            return true;
        }

        private bool IsCovered(PreconditionState check, List<PreconditionState> others)
        {
            foreach(var state in others)
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
