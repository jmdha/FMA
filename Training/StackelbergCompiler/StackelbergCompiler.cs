using CommandLine;
using PDDLSharp.CodeGenerators;
using PDDLSharp.CodeGenerators.PDDL;
using PDDLSharp.ErrorListeners;
using PDDLSharp.Models.PDDL;
using PDDLSharp.Models.PDDL.Domain;
using PDDLSharp.Models.PDDL.Problem;
using PDDLSharp.Parsers;
using PDDLSharp.Parsers.PDDL;
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
            opts.DomainFilePath = PathHelper.RootPath(opts.DomainFilePath);
            opts.ProblemFilePath = PathHelper.RootPath(opts.ProblemFilePath);
            opts.MetaActionFile = PathHelper.RootPath(opts.MetaActionFile);
            opts.OutputPath = PathHelper.RootPath(opts.OutputPath);

            ConsoleHelper.WriteLineColor("Verifying paths...");
            if (!Directory.Exists(opts.OutputPath))
                Directory.CreateDirectory(opts.OutputPath);
            if (!File.Exists(opts.DomainFilePath))
                throw new FileNotFoundException($"Domain file not found: {opts.DomainFilePath}");
            if (!File.Exists(opts.ProblemFilePath))
                throw new FileNotFoundException($"Problem file not found: {opts.ProblemFilePath}");
            if (!File.Exists(opts.MetaActionFile))
                throw new FileNotFoundException($"Meta action file not found: {opts.MetaActionFile}");
            ConsoleHelper.WriteLineColor($"Done!", ConsoleColor.Green);

            ConsoleHelper.WriteLineColor("Parsing files...");
            IErrorListener listener = new ErrorListener();
            IParser<INode> parser = new PDDLParser(listener);

            var domain = parser.ParseAs<DomainDecl>(new FileInfo(opts.DomainFilePath));
            var problem = parser.ParseAs<ProblemDecl>(new FileInfo(opts.ProblemFilePath));
            var metaAction = parser.ParseAs<ActionDecl>(new FileInfo(opts.MetaActionFile));
            ConsoleHelper.WriteLineColor($"Done!", ConsoleColor.Green);

            ConsoleHelper.WriteLineColor("Generating domain/problem...");
            var simplifiedConditionalDec = CompileToStackelberg(new PDDLDecl(domain, problem), metaAction);
            ConsoleHelper.WriteLineColor($"Done!", ConsoleColor.Green);

            ConsoleHelper.WriteLineColor("Outputting files...");
            ICodeGenerator<INode> generator = new PDDLCodeGenerator(listener);
            generator.Readable = true;

            generator.Generate(simplifiedConditionalDec.Domain, Path.Combine(opts.OutputPath, "simplified_domain.pddl"));
            generator.Generate(simplifiedConditionalDec.Problem, Path.Combine(opts.OutputPath, "simplified_problem.pddl"));
            ConsoleHelper.WriteLineColor($"Done!", ConsoleColor.Green);
        }

        public static PDDLDecl CompileToStackelberg(PDDLDecl pddlDecl, ActionDecl metaAction)
        {
            ConditionalEffectCompiler compiler = new ConditionalEffectCompiler();
            var conditionalDecl = compiler.GenerateConditionalEffects(pddlDecl.Domain, pddlDecl.Problem, metaAction);
            ConditionalEffectSimplifyer abstractor = new ConditionalEffectSimplifyer();
            return abstractor.SimplifyConditionalEffects(conditionalDecl.Domain, conditionalDecl.Problem);
        }
    }
}