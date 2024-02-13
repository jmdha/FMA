using PDDLSharp.Models.PDDL.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace P10.RefinementStrategies
{
    public class SomeStrategy : IRefinementStrategy
    {
        public ActionDecl Refine()
        {
            return new ActionDecl("none");
        }
    }
}
