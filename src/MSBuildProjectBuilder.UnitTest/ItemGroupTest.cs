using Microsoft.Build.Construction;
using Microsoft.MSBuildProjectBuilder;
using NUnit.Framework;
using Shouldly;
using System.Text.RegularExpressions;

namespace MSBuildProjectBuilder.UnitTest
{
    [TestFixture]
    public class ItemGroupTest
    {
        private ProjectBuilder _project;

        [OneTimeSetUp]
        public void TestInitialize()
        {
            _project = new ProjectBuilder();
        }

        [Test]
        public void AddItemGroup()
        {
            string expectedOutput =
@"<?xml version=""1.0"" encoding=""utf-16""?>
<Project ToolsVersion=""14.0"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
  <ItemGroup Condition=""'true'=='true'"" Label=""GroupLabel"" />
</Project>";
            ProjectItemGroupElement itemGroup;
            _project.AddItemGroup("'true'=='true'", "GroupLabel", out itemGroup);
            _project.ProjectRoot.RawXml.NormalizeNewLine().ShouldBe(expectedOutput.NormalizeNewLine());
        }

        [Test]
        public void AddItemGroupAfterElement()
        {
            string expectedOutput =
@"<?xml version=""1.0"" encoding=""utf-16""?>
<Project ToolsVersion=""14.0"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
  <ItemGroup Condition=""'true'=='true'"" Label=""GroupLabel"" />
  <ItemGroup Condition=""'cat'!='dog'"" Label=""GroupLabel3"" />
  <ItemGroup Condition=""'true1'=='true1'"" Label=""GroupLabel2"" />
</Project>";
            ProjectItemGroupElement itemGroup1;
            ProjectItemGroupElement itemGroup2;
            ProjectItemGroupElement itemGroup3;
            _project.Create()
                .AddItemGroup("'true'=='true'", "GroupLabel", out itemGroup1)
                .AddItemGroup("'true1'=='true1'", "GroupLabel2", out itemGroup2)
                .AddItemGroupAfterElement("'cat'!='dog'", "GroupLabel3", itemGroup1, out itemGroup3);
            _project.ProjectRoot.RawXml.NormalizeNewLine().ShouldBe(expectedOutput.NormalizeNewLine());
        }

        [Test]
        public void AddItemGroupBeforeElement()
        {
            string expectedOutput =
@"<?xml version=""1.0"" encoding=""utf-16""?>
<Project ToolsVersion=""14.0"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
  <ItemGroup Condition=""'cat'!='dog'"" Label=""GroupLabel3"" />
  <ItemGroup Condition=""'true'=='true'"" Label=""GroupLabel"" />
  <ItemGroup Condition=""'true1'=='true1'"" Label=""GroupLabel2"" />
</Project>";
            ProjectItemGroupElement itemGroup1;
            ProjectItemGroupElement itemGroup2;
            ProjectItemGroupElement itemGroup3;
            _project.Create()
                .AddItemGroup("'true'=='true'", "GroupLabel", out itemGroup1)
                .AddItemGroup("'true1'=='true1'", "GroupLabel2", out itemGroup2)
                .AddItemGroupBeforeElement("'cat'!='dog'", "GroupLabel3", itemGroup1, out itemGroup3);
            _project.ProjectRoot.RawXml.NormalizeNewLine().ShouldBe(expectedOutput.NormalizeNewLine());
        }
    }
}
