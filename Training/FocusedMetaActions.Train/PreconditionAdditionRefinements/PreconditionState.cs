using PDDLSharp.CodeGenerators.PDDL;
using PDDLSharp.ErrorListeners;
using PDDLSharp.Models.PDDL;
using PDDLSharp.Models.PDDL.Domain;

namespace FocusedMetaActions.Train.PreconditionAdditionRefinements
{
    public class PreconditionState
    {
        public int TotalValidStates { get; set; }
        public int TotalInvalidStates { get; set; }
        public int ValidStates { get; set; }
        public int InvalidStates { get; set; }
        public int Applicability { get; set; }
        public ActionDecl MetaAction { get; set; }
        public List<IExp> Precondition { get; set; }
        public int hValue { get; set; }

        public PreconditionState(int totalValidStates, int totalInvalidStates, int validStates, int invalidStates, int applicability, ActionDecl metaAction, List<IExp> precondition, int hValue)
        {
            TotalValidStates = totalValidStates;
            TotalInvalidStates = totalInvalidStates;
            Applicability = applicability;
            ValidStates = validStates;
            InvalidStates = invalidStates;
            MetaAction = metaAction;
            Precondition = precondition;
            this.hValue = hValue;
        }

        public override bool Equals(object? obj)
        {
            if (obj is PreconditionState other)
            {
                if (other.Precondition.Count != Precondition.Count) return false;
                for (int i = 0; i < other.Precondition.Count; i++)
                    if (!other.Precondition[i].Equals(Precondition[i]))
                        return false;
                return true;
            }
            return false;
        }

        public override int GetHashCode()
        {
            var hash = 1;
            foreach (var item in Precondition)
                hash ^= item.GetHashCode();
            return hash;
        }

        public override string ToString()
        {
            var listener = new ErrorListener();
            var codeGenerator = new PDDLCodeGenerator(listener);
            var preconStr = "";
            foreach (var precon in Precondition)
                preconStr += $"{codeGenerator.Generate(precon)}, ";
            return preconStr;
        }
    }
}
