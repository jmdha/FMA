using PDDLSharp.Models.PDDL.Domain;

namespace P10
{
    public class DuplicateRemover
    {
        public List<ActionDecl> RemoveDuplicates(List<ActionDecl> candidates, DomainDecl domain)
        {
            var returnList = new List<ActionDecl>();
            var nonDuplicateIndexes = new List<int>();
            var annonymizedCandidates = GetAnnonymised(candidates);
            var annonymizedActions = GetAnnonymised(domain.Actions);

            for (int i = 0; i < candidates.Count; i++)
            {
                if (!annonymizedActions.Any(x => x.Equals(annonymizedCandidates[i])) &&
                    !annonymizedCandidates.GetRange(i + 1, candidates.Count - i - 1).Any(x => x.Equals(annonymizedCandidates[i])))
                {
                    nonDuplicateIndexes.Add(i);
                }
            }

            foreach (var index in nonDuplicateIndexes)
                returnList.Add(candidates[index]);
            return returnList;
        }

        private List<ActionDecl> GetAnnonymised(List<ActionDecl> actions)
        {
            var returnList = new List<ActionDecl>();

            foreach (var action in actions)
            {
                int argIndex = 0;
                var copy = action.Copy();
                copy.Name = "Action";
                foreach (var param in copy.Parameters.Values)
                {
                    var find = copy.FindNames(param.Name);
                    foreach (var found in find)
                        found.Name = $"?{argIndex}";
                    argIndex++;
                }
                returnList.Add(copy);
            }

            return returnList;
        }
    }
}
