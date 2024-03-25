using PDDLSharp.ErrorListeners;
using PDDLSharp.Models.PDDL.Domain;
using PDDLSharp.Models.PDDL.Overloads;
using PDDLSharp.Parsers.PDDL;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        [DataRow("TestData/satellite/domain.pddl", "TestData/satellite/p10.pddl", "switch_off")]
        [DataRow("TestData/ferry/domain.pddl", "TestData/ferry/p10.pddl", "board", "debark")]
        public void Can_RepairMetaActionsToNormalActions(string domain, string problem, params string[] expectedActions)
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
            var metaActions = enhancedDomain.Actions.Where(x => x.Name.Contains('$'));

            foreach (var expected in expectedActions)
            {
                var action = targetActions.First(x => x.Name == expected);
                Assert.IsNotNull(action);

                var anonym = action.Copy().Annonymise();
                bool any = false;
                foreach (var meta in metaActions)
                {
                    if (!meta.Name.Contains(expected))
                        continue;
                    var metaAnonym = meta.Copy().Annonymise();
                    if (anonym.Equals(metaAnonym))
                    {
                        any = true;
                        break;
                    }
                }
                if (!any)
                {
                    Console.WriteLine($"Expected a meta action to be equivalent to '{expected}'!");
                    Assert.Fail();
                }
            }
        }
    }
}
