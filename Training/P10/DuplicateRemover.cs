using PDDLSharp.Models.PDDL.Domain;

namespace P10
{
    public static class DuplicateRemover
    {
        public static List<ActionDecl> Unique(this List<ActionDecl> candidates, DomainDecl domain)
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

        public static List<ActionDecl> Unique(this List<ActionDecl> candidates, DomainDecl domain, List<ActionDecl> others)
        {
            var returnList = new List<ActionDecl>();
            var nonDuplicateIndexes = new List<int>();
            var annonymizedCandidates = GetAnnonymised(candidates);
            var annonymizedActions = GetAnnonymised(domain.Actions);
            annonymizedActions.AddRange(GetAnnonymised(others));

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

        private static List<ActionDecl> GetAnnonymised(List<ActionDecl> actions)
        {
            var returnList = new List<ActionDecl>();

            foreach (var action in actions)
                returnList.Add(Annonymise(action));

            return returnList;
        }

        private static ActionDecl Annonymise(ActionDecl action)
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
            return copy;
        }
    }
}
