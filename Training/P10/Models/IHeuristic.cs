using PDDLSharp.Models.PDDL.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace P10.Models
{
    public interface IHeuristic<T>
    {
        public int GetValue(T metaAction);
    }
}
