// Tests for the pure rules behind the project validator (Editor/Validation).
//
// The validator lives in its own editor assembly, HoldTheHill.Editor.Validation. These tests
// compile only once HoldTheHill.Tests references that assembly and the symbol
// HTH_VALIDATION_TESTS is defined for it (see Editor/Validation/README.md). Until then the file is
// empty on purpose, so the test assembly keeps compiling.
#if UNITY_EDITOR && HTH_VALIDATION_TESTS
using HoldTheHill.EditorTools.Validation;
using HoldTheHill.Features.Towers;
using System.Collections.Generic;
using NUnit.Framework;

namespace HoldTheHill.Tests
{
    /// <summary>
    /// Covers the logic that does not need the Asset Database: YAML reference scanning, class
    /// scanning, .meta rules, duplicate ids, skill prerequisites and report counting.
    /// </summary>
    public class GrayboxValidatorTests
    {
        private const string Yaml =
            "%YAML 1.1\n" +
            "%TAG !u! tag:unity3d.com,2011:\n" +
            "--- !u!1 &100\n" +
            "GameObject:\n" +
            "  m_Component:\n" +
            "  - component: {fileID: 200}\n" +
            "--- !u!114 &200\n" +
            "MonoBehaviour:\n" +
            "  m_Script: {fileID: 11500000, guid: 0123456789abcdef0123456789abcdef, type: 3}\n" +
            "  _icon: {fileID: 21300000, guid: FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF, type: 3}\n" +
            "  _none: {fileID: 0}\n" +
            "  _builtin: {fileID: 10202, guid: 0000000000000000e000000000000000, type: 0}\n" +
            "  _local: {fileID: 999}\n";

        private const string Source =
            "using UnityEngine;\n" +
            "// class Fake : MonoBehaviour { }\n" +
            "/* class AlsoFake : MonoBehaviour { } */\n" +
            "namespace N {\n" +
            "  public class Real : MonoBehaviour { string s = \"class Nope : MonoBehaviour {\"; }\n" +
            "  public abstract class Base : MonoBehaviour { }\n" +
            "  internal sealed class Second : Base, IFoo<int, string> { }\n" +
            "  public class Helper { }\n" +
            "}\n";

        // ---------- YAML references ----------

        [Test]
        public void YamlScanner_FindsGuidAndLocalReferences_AndSkipsNone()
        {
            List<YamlReference> references = YamlReferenceScanner.FindReferences(Yaml);

            Assert.AreEqual(5, references.Count, "fileID 0 is 'none' and must be skipped");

            YamlReference script = references.Find(r => r.Property == "m_Script");
            Assert.IsNotNull(script);
            Assert.AreEqual("0123456789abcdef0123456789abcdef", script.Guid);
            Assert.AreEqual(11500000L, script.FileId);
            Assert.AreEqual(9, script.Line);

            YamlReference icon = references.Find(r => r.Property == "_icon");
            Assert.AreEqual("ffffffffffffffffffffffffffffffff", icon.Guid, "guids are lower-cased");

            YamlReference local = references.Find(r => r.Property == "_local");
            Assert.IsTrue(local.IsLocal);
        }

        [Test]
        public void YamlScanner_FindsTheObjectsAFileDefines()
        {
            HashSet<long> anchors = YamlReferenceScanner.FindAnchors(Yaml);

            CollectionAssert.AreEquivalent(new long[] { 100, 200 }, anchors);
            Assert.IsFalse(anchors.Contains(999), "a local reference to 999 would be dangling");
        }

        [Test]
        public void YamlScanner_RecognisesBuiltInGuids_AndTextYaml()
        {
            Assert.IsTrue(YamlReferenceScanner.IsBuiltInGuid("0000000000000000e000000000000000"));
            Assert.IsTrue(YamlReferenceScanner.IsBuiltInGuid("0000000000000000f000000000000000"));
            Assert.IsFalse(YamlReferenceScanner.IsBuiltInGuid("0123456789abcdef0123456789abcdef"));

            Assert.IsTrue(YamlReferenceScanner.IsUnityYaml(Yaml));
            Assert.IsFalse(YamlReferenceScanner.IsUnityYaml("UnityFS\0binary"));
            Assert.IsFalse(YamlReferenceScanner.IsUnityYaml(null));
        }

        // ---------- class scanning ----------

        [Test]
        public void ClassScanner_IgnoresClassesInCommentsAndStrings()
        {
            List<DeclaredClass> classes = SourceClassScanner.Scan(Source);

            var names = new List<string>();
            foreach (DeclaredClass c in classes) names.Add(c.Name);

            CollectionAssert.AreEqual(new[] { "Real", "Base", "Second", "Helper" }, names);
        }

        [Test]
        public void ClassScanner_ReadsAbstractAndBaseNames()
        {
            List<DeclaredClass> classes = SourceClassScanner.Scan(Source);

            DeclaredClass baseClass = classes.Find(c => c.Name == "Base");
            Assert.IsTrue(baseClass.IsAbstract);

            DeclaredClass second = classes.Find(c => c.Name == "Second");
            Assert.IsFalse(second.IsAbstract);
            CollectionAssert.AreEqual(new[] { "Base", "IFoo" }, second.BaseNames);
        }

        [Test]
        public void MonoBehaviourNames_WithoutResolver_CountsDirectMonoBehaviourBasesOnly()
        {
            List<string> names = SourceClassScanner.MonoBehaviourNames(Source, null);

            CollectionAssert.AreEqual(new[] { "Real" }, names, "Base is abstract, Second derives indirectly");
        }

        [Test]
        public void MonoBehaviourNames_WithResolver_FindsEveryConcreteMonoBehaviour()
        {
            List<string> names = SourceClassScanner.MonoBehaviourNames(
                Source, name => name == "Real" || name == "Second" || name == "Base");

            CollectionAssert.AreEqual(new[] { "Real", "Second" }, names, "abstract classes are skipped");
        }

        [Test]
        public void ClassScanner_HandlesVerbatimStringsAndGenerics()
        {
            const string code =
                "public class Holder<T> where T : class {\n" +
                "  string path = @\"C:\\class X : MonoBehaviour {\";\n" +
                "}\n" +
                "public class Widget : MonoBehaviour { }\n";

            List<DeclaredClass> classes = SourceClassScanner.Scan(code);

            var names = new List<string>();
            foreach (DeclaredClass c in classes) names.Add(c.Name);
            CollectionAssert.AreEqual(new[] { "Holder", "Widget" }, names);
        }

        // ---------- .meta rules ----------

        [Test]
        public void MetaRules_TellWhichPathsUnityIgnores()
        {
            Assert.IsTrue(MetaFileRules.IsIgnoredByUnity("Assets/_Game/Balance~/sim-report.csv"));
            Assert.IsTrue(MetaFileRules.IsIgnoredByUnity("Assets/Foo/.hidden/file.png"));
            Assert.IsTrue(MetaFileRules.IsIgnoredByUnity("Assets/Foo/scratch.tmp"));
            Assert.IsFalse(MetaFileRules.IsIgnoredByUnity("Assets/Foo/Icon.png"));
            Assert.IsFalse(MetaFileRules.IsIgnoredByUnity("Assets/Foo/Icon.png.meta"));
        }

        [Test]
        public void MetaRules_SeparateSpritesFromOtherFiles_AndMapMetaToAsset()
        {
            Assert.IsTrue(MetaFileRules.IsSpriteLike("Assets/Icons/PNG/TowerLinearIcon.png"));
            Assert.IsTrue(MetaFileRules.IsSpriteLike("a/b/c.ASEPRITE"));
            Assert.IsFalse(MetaFileRules.IsSpriteLike("Assets/Scripts/Tower.cs"));

            Assert.IsTrue(MetaFileRules.IsMetaFile("a/b.png.meta"));
            Assert.AreEqual("a/b.png", MetaFileRules.AssetPathForMeta("a/b.png.meta"));
            Assert.AreEqual("a/b.png", MetaFileRules.AssetPathForMeta("a/b.png"));
        }

        // ---------- duplicate ids ----------

        [Test]
        public void DuplicateIds_ReportEachRepeatedId_WithItsOwners()
        {
            var input = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("a", "A1.asset"),
                new KeyValuePair<string, string>("b", "B.asset"),
                new KeyValuePair<string, string>("a", "A2.asset"),
                new KeyValuePair<string, string>("", "Empty1.asset"),
                new KeyValuePair<string, string>("", "Empty2.asset"),
                new KeyValuePair<string, string>("A", "Upper.asset"),
            };

            Dictionary<string, List<string>> duplicates = DuplicateIdRules.Find(input);

            Assert.AreEqual(1, duplicates.Count, "empty ids are ignored and ids are case-sensitive");
            CollectionAssert.AreEqual(new[] { "A1.asset", "A2.asset" }, duplicates["a"]);
        }

        // ---------- skill prerequisites ----------

        [Test]
        public void SkillGraph_AcceptsAValidChain()
        {
            var nodes = new List<SkillNodeRecord>
            {
                new SkillNodeRecord("root", null),
                new SkillNodeRecord("mid", "root"),
                new SkillNodeRecord("leaf", "mid"),
            };

            Assert.AreEqual(0, SkillGraphRules.Analyse(nodes).Count);
        }

        [Test]
        public void SkillGraph_FlagsMissingOutsideSelfAndCyclicPrerequisites()
        {
            var nodes = new List<SkillNodeRecord>
            {
                new SkillNodeRecord("gone", null, prerequisiteMissing: true),
                new SkillNodeRecord("outside", "not_in_tree"),
                new SkillNodeRecord("self", "self"),
                new SkillNodeRecord("loopA", "loopB"),
                new SkillNodeRecord("loopB", "loopA"),
                new SkillNodeRecord("fine", null),
            };

            List<SkillGraphProblem> problems = SkillGraphRules.Analyse(nodes);

            Assert.AreEqual(5, problems.Count);
            Assert.AreEqual(SkillGraphProblemKind.PrerequisiteMissing, Find(problems, "gone").Kind);
            Assert.AreEqual(SkillGraphProblemKind.PrerequisiteNotInTree, Find(problems, "outside").Kind);
            Assert.AreEqual(SkillGraphProblemKind.SelfPrerequisite, Find(problems, "self").Kind);
            Assert.AreEqual(SkillGraphProblemKind.Cycle, Find(problems, "loopA").Kind);
            Assert.AreEqual(SkillGraphProblemKind.Cycle, Find(problems, "loopB").Kind);
        }

        [Test]
        public void SkillGraph_NodeBehindACycle_IsNotReportedAsTheCycle()
        {
            var nodes = new List<SkillNodeRecord>
            {
                new SkillNodeRecord("loopA", "loopB"),
                new SkillNodeRecord("loopB", "loopA"),
                new SkillNodeRecord("tail", "loopA"),
            };

            List<SkillGraphProblem> problems = SkillGraphRules.Analyse(nodes);

            Assert.AreEqual(2, problems.Count);
            Assert.IsNull(problems.Find(p => p.NodeId == "tail"));
        }

        private static SkillGraphProblem Find(List<SkillGraphProblem> problems, string id)
        {
            SkillGraphProblem found = problems.Find(p => p.NodeId == id);
            Assert.IsNotNull(found, id);
            return found;
        }

        // ---------- report ----------

        [Test]
        public void Report_CountsBySeverity_AndExitsNonZeroOnlyForErrors()
        {
            var findings = new List<ValidationFinding>
            {
                new ValidationFinding(ValidationSeverity.Warning, "A", "w", "Assets/w.asset"),
                new ValidationFinding(ValidationSeverity.Info, "B", "i", null),
            };

            Assert.AreEqual(0, ValidationReport.ExitCode(findings));
            Assert.AreEqual(1, ValidationReport.Count(findings, ValidationSeverity.Warning));

            findings.Add(new ValidationFinding(ValidationSeverity.Error, "C", "e", "Assets/e.prefab"));

            Assert.AreEqual(1, ValidationReport.ExitCode(findings));
            Assert.AreEqual("1 error(s), 1 warning(s), 1 info", ValidationReport.Summary(findings));
        }

        [Test]
        public void Report_ListsWorstFirst()
        {
            var findings = new List<ValidationFinding>
            {
                new ValidationFinding(ValidationSeverity.Info, "A", "info message", null),
                new ValidationFinding(ValidationSeverity.Error, "B", "error message", null),
                new ValidationFinding(ValidationSeverity.Warning, "C", "warning message", null),
            };

            string text = ValidationReport.ToText(findings);

            int error = text.IndexOf("error message");
            int warning = text.IndexOf("warning message");
            int info = text.IndexOf("info message");
            Assert.Less(error, warning);
            Assert.Less(warning, info);
        }
    }
}
#endif
