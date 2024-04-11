using CommandLine.Text;
using Microsoft.VisualBasic.FileIO;
using PDDLSharp.CodeGenerators.PDDL;
using PDDLSharp.ErrorListeners;
using PDDLSharp.Models.PDDL;
using PDDLSharp.Models.PDDL.Domain;
using PDDLSharp.Models.PDDL.Expressions;
using PDDLSharp.Models.PDDL.Overloads;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Reflection;
using System.Text.RegularExpressions;
using Tools;
using static System.Collections.Specialized.BitVector32;

namespace MetaActionCandidateGenerator.CandidateGenerators
{
    public class CPDDLInvariantMetaActions : InvariantMetaActions
    {
        public static string CPDDLExecutable = PathHelper.RootPath("../Dependencies/cpddl/bin/pddl");
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
            foreach (var predicate in pddlDecl.Domain.Predicates.Predicates)
            {
                if (!Statics.Any(x => x.Name.ToUpper() == predicate.Name.ToUpper()))
                {
                    foreach (var action in pddlDecl.Domain.Actions)
                    {
                        if (action.Effects.FindNames(predicate.Name).Count == 0)
                            continue;

                        bool invarianted = rules.Any(x => x.Any(y => y.Predicate == predicate.Name));
                        if (invarianted)
                            candidates.AddRange(GeneateCandidates(rules, pddlDecl, predicate, action));
                        else
                        {
                            if (!predicate.CanOnlyBeSetToFalse(pddlDecl.Domain))
                                candidates.Add(GenerateMetaAction(
                                    $"meta_{predicate.Name}",
                                    new List<IExp>(),
                                    new List<IExp>() { predicate },
                                    action));
                            if (!predicate.CanOnlyBeSetToTrue(pddlDecl.Domain))
                                candidates.Add(GenerateMetaAction(
                                    $"meta_{predicate.Name}_false",
                                    new List<IExp>(),
                                    new List<IExp>() { new NotExp(predicate) },
                                    action));
                        }
                    }
                }
            }

            return candidates.Distinct(pddlDecl.Domain.Actions);
        }

        private List<List<PredicateRule>> ExecuteCPDDL(PDDLDecl pddlDecl)
        {
            PathHelper.RecratePath(TempFolder);
            var codeGenerator = new PDDLCodeGenerator(new ErrorListener());
            codeGenerator.Generate(pddlDecl.Domain, Path.Combine(TempFolder, "domain.pddl"));
            codeGenerator.Generate(pddlDecl.Problem, Path.Combine(TempFolder, "problem.pddl"));

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

            if (!File.Exists(Path.Combine(TempFolder, "output.txt")))
                return new List<List<PredicateRule>>();

            var rules = new List<List<PredicateRule>>();

            foreach (var line in File.ReadLines(Path.Combine(TempFolder, "output.txt")))
            {
                if (line.EndsWith(":=1"))
                {
                    var inner = line.Substring(line.IndexOf('{') + 1, line.IndexOf('}') - line.IndexOf('{') - 1);
                    var subRules = new List<PredicateRule>();
                    var subRulesStr = inner.Split(',');
                    bool valid = true;
                    foreach (var subRule in subRulesStr)
                    {
                        var subRuleStr = subRule.Trim();
                        var predName = subRuleStr.Substring(0, subRuleStr.IndexOf(' '));
                        if (predName.StartsWith("NOT-"))
                        {
                            valid = false;
                            break;
                        }
                        var fixArgs = new List<string>();
                        var args = subRuleStr.Substring(subRuleStr.IndexOf(' ')).Split(' ').ToList();
                        args.RemoveAll(x => x == "");
                        for (int i = 0; i < args.Count; i++)
                        {
                            if (args[i].StartsWith('C'))
                                fixArgs.Add(args[i].Substring(0, args[i].IndexOf(':')));
                            else if (args[i].StartsWith('V'))
                                fixArgs.Add(args[i].Substring(0, args[i].IndexOf(':')));
                            else
                                valid = false;
                        }
                        if (!valid)
                            break;
                        subRules.Add(new PredicateRule(predName, fixArgs));
                    }

                    if (valid)
                        rules.Add(subRules);
                }
            }

            return rules;
        }

        private List<ActionDecl> GeneateCandidates(List<List<PredicateRule>> rules, PDDLDecl pddlDecl, PredicateExp predicate, ActionDecl staticsReference)
        {
            var candidates = new List<ActionDecl>();
            var preconditions = new List<IExp>();
            var effects = new List<IExp>() { predicate };

            // Singles
            InsertSinglesFromRules(rules, pddlDecl, predicate, ref preconditions, ref effects);

            // Complexes
            var complexes = rules.Where(x => x.Count > 1 && x.Any(y => y.Predicate == predicate.Name)).ToList();
            if (complexes.Count == 0)
            {
                candidates.Add(GenerateMetaAction(
                    $"meta_{predicate.Name}",
                    preconditions,
                    effects,
                    staticsReference));
            }
            else
            {
                foreach (var complex in complexes)
                {
                    var match = complex.First(x => x.Predicate == predicate.Name);
                    foreach (var option in complex)
                    {
                        if (option.Predicate == predicate.Name)
                            continue;

                        var newPreconditions = new List<IExp>(preconditions);
                        var newEffects = new List<IExp>(effects);

                        var sample = pddlDecl.Domain.Predicates!.Predicates.First(x => x.Name == option.Predicate);
                        if (!sample.CanOnlyBeSetToFalse(pddlDecl.Domain) &&
                            !sample.CanOnlyBeSetToTrue(pddlDecl.Domain))
                        {
                            foreach (var eff in effects)
                            {
                                if (eff is NotExp not && not.Child is PredicateExp pred && pred.Name == match.Predicate)
                                    UpdateByDirection(GenerateMutated(option, match, sample, pred), ref newPreconditions, ref newEffects, false);
                                else if (eff is PredicateExp pred2 && pred2.Name == match.Predicate)
                                    UpdateByDirection(GenerateMutated(option, match, sample, pred2), ref newPreconditions, ref newEffects, true);
                            }
                        }

                        if (newPreconditions.Count != preconditions.Count &&
                            newEffects.Count != effects.Count)
                            candidates.Add(GenerateMetaAction(
                                $"meta_{predicate.Name}",
                                newPreconditions,
                                newEffects,
                                staticsReference));
                    }
                }
            }
            return candidates;
        }

        private void InsertSinglesFromRules(List<List<PredicateRule>> rules, PDDLDecl pddlDecl, PredicateExp predicate, ref List<IExp> preconditions, ref List<IExp> effects)
        {
            var singles = rules.Where(x => x.Count == 1 && x[0].Predicate == predicate.Name).ToList();

            foreach (var single in singles)
            {
                var newArgs = new List<NameExp>();
                int offset = 0;
                foreach (var arg in single[0].Args)
                {
                    if (arg.StartsWith('V'))
                        newArgs.Add(predicate.Arguments[offset].Copy());
                    else if (arg.StartsWith('C'))
                        newArgs.Add(new NameExp($"{predicate.Arguments[offset].Name}{offset}", predicate.Arguments[offset].Type.Copy()));
                    offset++;
                }

                var newPredicate = new PredicateExp(single[0].Predicate, newArgs);

                if (!newPredicate.CanOnlyBeSetToFalse(pddlDecl.Domain) &&
                    !newPredicate.CanOnlyBeSetToTrue(pddlDecl.Domain))
                {
                    UpdateByDirection(newPredicate, ref preconditions, ref effects, true);
                }
            }
        }

        private void UpdateByDirection(PredicateExp predicate, ref List<IExp> preconditions, ref List<IExp> effects, bool direction)
        {
            if (direction)
            {
                preconditions.Add(predicate);
                effects.Add(GenerateNegated(predicate.Copy()));
            }
            else
            {
                //preconditions.Add(GenerateNegated(predicate.Copy()));
                effects.Add(predicate);
            }
        }

        private NotExp GenerateNegated(IExp exp)
        {
            var newNot = new NotExp(exp);
            newNot.Child.Parent = newNot;
            return newNot;
        }

        private PredicateExp GenerateMutated(PredicateRule option, PredicateRule match, PredicateExp sample, PredicateExp predicate)
        {
            var newArgs = new List<NameExp>();
            int index = 0;
            foreach (var arg in option.Args)
            {
                var targetIndex = match.Args.IndexOf(arg);
                if (targetIndex != -1)
                    newArgs.Add(predicate.Arguments[targetIndex].Copy());
                else
                    newArgs.Add(new NameExp($"?{arg}", sample.Arguments[index].Type.Copy()));
                index++;
            }
            return new PredicateExp(option.Predicate, newArgs);
        }

        private class PredicateRule
        {
            public string Predicate { get; set; }
            public List<string> Args { get; set; }

            public PredicateRule(string predicate, List<string> args)
            {
                Predicate = predicate;
                Args = args;
            }

            public override string ToString()
            {
                var args = "";
                foreach (var arg in Args)
                    args += $"{arg}, ";
                args = args.Trim();
                if (args.EndsWith(','))
                    args = args.Substring(0, args.Length - 1);
                return $"{Predicate}: {args}";
            }
        }
    }
}