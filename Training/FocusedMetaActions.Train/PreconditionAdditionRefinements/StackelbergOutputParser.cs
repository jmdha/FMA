using PDDLSharp.ErrorListeners;
using PDDLSharp.Models.PDDL;
using PDDLSharp.Models.PDDL.Domain;
using PDDLSharp.Models.PDDL.Expressions;
using PDDLSharp.Models.PDDL.Overloads;
using PDDLSharp.Parsers.PDDL;

namespace FocusedMetaActions.Train.PreconditionAdditionRefinements
{
    public static class StackelbergOutputParser
    {
        /// <summary>
        /// This is a rather lazy parsing method for the output of the state exploration from the Stackelberg Planner.
        /// </summary>
        /// <param name="currentMetaAction"></param>
        /// <param name="workingDir"></param>
        /// <param name="closedList"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static List<PreconditionState> ParseOutput(ActionDecl currentMetaAction, string workingDir, List<PreconditionState> closedList)
        {
            currentMetaAction.EnsureAnd();
            var targetFile = new FileInfo(Path.Combine(workingDir, StateExploreVerifier.StateInfoFile));

            var listener = new ErrorListener();
            var parser = new PDDLParser(listener);
            var toCheck = new List<PreconditionState>();

            var text = File.ReadAllText(targetFile.FullName);
            var lines = text.Split('\n').ToList();
            var checkedMetaActions = new HashSet<ActionDecl>();
            for (int i = 2; i < lines.Count; i += 3)
            {
                if (i + 4 > lines.Count)
                    break;

                var applicability = Convert.ToInt32(lines[i + 2]);
                if (applicability == 0)
                    continue;

                var metaAction = currentMetaAction.Copy();
                var preconditions = new List<IExp>();

                var typesStr = lines[i].Trim();
                var types = new List<string>();
                if (typesStr != "")
                {
                    types = typesStr.Split(' ').ToList();
                    types.RemoveAll(x => x == "");
                }

                var facts = lines[i + 1].Split('|');
                foreach (var fact in facts)
                {
                    if (fact == "")
                        continue;
                    bool isNegative = fact.Contains("NegatedAtom");
                    var predText = fact.Replace("NegatedAtom", "").Replace("Atom", "").Trim();
                    var predName = predText;
                    var paramString = "";
                    if (predText.Contains(' '))
                    {
                        predName = predText.Substring(0, predText.IndexOf(' ')).Trim();
                        paramString = predText.Substring(predText.IndexOf(' ')).Trim();
                    }
                    var paramStrings = paramString.Split(' ');

                    var newPredicate = new PredicateExp(predName);
                    foreach (var item in paramStrings)
                    {
                        if (item == "")
                            continue;
                        var index = Int32.Parse(item);
                        if (index >= metaAction.Parameters.Values.Count)
                        {
                            if (types.Count == 0)
                                throw new Exception("Added precondition is trying to reference a added parameter, but said parameter have not been added! (Stackelberg Output Malformed)");
                            var newNamed = new NameExp($"?{item.Trim()}", new TypeExp(types[metaAction.Parameters.Values.Count - index]));
                            metaAction.Parameters.Values.Add(newNamed);
                            newPredicate.Arguments.Add(newNamed);
                        }
                        else
                        {
                            var param = metaAction.Parameters.Values[index];
                            newPredicate.Arguments.Add(new NameExp(param.Name));
                        }
                    }

                    if (isNegative)
                        preconditions.Add(new NotExp(newPredicate));
                    else
                        preconditions.Add(newPredicate);
                }

                if (metaAction.Preconditions is AndExp and)
                {
                    // Remove preconditions that have the same effect
                    if (metaAction.Effects is AndExp effAnd)
                    {
                        var cpy = effAnd.Copy();
                        cpy.RemoveContext();
                        cpy.RemoveTypes();
                        if (cpy.Children.All(x => preconditions.Any(y => y.Equals(x))))
                            continue;
                        if (IsSubset(preconditions, cpy.Children))
                            continue;
                    }

                    // Prune some nonsensical preconditions, before the new ones are added
                    var preCpy = and.Copy();
                    preCpy.RemoveContext();
                    preCpy.RemoveTypes();
                    if (preconditions.All(x => preCpy.Children.Contains(x)))
                        continue;
                    if (preconditions.Any(x => preCpy.Children.Contains(new NotExp(x))))
                        continue;
                    if (preconditions.Any(x => x is NotExp not && preCpy.Children.Contains(not.Child)))
                        continue;

                    // Prune some nonsensical preconditions.
                    if (preconditions.Any(x => preconditions.Contains(new NotExp(x))))
                        continue;
                    if (preconditions.Any(x => x is NotExp not && preconditions.Contains(not.Child)))
                        continue;

                    foreach (var precon in preconditions)
                        if (!and.Children.Contains(precon))
                            and.Children.Add(precon);

                    // Prune some nonsensical preconditions, after the new ones are added
                    preCpy = and.Copy();
                    preCpy.RemoveContext();
                    preCpy.RemoveTypes();
                    if (preCpy.Any(x => preCpy.Contains(new NotExp(x))))
                        continue;
                    if (preCpy.Any(x => x is NotExp not && preCpy.Contains(not.Child)))
                        continue;
                }

                if (!checkedMetaActions.Contains(metaAction))
                {
                    checkedMetaActions.Add(metaAction);
                    var newState = new PreconditionState(
                        applicability,
                        metaAction,
                        preconditions,
                        -1);
                    if (!closedList.Contains(newState))
                        toCheck.Add(newState);
                }
            }
            return toCheck;
        }

        private static bool IsSubset(List<IExp> set1, List<IExp> set2)
        {
            foreach (var item in set1)
                if (!set2.Contains(item))
                    return false;
            return true;
        }
    }
}
