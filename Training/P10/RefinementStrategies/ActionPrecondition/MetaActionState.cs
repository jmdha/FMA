using PDDLSharp.Models.PDDL.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace P10.RefinementStrategies.ActionPrecondition
{
    public class MetaActionState
    {
        public ActionDecl MetaAction { get; set; }
        public List<string> AppliedActions { get; set; }

        public MetaActionState(ActionDecl metaAction, List<string> appliedActions)
        {
            MetaAction = metaAction;
            AppliedActions = appliedActions;
        }

        public override bool Equals(object? obj)
        {
            if (obj is MetaActionState other)
            {
                if (!other.MetaAction.Equals(MetaAction)) return false;
                if (other.AppliedActions.Count != AppliedActions.Count) return false;
                foreach (var item in AppliedActions)
                    if (!other.AppliedActions.Contains(item))
                        return false;
                return true;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(MetaAction, AppliedActions);
        }
    }
}
