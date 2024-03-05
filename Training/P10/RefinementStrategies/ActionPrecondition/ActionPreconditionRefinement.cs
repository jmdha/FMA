using P10.Models;
using P10.RefinementStrategies.ActionPrecondition.Heuristics;
using PDDLSharp.Models.PDDL;
using PDDLSharp.Models.PDDL.Domain;
using PDDLSharp.Models.PDDL.Expressions;

namespace P10.RefinementStrategies.ActionPrecondition
{
    public class ActionPreconditionRefinement : IRefinementStrategy
    {
        public IHeuristic<MetaActionState> Heuristic { get; set; }
        private readonly HashSet<MetaActionState> _closedList = new HashSet<MetaActionState>();
        private readonly PriorityQueue<MetaActionState, int> _openList = new PriorityQueue<MetaActionState, int>();
        private readonly PriorityQueue<MetaActionState, int> _bestList = new PriorityQueue<MetaActionState, int>();
        private bool _haveExhausted = false;

        public ActionPreconditionRefinement()
        {
            Heuristic = new hSum<MetaActionState>(new List<IHeuristic<MetaActionState>>() {
                new hParams()
            });
        }

        public ActionDecl? Refine(PDDLDecl pddlDecl, ActionDecl currentMetaAction)
        {
            if (_bestList.Count == 0)
            {
                if (_haveExhausted)
                    return null;

                _openList.Enqueue(new MetaActionState(currentMetaAction, new List<string>()), int.MaxValue);
                while (_openList.Count > 0)
                {
                    var state = _openList.Dequeue();
                    _closedList.Add(state);
                    foreach (var action in pddlDecl.Domain.Actions)
                    {
                        if (ContainsPred(state.MetaAction, action) && !state.AppliedActions.Contains(action.Name))
                        {
                            var newStates = GenerateStates(state, action);
                            foreach (var newState in newStates)
                            {
                                if (!_closedList.Contains(newState))
                                {
                                    var hValue = Heuristic.GetValue(newState);
                                    _bestList.Enqueue(newState, hValue);
                                    _openList.Enqueue(newState, hValue);
                                }
                            }
                        }
                    }
                }

                _haveExhausted = true;
            }

            if (_bestList.Count == 0)
                return null;

            return _bestList.Dequeue().MetaAction;
        }

        private List<MetaActionState> GenerateStates(MetaActionState from, ActionDecl apply)
        {
            var newStates = new List<MetaActionState>();

            apply = apply.Copy();
            var appliedArr = new string[from.AppliedActions.Count];
            from.AppliedActions.CopyTo(appliedArr);
            var applied = appliedArr.ToList();
            applied.Add(apply.Name);

            var actEffect = from.MetaAction.Effects.FindTypes<PredicateExp>();
            foreach (var effect in actEffect)
            {
                var applyRefs = apply.FindTypes<PredicateExp>().Where(x => x.Name == effect.Name).Distinct().ToList();
                foreach (var applyRef in applyRefs)
                {
                    var newApply = apply.Copy();
                    var newMeta = from.MetaAction.Copy();
                    for (int i = 0; i < applyRef.Arguments.Count; i++)
                    {
                        var allRefs = newApply.FindNames(applyRef.Arguments[i].Name);
                        foreach (var refe in allRefs)
                            refe.Name = effect.Arguments[i].Name;
                    }
                    if (newApply.Preconditions is AndExp preAnd && preAnd.Any(x => x.Equals(effect)))
                        continue;
                    foreach (var arg in newApply.Parameters.Values)
                        if (!newMeta.Parameters.Values.Any(x => x.Name == arg.Name))
                            newMeta.Parameters.Values.Add(arg.Copy());
                    if (newMeta.Preconditions is AndExp and && newApply.Preconditions is AndExp and2)
                        foreach (var child in and2.Children)
                            if (!and.Any(x => x.Equals(child)))
                                and.Add(child);
                    //if (newMeta.Effects is AndExp and3 && newApply.Effects is AndExp and4)
                    //    foreach (var child in and4.Children)
                    //        if (!and3.Any(x => x.Equals(child)))
                    //            and3.Add(child);

                    newStates.Add(new MetaActionState(
                        newMeta,
                        applied
                        ));
                }
            }

            return newStates;
        }

        private bool ContainsPred(ActionDecl metaAction, ActionDecl action)
        {
            var metaEffects = metaAction.FindTypes<PredicateExp>();
            var actionEffects = action.FindTypes<PredicateExp>();
            return metaEffects.Any(x => actionEffects.Any(y => y.Name == x.Name));
        }
    }
}
