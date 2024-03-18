using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace P10
{
    public class CSVWriter
    {
        public string FileName { get; set; }
        public string WorkingDir { get; set; }

        private Dictionary<string, List<string>> _csvData = new Dictionary<string, List<string>>();

        public CSVWriter(string fileName, string workingDir)
        {
            FileName = fileName;
            WorkingDir = workingDir;
        }

        public void Append(string col, string value, int row = 0)
        {
            if (!_csvData.ContainsKey(col))
                _csvData.Add(col, new List<string>());
            if (_csvData[col].Count <= row)
                _csvData[col].Add(value);
            else
                _csvData[col][row] = value;
            UpdateCSVFile();
        }

        private void UpdateCSVFile()
        {
            var target = Path.Combine(WorkingDir, FileName);
            if (File.Exists(target))
                File.Delete(target);
            var text = "";
            foreach (var col in _csvData.Keys)
                text += $"{col},";
            text = text.Remove(text.Length - 1);
            text += Environment.NewLine;

            var maxRow = _csvData.Max(x => x.Value.Count);
            for(int i = 0; i < maxRow; i++)
            {
                foreach(var col in _csvData.Keys)
                {
                    if (_csvData[col].Count > i)
                        text += $"{_csvData[col][i]},";
                    else
                        text += ",";
                }
                text = text.Remove(text.Length - 1);
                text += Environment.NewLine;
            }

            File.WriteAllText(target, text);
        }
    }
}
