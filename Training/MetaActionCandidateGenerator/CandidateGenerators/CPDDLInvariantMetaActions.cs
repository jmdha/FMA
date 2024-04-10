using PDDLSharp.CodeGenerators.PDDL;
using PDDLSharp.ErrorListeners;
using PDDLSharp.Models.PDDL;
using PDDLSharp.Models.PDDL.Domain;
using PDDLSharp.Models.PDDL.Expressions;
using PDDLSharp.Models.PDDL.Overloads;
using System.Diagnostics;
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
                    int counter = 0;
                    foreach (var action in pddlDecl.Domain.Actions)
                    {
                        if (action.Effects.FindNames(predicate.Name).Count == 0)
                            continue;

                        // Possitives
                        if (!predicate.CanOnlyBeSetToFalse(pddlDecl.Domain))
                        {
                            var handled = new List<string>();
                            var preconditions = new List<IExp>();
                            var effects = new List<IExp>() { predicate };
                            var singles = rules.Where(x => x.Count == 1 && x[0].Predicate == predicate.Name).ToList();

                            foreach(var single in singles)
                            {
                                var newArgs = new List<NameExp>();
                                int offset = 0;
                                foreach(var arg in single[0].Args)
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
                                    preconditions.Add(newPredicate);
                                    var negated = new NotExp(newPredicate.Copy());
                                    negated.Child.Parent = negated;
                                    effects.Add(negated);
                                    handled.Add(single[0].Predicate);
                                }
                            }
                            var complexes = rules.Where(x => x.Count > 1 && x.Any(y => y.Predicate == predicate.Name)).ToList();
                            if (complexes.Count == 0)
                            {
                                candidates.Add(GenerateMetaAction(
                                    $"meta_{predicate.Name}_{counter++}",
                                    preconditions,
                                    effects,
                                    action));
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

                                        var sample = pddlDecl.Domain.Predicates.Predicates.First(x => x.Name == option.Predicate);
                                        if (!sample.CanOnlyBeSetToFalse(pddlDecl.Domain) &&
                                            !sample.CanOnlyBeSetToTrue(pddlDecl.Domain))
                                        {
                                            foreach (var eff in effects)
                                            {
                                                if (eff is NotExp not && not.Child is PredicateExp pred && pred.Name == match.Predicate)
                                                {
                                                    var newArgs = new List<NameExp>();
                                                    int index = 0;
                                                    foreach (var arg in option.Args)
                                                    {
                                                        var targetIndex = match.Args.IndexOf(arg);
                                                        if (targetIndex != -1)
                                                            newArgs.Add(pred.Arguments[targetIndex].Copy());
                                                        else
                                                             newArgs.Add(new NameExp($"?{arg}", sample.Arguments[index].Type.Copy()));
                                                        index++;
                                                    }
                                                    var newPredicate = new PredicateExp(option.Predicate, newArgs);
                                                    newEffects.Add(newPredicate);
                                                }
                                                else if (eff is PredicateExp pred2 && pred2.Name == match.Predicate)
                                                {
                                                    var newArgs = new List<NameExp>();
                                                    int index = 0;
                                                    foreach (var arg in option.Args)
                                                    {
                                                        var targetIndex = match.Args.IndexOf(arg);
                                                        if (targetIndex != -1)
                                                            newArgs.Add(pred2.Arguments[targetIndex].Copy());
                                                        else
                                                            newArgs.Add(new NameExp($"?{arg}", sample.Arguments[index].Type.Copy()));
                                                        index++;
                                                    }
                                                    var newPredicate = new PredicateExp(option.Predicate, newArgs);
                                                    newPreconditions.Add(newPredicate);
                                                    newEffects.Add(new NotExp(newPredicate));
                                                }
                                            }
                                        }

                                        if (newPreconditions.Count != preconditions.Count &&
                                            newEffects.Count != effects.Count)
                                                candidates.Add(GenerateMetaAction(
                                                    $"meta_{predicate.Name}_{counter++}",
                                                    newPreconditions,
                                                    newEffects,
                                                    action));
                                    }
                                }
                            }
                        }

                        // Negatives (TODO)
                        if (!predicate.CanOnlyBeSetToTrue(pddlDecl.Domain))
                            candidates.Add(GenerateMetaAction(
                                $"meta_{predicate.Name}_{counter++}_false",
                                new List<IExp>(),
                                new List<IExp>() { new NotExp(predicate) },
                                action));
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

            var rules = new List<List<PredicateRule>>();

            foreach (var line in File.ReadLines(Path.Combine(TempFolder, "output.txt")))
            {
                if (line.Contains(":=1"))
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