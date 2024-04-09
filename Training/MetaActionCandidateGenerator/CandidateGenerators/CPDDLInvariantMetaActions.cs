using PDDLSharp.Models.PDDL.Domain;
using PDDLSharp.Models.PDDL.Expressions;
using PDDLSharp.Models.PDDL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PDDLSharp.Models.PDDL.Overloads;
using Tools;
using System.Diagnostics;
using PDDLSharp.CodeGenerators.PDDL;
using PDDLSharp.ErrorListeners;

namespace MetaActionCandidateGenerator.CandidateGenerators
{
    public class CPDDLInvariantMetaActions : InvariantMetaActions
    {
        public static readonly string CPDDLExecutable = PathHelper.RootPath("../Dependencies/cpddl/bin/pddl");
        public static string TempFolder = PathHelper.RootPath("temp/cpddl");

        internal override List<ActionDecl> GenerateCandidatesInner(PDDLDecl pddlDecl)
        {
            if (!File.Exists(CPDDLExecutable))
                throw new FileNotFoundException($"Could not find the file: {CPDDLExecutable}");
            if (pddlDecl.Domain.Predicates == null)
                throw new Exception("No predicates defined in domain!");
            Initialize(pddlDecl);
            ContextualizeIfNotAlready(pddlDecl);

            var rules = ExecuteCPDDL(pddlDecl);

            var candidates = new List<ActionDecl>();
            int count = 0;
            foreach (var pred in pddlDecl.Domain.Predicates.Predicates)
            {
                if (Statics.Any(x => x.Name == pred.Name))
                    continue;
                if (pred.Arguments.Count == 0)
                    continue;
                if (pred.CanOnlyBeSetToTrue(pddlDecl.Domain) || pred.CanOnlyBeSetToFalse(pddlDecl.Domain))
                    continue;

                var permutations = GeneratePermutations(pred.Arguments.Count);
                if (permutations.Count > 0 && permutations[0].Length > 1)
                {
                    permutations.RemoveAll(x => x.All(y => y == true));
                    permutations.RemoveAll(x => x.All(y => y == false));
                }

                foreach(var rule in rules)
                    if (rule.Predicate == pred.Name)
                        foreach(var fix in rule.Fixed)
                            permutations.RemoveAll(x => x[fix] == true);

                foreach (var permutation in permutations)
                {
                    foreach (var action in pddlDecl.Domain.Actions)
                    {
                        if (action.FindNames(pred.Name).Count == 0)
                            continue;
                        var mutated = GetMutatedPredicate(pred, permutation.ToList());
                        if (!mutated.Equals(pred))
                            candidates.Add(GenerateMetaAction(
                                $"meta_{pred.Name}_{action.Name}_{count++}",
                                new List<IExp>() { pred, new NotExp(GetEqualsPredicate(pred, mutated)) },
                                new List<IExp>() { mutated, new NotExp(pred) },
                                action));
                    }
                }
            }

            return candidates.Distinct(pddlDecl.Domain.Actions);
        }

        private List<PredicateRule> ExecuteCPDDL(PDDLDecl pddlDecl)
        {
            PathHelper.RecratePath(TempFolder);
            var codeGenerator = new PDDLCodeGenerator(new ErrorListener());
            codeGenerator.Generate(pddlDecl.Domain, Path.Combine(TempFolder,"domain.pddl"));
            codeGenerator.Generate(pddlDecl.Problem, Path.Combine(TempFolder,"problem.pddl"));

            var process = new Process
            {
                StartInfo = new ProcessStartInfo()
                {
                    FileName = CPDDLExecutable,
                    Arguments = "--lmg-out output.txt --lmg-stop domain.pddl problem.pddl",
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    WorkingDirectory = TempFolder
                }
            };
            process.Start();
            process.WaitForExit();

            var rules = new List<PredicateRule>();

            foreach(var line in File.ReadLines(Path.Combine(TempFolder, "output.txt")))
            {
                if (line.Contains(":=1"))
                {
                    var inner = line.Substring(line.IndexOf('{') + 1, line.IndexOf('}') - line.IndexOf('{') - 1);
                    if (inner.Contains(','))
                        continue;
                    var predName = inner.Substring(0, inner.IndexOf(' '));
                    var fix = new List<int>();
                    var count = new List<int>();
                    var args = inner.Substring(inner.IndexOf(' ')).Split(' ').ToList();
                    args.RemoveAll(x => x == "");
                    for (int i = 0; i < args.Count; i++) 
                    {
                        if (args[i].StartsWith('C'))
                            count.Add(i);
                        else
                            fix.Add(i);
                    }

                    rules.Add(new PredicateRule(predName, fix, count));
                }
            }

            return rules;
        }

        private class PredicateRule
        {
            public string Predicate { get; set; }
            public List<int> Fixed { get; set; }
            public List<int> Counted { get; set; }

            public PredicateRule(string predicate, List<int> @fixed, List<int> counted)
            {
                Predicate = predicate;
                Fixed = @fixed;
                Counted = counted;
            }
        }
    }
}