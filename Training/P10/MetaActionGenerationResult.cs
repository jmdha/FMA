using CSVToolsSharp;
using System.Text;

namespace P10
{
    public class MetaActionGenerationResult
    {
        [CSVColumn("id")]
        public string ID { get; set; } = "";
        [CSVColumn("domain")]
        public string Domain { get; set; } = "";
        [CSVColumn("generator")]
        public string Generator { get; set; } = "";
        [CSVColumn("candidates")]
        public int TotalCandidates { get; set; } = 0;

        public MetaActionGenerationResult() { }

        public override string ToString()
        {
            var sb = new StringBuilder();

            sb.AppendLine($"ID: {ID}");
            sb.AppendLine($"Domain: {Domain}");
            sb.AppendLine($"Generator: {Generator}");
            sb.AppendLine($"Total Candidates: {TotalCandidates}");

            return sb.ToString();
        }
    }
}
