using FocusedMetaActions.Train.Helpers;
using PDDLSharp.CodeGenerators.FastDownward.Plans;
using PDDLSharp.CodeGenerators.PDDL;
using PDDLSharp.ErrorListeners;
using PDDLSharp.Models.FastDownward.Plans;
using PDDLSharp.Models.PDDL.Domain;
using PDDLSharp.Models.PDDL.Expressions;
using PDDLSharp.Parsers.FastDownward.Plans;
using PDDLSharp.Toolkits;

namespace FocusedMetaActions.Train.MacroExtractor
{
    /// <summary>
    /// This file is basically just a direct extract of the <seealso href="https://github.com/kris701/MARMA/blob/main/Training/MacroExtractor/MacroExtractor.cs">one from MARMA</seealso>
    /// </summary>
    public class Extractor
    {
        public static string _metaActionName = "$";
        public static string _macroActionName = "$macro";
        public static string[] _RemoveNamesFromActions = { "attack_", "fix_" };

        public void ExtractMacros(DomainDecl domain, List<string> followerPlans, string outPath)
        {
            outPath = PathHelper.RootPath(outPath);

            var repairSequences = ExtractMacros(domain, followerPlans);
            var listener = new ErrorListener();
            var codeGenerator = new PDDLCodeGenerator(listener);
            var planGenerator = new FastDownwardPlanGenerator(listener);
            foreach (var item in repairSequences)
                PathHelper.RecratePath(Path.Combine(outPath, item.MetaAction.ActionName));
            int id = 1;
            foreach (var replacement in repairSequences)
            {
                codeGenerator.Generate(replacement.Macro, Path.Combine(outPath, replacement.MetaAction.ActionName, $"macro{id}.pddl"));
                planGenerator.Generate(replacement.Replacement, Path.Combine(outPath, replacement.MetaAction.ActionName, $"macro{id++}_replacement.plan"));
            }
        }

        private List<RepairSequence> ExtractMacros(DomainDecl domain, List<string> planFiles)
        {
            var planSequences = ExtractUniquePlanSequences(planFiles);
            var macros = GenerateMacros(planSequences, domain);
            return macros.ToList();
        }

        private static Dictionary<GroundedAction, HashSet<ActionPlan>> ExtractUniquePlanSequences(List<string> followerPlanFiles)
        {
            var followerPlans = PathHelper.ResolveFileWildcards(followerPlanFiles);

            var planSequences = new Dictionary<GroundedAction, HashSet<ActionPlan>>();
            var listener = new ErrorListener();
            var parser = new FDPlanParser(listener);

            foreach (var planFile in followerPlans)
            {
                var plan = parser.Parse(planFile);
                if (plan.Plan.Count == 0)
                    continue;
                int metaActionIndex = IndexOfMetaAction(plan);
                if (metaActionIndex == -1)
                    continue;
                var metaAction = plan.Plan[metaActionIndex];
                var nameDictionary = GenerateNameReplacementDict(metaAction);
                RenameActionArguments(metaAction, nameDictionary);
                if (!planSequences.ContainsKey(metaAction))
                    planSequences.Add(metaAction, new HashSet<ActionPlan>());

                var repairSequence = plan.Plan.GetRange(metaActionIndex + 1, plan.Plan.Count - metaActionIndex - 1);
                foreach (var action in repairSequence)
                    RenameActionArguments(action, nameDictionary);

                planSequences[metaAction].Add(new ActionPlan(repairSequence, repairSequence.Count));
            }

            return planSequences;
        }

        private static Dictionary<string, string> GenerateNameReplacementDict(GroundedAction metaAction)
        {
            var returnDict = new Dictionary<string, string>();
            int argIndex = 0;
            foreach (var arg in metaAction.Arguments)
                if (!returnDict.ContainsKey(arg.Name))
                    returnDict.Add(arg.Name, $"?{argIndex++}");
            return returnDict;
        }

        private static void RenameActionArguments(GroundedAction action, Dictionary<string, string> replacements)
        {
            foreach (var arg in action.Arguments)
                if (replacements.ContainsKey(arg.Name))
                    arg.Name = replacements[arg.Name];
            foreach (var name in _RemoveNamesFromActions)
                action.ActionName = action.ActionName.Replace(name, "");
        }

        private static int IndexOfMetaAction(ActionPlan leaderPlan)
        {
            for (int i = 0; i < leaderPlan.Plan.Count; i++)
                if (leaderPlan.Plan[i].ActionName.Contains(_metaActionName))
                    return i;
            return -1;
        }

        private static List<RepairSequence> GenerateMacros(Dictionary<GroundedAction, HashSet<ActionPlan>> from, DomainDecl domain)
        {
            var returnList = new List<RepairSequence>();

            foreach (var key in from.Keys)
            {
                foreach (var actionPlan in from[key])
                {
                    var macro = GenerateMacroInstance(key.ActionName, actionPlan, domain);
                    if (macro.Effects is AndExp and && and.Children.Count == 0)
                        continue;

                    int id = 0;
                    var changeParams = macro.Parameters.Values.Where(x => !x.Name.StartsWith("?"));
                    var replacementDict = new Dictionary<string, string>();
                    foreach (var arg in changeParams)
                        replacementDict.Add(arg.Name, $"?O{id++}");
                    foreach (var arg in replacementDict.Keys)
                    {
                        var allRefs = macro.FindNames(arg);
                        foreach (var aRef in allRefs)
                            aRef.Name = replacementDict[arg];
                    }

                    foreach (var step in actionPlan.Plan)
                        RenameActionArguments(step, replacementDict);

                    var newSeq = new RepairSequence(key, macro, actionPlan);
                    if (!returnList.Contains(newSeq))
                        returnList.Add(newSeq);
                }
            }

            return returnList;
        }

        private static ActionDecl GenerateMacroInstance(string newName, ActionPlan plan, DomainDecl domain)
        {
            var combiner = new ActionDeclCombiner();
            var planActionInstances = new List<ActionDecl>();
            foreach (var actionPlan in plan.Plan)
                planActionInstances.Add(GenerateActionInstance(actionPlan, domain));
            var combined = combiner.Combine(planActionInstances);
            combined.Name = newName.Replace(_metaActionName, _macroActionName);
            return combined;
        }

        private static ActionDecl GenerateActionInstance(GroundedAction action, DomainDecl domain)
        {
            action.ActionName = RemoveNumberSufix(action);
            ActionDecl target = domain.Actions.First(x => x.Name == action.ActionName).Copy();
            var allNames = target.FindTypes<NameExp>();
            for (int i = 0; i < action.Arguments.Count; i++)
            {
                var allRefs = allNames.Where(x => x.Name == target.Parameters.Values[i].Name).ToList();
                foreach (var referene in allRefs)
                    referene.Name = action.Arguments[i].Name;
            }
            return target;
        }

        // This is a very lazy solution...
        // I will deal with it later
        private static string RemoveNumberSufix(GroundedAction action)
        {
            for (int i = 0; i < 1000; i++)
                if (action.ActionName.EndsWith($"_{i}"))
                    return action.ActionName.Replace($"_{i}", "");
            return action.ActionName;
        }
    }
}
