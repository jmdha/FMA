using CommandLine;
using CSVToolsSharp;
using MetaActionCandidateGenerator.CandidateGenerators;
using P10.RefinementStrategies;
using P10.UsefulnessCheckers;
using P10.Verifiers;
using PDDLSharp.CodeGenerators.PDDL;
using PDDLSharp.Contextualisers.PDDL;
using PDDLSharp.ErrorListeners;
using PDDLSharp.Models.PDDL;
using PDDLSharp.Models.PDDL.Domain;
using PDDLSharp.Models.PDDL.Overloads;
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

        public static void Run(Options opts)
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
                ExternalPaths.FastDownwardPath = opts.FastDownwardPath;
            }
            if (opts.StackelbergPath != "")
            {
                opts.StackelbergPath = PathHelper.RootPath(opts.StackelbergPath);
                if (!File.Exists(opts.StackelbergPath))
                    throw new FileNotFoundException($"Stackelberg Planner path not found: {opts.StackelbergPath}");
                ExternalPaths.StackelbergPath = opts.StackelbergPath;
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
            BaseVerifier.ShowSTDOut = opts.StackelbergDebug;
            ConsoleHelper.WriteLineColor($"Done!", ConsoleColor.Green);

            var csv = new CSVManager(new FileInfo(Path.Combine(opts.OutputPath, "general.csv")));

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
            csv.AppendToFile("domain", domain.Name!.Name);
            csv.AppendToFile("problems", $"{problems.Count}");
            ConsoleHelper.WriteLineColor($"Done!", ConsoleColor.Green);

            ConsoleHelper.WriteLineColor($"Generating Initial Candidates", ConsoleColor.Blue);
            var candidates = new List<ActionDecl>();
            var codeGenerator = new PDDLCodeGenerator(listener);
            codeGenerator.Readable = true;
            var candidateCSV = new CSVManager(new FileInfo(Path.Combine(opts.OutputPath, "candidates.csv")));
            foreach (var generator in opts.GeneratorStrategies)
            {
                ConsoleHelper.WriteLineColor($"\tGenerating with: {Enum.GetName(typeof(GeneratorStrategies), generator)}", ConsoleColor.Magenta);
                var newCandidates = CandidateGeneratorBuilder.GetGenerator(generator).GenerateCandidates(baseDecl);
                candidates.AddRange(newCandidates);
                candidateCSV.AppendToFile("domain", domain.Name!.Name);
                candidateCSV.AppendToFile("generator", $"{Enum.GetName(typeof(GeneratorStrategies), generator)}");
                candidateCSV.AppendToFile("candidates", $"{newCandidates.Count}");
                candidateCSV.Context.Row++;
            }
            foreach (var candidiate in candidates)
                codeGenerator.Generate(candidiate, Path.Combine(_candidateOutput, $"{candidiate.Name}.pddl"));
            ConsoleHelper.WriteLineColor($"\tTotal candidates: {candidates.Count}", ConsoleColor.Magenta);
            csv.AppendToFile($"total_candidates", $"{candidates.Count}");
            ConsoleHelper.WriteLineColor($"Done!", ConsoleColor.Green);

            if (opts.RemoveDuplicates)
            {
                ConsoleHelper.WriteLineColor($"Pruning for duplicate meta action candidates", ConsoleColor.Blue);
                var preCount = candidates.Count;
                candidates = candidates.Distinct(baseDecl.Domain.Actions);
                ConsoleHelper.WriteLineColor($"\tRemoved {preCount - candidates.Count} candidates", ConsoleColor.Magenta);
                ConsoleHelper.WriteLineColor($"\tTotal candidates: {candidates.Count}", ConsoleColor.Magenta);
                csv.AppendToFile($"pre_duplicates_removed", $"{preCount - candidates.Count}");
                ConsoleHelper.WriteLineColor($"Done!", ConsoleColor.Green);
            }

            if (opts.PreCheckUsefullness)
            {
                ConsoleHelper.WriteLineColor($"Pruning for useful meta action candidates", ConsoleColor.Blue);
                var checker = UsefulnessCheckerBuilder.GetUsefulnessChecker(opts.UsefulnessStrategy, opts.TempPath);
                var preCount = candidates.Count;
                candidates = checker.GetUsefulCandidates(domain, problems, candidates);
                ConsoleHelper.WriteLineColor($"\tRemoved {preCount - candidates.Count} candidates", ConsoleColor.Magenta);
                ConsoleHelper.WriteLineColor($"\tTotal candidates: {candidates.Count}", ConsoleColor.Magenta);
                csv.AppendToFile($"pre_not_useful", $"{preCount - candidates.Count}");
                ConsoleHelper.WriteLineColor($"Done!", ConsoleColor.Green);
            }

            ConsoleHelper.WriteLineColor($"Begining refinement process", ConsoleColor.Blue);
            int count = 1;
            var index = 0;
            var refinedCandidates = new List<ActionDecl>();
            foreach (var candidate in candidates)
            {
                ConsoleHelper.WriteLineColor($"\tCandidate: {count++} out of {candidates.Count}", ConsoleColor.Magenta);
                ConsoleHelper.WriteLineColor($"", ConsoleColor.Magenta);
                ConsoleHelper.WriteLineColor($"{codeGenerator.Generate(candidate)}", ConsoleColor.Cyan);
                ConsoleHelper.WriteLineColor($"", ConsoleColor.Magenta);
                var refiner = RefinementStrategyBuilder.GetStrategy(opts.RefinementStrategy, opts.TimeLimitS, candidate, index++, opts.TempPath, opts.OutputPath);
                var refined = refiner.Refine(domain, problems);
                if (refined.Count > 0)
                {
                    ConsoleHelper.WriteLineColor($"\tCandidate have been refined!", ConsoleColor.Green);
                    refinedCandidates.AddRange(refined);
                }
                else
                    ConsoleHelper.WriteLineColor($"\tCandidate could not be refined!", ConsoleColor.Red);
                ConsoleHelper.WriteLineColor($"", ConsoleColor.Magenta);
            }
            csv.AppendToFile($"total_refined", $"{refinedCandidates.Count}");
            ConsoleHelper.WriteLineColor($"\tTotal refined candidates: {refinedCandidates.Count}", ConsoleColor.Magenta);
            // Make sure names are unique
            while (refinedCandidates.DistinctBy(x => x.Name).Count() != refinedCandidates.Count)
            {
                foreach (var action in refinedCandidates)
                {
                    var others = refinedCandidates.Where(x => x.Name == action.Name);
                    int counter = 0;
                    foreach (var other in others)
                        if (action != other)
                            other.Name = $"{other.Name}_{counter++}";
                }
            }
            ConsoleHelper.WriteLineColor($"Done!", ConsoleColor.Green);

            if (opts.RemoveDuplicates)
            {
                ConsoleHelper.WriteLineColor($"Pruning for duplicate meta action refined candidates", ConsoleColor.Blue);
                var preCount = refinedCandidates.Count;
                refinedCandidates = refinedCandidates.Distinct(baseDecl.Domain.Actions);
                ConsoleHelper.WriteLineColor($"\tRemoved {preCount - refinedCandidates.Count} refined candidates", ConsoleColor.Magenta);
                ConsoleHelper.WriteLineColor($"\tTotal refined candidates: {refinedCandidates.Count}", ConsoleColor.Magenta);
                csv.AppendToFile($"post_duplicates_removed", $"{preCount - refinedCandidates.Count}");
                ConsoleHelper.WriteLineColor($"Done!", ConsoleColor.Green);
            }
            if (opts.PostCheckUsefullness)
            {
                ConsoleHelper.WriteLineColor($"Pruning for useful refined meta action", ConsoleColor.Blue);
                var checker = UsefulnessCheckerBuilder.GetUsefulnessChecker(opts.UsefulnessStrategy, opts.TempPath);
                var preCount = refinedCandidates.Count;
                refinedCandidates = checker.GetUsefulCandidates(domain, problems, refinedCandidates);
                ConsoleHelper.WriteLineColor($"\tRemoved {preCount - refinedCandidates.Count} refined candidates", ConsoleColor.Magenta);
                ConsoleHelper.WriteLineColor($"\tTotal meta actions: {refinedCandidates.Count}", ConsoleColor.Magenta);
                csv.AppendToFile($"post_not_useful", $"{preCount - refinedCandidates.Count}");
                ConsoleHelper.WriteLineColor($"Done!", ConsoleColor.Green);
            }

            ConsoleHelper.WriteLineColor($"Outputting all refined candidates", ConsoleColor.Magenta);
            foreach (var refinedCandidate in refinedCandidates)
                codeGenerator.Generate(refinedCandidate, Path.Combine(opts.OutputPath, $"{refinedCandidate.Name}.pddl"));
            ConsoleHelper.WriteLineColor($"Done!", ConsoleColor.Green);

            ConsoleHelper.WriteLineColor($"Outputting enhanced domain", ConsoleColor.Blue);
            var newDomain = domain.Copy();
            newDomain.Actions.AddRange(refinedCandidates);
            codeGenerator.Generate(newDomain, Path.Combine(opts.OutputPath, "enhancedDomain.pddl"));
            ConsoleHelper.WriteLineColor($"Done!", ConsoleColor.Green);
        }
    }
}