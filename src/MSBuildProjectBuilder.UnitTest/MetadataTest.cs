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
<Project ToolsVersion=""14.0"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
  <ItemGroup>
    <TestItem Include=""IncludeValue"">
      <TestMName Condition=""MyCondition"" Label=""MyLabel"">MValue</TestMName>
      <M2Name>M2Value</M2Name>
      <M3Name Label=""Rabbit"" />
    </TestItem>
  </ItemGroup>
</Project>";
            _project.Create()
                .AddItem(new[] { new Item("TestItem", "IncludeValue") })
                .AddMetadata(new[] { new Metadata("TestMName", "MValue") })
                .WithCondition("MyCondition")
                .WithLabel("MyLabel")
                .AddMetadata(new[] { new Metadata("M2Name", "M2Value") })
                .AddMetadata(new[] { new Metadata("M3Name", null) })
                .WithLabel("Rabbit")
                .ProjectRoot.RawXml.NormalizeNewLine()
                .ShouldBe(expectedOutput.NormalizeNewLine());
        }
    }
}
