using CBT.NuGet.Tasks;
using CBT.UnitTests.Common;
using Shouldly;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace CBT.NuGet.UnitTests
{
    public class ImportBuildPackagesTests : TestBase
    {
        [Fact]
        public void Test()
        {
            MockBuildEngine buildEngine = new MockBuildEngine();

            string[] modulePaths =
            {
                CreateDirectory("NotABuildPackage", new Dictionary<string, string>
                {
                    { "foo.props", String.Empty }
                }),
                CreateDirectory("Module.One", new Dictionary<string, string>
                {
                    { @"build\module.config", String.Empty },
                    { @"build\Module.One.props", String.Empty },
                    { @"build\Module.One.targets", String.Empty },
                }),
                CreateDirectory("BuildPackage.One", new Dictionary<string, string>
                {
                    { @"build\BuildPackage.One.props", String.Empty },
                    { @"build\BuildPackage.One.targets", String.Empty },
                })
            };

            string propsFile = Path.Combine(TestRootPath, "NuGetBuildPackages.props");
            string targetsFile = Path.Combine(TestRootPath, "NuGetBuildPackages.targets");

            ImportBuildPackages importBuildPackages = new ImportBuildPackages
            {
                BuildEngine = buildEngine,
                PropsFile = propsFile,
                TargetsFile = targetsFile,
                ModulePaths = modulePaths.Select(i => $"{new DirectoryInfo(i).Name}={i}").ToArray()
            };

            importBuildPackages.Run();

            File.Exists(propsFile).ShouldBeTrue();
            File.Exists(targetsFile).ShouldBeTrue();

            File.ReadAllText(propsFile).ShouldBe(
                $@"<?xml version=""1.0"" encoding=""utf-8""?>
<Project ToolsVersion=""4.0"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
  <PropertyGroup>
    <EnableBuildPackage_One Condition="" '$(EnableBuildPackage_One)' == '' "">false</EnableBuildPackage_One>
    <RunBuildPackage_One Condition="" '$(RunBuildPackage_One)' == '' "">true</RunBuildPackage_One>
  </PropertyGroup>
  <Import Project=""{modulePaths[2]}\build\BuildPackage.One.props"" Condition="" '$(EnableBuildPackage_One)' == 'true' And '$(RunBuildPackage_One)' == 'true' "" />
</Project>",
                StringCompareShould.IgnoreLineEndings);

            File.ReadAllText(targetsFile).ShouldBe(
                $@"<?xml version=""1.0"" encoding=""utf-8""?>
<Project ToolsVersion=""4.0"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
  <Import Project=""{modulePaths[2]}\build\BuildPackage.One.targets"" Condition="" '$(EnableBuildPackage_One)' == 'true' And '$(RunBuildPackage_One)' == 'true' "" />
</Project>",
                StringCompareShould.IgnoreLineEndings);
        }
    }
}