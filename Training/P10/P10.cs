using CommandLine;
using P10.RefinementStrategies.ActionPrecondition;
using P10.RefinementStrategies.GroundedPredicateAdditions;
using PDDLSharp.CodeGenerators.PDDL;
using PDDLSharp.Contextualisers.PDDL;
using PDDLSharp.ErrorListeners;
using PDDLSharp.Models.PDDL;
using PDDLSharp.Models.PDDL.Domain;
using PDDLSharp.Models.PDDL.Problem;
using PDDLSharp.Parsers.PDDL;
using Tools;

namespace P10
{
    public class P10 : BaseCLI
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
            var problemsPath = opts.ProblemsPath.ToList();
            if (problemsPath.Count == 0)
                throw new Exception("No problem files where given!");

            opts.OutputPath = PathHelper.RootPath(opts.OutputPath);
            opts.DomainPath = PathHelper.RootPath(opts.DomainPath);
            for (int i = 0; i < problemsPath.Count; i++)
                problemsPath[i] = PathHelper.RootPath(problemsPath[i]);

            if (!File.Exists(opts.DomainPath))
                throw new FileNotFoundException($"Domain file not found: {opts.DomainPath}");
            foreach (var problem in opts.ProblemsPath)
                if (!File.Exists(problem))
                    throw new FileNotFoundException($"Problem file not found: {problem}");

            PathHelper.RecratePath(opts.OutputPath);
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
            var candidates = MetaActionCandidateGenerator.MetaActionCandidateGenerator.GetMetaActionCandidates(baseDecl, opts.GeneratorStrategy);
            ConsoleHelper.WriteLineColor($"\tTotal candidates: {candidates.Count}", ConsoleColor.Magenta);
            ConsoleHelper.WriteLineColor($"Done!", ConsoleColor.Green);

            ConsoleHelper.WriteLineColor($"Begining refinement process", ConsoleColor.Blue);
            var refinedCandidates = new List<ActionDecl>();
            int count = 1;
            foreach (var candidate in candidates)
            {
                ConsoleHelper.WriteLineColor($"\tCandidate: {count++} out of {candidates.Count}", ConsoleColor.Magenta);
                var refiner = new MetaActionRefiner(candidate, GetRefinementStrategy(opts.RefinementStrategy));
                if (refiner.Refine(baseDecl, domain, problems))
                {
                    refinedCandidates.Add(refiner.RefinedMetaActionCandidate);
                    ConsoleHelper.WriteLineColor($"\tCandidate have been refined!", ConsoleColor.Magenta);
                }
                else
                    ConsoleHelper.WriteLineColor($"\tCandidate could not be refined!", ConsoleColor.Magenta);
            }
            ConsoleHelper.WriteLineColor($"Done!", ConsoleColor.Green);

            ConsoleHelper.WriteLineColor($"Outputting refined candidates", ConsoleColor.Blue);
            var codeGenerator = new PDDLCodeGenerator(listener);
            codeGenerator.Readable = true;
            foreach (var candidate in refinedCandidates)
                codeGenerator.Generate(candidate, Path.Combine(opts.OutputPath, $"{candidate.Name}.pddl"));
            ConsoleHelper.WriteLineColor($"Done!", ConsoleColor.Green);

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
                case Options.RefinementStrategies.ActionPrecondition: return new ActionPreconditionRefinement();
                case Options.RefinementStrategies.GroundedPredicateAdditions: return new GroundedPredicateAdditionsRefinement();
                default: throw new Exception("Unknown strategy!");
            }
        }
    }
}