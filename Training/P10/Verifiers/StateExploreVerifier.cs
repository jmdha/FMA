using PDDLSharp.CodeGenerators.PDDL;
using PDDLSharp.ErrorListeners;
using PDDLSharp.Models.PDDL;
using PDDLSharp.Models.PDDL.Domain;
using PDDLSharp.Models.PDDL.Expressions;
using PDDLSharp.Models.PDDL.Problem;
using PDDLSharp.Translators.StaticPredicateDetectors;

namespace P10.Verifiers
{
    public class StateExploreVerifier : BaseVerifier
    {
        public static string StateInfoFile = "out";
        public static int MaxPreconditionCombinations = 3;
        public static int MaxParameters = 1;

        public StateExploreVerifier()
        {
            SearchString = $"--search \"state_explorer(optimal_engine=symbolic(plan_reuse_minimal_task_upper_bound=false, plan_reuse_upper_bound=true), upper_bound_pruning=false, max_precondition_size={MaxPreconditionCombinations}, max_parameters={MaxParameters})\"";
        }

        public void UpdateSearchString(PDDLDecl from)
        {
            // Until the stackelberg planner works with this
            var start = $"--search \"state_explorer(optimal_engine=symbolic(plan_reuse_minimal_task_upper_bound=false, plan_reuse_upper_bound=true), upper_bound_pruning=false, max_precondition_size={MaxPreconditionCombinations}, max_parameters={MaxParameters}, ";

            var statics = SimpleStaticPredicateDetector.FindStaticPredicates(from);
            var staticsString = "statics=[";
            if (from.Problem.Init != null)
            {
                foreach (var init in from.Problem.Init.Predicates)
                {
                    if (init is PredicateExp pred && statics.Any(x => x.Name == pred.Name))
                    {
                        staticsString += $"{pred.Name}#";
                        var predString = "";
                        foreach (var arg in pred.Arguments)
                            predString += $"{arg.Name}#";
                        predString = predString.Trim();
                        staticsString += $"{predString},";
                    }
                }
            }
            staticsString += "], ";

            var typesString = "types=[";
            if (from.Problem.Objects != null)
            {
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
                foreach (var type in typeDict.Keys)
                {
                    typesString += $"{type}";
                    var objStr = "";
                    foreach (var obj in typeDict[type])
                        objStr += $"#{obj}";
                    objStr = objStr.Trim();
                    typesString += $"{objStr},";
                }
                typesString = typesString.Remove(typesString.Length - 1);
            }
            typesString += "]";

            SearchString = $"{start}{staticsString}{typesString})\"";
        }

        public override bool Verify(DomainDecl domain, ProblemDecl problem, string workingDir, int timeLimitS)
        {
            var listener = new ErrorListener();
            var codeGenerator = new PDDLCodeGenerator(listener);
            var domainFile = Path.Combine(workingDir, $"tempDomain.pddl");
            var problemFile = Path.Combine(workingDir, $"tempProblem.pddl");
            codeGenerator.Generate(domain, domainFile);
            codeGenerator.Generate(problem, problemFile);
            var exitCode = ExecutePlanner(domainFile, problemFile, workingDir, timeLimitS);
            if (exitCode != 0)
                return false;
            if (File.Exists(Path.Combine(workingDir, StateInfoFile)))
                return false;
            return true;
        }
    }
}
