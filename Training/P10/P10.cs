using CommandLine;
using P10.RefinementStrategies;
using P10.RefinementStrategies.GroundedPredicateAdditions;
using P10.Verifiers;
using PDDLSharp.CodeGenerators.PDDL;
using PDDLSharp.Contextualisers.PDDL;
using PDDLSharp.ErrorListeners;
using PDDLSharp.Models.PDDL;
using PDDLSharp.Models.PDDL.Domain;
using PDDLSharp.Models.PDDL.Problem;
using PDDLSharp.Parsers.PDDL;
using Tools;
using static MetaActionCandidateGenerator.Options;

namespace P10
{
    public class P10 : BaseCLI
    {
        private static string _candidateOutput = "initial-candidates";

        private static void Main(string[] args)
        {
            var parser = new Parser(with => with.HelpWriter = null);
            var parserResult = parser.ParseArguments<Options>(args);
            parserResult.WithNotParsed(errs => DisplayHelp(parserResult, errs));
            parserResult.WithParsed(Run);
        }

        private static void Run(Options opts)
        {
            ConsoleHelper.WriteLineColor($"Checking files", ConsoleColor.Blue);
            var problemsPath = opts.ProblemsPath.ToList();
            if (problemsPath.Count == 0)
                throw new Exception("No problem files where given!");

            if (opts.FastDownwardPath != "")
            {
                opts.FastDownwardPath = PathHelper.RootPath(opts.FastDownwardPath);
                if (!File.Exists(opts.FastDownwardPath))
                    throw new FileNotFoundException($"Fast Downward path not found: {opts.FastDownwardPath}");
                UsefullnessChecker.FastDownwardPath = opts.FastDownwardPath;
            }
            if (opts.StackelbergPath != "")
            {
                opts.StackelbergPath = PathHelper.RootPath(opts.StackelbergPath);
                if (!File.Exists(opts.FastDownwardPath))
                    throw new FileNotFoundException($"Stackelberg Planner path not found: {opts.StackelbergPath}");
                BaseVerifier.StackelbergPath = opts.StackelbergPath;
            }

            opts.OutputPath = PathHelper.RootPath(opts.OutputPath);
            opts.TempPath = PathHelper.RootPath(opts.TempPath);
            opts.DomainPath = PathHelper.RootPath(opts.DomainPath);
            for (int i = 0; i < problemsPath.Count; i++)
                problemsPath[i] = PathHelper.RootPath(problemsPath[i]);
            _candidateOutput = Path.Combine(opts.TempPath, _candidateOutput);

            if (!File.Exists(opts.DomainPath))
                throw new FileNotFoundException($"Domain file not found: {opts.DomainPath}");
            foreach (var problem in opts.ProblemsPath)
                if (!File.Exists(problem))
                    throw new FileNotFoundException($"Problem file not found: {problem}");

            PathHelper.RecratePath(opts.OutputPath);
            PathHelper.RecratePath(opts.TempPath);
            PathHelper.RecratePath(_candidateOutput);
            ConsoleHelper.WriteLineColor($"Done!", ConsoleColor.Green);

            ConsoleHelper.WriteLineColor($"Parsing PDDL Files", ConsoleColor.Blue);
            var listener = new ErrorListener();
            var parser = new PDDLParser(listener);
            var contexturalizer = new PDDLContextualiser(listener);
            var domain = parser.ParseAs<DomainDecl>(new FileInfo(opts.DomainPath));
            var problems = new List<ProblemDecl>();
            foreach (var problem in opts.ProblemsPath)
                problems.Add(parser.ParseAs<ProblemDecl>(new FileInfo(problem)));
            var baseDecl = new PDDLDecl(domain, problems[0]);
            contexturalizer.Contexturalise(baseDecl);
            ConsoleHelper.WriteLineColor($"Done!", ConsoleColor.Green);

            ConsoleHelper.WriteLineColor($"Generating Initial Candidates", ConsoleColor.Blue);
            var candidates = new List<ActionDecl>();
            var codeGenerator = new PDDLCodeGenerator(listener);
            codeGenerator.Readable = true;
            foreach (var generator in opts.GeneratorStrategies)
            {
                ConsoleHelper.WriteLineColor($"\tGenerating with: {Enum.GetName(typeof(GeneratorStrategies), generator)}", ConsoleColor.Magenta);
                candidates.AddRange(MetaActionCandidateGenerator.MetaActionCandidateGenerator.GetMetaActionCandidates(baseDecl, generator));
            }
            foreach(var candidiate in candidates)
                codeGenerator.Generate(candidiate, Path.Combine(_candidateOutput, $"{candidiate.Name}.pddl"));
            ConsoleHelper.WriteLineColor($"\tTotal candidates: {candidates.Count}", ConsoleColor.Magenta);
            ConsoleHelper.WriteLineColor($"Done!", ConsoleColor.Green);

            if (opts.PreCheckUsefullness)
            {
                ConsoleHelper.WriteLineColor($"Pruning for useful meta action candidates", ConsoleColor.Blue);
                var checker = new UsefullnessChecker();
                candidates = checker.GetUsefulCandidates(domain, problems, candidates);
                ConsoleHelper.WriteLineColor($"\tTotal candidates: {candidates.Count}", ConsoleColor.Magenta);
                ConsoleHelper.WriteLineColor($"Done!", ConsoleColor.Green);
            }

            ConsoleHelper.WriteLineColor($"Begining refinement process", ConsoleColor.Blue);
            int count = 1;
            var refinedCandidates = new List<ActionDecl>();
            foreach (var candidate in candidates)
            {
                ConsoleHelper.WriteLineColor($"\tCandidate: {count++} out of {candidates.Count}", ConsoleColor.Magenta);
                ConsoleHelper.WriteLineColor($"", ConsoleColor.Magenta);
                ConsoleHelper.WriteLineColor($"{codeGenerator.Generate(candidate)}", ConsoleColor.Cyan);
                ConsoleHelper.WriteLineColor($"", ConsoleColor.Magenta);
                var refiner = new MetaActionRefiner(candidate, GetRefinementStrategy(opts.RefinementStrategy), opts.TempPath);
                var refined = refiner.Refine(domain, problems);
                if (refined.Count > 0)
                {
                    ConsoleHelper.WriteLineColor($"\tCandidate have been refined!", ConsoleColor.Green);
                    refinedCandidates.AddRange(refined);

                    ConsoleHelper.WriteLineColor($"\tOutputting refined candidate", ConsoleColor.Magenta);
                    foreach(var refinedCandidate in refined)
                        codeGenerator.Generate(refinedCandidate, Path.Combine(opts.OutputPath, $"{refinedCandidate.Name}.pddl"));
                    ConsoleHelper.WriteLineColor($"\tDone!", ConsoleColor.Green);
                }
                else
                    ConsoleHelper.WriteLineColor($"\tCandidate could not be refined!", ConsoleColor.Red);
                ConsoleHelper.WriteLineColor($"", ConsoleColor.Magenta);
            }
            ConsoleHelper.WriteLineColor($"Done!", ConsoleColor.Green);

            if (opts.PostCheckUsefullness)
            {
                ConsoleHelper.WriteLineColor($"Pruning for useful refined meta action", ConsoleColor.Blue);
                var checker = new UsefullnessChecker();
                refinedCandidates = checker.GetUsefulCandidates(domain, problems, refinedCandidates);
                ConsoleHelper.WriteLineColor($"\tTotal meta actions: {refinedCandidates.Count}", ConsoleColor.Magenta);
                ConsoleHelper.WriteLineColor($"Done!", ConsoleColor.Green);
            }

            ConsoleHelper.WriteLineColor($"Outputting enhanced domain", ConsoleColor.Blue);
            var newDomain = domain.Copy();
            newDomain.Actions.AddRange(refinedCandidates);
            codeGenerator.Generate(newDomain, Path.Combine(opts.OutputPath, "enhancedDomain.pddl"));
            ConsoleHelper.WriteLineColor($"Done!", ConsoleColor.Green);
        }

        private static IRefinementStrategy GetRefinementStrategy(Options.RefinementStrategies strategy)
        {
            switch (strategy)
            {
                case Options.RefinementStrategies.GroundedPredicateAdditions: return new GroundedPredicateAdditionsRefinement();
                default: throw new Exception("Unknown strategy!");
            }
        }
    }
}