using PDDLSharp.Models.FastDownward.Plans;
using PDDLSharp.Models.PDDL.Domain;

namespace FocusedMetaActions.Train.MacroExtractor
{
    public class RepairSequence
    {
        public GroundedAction MetaAction { get; set; }
        public ActionDecl Macro { get; set; }
        public ActionPlan Replacement { get; set; }

        public RepairSequence(GroundedAction metaAction, ActionDecl macro, ActionPlan replacements)
        {
            MetaAction = metaAction;
            Macro = macro;
            Replacement = replacements;
        }

        public override int GetHashCode()
        {
            return Macro.GetHashCode();
        }

        public override bool Equals(object? obj)
        {
            if (obj is RepairSequence seq)
            {
                if (!MetaAction.Equals(seq.MetaAction)) return false;
                return Macro.Equals(seq.Macro);
            }
            return false;
        }
    }
}
