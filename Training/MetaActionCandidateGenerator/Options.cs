using CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MetaActionCandidateGenerator
{
    public class Options
    {
        [Flags]
        public enum GeneratorStrategies { 
            None = 1, 
            PredicateMetaActions = 2
        }

        [Option("output", Required = true, HelpText = "Where to output the meta actions")]
        public string OutputPath { get; set; } = "";
        [Option("domain", Required = true, HelpText = "Path to the domain file")]
        public string DomainPath { get; set; } = "";
        [Option("problem", Required = true, HelpText = "Path to the problem file")]
        public string ProblemPath { get; set; } = "";
        [Option("generation-strategy", Required = true, HelpText = "The generator strategy")]
        public GeneratorStrategies GeneratorStrategy { get; set; }
    }
}
