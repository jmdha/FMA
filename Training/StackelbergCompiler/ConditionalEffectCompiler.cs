using PDDLSharp.Models.PDDL;
using PDDLSharp.Models.PDDL.Domain;
using PDDLSharp.Models.PDDL.Expressions;
using PDDLSharp.Models.PDDL.Problem;

namespace StackelbergCompiler
{
    public class ConditionalEffectCompiler
    {
        public PDDLDecl GenerateConditionalEffects(DomainDecl domain, ProblemDecl problem, ActionDecl metaAction)
        {
            var newDomain = domain.Copy();
            var newProblem = problem.Copy();

            // Find static predicates
            StaticPredicateDetector.GenerateStaticPredicates(domain);

            // Generate total goal
            TotalGoalGenerator.GenerateTotalGoal(newProblem, newDomain);

            // Problem
            InsertConstantsIntoObjects(newDomain, newProblem);
            GenerateNewInits(newProblem);
            GenerateNewGoal(newProblem);
            InsertTurnPredicateIntoInit(newProblem);

            // Domain
            RemoveConstants(newDomain);
            SplitActionsIntoLeaderFollowerVariants(newDomain);
            InsertAndIntoAllActions(newDomain);
            GenerateDomainPredicates(newDomain);
            UpdateLeaderActionsPredicatesAndEffects(newDomain);
            InsertConditionalEffectsToFollowerActions(newDomain);
            UpdateAndInsertMetaActionToFit(newDomain, metaAction);
            InsertTurnPredicateIntoActionsPreconditions(newDomain);
            InsertTurnPredicateIntoPredicates(newDomain);

            return new PDDLDecl(newDomain, newProblem);
        }

        #region Problem

        /// <summary>
        /// Simply treat constants from the domain as objects in the problem
        /// </summary>
        /// <param name="domain"></param>
        /// <param name="problem"></param>
        private void InsertConstantsIntoObjects(DomainDecl domain, ProblemDecl problem)
        {
            if (domain.Constants != null && problem.Objects != null)
                foreach (var cons in domain.Constants.Constants)
                    problem.Objects.Objs.Add(cons);
        }

        /// <summary>
        /// Insert the `leader-turn` predicate to the goal init
        /// </summary>
        /// <param name="problem"></param>
        private void InsertTurnPredicateIntoInit(ProblemDecl problem)
        {
            if (problem.Init != null)
                problem.Init.Predicates.Add(
                    new PredicateExp(ReservedNames.LeaderTurnPredicate));
        }

        /// <summary>
        /// Insert leader prefix predicates and copy them into the existing problem inits.
        /// Also fills the init with the total goal predicates.
        /// </summary>
        /// <param name="problem"></param>
        private void GenerateNewInits(ProblemDecl problem)
        {
            if (problem.Init != null)
            {
                var initPredicates = problem.Init.FindTypes<PredicateExp>();
                problem.Init.Predicates.AddRange(GeneratePrefixPredicates(initPredicates, ReservedNames.LeaderStatePrefix));
                problem.Init.Predicates.AddRange(TotalGoalGenerator.CopyTotalGoal());
            }
        }

        /// <summary>
        /// Replace the existing goal expression with the total goal predicates.
        /// </summary>
        /// <param name="problem"></param>
        private void GenerateNewGoal(ProblemDecl problem)
        {
            if (problem.Goal != null)
            {
                var goals = TotalGoalGenerator.CopyTotalGoal();
                problem.Goal.GoalExp = new AndExp(goals.Cast<IExp>().ToList());
            }
        }

        #endregion

        #region Domain

        /// <summary>
        /// Remove constants from the domain, since they have been added to the problem instead
        /// </summary>
        /// <param name="domain"></param>
        private void RemoveConstants(DomainDecl domain)
        {
            if (domain.Constants != null)
                domain.Constants = null;
        }

        /// <summary>
        /// Insert the `leader-turn` predicate to the domain predicates
        /// </summary>
        /// <param name="problem"></param>
        private void InsertTurnPredicateIntoPredicates(DomainDecl domain)
        {
            if (domain.Predicates != null)
                domain.Predicates.Predicates.Add(
                    new PredicateExp(ReservedNames.LeaderTurnPredicate));
        }

        /// <summary>
        /// Convert all actions' Preconditions and Effects into having a root 'AndExp' (this makes some things easier later on)
        /// </summary>
        /// <param name="domain"></param>
        private void InsertAndIntoAllActions(DomainDecl domain)
        {
            foreach (var act in domain.Actions)
                InsertAndIntoAction(act);
        }
        private void InsertAndIntoAction(ActionDecl act)
        {
            if (act.Effects is not AndExp)
                act.Effects = new AndExp(new List<IExp>()
                    {
                        act.Effects
                    });
            if (act.Preconditions is not AndExp)
                act.Preconditions = new AndExp(new List<IExp>()
                    {
                        act.Preconditions
                    });
        }

        /// <summary>
        /// Inserts the the `leader-turn` into the "fix" actions and the `not (leader-turn)` into the "attack" actions
        /// </summary>
        /// <param name="domain"></param>
        private void InsertTurnPredicateIntoActionsPreconditions(DomainDecl domain)
        {
            foreach (var action in domain.Actions)
            {
                if (action.Name.StartsWith(ReservedNames.LeaderActionPrefix))
                {
                    if (action.Preconditions is AndExp leaderPreconditions)
                        leaderPreconditions.Children.Add(new PredicateExp(ReservedNames.LeaderTurnPredicate));
                }
                else if (action.Name.StartsWith(ReservedNames.FollowerActionPrefix))
                {
                    if (action.Preconditions is AndExp leaderPreconditions)
                        leaderPreconditions.Children.Add(new NotExp(new PredicateExp(ReservedNames.LeaderTurnPredicate)));
                }
            }
        }

        /// <summary>
        /// Insert the meta action, and update its preconditions and effects
        /// </summary>
        /// <param name="domain"></param>
        /// <param name="metaAction"></param>
        private void UpdateAndInsertMetaActionToFit(DomainDecl domain, ActionDecl metaAction)
        {
            InsertAndIntoAction(metaAction);

            // Update preconditions
            PrefixPredicatesInNode(
                metaAction.Preconditions.FindTypes<PredicateExp>(),
                ReservedNames.LeaderStatePrefix);

            // Update effects
            List<IExp> newChildren = new List<IExp>();
            var effects = metaAction.Effects.FindTypes<PredicateExp>();
            foreach (var effect in effects)
            {
                if (!StaticPredicateDetector.StaticPredicates.Contains(effect.Name))
                {
                    if (effect.Parent is NotExp not)
                    {
                        var newNormalEffect = effect.Copy();
                        newNormalEffect.Name = $"{ReservedNames.LeaderStatePrefix}{newNormalEffect.Name}";
                        newChildren.Add(new NotExp(newNormalEffect));
                    }
                    else
                    {
                        var newNormalEffect = effect.Copy();
                        newNormalEffect.Name = $"{ReservedNames.LeaderStatePrefix}{newNormalEffect.Name}";
                        newChildren.Add(newNormalEffect);
                    }

                    var newGoalEffect = effect.Copy();
                    newGoalEffect.Name = $"{ReservedNames.IsGoalPrefix}{newGoalEffect.Name}";
                    newChildren.Add(new NotExp(newGoalEffect));
                }
            }
            newChildren.Add(new NotExp(new PredicateExp(ReservedNames.LeaderTurnPredicate)));
            metaAction.Effects = new AndExp(newChildren);

            // Update name
            metaAction.Name = $"{ReservedNames.LeaderActionPrefix}{metaAction.Name}";
            domain.Actions.Add(metaAction);
        }

        /// <summary>
        /// Updates the leader actions to contain both the follower and leader effects.
        /// Only the leader preconditions are present, since the two states follow each other.
        /// </summary>
        /// <param name="domain"></param>
        private void UpdateLeaderActionsPredicatesAndEffects(DomainDecl domain)
        {
            foreach (var action in domain.Actions.Where(x => x.Name.StartsWith(ReservedNames.LeaderActionPrefix)))
            {
                // Preconditions
                PrefixPredicatesInNode(
                    action.Preconditions.FindTypes<PredicateExp>(),
                    ReservedNames.LeaderStatePrefix);

                // Effects
                var leaderEffect = action.Effects.Copy();
                var followerEffect = action.Effects.Copy();
                PrefixPredicatesInNode(
                    leaderEffect.FindTypes<PredicateExp>(),
                    ReservedNames.LeaderStatePrefix);

                List<IExp> newChildren = new List<IExp>();
                if (leaderEffect is AndExp leaderAnd)
                    newChildren.AddRange(leaderAnd.Children);
                if (followerEffect is AndExp followerAnd)
                    newChildren.AddRange(followerAnd.Children);
                action.Effects = new AndExp(newChildren);
            }
        }

        /// <summary>
        /// Adds conditional effects to all the follower actions.
        /// Basically takes and says if the leader is having the same predicate, then the "is-goal-" variant is also sets to true.
        /// </summary>
        /// <param name="domain"></param>
        private void InsertConditionalEffectsToFollowerActions(DomainDecl domain)
        {
            foreach (var action in domain.Actions.Where(x => x.Name.StartsWith(ReservedNames.FollowerActionPrefix)))
            {
                var newExpressions = new List<IExp>();
                var predicates = action.Effects.FindTypes<PredicateExp>();
                foreach (var pred in predicates)
                {
                    if (pred.Parent is NotExp)
                    {
                        var trueWhen = new WhenExp(action);
                        trueWhen.Condition = new NotExp(trueWhen, new PredicateExp(
                            trueWhen,
                            $"{ReservedNames.LeaderStatePrefix}{pred.Name}",
                            pred.Arguments));
                        trueWhen.Effect = new PredicateExp(
                            trueWhen,
                            $"{ReservedNames.IsGoalPrefix}{pred.Name}",
                            pred.Arguments);
                        newExpressions.Add(trueWhen);

                        var falseWhen = new WhenExp(action);
                        falseWhen.Condition = new PredicateExp(
                            falseWhen,
                            $"{ReservedNames.LeaderStatePrefix}{pred.Name}",
                            pred.Arguments);
                        falseWhen.Effect = new NotExp(new PredicateExp(
                            falseWhen,
                            $"{ReservedNames.IsGoalPrefix}{pred.Name}",
                            pred.Arguments));
                        newExpressions.Add(falseWhen);
                    }
                    else
                    {
                        var trueWhen = new WhenExp(action);
                        trueWhen.Condition = new PredicateExp(
                            trueWhen,
                            $"{ReservedNames.LeaderStatePrefix}{pred.Name}",
                            pred.Arguments);
                        trueWhen.Effect = new PredicateExp(
                            trueWhen,
                            $"{ReservedNames.IsGoalPrefix}{pred.Name}",
                            pred.Arguments);
                        newExpressions.Add(trueWhen);

                        var falseWhen = new WhenExp(action);
                        falseWhen.Condition = new NotExp(new PredicateExp(
                            falseWhen,
                            $"{ReservedNames.LeaderStatePrefix}{pred.Name}",
                            pred.Arguments));
                        falseWhen.Effect = new NotExp(new PredicateExp(
                            falseWhen,
                            $"{ReservedNames.IsGoalPrefix}{pred.Name}",
                            pred.Arguments));
                        newExpressions.Add(falseWhen);
                    }
                }

                if (action.Effects is AndExp and)
                    and.Children.AddRange(newExpressions);
            }
        }

        /// <summary>
        /// Generates all the leader state predicates and is-goal predicates
        /// </summary>
        /// <param name="domain"></param>
        private void GenerateDomainPredicates(DomainDecl domain)
        {
            if (domain.Predicates != null)
            {
                var newPredicates = new List<PredicateExp>();
                newPredicates.AddRange(GeneratePrefixPredicates(domain.Predicates.Predicates, ReservedNames.LeaderStatePrefix));
                newPredicates.AddRange(GeneratePrefixPredicates(domain.Predicates.Predicates, ReservedNames.IsGoalPrefix));
                domain.Predicates.Predicates.AddRange(newPredicates);
            }
        }

        /// <summary>
        /// Takes all actions and copies them into a leader and follower variant
        /// </summary>
        /// <param name="domain"></param>
        private void SplitActionsIntoLeaderFollowerVariants(DomainDecl domain)
        {
            List<ActionDecl> newActions = new List<ActionDecl>();
            foreach (var action in domain.Actions)
            {
                var leaderAct = action.Copy();
                leaderAct.Name = $"{ReservedNames.LeaderActionPrefix}{leaderAct.Name}";
                var followerAct = action.Copy();
                followerAct.Name = $"{ReservedNames.FollowerActionPrefix}{followerAct.Name}";
                newActions.Add(leaderAct);
                newActions.Add(followerAct);
            }
            domain.Actions = newActions;
        }

        #endregion

        /// <summary>
        /// Takes a list of predicates, and "copies" them but with a prefix.
        /// </summary>
        /// <param name="predicates"></param>
        /// <param name="prefix"></param>
        /// <returns></returns>
        private List<PredicateExp> GeneratePrefixPredicates(List<PredicateExp> predicates, string prefix)
        {
            var copyPredicates = new List<PredicateExp>();
            foreach (var pred in predicates)
                if (!StaticPredicateDetector.StaticPredicates.Contains(pred.Name))
                    copyPredicates.Add(pred.Copy());
            PrefixPredicatesInNode(copyPredicates, prefix);
            return copyPredicates;
        }

        /// <summary>
        /// Takes a list of predicates and prefixes them
        /// </summary>
        /// <param name="predicates"></param>
        /// <param name="prefix"></param>
        private void PrefixPredicatesInNode(List<PredicateExp> predicates, string prefix)
        {
            foreach (var predicate in predicates)
                if (!StaticPredicateDetector.StaticPredicates.Contains(predicate.Name))
                    predicate.Name = $"{prefix}{predicate.Name}";
        }
    }
}
