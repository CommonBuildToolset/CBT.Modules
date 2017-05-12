using Microsoft.MSBuildProjectBuilder;
using Shouldly;
using System.IO;
using Xunit;

namespace MSBuildProjectBuilder.UnitTests
{
    public class ProjectTest
    {

        [Fact]
        public void Create()
        {
            string expectedOutput =
@"<?xml version=""1.0"" encoding=""utf-16""?>
<Project ToolsVersion=""4.0"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
</Project>";
            ProjectBuilder.Create()
                .ProjectRoot
                .RawXmlShouldBe(expectedOutput);

            expectedOutput =
@"<?xml version=""1.0"" encoding=""utf-16""?>
<Project ToolsVersion=""14.0"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"" DefaultTargets=""TestDefaultTarget"" InitialTargets=""TestInitialTarget"" Label=""TestLabel"">
</Project>";
            ProjectBuilder.Create("14.0", "TestDefaultTarget", "TestInitialTarget", "TestLabel")
                .ProjectRoot
                .RawXmlShouldBe(expectedOutput);
        }

        [Fact]
        public void Save()
        {
            string tmpFile = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName(), "test.proj");

            ProjectBuilder.Create("14.0", "TestDefaultTarget", "TestInitialTarget", "TestLabel")
                    .Save(tmpFile);
            File.Exists(tmpFile).ShouldBe(true);
            File.Delete(tmpFile);
        }
    }
}
