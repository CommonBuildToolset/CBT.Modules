using Microsoft.MSBuildProjectBuilder;
using Xunit;

namespace MSBuildProjectBuilder.UnitTest
{

    public class PropertyTest
    {

        [Fact]
        public void AddProperty()
        {
            string expectedOutput =
@"<?xml version=""1.0"" encoding=""utf-16""?>
<Project ToolsVersion=""4.0"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
  <PropertyGroup Label=""groupLabel"">
    <item1 Label=""my item label"" Condition=""my item condition"">value1</item1>
    <item2 Label=""my item label"" Condition=""my item condition"">value2</item2>
  </PropertyGroup>
</Project>";
            ProjectBuilder.Create()
                .AddPropertyGroup(label: "groupLabel")
                .AddProperty( 
                    new Property("item1", "value1", "my item condition", "my item label"),
                    new Property("item2", "value2", "my item condition", "my item label")
                )
                .ProjectRoot
                .RawXmlShouldBe(expectedOutput);

            expectedOutput =
@"<?xml version=""1.0"" encoding=""utf-16""?>
<Project ToolsVersion=""4.0"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
  <PropertyGroup>
    <item1 Label=""my item label"" Condition=""my item condition"">value1</item1>
    <item2 Label=""my item label"" Condition=""my item condition"">value2</item2>
  </PropertyGroup>
  <PropertyGroup>
    <Name>Value</Name>
  </PropertyGroup>
</Project>";
            ProjectBuilder.Create()
                .AddProperty(
                        new Property("item1", "value1", "my item condition", "my item label"),
                        new Property("item2", "value2", "my item condition", "my item label")
                )
                .AddPropertyGroup()
                .AddProperty("Name=Value")
                .ProjectRoot
                .RawXmlShouldBe(expectedOutput);
        }
    }
}
