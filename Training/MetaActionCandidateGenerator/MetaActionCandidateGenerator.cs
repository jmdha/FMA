using CommandLine;
using MetaActionCandidateGenerator.CandidateGenerators;
using PDDLSharp.CodeGenerators.PDDL;
using PDDLSharp.ErrorListeners;
using PDDLSharp.Models.PDDL;
using PDDLSharp.Models.PDDL.Domain;
using PDDLSharp.Parsers.PDDL;
using Tools;
using static MetaActionCandidateGenerator.Options;

namespace MetaActionCandidateGenerator
{
    public class MetaActionCandidateGenerator : BaseCLI
    {
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
            opts.OutputPath = PathHelper.RootPath(opts.OutputPath);
            opts.DomainPath = PathHelper.RootPath(opts.DomainPath);
            opts.ProblemPath = PathHelper.RootPath(opts.ProblemPath);

            if (!File.Exists(opts.DomainPath))
                throw new FileNotFoundException($"Domain file not found: {opts.DomainPath}");
            if (!File.Exists(opts.ProblemPath))
                throw new FileNotFoundException($"Problem file not found: {opts.ProblemPath}");
            ConsoleHelper.WriteLineColor($"Done!", ConsoleColor.Green);

            ConsoleHelper.WriteLineColor($"Parsing PDDL Files", ConsoleColor.Blue);
            var listener = new ErrorListener();
            var parser = new PDDLParser(listener);
            var pddlDecl = parser.ParseDecl(new FileInfo(opts.DomainPath), new FileInfo(opts.ProblemPath));
            ConsoleHelper.WriteLineColor($"Done!", ConsoleColor.Green);

            ConsoleHelper.WriteLineColor($"Generating Candidates", ConsoleColor.Blue);
            var candidates = GetMetaActionCandidates(pddlDecl, opts.GeneratorStrategy);
            ConsoleHelper.WriteLineColor($"Done!", ConsoleColor.Green);

            ConsoleHelper.WriteLineColor($"Outputting Files", ConsoleColor.Blue);
            PathHelper.RecratePath(opts.OutputPath);
            var codeGenerator = new PDDLCodeGenerator(listener);
            foreach (var candidate in candidates)
                codeGenerator.Generate(candidate, Path.Combine(opts.OutputPath, $"{candidate.Name}.pddl"));
            ConsoleHelper.WriteLineColor($"Done!", ConsoleColor.Green);
        }

        private static ICandidateGenerator GetGenerator(GeneratorStrategies strategy)
        {
            switch (strategy)
            {
                case GeneratorStrategies.PredicateMetaActions: return new PredicateMetaActions();
                case GeneratorStrategies.StrippedMetaActions: return new StrippedMetaActions();
                default: throw new Exception("Unknown generator strategy!");
            }
        }

        public static List<ActionDecl> GetMetaActionCandidates(PDDLDecl pddlDecl, GeneratorStrategies strategy)
        {
            var generator = GetGenerator(strategy);
            var candidates = generator.GenerateCandidates(pddlDecl);
            foreach (var candidate in candidates)
                candidate.Name = $"${candidate.Name}";
            return candidates;
        }
    }
}
