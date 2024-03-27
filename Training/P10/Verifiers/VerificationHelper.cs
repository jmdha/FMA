using PDDLSharp.CodeGenerators.PDDL;
using PDDLSharp.ErrorListeners;
using PDDLSharp.Models.PDDL;
using PDDLSharp.Models.PDDL.Domain;
using PDDLSharp.Models.PDDL.Problem;

namespace P10.Verifiers
{
    public static class VerificationHelper
    {
        public static bool IsValid(DomainDecl domain, List<ProblemDecl> problems, ActionDecl metaAction, string workingDir, int timeLimitS, string cachePath)
        {
            var code = GetDeterministicHashCode(domain, problems, metaAction, timeLimitS);
            if (File.Exists(Path.Combine(cachePath, $"{code}-valid.txt")))
                return true;
            if (File.Exists(Path.Combine(cachePath, $"{code}-invalid.txt")))
                return false;

            var verifier = new FrontierVerifier();
            foreach (var problem in problems)
            {
                var compiled = StackelbergCompiler.StackelbergCompiler.CompileToStackelberg(new PDDLDecl(domain, problem), metaAction.Copy());
                if (!verifier.Verify(compiled.Domain, compiled.Problem, workingDir, timeLimitS))
                {
                    File.Create(Path.Combine(cachePath, $"{code}-invalid.txt"));
                    return false;
                }
            }
            File.Create(Path.Combine(cachePath, $"{code}-valid.txt"));
            return true;
        }

        private static Dictionary<INode, string> _textCache = new Dictionary<INode, string>();
        private static string GetCached(INode node)
        {
            if (_textCache.ContainsKey(node))
                return _textCache[node];
            var codeGenerator = new PDDLCodeGenerator(new ErrorListener());
            _textCache.Add(node, codeGenerator.Generate(node));
            return _textCache[node];
        }
        private static int GetDeterministicHashCode(DomainDecl domain, List<ProblemDecl> problems, ActionDecl metaAction, int timeLimit)
        {
            var problemsStr = "";
            foreach (var problem in problems)
                problemsStr += GetCached(problem);
            var str = $"{GetCached(domain)}_{problemsStr}_{GetCached(metaAction)}_{timeLimit}";

            unchecked
            {
                int hash1 = (5381 << 16) + 5381;
                int hash2 = hash1;

                for (int i = 0; i < str.Length; i += 2)
                {
                    hash1 = (hash1 << 5) + hash1 ^ str[i];
                    if (i == str.Length - 1)
                        break;
                    hash2 = (hash2 << 5) + hash2 ^ str[i + 1];
                }

                return hash1 + hash2 * 1566083941;
            }
        }
    }
}
