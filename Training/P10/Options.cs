using CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static MetaActionCandidateGenerator.Options;

namespace P10
{
    public class Options
    {
        [Option("output", Required = true, HelpText = "Where to output the meta actions")]
        public string OutputPath { get; set; } = "";
        [Option("domain", Required = true, HelpText = "Path to the domain file")]
        public string DomainPath { get; set; } = "";
        [Option("problems", Required = true, HelpText = "Path to the problem file")]
        public IEnumerable<string> ProblemsPath { get; set; } = new List<string>();
        [Option("strategy", Required = true, HelpText = "The generator strategy")]
        public GeneratorStrategies GeneratorStrategy { get; set; }
    }
}
