using PDDLSharp.Models.PDDL.Domain;
using PDDLSharp.Models.PDDL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Stackelberg.MetaAction.Compiler.Compilers;

namespace P10.Helpers
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
