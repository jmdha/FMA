using CSVToolsSharp;
using System.Text;

namespace FocusedMetaActions.Train
{
    public class GeneralResult
    {
        [CSVColumn("id")]
        public string ID { get; set; } = "";
        [CSVColumn("domain")]
        public string Domain { get; set; } = "";
        [CSVColumn("problems")]
        public int Problems { get; set; } = 0;
        [CSVColumn("Total-Candidates")]
        public int TotalCandidates { get; set; } = 0;
        [CSVColumn("Pre-Not-Useful-Removed")]
        public int PreNotUsefulRemoved { get; set; } = 0;
        [CSVColumn("Post-Duplicates-Removed")]
        public int PostDuplicatesRemoved { get; set; } = 0;
        [CSVColumn("Post-Not-Useful-Removed")]
        public int PostNotUsefulRemoved { get; set; } = 0;
        [CSVColumn("Total-Refined")]
        public int TotalRefinedCandidates { get; set; } = 0;

        public GeneralResult() { }

        public override string ToString()
        {
            var sb = new StringBuilder();

            sb.AppendLine($"ID: {ID}");
            sb.AppendLine($"Domain: {Domain}");
            sb.AppendLine($"Problems: {Problems}");
            sb.AppendLine($"Total Candidates: {TotalCandidates}");
            sb.AppendLine($"(Pre) Removed Non-useful: {PreNotUsefulRemoved}");
            sb.AppendLine($"(Post) Removed Duplicates: {PostDuplicatesRemoved}");
            sb.AppendLine($"(Post) Removed Non-useful: {PostNotUsefulRemoved}");
            sb.AppendLine($"Total Refined Candidates: {TotalRefinedCandidates}");

            return sb.ToString();
        }
    }
}
