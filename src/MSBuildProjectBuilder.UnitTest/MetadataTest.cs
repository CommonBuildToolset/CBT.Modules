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
            ProjectItemElement projectItem;
            string expectedOutput =
@"<?xml version=""1.0"" encoding=""utf-16""?>
<Project ToolsVersion=""14.0"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
  <ItemGroup>
    <TestItem Include=""TestValue"" Label=""TestLabel"" Condition=""'true'=='true'"">
      <Dummy />
    </TestItem>
  </ItemGroup>
</Project>";
            _project.Create()
                .AddItem("TestItem", "TestValue", "'true'=='true'", "TestLabel", null, out projectItem)
                .AddMetadata("Dummy", null, null, null, projectItem);
            _project.ProjectRoot.RawXml.NormalizeNewLine().ShouldBe(expectedOutput.NormalizeNewLine());

            ProjectItemGroupElement itemGroup;
            ProjectItemElement item;
            expectedOutput =
@"<?xml version=""1.0"" encoding=""utf-16""?>
<Project ToolsVersion=""14.0"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
  <ItemGroup Label=""newGroup"">
    <NewItem Include=""NewValue"" />
    <NewItem2 Include=""NewValue2"">
      <Dummy Label=""label"" Condition=""Condition"">Value</Dummy>
    </NewItem2>
  </ItemGroup>
</Project>";
            _project.Create()
                .AddItemGroup(null, "newGroup", out itemGroup)
                .AddItem("NewItem", "NewValue", null, null, itemGroup)
                .AddItem("NewItem2", "NewValue2", null, null, itemGroup, out item)
                .AddMetadata("Dummy", "Value", "Condition", "label", item);
            _project.ProjectRoot.RawXml.NormalizeNewLine().ShouldBe(expectedOutput.NormalizeNewLine());
        }

        [Test]
        public void AddMetadataAfterMetadataElement()
        {
            ProjectItemGroupElement itemGroup;
            ProjectItemElement item;
            ProjectItemElement item2;
            ProjectMetadataElement metadata;
            ProjectMetadataElement metadata2;
            string expectedOutput =
@"<?xml version=""1.0"" encoding=""utf-16""?>
<Project ToolsVersion=""14.0"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
  <ItemGroup Label=""newGroup"">
    <NewItem2 Include=""NewValue2"">
      <Dummy Label=""label"" Condition=""Condition"">Value</Dummy>
      <Dummy2 Label=""label2"" Condition=""Condition2"">Value2</Dummy2>
    </NewItem2>
    <Test2 Include=""value2"" />
    <NewItem Include=""NewValue"" />
  </ItemGroup>
</Project>";
            _project.Create()
                .AddItemGroup(null, "newGroup", out itemGroup)
                .AddItem("NewItem2", "NewValue2", null, null, itemGroup, out item)
                .AddMetadata("Dummy", "Value", "Condition", "label", item, out metadata)
                .AddMetadataAfterMetadataElement("Dummy2", "Value2", "Condition2", "label2", metadata, out metadata2)
                .AddItem("NewItem", "NewValue", null, null, itemGroup)
                .AddItemAfterItemElement("Test2", "value2", null, null, item, out item2);
            _project.ProjectRoot.RawXml.NormalizeNewLine().ShouldBe(expectedOutput.NormalizeNewLine());
        }

        [Test]
        public void AddMetadataBeforeMetadataElement()
        {
            ProjectItemGroupElement itemGroup;
            ProjectItemElement item;
            ProjectItemElement item2;
            ProjectMetadataElement metadata;
            ProjectMetadataElement metadata2;
            string expectedOutput =
@"<?xml version=""1.0"" encoding=""utf-16""?>
<Project ToolsVersion=""14.0"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
  <ItemGroup Label=""newGroup"">
    <NewItem2 Include=""NewValue2"">
      <Dummy2 Label=""label2"" Condition=""Condition2"">Value2</Dummy2>
      <Dummy Label=""label"" Condition=""Condition"">Value</Dummy>
    </NewItem2>
    <Test2 Include=""value2"" />
    <NewItem Include=""NewValue"" />
  </ItemGroup>
</Project>";
            _project.Create()
                .AddItemGroup(null, "newGroup", out itemGroup)
                .AddItem("NewItem2", "NewValue2", null, null, itemGroup, out item)
                .AddMetadata("Dummy", "Value", "Condition", "label", item, out metadata)
                .AddMetadataBeforeMetadataElement("Dummy2", "Value2", "Condition2", "label2", metadata, out metadata2)
                .AddItem("NewItem", "NewValue", null, null, itemGroup)
                .AddItemAfterItemElement("Test2", "value2", null, null, item, out item2);
            _project.ProjectRoot.RawXml.NormalizeNewLine().ShouldBe(expectedOutput.NormalizeNewLine());
        }
    }
}
