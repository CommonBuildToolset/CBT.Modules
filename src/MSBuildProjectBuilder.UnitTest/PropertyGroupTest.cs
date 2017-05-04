using Microsoft.Build.Construction;
using Microsoft.MSBuildProjectBuilder;
using NUnit.Framework;
using Shouldly;
using System.Text.RegularExpressions;

namespace MSBuildProjectBuilder.UnitTest
{
    [TestFixture]
    public class PropertyGroupTest
    {
        private ProjectBuilder _project;

        [OneTimeSetUp]
        public void TestInitialize()
        {
            _project = new ProjectBuilder();
        }

        [Test]
        public void AddPropertyGroup()
        {
            string expectedOutput =
@"<?xml version=""1.0"" encoding=""utf-16""?>
<Project ToolsVersion=""14.0"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
  <PropertyGroup Condition=""'true'=='true'"" Label=""GroupLabel"" />
</Project>";
            ProjectPropertyGroupElement propertyGroup;
            _project.AddPropertyGroup("'true'=='true'", "GroupLabel", out propertyGroup);
            _project.ProjectRoot.RawXml.NormalizeNewLine().ShouldBe(expectedOutput.NormalizeNewLine());
        }

        [Test]
        public void AddPropertyGroupAfterElement()
        {
            string expectedOutput =
@"<?xml version=""1.0"" encoding=""utf-16""?>
<Project ToolsVersion=""14.0"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
  <PropertyGroup Condition=""'true'=='true'"" Label=""GroupLabel"" />
  <PropertyGroup Condition=""'cat'!='dog'"" Label=""GroupLabel3"" />
  <PropertyGroup Condition=""'true1'=='true1'"" Label=""GroupLabel2"" />
</Project>";
            ProjectPropertyGroupElement propertyGroup1;
            ProjectPropertyGroupElement propertyGroup2;
            ProjectPropertyGroupElement propertyGroup3;
            _project.Create()
                .AddPropertyGroup("'true'=='true'", "GroupLabel", out propertyGroup1)
                .AddPropertyGroup("'true1'=='true1'", "GroupLabel2", out propertyGroup2)
                .AddPropertyGroupAfterElement("'cat'!='dog'","GroupLabel3", propertyGroup1, out propertyGroup3);
            _project.ProjectRoot.RawXml.NormalizeNewLine().ShouldBe(expectedOutput.NormalizeNewLine());
        }

        [Test]
        public void AddPropertyGroupBeforeElement()
        {
            string expectedOutput =
@"<?xml version=""1.0"" encoding=""utf-16""?>
<Project ToolsVersion=""14.0"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
  <PropertyGroup Condition=""'cat'!='dog'"" Label=""GroupLabel3"" />
  <PropertyGroup Condition=""'true'=='true'"" Label=""GroupLabel"" />
  <PropertyGroup Condition=""'true1'=='true1'"" Label=""GroupLabel2"" />
</Project>";
            ProjectPropertyGroupElement propertyGroup1;
            ProjectPropertyGroupElement propertyGroup2;
            ProjectPropertyGroupElement propertyGroup3;
            _project.Create()
                .AddPropertyGroup("'true'=='true'", "GroupLabel", out propertyGroup1)
                .AddPropertyGroup("'true1'=='true1'", "GroupLabel2", out propertyGroup2)
                .AddPropertyGroupBeforeElement("'cat'!='dog'", "GroupLabel3", propertyGroup1, out propertyGroup3);
            _project.ProjectRoot.RawXml.NormalizeNewLine().ShouldBe(expectedOutput.NormalizeNewLine());
        }
    }
}
