using CommandLine;
using PDDLSharp.CodeGenerators;
using PDDLSharp.CodeGenerators.PDDL;
using PDDLSharp.ErrorListeners;
using PDDLSharp.Models.PDDL;
using PDDLSharp.Models.PDDL.Domain;
using PDDLSharp.Models.PDDL.Problem;
using PDDLSharp.Parsers;
using PDDLSharp.Parsers.PDDL;
using System.Diagnostics;
using Tools;

namespace StackelbergCompiler
{
    public class StackelbergCompiler : BaseCLI
    {
        static int Main(string[] args)
        {
            Parser.Default.ParseArguments<Options>(args)
              .WithParsed(RunStackelbergCompiler)
              .WithNotParsed(HandleParseError);
            return 0;
        }

        public static void RunStackelbergCompiler(Options opts)
        {
            Stopwatch watch = new Stopwatch();

            opts.DomainFilePath = PathHelper.RootPath(opts.DomainFilePath);
            opts.ProblemFilePath = PathHelper.RootPath(opts.ProblemFilePath);
            opts.MetaActionFile = PathHelper.RootPath(opts.MetaActionFile);
            opts.OutputPath = PathHelper.RootPath(opts.OutputPath);

            ConsoleHelper.WriteLineColor("Verifying paths...");
            watch.Start();
            if (!Directory.Exists(opts.OutputPath))
                Directory.CreateDirectory(opts.OutputPath);
            if (!File.Exists(opts.DomainFilePath))
                throw new FileNotFoundException($"Domain file not found: {opts.DomainFilePath}");
            if (!File.Exists(opts.ProblemFilePath))
                throw new FileNotFoundException($"Problem file not found: {opts.ProblemFilePath}");
            if (!File.Exists(opts.MetaActionFile))
                throw new FileNotFoundException($"Meta action file not found: {opts.MetaActionFile}");
            watch.Stop();
            ConsoleHelper.WriteLineColor($"Done! [{watch.ElapsedMilliseconds}ms]", ConsoleColor.Green);

            ConsoleHelper.WriteLineColor("Parsing files...");
            watch.Restart();
            IErrorListener listener = new ErrorListener();
            IParser<INode> parser = new PDDLParser(listener);

            var domain = parser.ParseAs<DomainDecl>(new FileInfo(opts.DomainFilePath));
            var problem = parser.ParseAs<ProblemDecl>(new FileInfo(opts.ProblemFilePath));
            var metaAction = parser.ParseAs<ActionDecl>(new FileInfo(opts.MetaActionFile));
            watch.Stop();
            ConsoleHelper.WriteLineColor($"Done! [{watch.ElapsedMilliseconds}ms]", ConsoleColor.Green);

            ConsoleHelper.WriteLineColor("Generating conditional domain/problem...");
            watch.Restart();
            ConditionalEffectCompiler compiler = new ConditionalEffectCompiler();
            var conditionalDecl = compiler.GenerateConditionalEffects(domain, problem, metaAction);
            watch.Stop();
            ConsoleHelper.WriteLineColor($"Done! [{watch.ElapsedMilliseconds}ms]", ConsoleColor.Green);

            ConsoleHelper.WriteLineColor("Generating simplified domain/problem...");
            watch.Restart();
            ConditionalEffectSimplifyer abstractor = new ConditionalEffectSimplifyer();
            var simplifiedConditionalDec = abstractor.SimplifyConditionalEffects(conditionalDecl.Domain, conditionalDecl.Problem);
            watch.Stop();
            ConsoleHelper.WriteLineColor($"Done! [{watch.ElapsedMilliseconds}ms]", ConsoleColor.Green);

            ConsoleHelper.WriteLineColor("Outputting files...");
            watch.Restart();
            ICodeGenerator<INode> generator = new PDDLCodeGenerator(listener);
            generator.Readable = true;

            generator.Generate(conditionalDecl.Domain, Path.Combine(opts.OutputPath, "conditional_domain.pddl"));
            generator.Generate(conditionalDecl.Problem, Path.Combine(opts.OutputPath, "conditional_problem.pddl"));
            generator.Generate(simplifiedConditionalDec.Domain, Path.Combine(opts.OutputPath, "simplified_domain.pddl"));
            generator.Generate(simplifiedConditionalDec.Problem, Path.Combine(opts.OutputPath, "simplified_problem.pddl"));
            watch.Stop();
            ConsoleHelper.WriteLineColor($"Done! [{watch.ElapsedMilliseconds}ms]", ConsoleColor.Green);
        }
    }
}