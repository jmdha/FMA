namespace P10
{
    public class CSVWriter
    {
        public string FileName { get; set; }
        public string WorkingDir { get; set; }

        private readonly Dictionary<string, List<string>> _csvData = new Dictionary<string, List<string>>();

        public CSVWriter(string fileName, string workingDir)
        {
            FileName = fileName;
            WorkingDir = workingDir;

            var target = Path.Combine(workingDir, FileName);
            if (File.Exists(target))
            {
                var lines = File.ReadAllLines(target);
                var headers = lines[0].Split(',').ToList();
                foreach (var header in headers)
                    _csvData.Add(header, new List<string>());
                foreach (var line in lines.Skip(1))
                {
                    var split = line.Split(',').ToList();
                    for (int i = 0; i < split.Count; i++)
                        _csvData[headers[i]].Add(split[i]);
                }
            }
        }

        public void Append(string col, string value, int row = 0)
        {
            if (_csvData.Keys.Count > 0)
            {
                if (_csvData[_csvData.Keys.ElementAt(0)].Count <= row)
                {
                    foreach (var key in _csvData.Keys)
                    {
                        while (_csvData[key].Count <= row)
                            _csvData[key].Add("");
                    }
                }
            }

            if (!_csvData.ContainsKey(col))
            {
                _csvData.Add(col, new List<string>(row + 1));
                while (_csvData[col].Count <= row)
                    _csvData[col].Add("");
            }
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
            for (int i = 0; i < maxRow; i++)
            {
                foreach (var col in _csvData.Keys)
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
