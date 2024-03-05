using PDDLSharp.Models.PDDL.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace P10.RefinementStrategies.GroundedPredicateAdditions
{
    public interface IHeuristic
    {
        public int GetValue(PreconditionState preconditions);
    }
}
