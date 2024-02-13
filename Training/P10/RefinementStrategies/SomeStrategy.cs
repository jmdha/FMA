using PDDLSharp.Models.PDDL.Domain;

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
