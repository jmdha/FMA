using PDDLSharp.Models.PDDL.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace P10.RefinementStrategies.ActionPrecondition.Heuristics
{
    public class hSum : IHeuristic
    {
        public List<IHeuristic> Heuristics { get; set; }

        public hSum(List<IHeuristic> heuristics)
        {
            Heuristics = heuristics;
        }

        public int GetValue(MetaActionState metaAction)
        {
            var value = 0;
            foreach (var heuristic in Heuristics)
                value += heuristic.GetValue(metaAction);
            return value;
        }
    }
}
