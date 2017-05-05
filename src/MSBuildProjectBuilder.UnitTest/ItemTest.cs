using Microsoft.MSBuildProjectBuilder;
using NUnit.Framework;
using Shouldly;

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
  <ItemGroup Label=""groupLabel"">
    <item1 Include=""value1"" Label=""my item label"" Condition=""my item condition"" />
    <item2 Include=""value2"" Label=""my item label"" Condition=""my item condition"" />
  </ItemGroup>
</Project>";
            _project.Create()
                .AddItemGroup()
                .WithLabel("groupLabel")
                .AddItem(new[] { new Item("item1", "value1"), new Item("item2", "value2")})
                .WithLabel("my item label")
                .WithCondition("my item condition")
                .ProjectRoot.RawXml.NormalizeNewLine()
                .ShouldBe(expectedOutput.NormalizeNewLine());

            expectedOutput =
@"<?xml version=""1.0"" encoding=""utf-16""?>
<Project ToolsVersion=""14.0"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
  <ItemGroup>
    <item1 Include=""value1"" Label=""my item label"" Condition=""my item condition"" />
    <item2 Include=""value2"" Label=""my item label"" Condition=""my item condition"" />
  </ItemGroup>
  <ItemGroup>
    <Name Include=""Value"" />
  </ItemGroup>
</Project>";
            _project = new ProjectBuilder();
            _project.AddItem(new[] { new Item("item1", "value1"), new Item("item2", "value2") })
                .WithLabel("my item label")
                .WithCondition("my item condition")
                .AddItemGroup()
                .AddItem(new [] { new Item("Name", "Value")})
                .ProjectRoot.RawXml.NormalizeNewLine()
                .ShouldBe(expectedOutput.NormalizeNewLine());
        }
    }
}
