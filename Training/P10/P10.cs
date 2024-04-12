using CommandLine;
using CSVToolsSharp;
using MetaActionCandidateGenerator.CandidateGenerators;
using P10.PreconditionAdditionRefinements;
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
        private static int _returnCode = 0;

        private static int Main(string[] args)
        {
            var parser = new Parser(with => with.HelpWriter = null);
            var parserResult = parser.ParseArguments<Options>(args);
            parserResult.WithNotParsed(errs => DisplayHelp(parserResult, errs));
            parserResult.WithParsed(Run);
            return _returnCode;
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
                ExternalPaths.FastDownwardPath = opts.FastDownwardPath;
            }
            if (!File.Exists(ExternalPaths.FastDownwardPath))
                throw new FileNotFoundException($"Fast Downward path not found: {opts.FastDownwardPath}");
            if (opts.StackelbergPath != "")
            {
                opts.StackelbergPath = PathHelper.RootPath(opts.StackelbergPath);
                ExternalPaths.StackelbergPath = opts.StackelbergPath;
            }
            if (!File.Exists(ExternalPaths.StackelbergPath))
                throw new FileNotFoundException($"Stackelberg Planner path not found: {opts.StackelbergPath}");

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
            if (opts.GeneratorStrategies.Contains(GeneratorStrategies.CPDDLInvariantMetaActions))
            {
                if (opts.CPDDLPath != "")
                {
                    opts.CPDDLPath = PathHelper.RootPath(opts.CPDDLPath);
                    CPDDLInvariantMetaActions.CPDDLExecutable = opts.CPDDLPath;
                }
                if (!File.Exists(CPDDLInvariantMetaActions.CPDDLExecutable))
                    throw new FileNotFoundException($"CPDDL executable not found: {CPDDLInvariantMetaActions.CPDDLExecutable}");
                CPDDLInvariantMetaActions.TempFolder = Path.Combine(opts.TempPath, "cpddl");
            }
            ConsoleHelper.WriteLineColor($"Done!", ConsoleColor.Green);

            var generalResult = new P10Result();

            ConsoleHelper.WriteLineColor($"Parsing PDDL Files", ConsoleColor.Blue);
            var listener = new ErrorListener();
            var parser = new PDDLParser(listener);
            var contexturalizer = new PDDLContextualiser(listener);
            var domain = parser.ParseAs<DomainDecl>(new FileInfo(opts.DomainPath));
            var problems = new List<ProblemDecl>();
            foreach (var problem in opts.ProblemsPath)
                problems.Add(parser.ParseAs<ProblemDecl>(new FileInfo(problem)));
            var baseDecl = new PDDLDecl(domain, problems[problems.Count - 1]);
            contexturalizer.Contexturalise(baseDecl);
            generalResult.Domain = domain.Name!.Name;
            generalResult.Problems = problems.Count;
            ConsoleHelper.WriteLineColor($"Done!", ConsoleColor.Green);

            ConsoleHelper.WriteLineColor($"Generating Initial Candidates", ConsoleColor.Blue);
            var candidates = new List<ActionDecl>();
            var codeGenerator = new PDDLCodeGenerator(listener);
            codeGenerator.Readable = true;
            var generatorResults = new List<MetaActionGenerationResult>();
            foreach (var generator in opts.GeneratorStrategies)
            {
                ConsoleHelper.WriteLineColor($"\tGenerating with: {Enum.GetName(typeof(GeneratorStrategies), generator)}", ConsoleColor.Magenta);
                var newCandidates = CandidateGeneratorBuilder.GetGenerator(generator).GenerateCandidates(baseDecl);
                candidates.AddRange(newCandidates);
                generatorResults.Add(new MetaActionGenerationResult()
                {
                    Domain = domain.Name!.Name,
                    TotalCandidates = newCandidates.Count,
                    Generator = $"{Enum.GetName(typeof(GeneratorStrategies), generator)}"
                });
            }
            File.WriteAllText(Path.Combine(opts.OutputPath, "candidates.csv"), CSVSerialiser.Serialise(generatorResults, new CSVSerialiserOptions() { PrettyOutput = true }));
            foreach (var candidiate in candidates)
                codeGenerator.Generate(candidiate, Path.Combine(_candidateOutput, $"{candidiate.Name}.pddl"));
            ConsoleHelper.WriteLineColor($"\tTotal candidates: {candidates.Count}", ConsoleColor.Magenta);
            generalResult.TotalCandidates = candidates.Count;
            ConsoleHelper.WriteLineColor($"Done!", ConsoleColor.Green);

            if (opts.RemoveDuplicates)
            {
                ConsoleHelper.WriteLineColor($"Pruning for duplicate meta action candidates", ConsoleColor.Blue);
                var preCount = candidates.Count;
                candidates = candidates.Distinct(baseDecl.Domain.Actions);
                ConsoleHelper.WriteLineColor($"\tRemoved {preCount - candidates.Count} candidates", ConsoleColor.Magenta);
                ConsoleHelper.WriteLineColor($"\tTotal candidates: {candidates.Count}", ConsoleColor.Magenta);
                generalResult.PreDuplicatesRemoved = preCount - candidates.Count;
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
                generalResult.PreNotUsefulRemoved = preCount - candidates.Count;
                ConsoleHelper.WriteLineColor($"Done!", ConsoleColor.Green);
            }

            ConsoleHelper.WriteLineColor($"Begining refinement process", ConsoleColor.Blue);
            int count = 1;
            var refinedCandidates = new List<ActionDecl>();
            var refinementResults = new List<RefinementResult>();
            foreach (var candidate in candidates)
            {
                ConsoleHelper.WriteLineColor($"\tCandidate: {count++} out of {candidates.Count}", ConsoleColor.Magenta);
                ConsoleHelper.WriteLineColor($"", ConsoleColor.Magenta);
                ConsoleHelper.WriteLineColor($"{codeGenerator.Generate(candidate)}", ConsoleColor.Cyan);
                ConsoleHelper.WriteLineColor($"", ConsoleColor.Magenta);
                var refiner = new PreconditionAdditionRefinement(opts.TimeLimitS, candidate, opts.TempPath, opts.OutputPath, opts.MaxPreconditionCombinations, opts.MaxAddedParameters, opts.LearningCache);
                var refinedResult = refiner.Refine(domain, problems);
                refinementResults.Add(refinedResult);
                if (refinedResult.RefinedMetaActions.Count > 0)
                {
                    ConsoleHelper.WriteLineColor($"\tCandidate have been refined!", ConsoleColor.Green);
                    refinedCandidates.AddRange(refinedResult.RefinedMetaActions);
                }
                else
                    ConsoleHelper.WriteLineColor($"\tCandidate could not be refined!", ConsoleColor.Red);
                ConsoleHelper.WriteLineColor($"", ConsoleColor.Magenta);
            }
            File.WriteAllText(Path.Combine(opts.OutputPath, "refinement.csv"), CSVSerialiser.Serialise(refinementResults));
            generalResult.TotalRefinedCandidates = refinedCandidates.Count;
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
                generalResult.PostDuplicatesRemoved = preCount - refinedCandidates.Count;
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
                generalResult.PostNotUsefulRemoved = preCount - refinedCandidates.Count;
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

            File.WriteAllText(Path.Combine(opts.OutputPath, "general.csv"), CSVSerialiser.Serialise(new List<P10Result>() { generalResult }));

            ConsoleHelper.WriteLineColor($"Final Report:", ConsoleColor.Blue);
            ConsoleHelper.WriteLineColor($"General Results:", ConsoleColor.Blue);
            ConsoleHelper.WriteLineColor($"{generalResult}", ConsoleColor.DarkGreen);
            ConsoleHelper.WriteLineColor($"Generators Results:", ConsoleColor.Blue);
            foreach (var genResult in generatorResults)
                ConsoleHelper.WriteLineColor($"{genResult}", ConsoleColor.DarkGreen);
            ConsoleHelper.WriteLineColor($"Refinement Results:", ConsoleColor.Blue);
            foreach (var refResult in refinementResults)
                ConsoleHelper.WriteLineColor($"{refResult}", ConsoleColor.DarkGreen);

            if (refinedCandidates.Count == 0)
                _returnCode = 1;
        }
    }
}