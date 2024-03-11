using P10.Models;
using P10.RefinementStrategies.GroundedPredicateAdditions.Heuristics;
using P10.Verifiers;
using PDDLSharp.CodeGenerators.PDDL;
using PDDLSharp.ErrorListeners;
using PDDLSharp.Models.FastDownward.SAS;
using PDDLSharp.Models.PDDL;
using PDDLSharp.Models.PDDL.Domain;
using PDDLSharp.Models.PDDL.Expressions;
using PDDLSharp.Models.PDDL.Problem;
using PDDLSharp.Parsers.FastDownward.SAS;
using PDDLSharp.Parsers.PDDL;
using System.Diagnostics;
using Tools;

namespace P10.RefinementStrategies.GroundedPredicateAdditions
{
    public class GroundedPredicateAdditionsRefinement : IRefinementStrategy
    {
        public IHeuristic<PreconditionState> Heuristic { get; set; }
        private readonly PriorityQueue<PreconditionState, int> _openList = new PriorityQueue<PreconditionState, int>();
        private bool _isInitialized = false;
        private int _initialPossibilities = 0;
        private readonly Stopwatch _watch = new Stopwatch();

        public GroundedPredicateAdditionsRefinement()
        {
            Heuristic = new hSum<PreconditionState>(new List<IHeuristic<PreconditionState>>() {
                new hMustBeApplicable(),
                new hMustBeValid(),
                new hWeighted<PreconditionState>(new hMostValid(), 10000),
                new hWeighted<PreconditionState>(new hFewestPre(), 1000),
                new hMostApplicable(),
            });
        }

        public ActionDecl? Refine(DomainDecl domain, List<ProblemDecl> problems, ActionDecl currentMetaAction, ActionDecl originalMetaAction, string workingDir)
        {
            if (!_isInitialized)
            {
                var searchWorkingDir = Path.Combine(workingDir, "state-search");
                PathHelper.RecratePath(searchWorkingDir);
                ConsoleHelper.WriteLineColor($"\t\tInitial state space exploration started...", ConsoleColor.Magenta);
                _isInitialized = true;
                var pddlDecl = new PDDLDecl(domain, problems[0]);
                var compiled = StackelbergCompiler.StackelbergCompiler.CompileToStackelberg(pddlDecl, originalMetaAction.Copy());

                var verifier = new StateExploreVerifier();
                if (File.Exists(Path.Combine(searchWorkingDir, StateExploreVerifier.StateInfoFile)))
                    File.Delete(Path.Combine(searchWorkingDir, StateExploreVerifier.StateInfoFile));
                verifier.Verify(compiled.Domain, compiled.Problem, Path.Combine(workingDir, "state-search"));
                if (!UpdateOpenList(originalMetaAction, searchWorkingDir))
                    return null;
                ConsoleHelper.WriteLineColor($"\t\tExploration finished", ConsoleColor.Magenta);
                _watch.Start();
            }
            if (_openList.Count == 0)
                return null;

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

            var listener = new ErrorListener();
            var parser = new PDDLParser(listener);

            var text = File.ReadAllText(targetFile.FullName);
            var lines = text.Split('\n').ToList();
            lines.RemoveAll(x => x == "");
            var totalValidStates = Convert.ToInt32(lines[0]);
            var totalInvalidStates = Convert.ToInt32(lines[1]);
            for (int i = 2; i < lines.Count; i += 3)
            {
                var preconditions = new List<IExp>();

                var facts = lines[i].Split('|').ToList();
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
                        newPredicate.Arguments.Add(new NameExp(currentMetaAction.Parameters.Values[index].Name));
                    }

                    if (isNegative)
                        preconditions.Add(new NotExp(newPredicate));
                    else
                        preconditions.Add(newPredicate);
                }
                var invalidStates = Convert.ToInt32(lines[i + 2]);
                var applicability = Convert.ToInt32(lines[i + 1]);

                var metaAction = currentMetaAction.Copy();
                if (metaAction.Preconditions is AndExp and)
                {
                    var andNode = new AndExp(and);
                    foreach (var precon in preconditions)
                        andNode.Children.Add(precon);
                    and.Add(andNode);

                    //// Prune some nonsensical preconditions.
                    //if (andNode.Children.Any(x => andNode.Children.Contains(new NotExp(x))))
                    //    continue;
                }

                var newState = new PreconditionState(
                    totalValidStates,
                    totalInvalidStates,
                    totalValidStates + totalInvalidStates - (totalInvalidStates - invalidStates), 
                    invalidStates, 
                    applicability, 
                    metaAction, 
                    preconditions);
                var hValue = Heuristic.GetValue(newState);
                if (hValue != int.MaxValue)
                    _openList.Enqueue(newState, hValue);
            }
            _initialPossibilities = _openList.Count;

            return true;
        }
    }
}
