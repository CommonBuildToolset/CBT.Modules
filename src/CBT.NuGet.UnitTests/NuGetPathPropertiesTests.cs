using CBT.NuGet.Internal;
using CBT.NuGet.Tasks;
using Shouldly;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CBT.UnitTests.Common;
using NuGet.Frameworks;
using NuGet.ProjectModel;
using NuGet.Versioning;
using Xunit;

namespace CBT.NuGet.UnitTests
{
    public class NuGetPathPropertiesTests : TestBase
    {
        private readonly string _packageReferenceRestoreFlagContents = @"{""RestoreOutputAbsolutePath"": ""#RestoreOutputPath#"",""PackageImportOrder"": [{""Id"": ""Newtonsoft.Json"",""Version"": ""6.0.3""}],""RestoreProjectStyle"": ""PackageReference"",""ProjectJsonPath"": """"}";
        private readonly string _packageConfigRestoreFlagContents = @"{""RestoreOutputAbsolutePath"": ""d:\\git\\CBT.Examples\\obj\\AnyCPU\\Debug\\ClassLibrary.csproj\\B34D2B84\\"",""PackageImportOrder"": [],""RestoreProjectStyle"": ""Unknown"",""ProjectJsonPath"": """"}";
        private readonly string _packageProjectJsonRestoreFlagContents = @"{""RestoreOutputAbsolutePath"": ""d:\\git\\CBT.Examples\\obj\\AnyCPU\\Debug\\ClassLibrary.csproj\\B34D2B84\\"",""PackageImportOrder"": [{""Id"": ""Newtonsoft.Json"",""Version"": ""6.0.1""}],""RestoreProjectStyle"": ""ProjectJson"",""ProjectJsonPath"": ""#JsonFile#""}";
        private readonly string _packageConfigFileContents = @"<packages>
    <package id=""Newtonsoft.Json"" version=""7.0.1""/>
    <package id=""Newtonsoft.Json"" version=""6.0.1""/>
</packages>";
        private readonly string _packageProjectJsonFileContents = @"{  ""dependencies"": {    ""NewtonSoft.Json"": ""6.0.3""  },  ""frameworks"": {    ""net45"": {}  },  ""runtimes"": {    ""win"": {}  }}";

        private readonly CBTTaskLogHelper _log = new CBTTaskLogHelper(new MockTask
        {
            BuildEngine = new MockBuildEngine()
        });

        [Fact]
        public void ReadJsonFileTest()
        {
            PackageRestoreData packageRestoreData = LoadPackageRestoreObject("foo.proj", GetTempFileName(), _packageReferenceRestoreFlagContents);
            packageRestoreData.RestoreProjectStyle.ShouldBe("PackageReference");
            packageRestoreData.ProjectJsonPath.ShouldBe(string.Empty);
            packageRestoreData.RestoreOutputAbsolutePath.ShouldBe("#RestoreOutputPath#");
            packageRestoreData.PackageImportOrder.Count.ShouldBe(1);
            packageRestoreData.PackageImportOrder.First().Id.ShouldBe("Newtonsoft.Json");
            packageRestoreData.PackageImportOrder.First().Version.ShouldBe("6.0.3");
        }

        [Fact]
        public void VerifyPackagesConfigParserTest()
        {
            string packageConfigFile = Path.Combine(TestRootPath, "packages.config");

            PackageRestoreData packageRestoreData = LoadPackageRestoreObject(packageConfigFile, GetTempFileName(), _packageConfigRestoreFlagContents);

            File.WriteAllText(packageConfigFile, _packageConfigFileContents);

            string packagePath = CreatePackagesFolder(new List<Tuple<string, string>>
            {
                new Tuple<string, string>("Newtonsoft.Json", "6.0.1"),
                new Tuple<string, string>("Newtonsoft.Json", "7.0.1")
            });

            MockSettings settings = new MockSettings
            {
                {
                    "config", new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        {"repositoryPath", packagePath},
                    }
                }
            };

            bool result = new NuGetPackagesConfigParser(settings, _log).TryGetPackages(packageConfigFile, packageRestoreData, out IEnumerable<PackageIdentityWithPath> packages);

            result.ShouldBeTrue();

            packageConfigFile = Path.Combine(TestRootPath, "foo.proj");

            packageRestoreData = LoadPackageRestoreObject(packageConfigFile, GetTempFileName(), _packageConfigRestoreFlagContents);

            result = new NuGetPackagesConfigParser(settings, _log).TryGetPackages(packageConfigFile, packageRestoreData, out packages);

            result.ShouldBeTrue();

            IList<PackageIdentityWithPath> packageIdentityWithPaths = packages as IList<PackageIdentityWithPath> ?? packages.ToList();
            packageIdentityWithPaths.Count.ShouldBe(2);
            packageIdentityWithPaths.Select(i => i.Id).ShouldBe(new [] { "Newtonsoft.Json", "Newtonsoft.Json" });
            packageIdentityWithPaths.Select(i => i.Version.ToString()).ShouldBe(new[] { "6.0.1", "7.0.1" });
        }

        [Fact]
        public void VerifyProjectJsonParserTest()
        {

            string projectJsonFile = Path.Combine(TestRootPath, "project.json");
            string projectLockJsonFile = Path.Combine(TestRootPath, "project.lock.json");
            string packageProjectJsonRestoreFlagContents = _packageProjectJsonRestoreFlagContents.Replace("#JsonFile#", projectJsonFile.Replace(@"\",@"\\"));
            PackageRestoreData packageRestoreData = LoadPackageRestoreObject(projectJsonFile, GetTempFileName(), packageProjectJsonRestoreFlagContents);
            File.WriteAllText(projectJsonFile, _packageProjectJsonFileContents);

            CreateProjectJsonLockFile(projectLockJsonFile, new List<Tuple<string, string>> { new Tuple<string, string>("Newtonsoft.Json", "6.0.3") });

            string packagePath = CreatePackagesFolder(new List<Tuple<string, string>> { new Tuple<string, string>("Newtonsoft.Json", "6.0.3") }, @"\");

            MockSettings settings = new MockSettings
            {
                {
                    "config", new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        {"globalPackagesFolder", packagePath},
                    }
                }
            };

            bool result = new NuGetProjectJsonParser(settings, _log).TryGetPackages(projectJsonFile, packageRestoreData, out IEnumerable<PackageIdentityWithPath> packages);

            result.ShouldBeTrue();

            IList<PackageIdentityWithPath> packageIdentityWithPaths = packages as IList<PackageIdentityWithPath> ?? packages.ToList();

            packageIdentityWithPaths.Count.ShouldBe(1);
            packageIdentityWithPaths.First().Id.ShouldBe("Newtonsoft.Json");
            packageIdentityWithPaths.First().Version.ToString().ShouldBe("6.0.3");

            projectJsonFile = Path.Combine(TestRootPath, "foo.proj");
            result = new NuGetProjectJsonParser(settings, _log).TryGetPackages(projectJsonFile, packageRestoreData, out packages);

            result.ShouldBeTrue();

            packageIdentityWithPaths = packages as IList<PackageIdentityWithPath> ?? packages.ToList();

            packageIdentityWithPaths.Count.ShouldBe(1);
            packageIdentityWithPaths.First().Id.ShouldBe("Newtonsoft.Json");
            packageIdentityWithPaths.First().Version.ToString().ShouldBe("6.0.3");
        }

        [Fact]
        public void VerifyPackageReferenceParserTest()
        {
            string projectPackageReferenceFile = Path.Combine(TestRootPath, "foo.proj");
            string projectAssetsJsonFile = Path.Combine(TestRootPath, "project.assets.json");
            PackageRestoreData packageRestoreData = LoadPackageRestoreObject(projectPackageReferenceFile, GetTempFileName(), _packageReferenceRestoreFlagContents.Replace("#RestoreOutputPath#",TestRootPath.Replace(@"\", @"\\")));

            CreateProjectAssetsJsonFile(projectAssetsJsonFile, (new List<Tuple<string, string>> { new Tuple<string, string>("Newtonsoft.Json", "6.0.3") }));

            string packagePath = CreatePackagesFolder( new List<Tuple<string, string>> { new Tuple<string, string>("Newtonsoft.Json", "6.0.3") }, @"\");

            MockSettings settings = new MockSettings
            {
                {
                    "config", new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        {"globalPackagesFolder", packagePath},
                    }
                }
            };

            bool result = new NuGetPackageReferenceProjectParser(settings, _log).TryGetPackages(packagePath, packageRestoreData, out IEnumerable<PackageIdentityWithPath> packages);

            result.ShouldBeTrue();

            IList<PackageIdentityWithPath> packageIdentityWithPaths = packages as IList<PackageIdentityWithPath> ?? packages.ToList();

            packageIdentityWithPaths.Count.ShouldBe(1);
            packageIdentityWithPaths.First().Id.ShouldBe("Newtonsoft.Json");
            packageIdentityWithPaths.First().Version.ToString().ShouldBe("6.0.3");

        }

        [Fact]
        public void ValidatePackagesConfigNuGetPropertyGeneratorTest()
        {
            string packageConfigFile = Path.Combine(TestRootPath, "packages.config");

            PackageRestoreData packageRestoreData = LoadPackageRestoreObject(packageConfigFile, GetTempFileName(), _packageConfigRestoreFlagContents);

            File.WriteAllText(packageConfigFile, _packageConfigFileContents);

            string outputFile = Path.Combine(TestRootPath, "output.props");

            string expectedOutputContent = $@"<?xml version=""1.0"" encoding=""utf-8""?>
<Project ToolsVersion=""4.0"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
  <PropertyGroup>
    <MSBuildAllProjects>$(MSBuildAllProjects);$(MSBuildThisFileFullPath)</MSBuildAllProjects>
    <NuGetPath_Newtonsoft_Json>{TestRootPath}\packages\Newtonsoft.Json.7.0.1</NuGetPath_Newtonsoft_Json>
    <NuGetVersion_Newtonsoft_Json>7.0.1</NuGetVersion_Newtonsoft_Json>
  </PropertyGroup>
  <ItemGroup>
    <CBTNuGetPackageDir Include=""{TestRootPath}\packages\Newtonsoft.Json.6.0.1"" />
    <CBTNuGetPackageDir Include=""{TestRootPath}\packages\Newtonsoft.Json.7.0.1"" />
  </ItemGroup>
</Project>";

            string packagePath = CreatePackagesFolder(
                new List<Tuple<string, string>>
                {
                    new Tuple<string, string>("Newtonsoft.Json", "7.0.1"),
                    new Tuple<string, string>("Newtonsoft.Json", "6.0.1"),
                },
                @"\");

            MockSettings settings = new MockSettings
            {
                {
                    "config", new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        {"repositoryPath", packagePath},
                    }
                }
            };

            new NuGetPropertyGenerator(_log, settings, packageConfigFile).Generate(outputFile, "NuGetVersion_", "NuGetPath_", packageRestoreData);

            _log.HasLoggedErrors.ShouldBeFalse();
            File.Exists(outputFile).ShouldBe(true);
            File.ReadAllText(outputFile).NormalizeNewLine().ShouldBe(expectedOutputContent.NormalizeNewLine());

        }

        [Fact]
        public void ValidatePackageReferenceNugetPropertyGeneratorTest()
        {
            string projectAssetsJsonFile = Path.Combine(TestRootPath, "project.assets.json");
            string projectPackageReferenceFile = Path.Combine(TestRootPath, "foo.proj");

            PackageRestoreData packageRestoreData = LoadPackageRestoreObject(projectPackageReferenceFile, GetTempFileName(), _packageReferenceRestoreFlagContents.Replace("#RestoreOutputPath#", TestRootPath.Replace(@"\", @"\\")));


            string outputFile = Path.Combine(TestRootPath, "output.props");
            string expectedOutputContent = $@"<?xml version=""1.0"" encoding=""utf-8""?>
<Project ToolsVersion=""4.0"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
  <PropertyGroup>
    <MSBuildAllProjects>$(MSBuildAllProjects);$(MSBuildThisFileFullPath)</MSBuildAllProjects>
    <NuGetPath_Newtonsoft_Json>{TestRootPath}\packages\newtonsoft.json\6.0.3</NuGetPath_Newtonsoft_Json>
    <NuGetVersion_Newtonsoft_Json>6.0.3</NuGetVersion_Newtonsoft_Json>
  </PropertyGroup>
  <ItemGroup>
    <CBTNuGetPackageDir Include=""{TestRootPath}\packages\newtonsoft.json\6.0.3"" />
  </ItemGroup>
</Project>";

            CreateProjectAssetsJsonFile(projectAssetsJsonFile, (new List<Tuple<string, string>> { new Tuple<string, string>("Newtonsoft.Json", "6.0.3") }));

            string packagePath = CreatePackagesFolder(new List<Tuple<string, string>> { new Tuple<string, string>("Newtonsoft.Json", "6.0.3") }, @"\");

            MockSettings settings = new MockSettings
            {
                {
                    "config", new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        {"globalPackagesFolder", packagePath},
                    }
                }
            };

            new NuGetPropertyGenerator(_log, settings, projectPackageReferenceFile).Generate(outputFile, "NuGetVersion_", "NuGetPath_", packageRestoreData);

            _log.HasLoggedErrors.ShouldBeFalse();
            File.Exists(outputFile).ShouldBe(true);
            File.ReadAllText(outputFile).NormalizeNewLine().ShouldBe(expectedOutputContent.NormalizeNewLine());
        }

        [Fact]
        public void ValidateProjectJsonNugetPropertyGeneratorTest()
        {
            string projectJsonFile = Path.Combine(TestRootPath, "project.json");
            string projectLockJsonFile = Path.Combine(TestRootPath, "project.lock.json");

            File.WriteAllText(projectJsonFile, _packageProjectJsonFileContents);

            string packageProjectJsonRestoreFlagContents = _packageProjectJsonRestoreFlagContents.Replace("#JsonFile#", projectJsonFile.Replace(@"\", @"\\"));

            PackageRestoreData packageRestoreData = LoadPackageRestoreObject(projectJsonFile, GetTempFileName(), packageProjectJsonRestoreFlagContents);

            CreateProjectJsonLockFile(projectLockJsonFile, new List<Tuple<string, string>> { new Tuple<string, string>("Newtonsoft.Json", "6.0.3") });

            string outputFile = Path.Combine(TestRootPath, "output.props");
            string expectedOutputContent = $@"<?xml version=""1.0"" encoding=""utf-8""?>
<Project ToolsVersion=""4.0"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
  <PropertyGroup>
    <MSBuildAllProjects>$(MSBuildAllProjects);$(MSBuildThisFileFullPath)</MSBuildAllProjects>
    <NuGetPath_Newtonsoft_Json>{TestRootPath}\packages\newtonsoft.json\6.0.3</NuGetPath_Newtonsoft_Json>
    <NuGetVersion_Newtonsoft_Json>6.0.3</NuGetVersion_Newtonsoft_Json>
  </PropertyGroup>
  <ItemGroup>
    <CBTNuGetPackageDir Include=""{TestRootPath}\packages\newtonsoft.json\6.0.3"" />
  </ItemGroup>
</Project>";

            string packagePath = CreatePackagesFolder(new List<Tuple<string, string>> { new Tuple<string, string>("Newtonsoft.Json", "6.0.3") }, @"\");

            MockSettings settings = new MockSettings
            {
                {
                    "config", new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        {"globalPackagesFolder", packagePath},
                    }
                }
            };

            new NuGetPropertyGenerator(_log, settings, projectJsonFile).Generate(outputFile, "NuGetVersion_", "NuGetPath_", packageRestoreData);

            _log.HasLoggedErrors.ShouldBeFalse();
            File.Exists(outputFile).ShouldBe(true);
            File.ReadAllText(outputFile).NormalizeNewLine().ShouldBe(expectedOutputContent.NormalizeNewLine());
        }


        private void CreateProjectJsonLockFile(string projectJsonLockFile, IList<Tuple<string, string>> packageList)
        {
            // Write out a project.lock.json file
            //
            new LockFileFormat().Write(projectJsonLockFile, new LockFile
            {
                Version = 1,
                Libraries = packageList.Select(i => new LockFileLibrary
                {
                    Name = i.Item1,
                    Version = new NuGetVersion(i.Item2),
                }).ToList(),
            });
        }

        private void CreateProjectAssetsJsonFile(string projectAssetsJsonFile, IList<Tuple<string, string>> packageList)
        {
            new LockFileFormat().Write(projectAssetsJsonFile, new LockFile
            {
                Version = 1,
                Libraries = packageList.Distinct().Select(i => new LockFileLibrary
                {
                    Name = i.Item1,
                    Version = new NuGetVersion(i.Item2),
                }).ToList(),
                Targets = new List<LockFileTarget>
                {
                    new LockFileTarget
                    {
                        TargetFramework = new NuGetFramework(".NETFramework,Version=v4.5"),
                        Libraries = packageList.Distinct().Select(i => new LockFileTargetLibrary {Name = i.Item1, Version = new NuGetVersion(i.Item2), Type = "package"}).ToList(),
                    }
                }
            });
        }

        private PackageRestoreData LoadPackageRestoreObject(string packageRestoreFile, string restoreFlag, string restoreContent)
        {
            string packageJsonFlagFile = restoreFlag;
            File.WriteAllText(packageJsonFlagFile, restoreContent);
            GenerateNuGetProperties genTask = new GenerateNuGetProperties
            {
                RestoreInfoFile = packageJsonFlagFile,
                PackageRestoreFile = packageRestoreFile
            };
            return genTask.GetPackageRestoreData();
        }

        private string CreatePackagesFolder(IList<Tuple<string,string>> dummyPackages, string idAndVersionDivider=".")
        {
            string packageFolder = Path.Combine(TestRootPath, "packages");
            Directory.CreateDirectory(packageFolder);
            foreach (Tuple<string, string> pkg in dummyPackages)
            {
                string dummyPackageFolder = Path.Combine(packageFolder, $"{pkg.Item1}{idAndVersionDivider}{pkg.Item2}");
                if (!Directory.Exists(dummyPackageFolder))
                {
                    Directory.CreateDirectory(dummyPackageFolder);
                }
            }
            return packageFolder;
        }
    }
}
