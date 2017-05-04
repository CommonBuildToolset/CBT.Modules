using System;
using Microsoft.Build.Construction;
using Microsoft.MSBuildProjectBuilder;
using NUnit.Framework;
using Shouldly;

namespace MSBuildProjectBuilder.UnitTest
{
    [TestFixture]
    public class PropertyTest
    {
        private ProjectBuilder _project;

        [OneTimeSetUp]
        public void TestInitialize()
        {
            // UnitTest need to call .Create() to create a new empty project so test don't just keep adding to the current project.
            _project = new ProjectBuilder();
        }

        [Test]
        public void AddProperty()
        {
            string expectedOutput =
@"<?xml version=""1.0"" encoding=""utf-16""?>
<Project ToolsVersion=""14.0"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
  <PropertyGroup>
    <TestProperty Label=""TestLabel"" Condition=""'true'=='true'"">TestValue</TestProperty>
  </PropertyGroup>
</Project>";            
            _project.Create()
                .AddProperty("TestProperty", "TestValue", "'true'=='true'", "TestLabel");
            _project.ProjectRoot.RawXml.ShouldBe(expectedOutput);

            expectedOutput =
@"<?xml version=""1.0"" encoding=""utf-16""?>
<Project ToolsVersion=""14.0"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
  <PropertyGroup>
    <TestProperty />
  </PropertyGroup>
</Project>";
            _project.Create()
                .AddProperty("TestProperty");
            _project.ProjectRoot.RawXml.ShouldBe(expectedOutput);

            ProjectPropertyGroupElement propertyGroup;
            ProjectPropertyElement property;
            expectedOutput =
@"<?xml version=""1.0"" encoding=""utf-16""?>
<Project ToolsVersion=""14.0"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
  <PropertyGroup>
    <TestProperty />
  </PropertyGroup>
  <PropertyGroup Label=""newGroup"">
    <NewProperty>NewValue</NewProperty>
    <NewProperty2>NewValue2</NewProperty2>
  </PropertyGroup>
</Project>";
            _project.Create()
                .AddProperty("TestProperty")
                .AddPropertyGroup(null, "newGroup", out propertyGroup)
                .AddProperty("NewProperty", "NewValue", null, null, propertyGroup)
                .AddProperty("NewProperty2", "NewValue2", null, null, propertyGroup, out property);
            _project.ProjectRoot.RawXml.ShouldBe(expectedOutput);
        }

        [Test]
        public void AddPropertyAfterPropertyElement()
        {
            ProjectPropertyGroupElement propertyGroup;
            ProjectPropertyElement property;
            ProjectPropertyElement property2;
            string expectedOutput =
@"<?xml version=""1.0"" encoding=""utf-16""?>
<Project ToolsVersion=""14.0"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
  <PropertyGroup Label=""newGroup"">
    <NewProperty2>NewValue2</NewProperty2>
    <Test2>value2</Test2>
    <NewProperty>NewValue</NewProperty>
  </PropertyGroup>
</Project>";
            _project.Create()
                .AddPropertyGroup(null, "newGroup", out propertyGroup)
                .AddProperty("NewProperty2", "NewValue2", null, null, propertyGroup, out property)
                .AddProperty("NewProperty", "NewValue", null, null, propertyGroup)
                .AddPropertyAfterPropertyElement("Test2", "value2", null, null, property, out property2);
            _project.ProjectRoot.RawXml.ShouldBe(expectedOutput);
        }

        [Test]
        public void AddPropertyBeforePropertyElement()
        {
            ProjectPropertyGroupElement propertyGroup;
            ProjectPropertyElement property;
            ProjectPropertyElement property2;
            string expectedOutput =
@"<?xml version=""1.0"" encoding=""utf-16""?>
<Project ToolsVersion=""14.0"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
  <PropertyGroup Label=""newGroup"">
    <Test2>value2</Test2>
    <NewProperty2>NewValue2</NewProperty2>
    <NewProperty>NewValue</NewProperty>
  </PropertyGroup>
</Project>";
            _project.Create()
                .AddPropertyGroup(null, "newGroup", out propertyGroup)
                .AddProperty("NewProperty2", "NewValue2", null, null, propertyGroup, out property)
                .AddProperty("NewProperty", "NewValue", null, null, propertyGroup)
                .AddPropertyBeforePropertyElement("Test2", "value2", null, null, property, out property2);
            _project.ProjectRoot.RawXml.ShouldBe(expectedOutput);
        }
    }
}
