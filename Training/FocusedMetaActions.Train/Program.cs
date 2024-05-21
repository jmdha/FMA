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
using PDDLSharp.ErrorListeners;
using PDDLSharp.Models.PDDL.Domain;
using PDDLSharp.Models.PDDL.Overloads;
using PDDLSharp.Models.PDDL.Problem;
using PDDLSharp.Parsers.PDDL;
using System.Diagnostics;

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

            generalResult.Domain = domain.Name!.Name;
            generalResult.Problems = problems.Count;
            ConsoleHelper.WriteLineColor($"Done!", ConsoleColor.Green);

            var candidates = GenerateInitialMetaActions(
                opts.GeneratorOption,
                domain,
                problems,
                opts.Args.ToList());
            generalResult.TotalCandidates = candidates.Count;
            var generatorResult = new MetaActionGenerationResult()
            {
                ID = ID,
                Domain = domain.Name!.Name,
                TotalCandidates = candidates.Count,
                Generator = $"{Enum.GetName(opts.GeneratorOption)}"
            };

            if (opts.PreUsefulnessStrategy != Options.UsefulnessStrategies.None)
            {
                var postPruning = UsefulnessPruning(
                    opts.TempPath,
                    opts.PreUsefulnessStrategy,
                    domain,
                    usefulnessProblems,
                    candidates,
                    opts.UsefulnessTimeLimitS);
                generalResult.PreNotUsefulRemoved = candidates.Count - postPruning.Count;
                candidates = postPruning;
            }

            ConsoleHelper.WriteLineColor($"Begining refinement process", ConsoleColor.Blue);
            var codeGenerator = new PDDLCodeGenerator(listener);
            codeGenerator.Readable = true;
            int count = 1;
            var refinedCandidates = new List<ActionDecl>();
            var refinementResults = new List<RefinementResult>();
            var watch = new Stopwatch();
            watch.Start();
            foreach (var candidate in candidates)
            {
                if (opts.SkipRefinement)
                {
                    ConsoleHelper.WriteLineColor($"\tRefinement skipped...", ConsoleColor.Yellow);
                    refinedCandidates.AddRange(candidates);
                    break;
                }

                ConsoleHelper.WriteLineColor($"\tCandidate: {count++} out of {candidates.Count}", ConsoleColor.Magenta);
                ConsoleHelper.WriteLineColor($"", ConsoleColor.Magenta);
                ConsoleHelper.WriteLineColor($"{codeGenerator.Generate(candidate)}", ConsoleColor.Cyan);
                ConsoleHelper.WriteLineColor($"", ConsoleColor.Magenta);
                var refiner = new PreconditionAdditionRefinement(opts.ValidationTimeLimitS, opts.ExplorationTimeLimitS, opts.RefinementTimeLimitS, candidate, opts.TempPath, opts.MaxPreconditionCombinations, opts.MaxAddedParameters);
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
                if (opts.TotalRefinementTimeLimitS > 0 && watch.ElapsedMilliseconds / 1000 > opts.TotalRefinementTimeLimitS)
                {
                    watch.Stop();
                    ConsoleHelper.WriteLineColor($"\tTotal refinement time limit reached!", ConsoleColor.Yellow);
                    break;
                }
            }
            generalResult.TotalRefinedCandidates = refinedCandidates.Count;
            ConsoleHelper.WriteLineColor($"\tTotal refined candidates: {refinedCandidates.Count}", ConsoleColor.Magenta);
            refinedCandidates = EnsureUniqueNames(refinedCandidates);
            ConsoleHelper.WriteLineColor($"Done!", ConsoleColor.Green);

            var preCount = refinedCandidates.Count;
            refinedCandidates = RemoveDuplicates(
                domain,
                refinedCandidates);
            generalResult.PostDuplicatesRemoved = preCount - refinedCandidates.Count;

            if (opts.PostUsefulnessStrategy != Options.UsefulnessStrategies.None)
            {
                var postPruning = UsefulnessPruning(
                    opts.TempPath,
                    opts.PostUsefulnessStrategy,
                    domain,
                    usefulnessProblems,
                    refinedCandidates,
                    opts.UsefulnessTimeLimitS);
                generalResult.PostNotUsefulRemoved = refinedCandidates.Count - postPruning.Count;
                refinedCandidates = postPruning;
            }

            OutputRefinedCandidates(
                opts.OutputPath,
                refinedCandidates);

            OutputEnhancedDomain(
                opts.OutputPath,
                domain,
                refinedCandidates);

            if (!opts.SkipMacroCache)
                GenerateMacroCache(
                    Path.Combine(opts.OutputPath, "cache"),
                    opts.TempPath,
                    refinedCandidates,
                    domain,
                    problems,
                    opts.CacheGenerationTimeLimitS);

            File.WriteAllText(Path.Combine(opts.OutputPath, "candidates.csv"), CSVSerialiser.Serialise(new List<MetaActionGenerationResult>() { generatorResult }));
            File.WriteAllText(Path.Combine(opts.OutputPath, "general.csv"), CSVSerialiser.Serialise(new List<GeneralResult>() { generalResult }));
            File.WriteAllText(Path.Combine(opts.OutputPath, "refinement.csv"), CSVSerialiser.Serialise(refinementResults));

            if (opts.PostValidityCheck)
                PostValidityCheck(
                    Path.Combine(opts.TempPath, "post-validity"),
                    domain,
                    problems,
                    refinedCandidates,
                    opts.ValidationTimeLimitS);

            PrintFinalReport(generalResult, generatorResult, refinementResults);

            if (opts.RemoveTempOnFinish && Directory.Exists(opts.TempPath))
                Directory.Delete(opts.TempPath, true);

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

        /// <summary>
        /// Generate initial meta action candidates
        /// </summary>
        /// <param name="generatorOption"></param>
        /// <param name="domain"></param>
        /// <param name="problems"></param>
        /// <param name="generatorArgs"></param>
        /// <returns></returns>
        private static List<ActionDecl> GenerateInitialMetaActions(MetaGeneratorBuilder.GeneratorOptions generatorOption, DomainDecl domain, List<ProblemDecl> problems, List<string> generatorArgs)
        {
            ConsoleHelper.WriteLineColor($"Generating Initial Candidates", ConsoleColor.Blue);
            var candidates = new List<ActionDecl>();
            var listener = new ErrorListener();
            var codeGenerator = new PDDLCodeGenerator(listener);
            codeGenerator.Readable = true;
            ConsoleHelper.WriteLineColor($"\tGenerating with: {Enum.GetName(generatorOption)}", ConsoleColor.Magenta);

            // Build Meta Action Generator args
            var args = new Dictionary<string, string>();
            foreach (var keyvalue in generatorArgs)
            {
                var key = keyvalue.Substring(0, keyvalue.IndexOf(';')).Trim();
                var value = keyvalue.Substring(keyvalue.IndexOf(';') + 1).Trim();
                args.Add(key, value);
            }

            var generator = MetaGeneratorBuilder.GetGenerator(generatorOption, domain, problems, args);
            var newCandidates = generator.GenerateCandidates();
            candidates.AddRange(newCandidates);
            foreach (var candidiate in candidates)
                codeGenerator.Generate(candidiate, Path.Combine(_candidateOutput, $"{candidiate.Name}.pddl"));
            ConsoleHelper.WriteLineColor($"\tTotal candidates: {candidates.Count}", ConsoleColor.Magenta);
            ConsoleHelper.WriteLineColor($"Done!", ConsoleColor.Green);
            return candidates;
        }

        /// <summary>
        /// Final print of all results
        /// </summary>
        /// <param name="generalResult"></param>
        /// <param name="generatorResults"></param>
        /// <param name="refinementResults"></param>
        private static void PrintFinalReport(GeneralResult generalResult, MetaActionGenerationResult generatorResult, List<RefinementResult> refinementResults)
        {
            ConsoleHelper.WriteLineColor($"Final Report:", ConsoleColor.Blue);
            ConsoleHelper.WriteLineColor($"General Results:", ConsoleColor.Blue);
            ConsoleHelper.WriteLineColor($"{generalResult}", ConsoleColor.DarkGreen);
            ConsoleHelper.WriteLineColor($"Generator Result:", ConsoleColor.Blue);
            ConsoleHelper.WriteLineColor($"{generatorResult}", ConsoleColor.DarkGreen);
            ConsoleHelper.WriteLineColor($"Refinement Results:", ConsoleColor.Blue);
            foreach (var refResult in refinementResults)
                ConsoleHelper.WriteLineColor($"{refResult}", ConsoleColor.DarkGreen);
        }

        /// <summary>
        /// Generate macro cache for valid meta actions for the reconstruction process.
        /// </summary>
        /// <param name="targetPath"></param>
        /// <param name="tempPath"></param>
        /// <param name="refinedCandidates"></param>
        /// <param name="domain"></param>
        /// <param name="problems"></param>
        /// <param name="timeLimit"></param>
        private static void GenerateMacroCache(string targetPath, string tempPath, List<ActionDecl> refinedCandidates, DomainDecl domain, List<ProblemDecl> problems, int timeLimit)
        {
            ConsoleHelper.WriteLineColor($"Generating macro cache for valid meta actions", ConsoleColor.Blue);
            var cacheGenerator = new CacheGenerator();
            PathHelper.RecratePath(targetPath);
            var count = 1;
            foreach (var metaAction in refinedCandidates)
            {
                ConsoleHelper.WriteLineColor($"\tGenerating cache for candidate '{metaAction.Name}' [{count++} out of {refinedCandidates.Count}]", ConsoleColor.Magenta);
                cacheGenerator.GenerateCache(domain, problems, metaAction, tempPath, targetPath, timeLimit);
            }
            ConsoleHelper.WriteLineColor($"Done!", ConsoleColor.Green);
        }

        /// <summary>
        /// Outputs the refined candidates
        /// </summary>
        /// <param name="targetPath"></param>
        /// <param name="refinedCandidates"></param>
        private static void OutputRefinedCandidates(string targetPath, List<ActionDecl> refinedCandidates)
        {
            ConsoleHelper.WriteLineColor($"Outputting all refined candidates", ConsoleColor.Blue);
            var listener = new ErrorListener();
            var codeGenerator = new PDDLCodeGenerator(listener);
            foreach (var refinedCandidate in refinedCandidates)
                codeGenerator.Generate(refinedCandidate, Path.Combine(targetPath, $"{refinedCandidate.Name}.pddl"));
            ConsoleHelper.WriteLineColor($"Done!", ConsoleColor.Green);
        }

        /// <summary>
        /// Output an enhanced version of the original domain, with the valid meta actions in it
        /// </summary>
        /// <param name="targetPath"></param>
        /// <param name="domain"></param>
        /// <param name="refinedCandidates"></param>
        private static void OutputEnhancedDomain(string targetPath, DomainDecl domain, List<ActionDecl> refinedCandidates)
        {
            ConsoleHelper.WriteLineColor($"Outputting enhanced domain", ConsoleColor.Blue);
            var newDomain = domain.Copy();
            newDomain.Actions.AddRange(refinedCandidates);
            var listener = new ErrorListener();
            var codeGenerator = new PDDLCodeGenerator(listener);
            codeGenerator.Generate(newDomain, Path.Combine(targetPath, "enhancedDomain.pddl"));
            ConsoleHelper.WriteLineColor($"Done!", ConsoleColor.Green);
        }

        /// <summary>
        /// Perform usefulness pruning on a set of meta action candidates.
        /// </summary>
        /// <param name="tempPath"></param>
        /// <param name="strategy"></param>
        /// <param name="domain"></param>
        /// <param name="problems"></param>
        /// <param name="refinedCandidates"></param>
        /// <param name="timeLimit"></param>
        /// <returns></returns>
        private static List<ActionDecl> UsefulnessPruning(string tempPath, Options.UsefulnessStrategies strategy, DomainDecl domain, List<ProblemDecl> problems, List<ActionDecl> refinedCandidates, int timeLimit)
        {
            ConsoleHelper.WriteLineColor($"Pruning for useful meta actions", ConsoleColor.Blue);
            var checker = UsefulnessCheckerBuilder.GetUsefulnessChecker(strategy, tempPath, timeLimit);
            var preCount = refinedCandidates.Count;
            var newCandidates = checker.GetUsefulCandidates(domain, problems, refinedCandidates);
            ConsoleHelper.WriteLineColor($"\tRemoved {preCount - newCandidates.Count} refined candidates", ConsoleColor.Magenta);
            ConsoleHelper.WriteLineColor($"\tTotal meta actions: {newCandidates.Count}", ConsoleColor.Magenta);
            ConsoleHelper.WriteLineColor($"Done!", ConsoleColor.Green);
            return newCandidates;
        }

        /// <summary>
        /// Remove equivalent meta actions
        /// </summary>
        /// <param name="domain"></param>
        /// <param name="candidates"></param>
        /// <returns></returns>
        private static List<ActionDecl> RemoveDuplicates(DomainDecl domain, List<ActionDecl> candidates)
        {
            ConsoleHelper.WriteLineColor($"Pruning for duplicate meta action refined candidates", ConsoleColor.Blue);
            var preCount = candidates.Count;
            var newCandidates = candidates.Distinct(domain.Actions);
            ConsoleHelper.WriteLineColor($"\tRemoved {preCount - newCandidates.Count} refined candidates", ConsoleColor.Magenta);
            ConsoleHelper.WriteLineColor($"\tTotal refined candidates: {newCandidates.Count}", ConsoleColor.Magenta);
            ConsoleHelper.WriteLineColor($"Done!", ConsoleColor.Green);
            return newCandidates;
        }

        /// <summary>
        /// Ensures that all names of the candidates are unique (so we dont overwrite output files)
        /// </summary>
        /// <param name="candidates"></param>
        /// <returns></returns>
        private static List<ActionDecl> EnsureUniqueNames(List<ActionDecl> candidates)
        {
            while (candidates.DistinctBy(x => x.Name).Count() != candidates.Count)
            {
                foreach (var action in candidates)
                {
                    var others = candidates.Where(x => x.Name == action.Name);
                    int counter = 0;
                    foreach (var other in others)
                        if (action != other)
                            other.Name = $"{other.Name}_{counter++}";
                }
            }
            return candidates;
        }

        /// <summary>
        /// Before finishing, do a final check of validity on the final candidates.
        /// If an invalid one is found, the program will continue like normal, but will write a warning to stdout
        /// </summary>
        /// <param name="tempPath"></param>
        /// <param name="domain"></param>
        /// <param name="problems"></param>
        /// <param name="candidates"></param>
        private static void PostValidityCheck(string tempPath, DomainDecl domain, List<ProblemDecl> problems, List<ActionDecl> candidates, int timeLimit)
        {
            ConsoleHelper.WriteLineColor($"Post validity check started", ConsoleColor.Blue);
            PathHelper.RecratePath(tempPath);

            int count = 1;
            foreach(var candidate in candidates)
            {
                ConsoleHelper.WriteLineColor($"\tChecking candidate {count++} out of {candidates.Count}", ConsoleColor.Magenta);
                var result = VerificationHelper.IsValid(domain, problems, candidate, tempPath, timeLimit);
                if (result == FrontierVerifier.FrontierResult.Invalid)
                    ConsoleHelper.WriteLineColor($"\t\tCandidate '{candidate.Name}' is invalid!", ConsoleColor.Red);
                else if (result == FrontierVerifier.FrontierResult.Inapplicable)
                    ConsoleHelper.WriteLineColor($"\t\tCandidate '{candidate.Name}' is inapplicable!", ConsoleColor.Yellow);
                else if (result == FrontierVerifier.FrontierResult.Valid)
                    ConsoleHelper.WriteLineColor($"\t\tCandidate '{candidate.Name}' is valid!", ConsoleColor.Green);
            }

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