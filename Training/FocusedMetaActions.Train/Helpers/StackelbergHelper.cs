using PDDLSharp.Models.PDDL;
using PDDLSharp.Models.PDDL.Domain;
using Stackelberg.MetaAction.Compiler.Compilers;

namespace FocusedMetaActions.Train.Helpers
{
    public static class StackelbergHelper
    {
        public static PDDLDecl CompileToStackelberg(PDDLDecl pddlDecl, ActionDecl metaAction)
        {
            ConditionalEffectCompiler compiler = new ConditionalEffectCompiler();
            var conditionalDecl = compiler.GenerateConditionalEffects(pddlDecl.Domain, pddlDecl.Problem, metaAction);
            ConditionalEffectSimplifyer abstractor = new ConditionalEffectSimplifyer();
            return abstractor.SimplifyConditionalEffects(conditionalDecl.Domain, conditionalDecl.Problem);
        }
    }
}
