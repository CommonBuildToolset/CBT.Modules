using Microsoft.MSBuildProjectBuilder;
using Xunit;

namespace MSBuildProjectBuilder.UnitTests
{
    public class ItemGroupTest
    {

        [Fact]
        public void AddItemGroup()
        {
            string expectedOutput =
@"<?xml version=""1.0"" encoding=""utf-16""?>
<Project ToolsVersion=""4.0"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
  <ItemGroup Label=""test label"" Condition=""test Condition"" />
</Project>";
            ProjectBuilder.Create()
                .AddItemGroup("test Condition", "test label")
                .ProjectRoot
                .RawXmlShouldBe(expectedOutput);

            expectedOutput =
@"<?xml version=""1.0"" encoding=""utf-16""?>
<Project ToolsVersion=""4.0"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
  <ItemGroup />
  <ItemGroup Label=""test label"" Condition=""test Condition"" />
  <ItemGroup Condition=""New Condition"" />
</Project>";
            ProjectBuilder.Create()
                .AddItemGroup()
                .AddItemGroup("test Condition", "test label")
                .AddItemGroup("New Condition")
                .ProjectRoot
                .RawXmlShouldBe(expectedOutput);
        }
    }
}
