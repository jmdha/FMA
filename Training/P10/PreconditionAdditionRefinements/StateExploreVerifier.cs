using P10.Verifiers;
using PDDLSharp.CodeGenerators.PDDL;
using PDDLSharp.ErrorListeners;
using PDDLSharp.Models.PDDL;
using PDDLSharp.Models.PDDL.Domain;
using PDDLSharp.Models.PDDL.Expressions;
using PDDLSharp.Models.PDDL.Problem;
using PDDLSharp.Translators.Tools;

namespace P10.PreconditionAdditionRefinements
{
    public class StateExploreVerifier : BaseVerifier
    {
        public enum StateExploreResult { Success, InvariantError, PDDLError, UnknownError, MetaActionValid, TimedOut }
        public static string StateInfoFile = "out";
        public int MaxPreconditionCombinations = 3;
        public int MaxParameters = 0;
        public int TimeLimit = 0;

        public StateExploreVerifier(int maxPreconditionCombinations, int maxParameters, int timeLimit)
        {
            MaxPreconditionCombinations = maxPreconditionCombinations;
            MaxParameters = maxParameters;
            TimeLimit = timeLimit;
            SearchString = $"--search \"state_explorer(optimal_engine=symbolic(plan_reuse_minimal_task_upper_bound=false, plan_reuse_upper_bound=true), upper_bound_pruning=false, max_precondition_size={MaxPreconditionCombinations}, max_parameters={MaxParameters}, exploration_time_limit={TimeLimit})\"";
        }

        public void UpdateSearchString(PDDLDecl from)
        {
            if (from.Problem.Objects == null)
                throw new Exception("Problem objects was null!");
            if (from.Problem.Init == null)
                throw new Exception("Problem init was null!");

            var start = $"--search \"state_explorer(optimal_engine=symbolic(plan_reuse_minimal_task_upper_bound=false, plan_reuse_upper_bound=true), upper_bound_pruning=false, max_precondition_size={MaxPreconditionCombinations}, max_parameters={MaxParameters}, exploration_time_limit={TimeLimit}, ";

            var staticNamesString = "static_names=[";
            var statics = SimpleStaticPredicateDetector.FindStaticPredicates(from);
            foreach (var staticPred in statics)
                staticNamesString += $"{staticPred.Name},";
            if (statics.Count > 0)
                staticNamesString = staticNamesString.Remove(staticNamesString.Length - 1);
            staticNamesString += "], ";

            var staticFactsString = "static_facts=[";
            bool any = false;
            foreach (var staticPred in statics)
            {
                var forThisStatic = "[";
                bool anyInner = false;
                foreach (var init in from.Problem.Init.Predicates)
                {
                    if (init is PredicateExp pred && pred.Name == staticPred.Name)
                    {
                        var items = "[";
                        foreach (var arg in pred.Arguments)
                            items += $"{arg.Name},";
                        items = items.Remove(items.Length - 1);
                        items += "],";
                        forThisStatic += items;
                        anyInner = true;
                        any = true;
                    }
                }
                if (anyInner)
                    forThisStatic = forThisStatic.Remove(forThisStatic.Length - 1);
                forThisStatic += "],";

                staticFactsString += forThisStatic;
            }
            if (any)
                staticFactsString = staticFactsString.Remove(staticFactsString.Length - 1);
            staticFactsString += "], ";

            var typeDict = new Dictionary<string, HashSet<string>>();
            foreach (var obj in from.Problem.Objects.Objs)
            {
                if (typeDict.ContainsKey(obj.Type.Name))
                    typeDict[obj.Type.Name].Add(obj.Name);
                else
                    typeDict.Add(obj.Type.Name, new HashSet<string>() { obj.Name });
                foreach (var subtype in obj.Type.SuperTypes)
                {
                    if (typeDict.ContainsKey(subtype))
                        typeDict[subtype].Add(obj.Name);
                    else
                        typeDict.Add(subtype, new HashSet<string>() { obj.Name });
                }
            }

            var typeNamesString = "type_names=[";
            foreach (var key in typeDict.Keys)
            {
                typeNamesString += $"{key},";
            }
            if (typeDict.Keys.Count > 0)
                typeNamesString = typeNamesString.Remove(typeNamesString.Length - 1);
            typeNamesString += "], ";

            var typeObjectsString = "type_objects=[";
            foreach (var key in typeDict.Keys)
            {
                var forThisType = "[";
                foreach (var item in typeDict[key])
                    forThisType += $"{item},";
                forThisType = forThisType.Remove(forThisType.Length - 1);
                forThisType += "],";
                typeObjectsString += forThisType;
            }
            if (typeDict.Keys.Count > 0)
                typeObjectsString = typeObjectsString.Remove(typeObjectsString.Length - 1);
            typeObjectsString += "]";

            SearchString = $"{start}{staticNamesString}{staticFactsString}{typeNamesString}{typeObjectsString})\"";
        }

        public override bool Verify(DomainDecl domain, ProblemDecl problem, string workingDir, int timeLimitS)
        {
            return VerifyCode(domain, problem, workingDir, timeLimitS) == StateExploreResult.Success;
        }
        public StateExploreResult VerifyCode(DomainDecl domain, ProblemDecl problem, string workingDir, int timeLimitS)
        {
            _domain = domain;
            _problem = problem;
            var listener = new ErrorListener();
            var codeGenerator = new PDDLCodeGenerator(listener);
            var domainFile = Path.Combine(workingDir, $"tempDomain.pddl");
            var problemFile = Path.Combine(workingDir, $"tempProblem.pddl");
            codeGenerator.Generate(domain, domainFile);
            codeGenerator.Generate(problem, problemFile);
            if (File.Exists(Path.Combine(workingDir, StateInfoFile)))
                File.Delete(Path.Combine(workingDir, StateInfoFile));
            if (timeLimitS != -1)
                timeLimitS *= 2;
            var exitCode = ExecutePlanner(domainFile, problemFile, workingDir, timeLimitS);
            if (TimedOut)
                return StateExploreResult.TimedOut;

            if (File.Exists(Path.Combine(workingDir, StateInfoFile)))
                return StateExploreResult.Success;
            else
            {
                if (_log.Contains("There should be no goal defined for a non-attack var! Error in PDDL!"))
                    return StateExploreResult.PDDLError;
                else if (_log.Contains("Mutex type changed to mutex_and because the domain has conditional effects"))
                    return StateExploreResult.InvariantError;
                else if (exitCode == 0)
                    return StateExploreResult.MetaActionValid;
                return StateExploreResult.UnknownError;
            }
        }
    }
}
