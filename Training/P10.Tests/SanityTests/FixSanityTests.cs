using PDDLSharp.ErrorListeners;
using PDDLSharp.Models.PDDL.Domain;
using PDDLSharp.Parsers.PDDL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Resources;
using System.Text;
using System.Threading.Tasks;

namespace P10.Tests.SanityTests
{
    [TestClass]
    public class FixSanityTests
    {
        [TestMethod]
        [DataRow("TestData/satellite/domain.pddl", "TestData/satellite/p01.pddl")]
        public void Can_RepairMetaActionsToNormalActions(string domain, string problem)
        {
            // ARRANGE
            var stackelbergPath = new FileInfo("../../../../../Dependencies/stackelberg-planner/src/fast-downward.py");
            Assert.IsTrue(stackelbergPath.Exists);
            var errorLiistener = new ErrorListener();
            var pddlParser = new PDDLParser(errorLiistener);
            var targetActions = pddlParser.ParseAs<DomainDecl>(new FileInfo(domain)).Actions;
            Assert.IsNotNull(targetActions);
            var outPath = "tempOut";
            var opts = new Options()
            {
                GeneratorStrategies = new List<MetaActionCandidateGenerator.Options.GeneratorStrategies>() { MetaActionCandidateGenerator.Options.GeneratorStrategies.StrippedMetaActions },
                DomainPath = domain,
                ProblemsPath = new List<string>() { problem },
                RefinementStrategy = Options.RefinementStrategies.GroundedPredicateAdditions,
                TimeLimitS = 60,
                OutputPath = outPath,
                StackelbergPath = stackelbergPath.FullName
            };

            // ACT
            P10.Run(opts);

            // ASSERT
            var enhancedDomainFile = new FileInfo(Path.Combine(outPath, "enhancedDomain.pddl"));
            Assert.IsTrue(enhancedDomainFile.Exists);
            var enhancedDomain = pddlParser.ParseAs<DomainDecl>(enhancedDomainFile);
            Assert.IsNotNull(enhancedDomain);
            
            foreach(var targetAction in targetActions)
            {

            }
        }
    }
}
