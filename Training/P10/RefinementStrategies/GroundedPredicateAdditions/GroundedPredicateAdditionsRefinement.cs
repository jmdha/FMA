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
                new hMostValid(),
                new hMostApplicable(),
                new hMustBeApplicable()
            });
        }

        public ActionDecl? Refine(DomainDecl domain, List<ProblemDecl> problems, ActionDecl currentMetaAction, ActionDecl originalMetaAction, string workingDir)
        {
            if (!_isInitialized)
            {
                ConsoleHelper.WriteLineColor($"\t\tInitial state space exploration started...", ConsoleColor.Magenta);
                _isInitialized = true;
                var pddlDecl = new PDDLDecl(domain, problems[0]);
                var compiled = StackelbergCompiler.StackelbergCompiler.CompileToStackelberg(pddlDecl, originalMetaAction.Copy());

                //AddParameterPredicates(compiled, originalMetaAction, workingDir);

                var verifier = new StateExploreVerifier();
                if (File.Exists(Path.Combine(workingDir, StateExploreVerifier.StateInfoFile)))
                    File.Delete(Path.Combine(workingDir, StateExploreVerifier.StateInfoFile));
                verifier.Verify(compiled.Domain, compiled.Problem, workingDir);
                if (!UpdateOpenList(originalMetaAction, workingDir))
                    return null;
                ConsoleHelper.WriteLineColor($"\t\tExploration finished", ConsoleColor.Magenta);
                _watch.Start();
            }
            if (_openList.Count == 0)
                return null;

            ConsoleHelper.WriteLineColor($"\t\t{_openList.Count} possibilities left [Est. {TimeSpan.FromMilliseconds((double)_openList.Count * ((double)(_watch.ElapsedMilliseconds + 1) / (double)(1 + (_initialPossibilities - _openList.Count)))).ToString("hh\\:mm\\:ss")} until finished]", ConsoleColor.Magenta);
            var state = _openList.Dequeue();
            ConsoleHelper.WriteLineColor($"\t\tBest Validity: {Math.Round((((double)state.ValidStates - (double)state.InvalidStates) / (double)state.ValidStates) * 100, 2)}%", ConsoleColor.Magenta);
            ConsoleHelper.WriteLineColor($"\t\tBest Applicability: {Math.Round(((double)state.Applicability / ((double)state.ValidStates + (double)state.InvalidStates)) * 100, 2)}%", ConsoleColor.Magenta);
#if DEBUG
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

        private void AddParameterPredicates(PDDLDecl compiled, ActionDecl originalMetaAction, string workingDir)
        {
            var parameterPredicates = new List<PredicateExp>();
            var count = 0;
            foreach (var parameter in originalMetaAction.Parameters.Values)
                parameterPredicates.Add(new PredicateExp($"param{count++}", new List<NameExp>() { parameter.Copy() }));

            if (compiled.Domain.Predicates != null)
                foreach (var parameter in parameterPredicates)
                    compiled.Domain.Predicates.Add(parameter);
            var metaAction = compiled.Domain.Actions.First(x => x.Name == $"fix_{originalMetaAction.Name}");
            if (metaAction != null && metaAction.Effects is AndExp and)
                foreach (var parameter in parameterPredicates)
                    and.Add(parameter);

            CheckIfTranslatorRemovedParameterFacts(compiled, parameterPredicates, workingDir);
        }

        private void CheckIfTranslatorRemovedParameterFacts(PDDLDecl compiled, List<PredicateExp> parameterPredicates, string workingDir)
        {
            workingDir = Path.Combine(workingDir, "translator-test");
            PathHelper.RecratePath(workingDir);
            var listener = new ErrorListener();
            var codeGenerator = new PDDLCodeGenerator(listener);
            var domainFile = Path.Combine(workingDir, $"tempDomain.pddl");
            var problemFile = Path.Combine(workingDir, $"tempProblem.pddl");
            codeGenerator.Generate(compiled.Domain, domainFile);
            codeGenerator.Generate(compiled.Problem, problemFile);

            using (ArgsCaller fdCaller = new ArgsCaller("python2"))
            {
                fdCaller.StdOut += (s, o) => { };
                fdCaller.StdErr += (s, o) => { };
                fdCaller.Arguments.Add(BaseVerifier.StackelbergPath, "");
                fdCaller.Arguments.Add(domainFile, "");
                fdCaller.Arguments.Add(problemFile, "");
                fdCaller.Process.StartInfo.WorkingDirectory = workingDir;
                fdCaller.Run();
                var sasFile = new FileInfo(Path.Combine(workingDir, "output.sas"));
                if (!sasFile.Exists)
                    throw new Exception("Stackelberg translator failed!");
                var sasParser = new FDSASParser(listener);
                var parsed = sasParser.ParseAs<SASDecl>(sasFile);
                if (parsed != null)
                {
                    foreach (var parameter in parameterPredicates)
                        if (!parsed.Variables.Any(x => x.SymbolicNames.Any(y => y.Contains(parameter.Name))))
                            throw new Exception($"Translator removed parameter predicate '{parameter.Name}'!");
                }
                else
                    throw new Exception("Exception while parsing SAS file");
            }
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
            var validStates = Convert.ToInt32(lines[0]);
            for (int i = 2; i < lines.Count; i += 3)
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

                var newState = new PreconditionState(validStates, invalidStates, applicability, metaAction, preconditions);
                var hValue = Heuristic.GetValue(newState);
                if (hValue != int.MaxValue)
                    _openList.Enqueue(newState, hValue);
            }
            _initialPossibilities = _openList.Count;

            return true;
        }
    }
}
