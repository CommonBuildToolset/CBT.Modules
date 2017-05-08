using Microsoft.MSBuildProjectBuilder;
using NUnit.Framework;
using Shouldly;

namespace MSBuildProjectBuilder.UnitTest
{
    [TestFixture]
    public class ItemGroupTest
    {
        private ProjectBuilder _project;
        [OneTimeSetUp]
        public void TestInitialize()
        {
            _project = new ProjectBuilder();
        }
        [Test]
        public void AddItemGroup()
        {
            string expectedOutput =
@"<?xml version=""1.0"" encoding=""utf-16""?>
<Project ToolsVersion=""4.0"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
  <ItemGroup Label=""test label"" Condition=""test Condition"" />
</Project>";
            _project.Create()
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
            _project = new ProjectBuilder();
            _project
                .AddItemGroup()
                .AddItemGroup("test Condition", "test label")
                .AddItemGroup("New Condition")
                .ProjectRoot
                .RawXmlShouldBe(expectedOutput);
        }
    }
}
