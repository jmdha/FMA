using CommandLine;
using CommandLine.Text;
using CSVToolsSharp;
using FocusedMetaActions.Train.Helpers;
using FocusedMetaActions.Train.MacroExtractor;
using FocusedMetaActions.Train.PreconditionAdditionRefinements;
using FocusedMetaActions.Train.UsefulnessCheckers;
using FocusedMetaActions.Train.Verifiers;
using MetaActionGenerators;
using PDDLSharp.CodeGenerators.PDDL;
using PDDLSharp.Contextualisers.PDDL;
using PDDLSharp.ErrorListeners;
using PDDLSharp.Models.PDDL;
using PDDLSharp.Models.PDDL.Domain;
using PDDLSharp.Models.PDDL.Overloads;
using PDDLSharp.Models.PDDL.Problem;
using PDDLSharp.Parsers.PDDL;

namespace FocusedMetaActions.Train
{
    public class Program
    {
        public static string ID = "";

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
            HandlePaths(opts);

            ID += $"{Enum.GetName(opts.GeneratorOption)}+{Enum.GetName(opts.PreUsefulnessStrategy)}+{Enum.GetName(opts.PostUsefulnessStrategy)}";
            var generalResult = new GeneralResult()
            {
                ID = ID
            };

            ConsoleHelper.WriteLineColor($"Parsing PDDL Files", ConsoleColor.Blue);
            var listener = new ErrorListener();
            var parser = new PDDLParser(listener);
            var contexturalizer = new PDDLContextualiser(listener);
            var domain = parser.ParseAs<DomainDecl>(new FileInfo(opts.DomainPath));
            var problems = new List<ProblemDecl>();
            var usefulnessProblems = new List<ProblemDecl>();
            var problemFiles = new List<FileInfo>();
            foreach (var problem in opts.ProblemsPath)
                problemFiles.Add(new FileInfo(problem));
            foreach (var problem in problemFiles)
                problems.Add(parser.ParseAs<ProblemDecl>(problem));
            if (opts.LastNUsefulness == -1)
                usefulnessProblems = problems;
            else
            {
                usefulnessProblems.AddRange(problems.TakeLast(opts.LastNUsefulness));
                problems.RemoveAll(x => usefulnessProblems.Contains(x));
            }
            if (problems.Count == 0)
                throw new Exception("No problems to train on!");
            if ((opts.PreUsefulnessStrategy != Options.UsefulnessStrategies.None || opts.PostUsefulnessStrategy != Options.UsefulnessStrategies.None) && usefulnessProblems.Count == 0)
                throw new Exception("No problems to perform usefulness checks on!");

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
            ConsoleHelper.WriteLineColor($"\tGenerating with: {Enum.GetName(opts.GeneratorOption)}", ConsoleColor.Magenta);

            // Build Meta Action Generator args
            var args = new Dictionary<string, string>();
            foreach (var keyvalue in opts.Args)
            {
                var key = keyvalue.Substring(0, keyvalue.IndexOf(';')).Trim();
                var value = keyvalue.Substring(keyvalue.IndexOf(';') + 1).Trim();
                args.Add(key, value);
            }

            var generator = MetaGeneratorBuilder.GetGenerator(opts.GeneratorOption, domain, problems, args);
            var newCandidates = generator.GenerateCandidates();
            candidates.AddRange(newCandidates);
            generatorResults.Add(new MetaActionGenerationResult()
            {
                ID = ID,
                Domain = domain.Name!.Name,
                TotalCandidates = newCandidates.Count,
                Generator = $"{Enum.GetName(opts.GeneratorOption)}"
            });
            File.WriteAllText(Path.Combine(opts.OutputPath, "candidates.csv"), CSVSerialiser.Serialise(generatorResults, new CSVSerialiserOptions() { PrettyOutput = true }));
            foreach (var candidiate in candidates)
                codeGenerator.Generate(candidiate, Path.Combine(_candidateOutput, $"{candidiate.Name}.pddl"));
            ConsoleHelper.WriteLineColor($"\tTotal candidates: {candidates.Count}", ConsoleColor.Magenta);
            generalResult.TotalCandidates = candidates.Count;
            ConsoleHelper.WriteLineColor($"Done!", ConsoleColor.Green);

            if (opts.PreUsefulnessStrategy != Options.UsefulnessStrategies.None)
            {
                ConsoleHelper.WriteLineColor($"Pruning for useful meta action candidates", ConsoleColor.Blue);
                var checker = UsefulnessCheckerBuilder.GetUsefulnessChecker(opts.PreUsefulnessStrategy, opts.TempPath, opts.UsefulnessTimeLimitS);
                var preCountt = candidates.Count;
                candidates = checker.GetUsefulCandidates(domain, usefulnessProblems, candidates);
                ConsoleHelper.WriteLineColor($"\tRemoved {preCountt - candidates.Count} candidates", ConsoleColor.Magenta);
                ConsoleHelper.WriteLineColor($"\tTotal candidates: {candidates.Count}", ConsoleColor.Magenta);
                generalResult.PreNotUsefulRemoved = preCountt - candidates.Count;
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
                var refiner = new PreconditionAdditionRefinement(opts.ValidationTimeLimitS, opts.ExplorationTimeLimitS, opts.RefinementTimeLimitS, candidate, opts.TempPath, opts.MaxPreconditionCombinations, opts.MaxAddedParameters, baseDecl);
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

            ConsoleHelper.WriteLineColor($"Pruning for duplicate meta action refined candidates", ConsoleColor.Blue);
            var preCount = refinedCandidates.Count;
            refinedCandidates = refinedCandidates.Distinct(baseDecl.Domain.Actions);
            ConsoleHelper.WriteLineColor($"\tRemoved {preCount - refinedCandidates.Count} refined candidates", ConsoleColor.Magenta);
            ConsoleHelper.WriteLineColor($"\tTotal refined candidates: {refinedCandidates.Count}", ConsoleColor.Magenta);
            generalResult.PostDuplicatesRemoved = preCount - refinedCandidates.Count;
            ConsoleHelper.WriteLineColor($"Done!", ConsoleColor.Green);
            if (opts.PostUsefulnessStrategy != Options.UsefulnessStrategies.None)
            {
                ConsoleHelper.WriteLineColor($"Pruning for useful refined meta action", ConsoleColor.Blue);
                var checker = UsefulnessCheckerBuilder.GetUsefulnessChecker(opts.PostUsefulnessStrategy, opts.TempPath, opts.UsefulnessTimeLimitS);
                var preCountt = refinedCandidates.Count;
                refinedCandidates = checker.GetUsefulCandidates(domain, usefulnessProblems, refinedCandidates);
                ConsoleHelper.WriteLineColor($"\tRemoved {preCountt - refinedCandidates.Count} refined candidates", ConsoleColor.Magenta);
                ConsoleHelper.WriteLineColor($"\tTotal meta actions: {refinedCandidates.Count}", ConsoleColor.Magenta);
                generalResult.PostNotUsefulRemoved = preCountt - refinedCandidates.Count;
                ConsoleHelper.WriteLineColor($"Done!", ConsoleColor.Green);
            }

            ConsoleHelper.WriteLineColor($"Outputting all refined candidates", ConsoleColor.Blue);
            foreach (var refinedCandidate in refinedCandidates)
                codeGenerator.Generate(refinedCandidate, Path.Combine(opts.OutputPath, $"{refinedCandidate.Name}.pddl"));
            ConsoleHelper.WriteLineColor($"Done!", ConsoleColor.Green);

            ConsoleHelper.WriteLineColor($"Outputting enhanced domain", ConsoleColor.Blue);
            var newDomain = domain.Copy();
            newDomain.Actions.AddRange(refinedCandidates);
            codeGenerator.Generate(newDomain, Path.Combine(opts.OutputPath, "enhancedDomain.pddl"));
            ConsoleHelper.WriteLineColor($"Done!", ConsoleColor.Green);

            ConsoleHelper.WriteLineColor($"Generating macro cache for valid meta actions", ConsoleColor.Blue);
            var cacheGenerator = new CacheGenerator();
            PathHelper.RecratePath(Path.Combine(opts.OutputPath, "cache"));
            count = 1;
            foreach (var metaAction in refinedCandidates)
            {
                ConsoleHelper.WriteLineColor($"\tGenerating cache for candidate '{metaAction.Name}' [{count++} out of {refinedCandidates.Count}]", ConsoleColor.Magenta);
                cacheGenerator.GenerateCache(domain, problems, metaAction, opts.TempPath, Path.Combine(opts.OutputPath, "cache"), opts.CacheGenerationTimeLimitS);
            }
            ConsoleHelper.WriteLineColor($"Done!", ConsoleColor.Green);

            File.WriteAllText(Path.Combine(opts.OutputPath, "general.csv"), CSVSerialiser.Serialise(new List<GeneralResult>() { generalResult }));

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

        /// <summary>
        /// Root all paths, make sure that override are valid, etc.
        /// </summary>
        /// <param name="opts"></param>
        /// <exception cref="Exception"></exception>
        /// <exception cref="FileNotFoundException"></exception>
        private static void HandlePaths(Options opts)
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
            opts.ProblemsPath = problemsPath;
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
        }

        private static void HandleParseError(IEnumerable<Error> errs)
        {
            var sentenceBuilder = SentenceBuilder.Create();
            foreach (var error in errs)
                if (error is not HelpRequestedError)
                    ConsoleHelper.WriteLineColor(sentenceBuilder.FormatError(error), ConsoleColor.Red);
        }

        private static void DisplayHelp<T>(ParserResult<T> result, IEnumerable<Error> errs)
        {
            var helpText = HelpText.AutoBuild(result, h =>
            {
                h.AddEnumValuesToHelpText = true;
                return h;
            }, e => e, verbsIndex: true);
            Console.WriteLine(helpText);
            HandleParseError(errs);
        }
    }
}