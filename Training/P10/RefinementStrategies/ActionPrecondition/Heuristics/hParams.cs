using P10.Models;
using P10.RefinementStrategies.ActionPrecondition;
using PDDLSharp.Models.PDDL.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace P10.RefinementStrategies.ActionPrecondition.Heuristics
{
    public class hParams : IHeuristic<MetaActionState>
    {
        public int GetValue(MetaActionState metaAction) => metaAction.MetaAction.Parameters.Values.Count;
    }
}
