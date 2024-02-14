using PDDLSharp.Models.PDDL.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace P10.RefinementStrategies.ActionPrecondition
{
    public interface IHeuristic
    {
        public int GetValue(MetaActionState metaAction);
    }
}
