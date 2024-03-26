using CSVToolsSharp;
using PDDLSharp.Models.PDDL.Domain;
using System.Text;

namespace P10.PreconditionAdditionRefinements
{
    public class RefinementResult
    {
        [CSVColumn("domain")]
        public string Domain { get; set; } = "";
        [CSVColumn("meta-action")]
        public string MetaAction { get; set; } = "";
        [CSVColumn("total-refinement-time")]
        public TimeSpan RefinementTime { get; set; } = TimeSpan.Zero;
        [CSVColumn("valid-refinements")]
        public int ValidRefinements { get; set; } = 0;
        [CSVColumn("already-valid")]
        public bool AlreadyValid { get; set; } = false;
        [CSVColumn("succeded")]
        public bool Succeded { get; set; } = true;
        [CSVColumn("state-space-search-time")]
        public TimeSpan StateSpaceSearchTime { get; set; } = TimeSpan.Zero;
        [CSVColumn("stackelberg-output-parsing")]
        public TimeSpan StackelbergOutputParsingTime { get; set; } = TimeSpan.Zero;
        [CSVColumn("initial-refinement-possibilities")]
        public int InitialRefinementPossibilities { get; set; } = 0;
        [CSVColumn("final-refinement-possibilities")]
        public int FinalRefinementPossibilities { get; set; } = 0;

        public List<ActionDecl> RefinedMetaActions { get; set; } = new List<ActionDecl>();

        public RefinementResult() { }

        public override string ToString()
        {
            var sb = new StringBuilder();

            sb.AppendLine($"Domain: {Domain}");
            sb.AppendLine($"Meta Action: {MetaAction}");
            sb.AppendLine($"Total Refinement Time: {RefinementTime}");
            sb.AppendLine($"Valid Refinements: {ValidRefinements}");
            sb.AppendLine($"Already Valid: {AlreadyValid}");
            sb.AppendLine($"Succeded: {Succeded}");
            sb.AppendLine($"State Space Search Time: {StateSpaceSearchTime}");
            sb.AppendLine($"Stackelberg Output Parsing Time: {StackelbergOutputParsingTime}");
            sb.AppendLine($"Initial Refinement Possibilities: {InitialRefinementPossibilities}");
            sb.AppendLine($"Final Refinement Possibilities: {FinalRefinementPossibilities}");

            return sb.ToString();
        }
    }
}
