using P10.Models;
using P10.RefinementStrategies.GroundedPredicateAdditions.Heuristics;
using P10.Verifiers;
using PDDLSharp.ErrorListeners;
using PDDLSharp.Models.PDDL;
using PDDLSharp.Models.PDDL.Domain;
using PDDLSharp.Models.PDDL.Expressions;
using PDDLSharp.Parsers.PDDL;
using System;
using Tools;

namespace P10.RefinementStrategies.GroundedPredicateAdditions
{
    public class GroundedPredicateAdditionsRefinement : IRefinementStrategy
    {
        public IVerifier Verifier { get; } = new FrontierVerifier();
        private static readonly string _stateInfoFile = "out";

        public IHeuristic<PreconditionState> Heuristic { get; set; }
        private readonly PriorityQueue<PreconditionState, int> _openList = new PriorityQueue<PreconditionState, int>();
        private bool _isInitialized = false;

        public GroundedPredicateAdditionsRefinement()
        {
            Heuristic = new hSum<PreconditionState>(new List<IHeuristic<PreconditionState>>() {
                new hMostValid()
            });
        }

        public ActionDecl? Refine(PDDLDecl pddlDecl, ActionDecl currentMetaAction, ActionDecl originalMetaAction, string workingDir)
        {
            if (!_isInitialized)
            {
                ConsoleHelper.WriteLineColor($"\t\tInitial state space exploration started...", ConsoleColor.Magenta);
                _isInitialized = true;
                var compiled = StackelbergCompiler.StackelbergCompiler.CompileToStackelberg(pddlDecl, originalMetaAction.Copy());
                var verifier = new StateExploreVerifier();
                verifier.Verify(compiled.Domain, compiled.Problem, workingDir);
                if (!UpdateOpenList(originalMetaAction, workingDir))
                    return null;
                ConsoleHelper.WriteLineColor($"\t\tExploration finished", ConsoleColor.Magenta);
            }
            if (_openList.Count == 0)
                return null;

            ConsoleHelper.WriteLineColor($"\t\t{_openList.Count} possibilities left", ConsoleColor.Magenta);
            var state = _openList.Dequeue();
            ConsoleHelper.WriteLineColor($"\t\tBest Validity: {Math.Round((((double)state.ValidStates - (double)state.InvalidStates) / (double)state.ValidStates) * 100, 2)}%", ConsoleColor.Magenta);
            return state.MetaAction;
        }

        private bool UpdateOpenList(ActionDecl currentMetaAction, string workingDir)
        {
            ConsoleHelper.WriteLineColor($"\t\tUpdating open list...", ConsoleColor.Magenta);
            var targetFile = new FileInfo(Path.Combine(workingDir, _stateInfoFile));

            if (!targetFile.Exists)
                return false;
            //throw new Exception("Stackelberg output does not exist!");

            var listener = new ErrorListener();
            var parser = new PDDLParser(listener);

            var text = File.ReadAllText(targetFile.FullName);
            var lines = text.Split('\n').ToList();
            lines.RemoveAll(x => x == "");
            var validStates = Convert.ToInt32(lines[0]);
            for (int i = 2; i < lines.Count; i += 2)
            {
                var preconditions = new List<IExp>();

                var facts = lines[i].Split('|').ToList();
                facts.RemoveAll(x => x == "");
                foreach (var fact in facts)
                {
                    if (fact.Contains("NegatedAtom"))
                    {
                        var predText = fact.Replace("NegatedAtom", "").Trim();
                        preconditions.Add(new NotExp(parser.ParseAs<PredicateExp>(predText)));
                    }
                    else
                    {
                        var predText = fact.Replace("Atom", "").Trim();
                        preconditions.Add(parser.ParseAs<PredicateExp>(predText));
                    }
                }
                var invalidStates = Convert.ToInt32(lines[i + 1]);

                var metaAction = currentMetaAction.Copy();
                if (metaAction.Preconditions is AndExp and)
                {
                    var andNode = new AndExp(and);
                    foreach (var precon in preconditions)
                        andNode.Children.Add(precon);
                    and.Add(andNode);

                    // Prune some nonsensical preconditions.
                    if (andNode.Children.Any(x => andNode.Children.Contains(new NotExp(x))))
                        continue;
                }

                var newState = new PreconditionState(validStates, invalidStates, metaAction, preconditions);
                _openList.Enqueue(newState, Heuristic.GetValue(newState));
            }

            targetFile.Delete();
            return true;
        }
    }
}
