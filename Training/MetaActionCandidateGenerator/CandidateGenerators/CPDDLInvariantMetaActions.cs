using CommandLine;
using CommandLine.Text;
using PDDLSharp.CodeGenerators.PDDL;
using PDDLSharp.ErrorListeners;
using PDDLSharp.Models.PDDL;
using PDDLSharp.Models.PDDL.Domain;
using PDDLSharp.Models.PDDL.Expressions;
using PDDLSharp.Models.PDDL.Overloads;
using System;
using System.Data;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Tools;

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
                        var predName = subRuleStr;
                        if (subRuleStr.Contains(' '))
                            predName = subRuleStr.Substring(0, subRuleStr.IndexOf(' '));
                        if (predName.StartsWith("NOT-"))
                        {
                            valid = false;
                            break;
                        }
                        var fixArgs = new List<string>();
                        if (subRuleStr.Contains(' '))
                        {
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
                        }
                        subRules.Add(new PredicateRule(predName, fixArgs));
                    }

                    if (valid)
                        rules.Add(subRules);
                }
            }

            rules = rules.OrderBy(x => x.Count).ToList();

            return rules;
        }

        private List<ActionDecl> GeneateCandidates(List<List<PredicateRule>> rules, PDDLDecl pddlDecl, PredicateExp predicate, ActionDecl staticsReference)
        {
            var candidateOptions = RefineForRules(rules, new Candidate(new List<IExp>(), new List<IExp>() { predicate }), pddlDecl.Domain, new List<List<PredicateRule>>());
            var validCandidateOptions = new List<Candidate>();
            foreach(var option in candidateOptions)
            {
                bool valid = true;
                foreach(var effect in option.Effects)
                {
                    if (!valid)
                        break;
                    if (effect is NotExp not && not.Child is PredicateExp pred && pred.CanOnlyBeSetToTrue(pddlDecl.Domain))
                        valid = false;
                    if (effect is PredicateExp pred2 && pred2.CanOnlyBeSetToFalse(pddlDecl.Domain))
                        valid = false;
                }
                if (!valid)
                    continue;
                foreach (var precon in option.Preconditions)
                {
                    if (!valid)
                        break;
                    if (precon is NotExp not && not.Child is PredicateExp pred && pred.CanOnlyBeSetToTrue(pddlDecl.Domain))
                        valid = false;
                    if (precon is PredicateExp pred2 && pred2.CanOnlyBeSetToFalse(pddlDecl.Domain))
                        valid = false;
                }
                if (valid)
                    validCandidateOptions.Add(option);
            }

            int version = 0;
            var candidates = new List<ActionDecl>();
            foreach (var option in validCandidateOptions)
                candidates.Add(GenerateMetaAction(
                    $"meta_{predicate.Name}_{version++}",
                    option.Preconditions,
                    option.Effects,
                    staticsReference));

            return candidates;
        }

        private List<Candidate> RefineForRules(List<List<PredicateRule>> rules, Candidate candidate, DomainDecl domain, List<List<PredicateRule>> covered)
        {
            var candidates = new List<Candidate>();

            bool recursed = false;
            var coveredNow = new List<List<PredicateRule>>(covered);
            for (int i = 0; i < candidate.Effects.Count; i++)
            {
                PredicateExp? reference = null;
                if (candidate.Effects[i] is PredicateExp pred)
                    reference = pred;
                else if (candidate.Effects[i] is NotExp not && not.Child is PredicateExp pred2)
                    reference = pred2;
                else
                    throw new ArgumentNullException();

                foreach(var ruleSet in rules)
                {
                    if (coveredNow.Contains(ruleSet))
                        continue;
                    if (!ruleSet.Any(x => x.Predicate == reference.Name))
                        continue;

                    if (ruleSet.Count == 1)
                    {
                        var target = GetMutatedUnaryIExp(candidate.Effects[i], ruleSet[0]);
                        if (!candidate.Effects.Contains(target))
                        {
                            candidate.Effects.Add(target);
                            if (target is NotExp not)
                                candidate.Preconditions.Add(not.Child);
                            else
                                candidate.Preconditions.Add(GenerateNegated(target));
                            i = -1;
                            coveredNow.Add(ruleSet);
                            break;
                        }
                    } 
                    else if (ruleSet.Count == 2)
                    {
                        var sourceRule = GetMatchingRule(candidate.Effects[i], ruleSet);
                        var targetRule = ruleSet.First(x => x != sourceRule);
                        var target = GetMutatedBinaryIExp(candidate.Effects[i], sourceRule, targetRule, domain);
                        if (!candidate.Effects.Contains(target))
                        {
                            candidate.Effects.Add(target);
                            if (target is NotExp not)
                                candidate.Preconditions.Add(not.Child);
                            else
                                candidate.Preconditions.Add(GenerateNegated(target));
                            i = -1;
                            coveredNow.Add(ruleSet);
                            break;
                        }
                    }
                    else
                    {
                        coveredNow.Add(ruleSet);
                        recursed = true;
                        var sourceRule = GetMatchingRule(candidate.Effects[i], ruleSet);
                        var others = ruleSet.Where(x => x != sourceRule);
                        foreach(var targetRule in others)
                        {
                            var target = GetMutatedBinaryIExp(candidate.Effects[i], sourceRule, targetRule, domain);
                            if (!candidate.Effects.Contains(target))
                            {
                                var cpy = candidate.Copy();
                                cpy.Effects.Add(target);
                                if (target is NotExp not)
                                    cpy.Preconditions.Add(not.Child);
                                else
                                    cpy.Preconditions.Add(GenerateNegated(target));
                                candidates.AddRange(RefineForRules(rules, cpy, domain, coveredNow));
                            }
                        }
                        i = candidate.Effects.Count;
                        break;
                    }
                }
            }
            if (!recursed)
                candidates.Add(candidate);

            return candidates;
        }

        private IExp GetMutatedUnaryIExp(IExp exp, PredicateRule rule)
        {
            var predicate = GetReferencePredicate(exp);

            int index = 0;
            foreach (var arg in predicate.Arguments)
            {
                if (rule.Args[index++].StartsWith('C'))
                {
                    if (arg.Name.EndsWith('_'))
                        arg.Name = $"{arg.Name.Substring(0, arg.Name.Length - 1)}";
                    else
                        arg.Name = $"{arg.Name}_";
                }
            }
            if (exp is PredicateExp)
                return GenerateNegated(predicate);
            else if (exp is NotExp not2 && not2.Child is PredicateExp)
                return predicate;

            throw new Exception();
        }

        private PredicateRule GetMatchingRule(IExp exp, List<PredicateRule> rules)
        {
            var predicate = GetReferencePredicate(exp);
            return rules.First(x => x.Predicate == predicate.Name);
        }

        private IExp GetMutatedBinaryIExp(IExp exp, PredicateRule sourceRule, PredicateRule targetRule, DomainDecl domain)
        {
            var predicate = GetReferencePredicate(exp);

            var sourceRuleArgs = new List<string>(sourceRule.Args);
            var targetRuleArgs = new List<string>(targetRule.Args);
            var sample = domain.Predicates!.Predicates.First(x => x.Name == targetRule.Predicate).Copy();
            for (int i = 0; i < targetRuleArgs.Count; i++)
            {
                if (targetRuleArgs[i].StartsWith('V'))
                    sample.Arguments[i].Name = predicate.Arguments[sourceRuleArgs.IndexOf(targetRuleArgs[i])].Name;
                else
                    sample.Arguments[i].Name = $"?{targetRuleArgs[i]}";
            }

            if (exp is PredicateExp)
                return GenerateNegated(sample);
            else if (exp is NotExp not2 && not2.Child is PredicateExp)
                return sample;
            throw new Exception();
        }

        private PredicateExp GetReferencePredicate(IExp exp)
        {
            if (exp is PredicateExp pred)
                return pred.Copy();
            else if (exp is NotExp not && not.Child is PredicateExp pred2)
                return pred2.Copy();
            else
                throw new ArgumentNullException("Impossible mutation generation");
        }

        private NotExp GenerateNegated(IExp exp)
        {
            var newNot = new NotExp(exp);
            newNot.Child.Parent = newNot;
            return newNot;
        }

        private class Candidate
        {
            public List<IExp> Preconditions { get; set; }
            public List<IExp> Effects { get; set; }

            public Candidate(List<IExp> preconditions, List<IExp> effects)
            {
                Preconditions = preconditions;
                Effects = effects;
            }

            public Candidate Copy()
            {
                var preconditions = new List<IExp>();
                foreach (var precon in Preconditions)
                    preconditions.Add(precon.Copy().Cast<IExp>());
                var effects = new List<IExp>();
                foreach (var effect in Effects)
                    effects.Add(effect.Copy().Cast<IExp>());

                return new Candidate(preconditions, effects);
            }
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