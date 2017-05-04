using Microsoft.Build.Construction;
using Microsoft.MSBuildProjectBuilder;
using NUnit.Framework;
using Shouldly;
using System.Text.RegularExpressions;

namespace MSBuildProjectBuilder.UnitTest
{
    [TestFixture]
    public class ItemTest
    {
        private ProjectBuilder _project;

        [OneTimeSetUp]
        public void TestInitialize()
        {
            // UnitTest need to call .Create() to create a new empty project so test don't just keep adding to the current project.
            _project = new ProjectBuilder();
        }

        [Test]
        public void AddItem()
        {
            string expectedOutput =
@"<?xml version=""1.0"" encoding=""utf-16""?>
<Project ToolsVersion=""14.0"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
  <ItemGroup>
    <TestItem Include=""TestValue"" Label=""TestLabel"" Condition=""'true'=='true'"" />
  </ItemGroup>
</Project>";
            _project.Create()
                .AddItem("TestItem", "TestValue", "'true'=='true'", "TestLabel");
            _project.ProjectRoot.RawXml.NormalizeNewLine().ShouldBe(expectedOutput.NormalizeNewLine());

            ProjectItemGroupElement itemGroup;
            ProjectItemElement item;
            expectedOutput =
@"<?xml version=""1.0"" encoding=""utf-16""?>
<Project ToolsVersion=""14.0"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
  <ItemGroup Label=""newGroup"">
    <NewItem Include=""NewValue"" />
    <NewItem2 Include=""NewValue2"" />
  </ItemGroup>
</Project>";
            _project.Create()
                .AddItemGroup(null, "newGroup", out itemGroup)
                .AddItem("NewItem", "NewValue", null, null, itemGroup)
                .AddItem("NewItem2", "NewValue2", null, null, itemGroup, out item);
            _project.ProjectRoot.RawXml.NormalizeNewLine().ShouldBe(expectedOutput.NormalizeNewLine());
        }

        [Test]
        public void AddItemAfterItemElement()
        {
            ProjectItemGroupElement itemGroup;
            ProjectItemElement item;
            ProjectItemElement item2;
            string expectedOutput =
@"<?xml version=""1.0"" encoding=""utf-16""?>
<Project ToolsVersion=""14.0"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
  <ItemGroup Label=""newGroup"">
    <NewItem2 Include=""NewValue2"" />
    <Test2 Include=""value2"" />
    <NewItem Include=""NewValue"" />
  </ItemGroup>
</Project>";
            _project.Create()
                .AddItemGroup(null, "newGroup", out itemGroup)
                .AddItem("NewItem2", "NewValue2", null, null, itemGroup, out item)
                .AddItem("NewItem", "NewValue", null, null, itemGroup)
                .AddItemAfterItemElement("Test2", "value2", null, null, item, out item2);
            _project.ProjectRoot.RawXml.NormalizeNewLine().ShouldBe(expectedOutput.NormalizeNewLine());
        }

        [Test]
        public void AddItemBeforeItemElement()
        {
            ProjectItemGroupElement itemGroup;
            ProjectItemElement item;
            ProjectItemElement item2;
            string expectedOutput =
@"<?xml version=""1.0"" encoding=""utf-16""?>
<Project ToolsVersion=""14.0"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
  <ItemGroup Label=""newGroup"">
    <Test2 Include=""value2"" />
    <NewItem2 Include=""NewValue2"" />
    <NewItem Include=""NewValue"" />
  </ItemGroup>
</Project>";
            _project.Create()
                .AddItemGroup(null, "newGroup", out itemGroup)
                .AddItem("NewItem2", "NewValue2", null, null, itemGroup, out item)
                .AddItem("NewItem", "NewValue", null, null, itemGroup)
                .AddItemBeforeItemElement("Test2", "value2", null, null, item, out item2);
            _project.ProjectRoot.RawXml.NormalizeNewLine().ShouldBe(expectedOutput.NormalizeNewLine());
        }
    }
}
