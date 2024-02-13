using PDDLSharp.Models.PDDL.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace P10
{
    public class MetaActionRefiner
    {
        public ActionDecl OriginalMetaActionCandidate { get; }
        public ActionDecl RefinedMetaActionCandidate { get; }

        public MetaActionRefiner(ActionDecl metaActionCandidate)
        {
            OriginalMetaActionCandidate = metaActionCandidate.Copy();
            RefinedMetaActionCandidate = metaActionCandidate.Copy();
        }

        public void Refine()
        {

        }
    }
}
