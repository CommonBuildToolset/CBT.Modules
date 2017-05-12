using Microsoft.MSBuildProjectBuilder;
using Xunit;

namespace MSBuildProjectBuilder.UnitTests
{
    public class TargetTest
    {

        [Fact]
        public void AddTarget()
        {
            string expectedOutput =
@"<?xml version=""1.0"" encoding=""utf-16""?>
<Project ToolsVersion=""4.0"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
  <PropertyGroup Label=""test label"" Condition=""test Condition"" />
  <Target Name=""foo"">
    <PropertyGroup>
      <fun>bar</fun>
    </PropertyGroup>
    <ItemGroup>
      <testing Include=""cows"" />
    </ItemGroup>
  </Target>
  <PropertyGroup>
    <outOfTarget>hello</outOfTarget>
  </PropertyGroup>
</Project>";
            ProjectBuilder.Create()
                .AddPropertyGroup("test Condition", "test label")
                .AddTarget("foo")
                .AddProperty("fun=bar")
                .AddItem("testing=cows")
                .ExitTarget()
                .AddProperty("outOfTarget=hello")
                .ProjectRoot
                .RawXmlShouldBe(expectedOutput);

            expectedOutput =
@"<?xml version=""1.0"" encoding=""utf-16""?>
<Project ToolsVersion=""4.0"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
  <Target Name=""foo"" AfterTargets=""after"" BeforeTargets=""before"" DependsOnTargets=""dep"" Inputs=""inp"" Outputs=""out"" Label=""lab"" Condition=""con"" />
  <PropertyGroup>
    <test>here</test>
  </PropertyGroup>
  <Target Name=""cow"" />
  <Target Name=""bar"">
    <PropertyGroup>
      <intarget>here</intarget>
    </PropertyGroup>
  </Target>
</Project>";
            ProjectBuilder.Create()
                .AddTarget("foo","con","lab","after","before","dep","inp","out", false)
                .ExitTarget()
                .AddProperty("test=here")
                .AddTarget("cow")
                .AddTarget("bar")
                .AddProperty("intarget=here")
                .ProjectRoot
                .RawXmlShouldBe(expectedOutput);
        }
    }
}
