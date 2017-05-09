using Microsoft.MSBuildProjectBuilder;
using Xunit;

namespace MSBuildProjectBuilder.UnitTest
{
    public class PropertyGroupTest
    {

        [Fact]
        public void AddPropertyGroup()
        {
            string expectedOutput =
@"<?xml version=""1.0"" encoding=""utf-16""?>
<Project ToolsVersion=""4.0"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
  <PropertyGroup Label=""test label"" Condition=""test Condition"" />
</Project>";
            ProjectBuilder.Create()
                .AddPropertyGroup("test Condition", "test label")
                .ProjectRoot
                .RawXmlShouldBe(expectedOutput);

            expectedOutput =
@"<?xml version=""1.0"" encoding=""utf-16""?>
<Project ToolsVersion=""4.0"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
  <PropertyGroup />
  <PropertyGroup Label=""test label"" Condition=""test Condition"" />
  <PropertyGroup Condition=""New Condition"" />
</Project>";
            ProjectBuilder.Create()
                .AddPropertyGroup()
                .AddPropertyGroup("test Condition", "test label")
                .AddPropertyGroup("New Condition")
                .ProjectRoot
                .RawXmlShouldBe(expectedOutput);
        }
    }
}
