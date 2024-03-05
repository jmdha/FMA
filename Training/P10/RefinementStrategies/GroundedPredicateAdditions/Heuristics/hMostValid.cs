using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace P10.RefinementStrategies.GroundedPredicateAdditions.Heuristics
{
    public class hMostValid : IHeuristic
    {
        public int GetValue(PreconditionState preconditions)
        {
            return preconditions.InvalidStates;
        }
    }
}
