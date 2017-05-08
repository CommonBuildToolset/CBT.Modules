using Microsoft.MSBuildProjectBuilder;
using Xunit;

namespace MSBuildProjectBuilder.UnitTest
{

    public class ItemTest
    {
        private ProjectBuilder _project = new ProjectBuilder();

        [Fact]
        public void AddItem()
        {
            string expectedOutput =
@"<?xml version=""1.0"" encoding=""utf-16""?>
<Project ToolsVersion=""4.0"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
  <ItemGroup Label=""groupLabel"">
    <item1 Include=""value1"" Label=""my item label"" Condition=""my item condition"" />
    <item2 Include=""value2"" Label=""my item label"" Condition=""my item condition"" />
  </ItemGroup>
</Project>";
            _project.Create()
                .AddItemGroup(label: "groupLabel")
                .AddItem( 
                    new Item("item1", "value1", "my item condition", "my item label"),
                    new Item("item2", "value2", "my item condition", "my item label")
                )
                .ProjectRoot
                .RawXmlShouldBe(expectedOutput);

            expectedOutput =
@"<?xml version=""1.0"" encoding=""utf-16""?>
<Project ToolsVersion=""4.0"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
  <ItemGroup>
    <item1 Include=""value1"" Label=""my item label"" Condition=""my item condition"" />
    <item2 Include=""value2"" Label=""my item label"" Condition=""my item condition"" />
  </ItemGroup>
  <ItemGroup>
    <Name Include=""Value"" />
  </ItemGroup>
</Project>";
            _project = new ProjectBuilder();
            _project.AddItem(
                        new Item("item1", "value1", "my item condition", "my item label"),
                        new Item("item2", "value2", "my item condition", "my item label")
                )
                .AddItemGroup()
                .AddItem("Name=Value")
                .ProjectRoot
                .RawXmlShouldBe(expectedOutput);
        }
    }
}
