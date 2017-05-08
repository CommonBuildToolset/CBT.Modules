using Microsoft.Build.Construction;
using Microsoft.MSBuildProjectBuilder;
using NUnit.Framework;
using Shouldly;
using System.Text.RegularExpressions;

namespace MSBuildProjectBuilder.UnitTest
{
    [TestFixture]
    public class MetadataTest
    {
        private ProjectBuilder _project;

        [OneTimeSetUp]
        public void TestInitialize()
        {
            // UnitTest need to call .Create() to create a new empty project so test don't just keep adding to the current project.
            _project = new ProjectBuilder();
        }

        [Test]
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
            _project.Create()
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
