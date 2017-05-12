using Microsoft.MSBuildProjectBuilder;
using Xunit;

namespace MSBuildProjectBuilder.UnitTests
{

    public class ImportTest
    {

        [Fact]
        public void AddImport()
        {
            string expectedOutput =
@"<?xml version=""1.0"" encoding=""utf-16""?>
<Project ToolsVersion=""4.0"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
  <Import Project=""build.props"" />
</Project>";
            ProjectBuilder.Create()
                .AddImport("build.props")
                .ProjectRoot
                .RawXmlShouldBe(expectedOutput);

            expectedOutput =
@"<?xml version=""1.0"" encoding=""utf-16""?>
<Project ToolsVersion=""4.0"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
  <Import Project=""build.props"" Condition=""Condition"" Label=""Label"" />
  <Import Project=""build2.props"" Condition=""Condition2"" Label=""Label2"" />
  <ItemGroup />
  <PropertyGroup />
  <Import Project=""test.props"" />
</Project>";
            ProjectBuilder.Create()
                .AddImport(new[] { new Import("build.props","Condition","Label") })
                .AddImport(new[] { new Import("build2.props", "Condition2", "Label2") })
                .AddItemGroup()
                .AddPropertyGroup()
                .AddImport("test.props")
                .ProjectRoot
                .RawXmlShouldBe(expectedOutput);
        }
    }
}
