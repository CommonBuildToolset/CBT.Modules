using Microsoft.MSBuildProjectBuilder;
using Xunit;

namespace MSBuildProjectBuilder.UnitTest
{
    public class MetadataTest
    {

        [Fact]
        public void AddMetadata()
        {
            string expectedOutput =
@"<?xml version=""1.0"" encoding=""utf-16""?>
<Project ToolsVersion=""4.0"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
  <ItemGroup>
    <TestItem Include=""IncludeValue"">
      <TestMName>MValue</TestMName>
      <M2Name>M2Value</M2Name>
      <M3Name Condition=""test condition"" Label=""test label"">
      </M3Name>
      <M4Name>
      </M4Name>
    </TestItem>
    <name Include=""value"">
      <foo>bar</foo>
      <bar>baz</bar>
      <met Condition=""con"" Label=""lab"">cow</met>
      <TestMName>MValue</TestMName>
      <M2Name>M2Value</M2Name>
      <M3Name Condition=""test condition"" Label=""test label"">
      </M3Name>
      <M4Name>
      </M4Name>
    </name>
    <foo Include=""bar"">
      <TestMName>MValue</TestMName>
      <M2Name>M2Value</M2Name>
      <M3Name Condition=""test condition"" Label=""test label"">
      </M3Name>
      <M4Name>
      </M4Name>
    </foo>
  </ItemGroup>
</Project>";
            ProjectBuilder.Create()
                .AddItem(
                    "TestItem=IncludeValue",
                    new Item("name", "value", metadata: new ItemMetadata[] { "foo=bar", "bar=baz", new ItemMetadata("met", "cow", "con", "lab") }),
                    "foo=bar"
                )
                .WithItemMetadata("TestMName=MValue")
                .WithItemMetadata(
                        new ItemMetadata("M2Name", "M2Value"),
                        new ItemMetadata("M3Name", null, "test condition", "test label"),
                        new ItemMetadata("M4Name", string.Empty)
                 )
                .ProjectRoot
                .RawXmlShouldBe(expectedOutput);
        }
    }
}
