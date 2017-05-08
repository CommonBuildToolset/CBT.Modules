using Microsoft.MSBuildProjectBuilder;
using NUnit.Framework;
using Shouldly;
using System.Text.RegularExpressions;

namespace MSBuildProjectBuilder.UnitTest
{
    [TestFixture]
    public class ProjectTest
    {
        private ProjectBuilder _project;

        [OneTimeSetUp]
        public void TestInitialize()
        {
            _project = new ProjectBuilder();
        }

        [Test]
        public void Create()
        {
            string expectedOutput =
@"<?xml version=""1.0"" encoding=""utf-16""?>
<Project ToolsVersion=""4.0"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
</Project>";
            _project.Create()
                .ProjectRoot
                .RawXmlShouldBe(expectedOutput);

            expectedOutput =
@"<?xml version=""1.0"" encoding=""utf-16""?>
<Project ToolsVersion=""14.0"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"" DefaultTargets=""TestDefaultTarget"" InitialTargets=""TestInitialTarget"" Label=""TestLabel"">
</Project>";
            _project.Create("test.csproj", "14.0", "TestDefaultTarget", "TestInitialTarget", "TestLabel")
                .ProjectRoot
                .RawXmlShouldBe(expectedOutput);
        }
    }
}
