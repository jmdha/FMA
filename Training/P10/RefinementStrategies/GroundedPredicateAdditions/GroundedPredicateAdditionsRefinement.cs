using P10.Models;
using P10.RefinementStrategies.GroundedPredicateAdditions.Heuristics;
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
        private static readonly string _stateInfoFile = "out";

        public IHeuristic<PreconditionState> Heuristic { get; set; }
        private readonly HashSet<PreconditionState> _closedList = new HashSet<PreconditionState>();
        private readonly PriorityQueue<PreconditionState, int> _openList = new PriorityQueue<PreconditionState, int>();

        public GroundedPredicateAdditionsRefinement()
        {
            Heuristic = new hSum<PreconditionState>(new List<IHeuristic<PreconditionState>>() {
                new hMostValid()
            });
        }

        public ActionDecl? Refine(PDDLDecl pddlDecl, ActionDecl currentMetaAction)
        {
            if (_openList.Count == 0)
            {
                var targetFile = new FileInfo(Path.Combine(MetaActionRefiner.StackelbergOutputPath, _stateInfoFile));

                if (!targetFile.Exists)
                    throw new Exception("Stackelberg output does not exist!");

                var listener = new ErrorListener();
                var parser = new PDDLParser(listener);

                var text = File.ReadAllText(targetFile.FullName);
                var lines = text.Split('\n').ToList();
                lines.RemoveAll(x => x == "");
                var validStates = Convert.ToInt32(lines[0].Replace("Valid: ", ""));
                for(int i = 0; i < lines.Count; i += 2)
                {
                    var preconditions = new List<IExp>();

                    var facts = lines[i].Replace("Facts: ","").Split(';').ToList();
                    facts.RemoveAll(x => x == "");
                    foreach(var fact in facts)
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
                    var invalidStates = Convert.ToInt32(lines[i + 1].Replace("Invalid: ", ""));

                    var metaAction = currentMetaAction.Copy();
                    if (metaAction.Preconditions is AndExp and)
                    {
                        var notNode = new NotExp(and);
                        var andNode = new AndExp(notNode);
                        foreach (var precon in preconditions)
                            andNode.Children.Add(precon);
                        and.Add(andNode);
                    }

                    var newState = new PreconditionState(validStates, invalidStates, metaAction, preconditions);
                    if (!_closedList.Contains(newState))
                        _openList.Enqueue(newState, Heuristic.GetValue(newState));
                }
            }

            if (_openList.Count == 0)
                return null;

            var selected = _openList.Dequeue();
            _closedList.Add(selected);
            ConsoleHelper.WriteLineColor($"\t\tBest Validity: {Math.Round((((double)selected.ValidStates - (double)selected.InvalidStates) / (double)selected.ValidStates) * 100, 2)}%", ConsoleColor.Magenta);
            return selected.MetaAction;
        }
    }
}
