using P10.Models;
using P10.RefinementStrategies.GroundedPredicateAdditions.Heuristics;
using PDDLSharp.Models.PDDL;
using PDDLSharp.Models.PDDL.Domain;
using PDDLSharp.Models.PDDL.Expressions;

namespace P10.RefinementStrategies.GroundedPredicateAdditions
{
    public class GroundedPredicateAdditionsRefinement : IRefinementStrategy
    {
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
                foreach (var file in new DirectoryInfo(MetaActionRefiner.StackelbergOutputPath).GetFiles())
                {
                    // parse files...

                    int validStates = 0;
                    int invalidStates = 0;
                    var metaAction = currentMetaAction.Copy();
                    var predicates = new List<PredicateExp>(){
                        new PredicateExp("test")
                    };

                    if (metaAction.Preconditions is AndExp and)
                    {
                        var notNode = new NotExp(and);
                        var andNode = new AndExp(notNode);
                        foreach (var precon in predicates)
                            andNode.Children.Add(precon);
                        and.Add(andNode);
                    }

                    var newState = new PreconditionState(validStates, invalidStates, metaAction, predicates);
                    if (!_closedList.Contains(newState))
                        _openList.Enqueue(newState, Heuristic.GetValue(newState));
                }
            }

            if (_openList.Count == 0)
                return null;

            var selected = _openList.Dequeue();
            _closedList.Add(selected);
            return selected.MetaAction;
        }
    }
}
