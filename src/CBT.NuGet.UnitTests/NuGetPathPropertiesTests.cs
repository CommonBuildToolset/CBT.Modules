using CBT.NuGet.Internal;
using CBT.NuGet.Tasks;
using Shouldly;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NuGet.Frameworks;
using NuGet.ProjectModel;
using NuGet.Versioning;
using Xunit;

namespace CBT.NuGet.UnitTests
{
    public class NuGetPathPropertiesTests : TestBase
    {
        private readonly string _packageReferenceRestoreFlagContents = @"{""RestoreOutputAbsolutePath"": ""#RestoreOutputPath#"",""PackageImportOrder"": [{""Id"": ""Newtonsoft.Json"",""Version"": ""6.0.3""}],""RestoreProjectStyle"": ""PackageReference"",""ProjectJsonPath"": """"}";
        private readonly string _packageConfigRestoreFlagContents = @"{""RestoreOutputAbsolutePath"": ""d:\\git\\CBT.Examples\\obj\\AnyCPU\\Debug\\ClassLibrary.csproj\\B34D2B84\\"",""PackageImportOrder"": [{""Id"": ""Newtonsoft.Json"",""Version"": ""6.0.1""}],""RestoreProjectStyle"": ""Unknown"",""ProjectJsonPath"": """"}";
        private readonly string _packageProjectJsonRestoreFlagContents = @"{""RestoreOutputAbsolutePath"": ""d:\\git\\CBT.Examples\\obj\\AnyCPU\\Debug\\ClassLibrary.csproj\\B34D2B84\\"",""PackageImportOrder"": [{""Id"": ""Newtonsoft.Json"",""Version"": ""6.0.1""}],""RestoreProjectStyle"": ""ProjectJson"",""ProjectJsonPath"": ""#JsonFile#""}";
        private readonly string _packageConfigFileContents = @"<packages><package id=""Newtonsoft.Json"" version=""6.0.1""/></packages>";
        private readonly string _packageProjectJsonFileContents = @"{  ""dependencies"": {    ""NewtonSoft.Json"": ""6.0.3""  },  ""frameworks"": {    ""net45"": {}  },  ""runtimes"": {    ""win"": {}  }}";

        [Fact]
        public void ReadJsonFileTest()
        {
            var packageRestoreData = LoadPackageRestoreObject(GetTempFileName(), _packageReferenceRestoreFlagContents);
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

            var packageRestoreData = LoadPackageRestoreObject(GetTempFileName(), _packageConfigRestoreFlagContents);
            string packageConfigFile = Path.Combine(TestRootPath, "packages.config");
            File.WriteAllText(packageConfigFile, _packageConfigFileContents);
            var packagePath =
                CreatePackagesFolder(
                    new List<Tuple<string, string>>() {new Tuple<string, string>("Newtonsoft.Json", "6.0.1")});
            var packages = (new NuGetPackagesConfigParser()).GetPackages(packagePath, packageConfigFile, packageRestoreData).ToDictionary(i => $"{i.Id}.{i.Version}", i => i, StringComparer.OrdinalIgnoreCase);
            packages.Count.ShouldBe(1);

            packageConfigFile = Path.Combine(TestRootPath, "foo.proj");
            packages = (new NuGetPackagesConfigParser()).GetPackages(packagePath, packageConfigFile, packageRestoreData).ToDictionary(i => $"{i.Id}.{i.Version}", i => i, StringComparer.OrdinalIgnoreCase);
            packages.Count.ShouldBe(1);
            packages.First().Value.Id.ShouldBe("Newtonsoft.Json");
            packages.First().Value.Version.ToString().ShouldBe("6.0.1");
        }

        [Fact]
        public void VerifyProjectJsonParserTest()
        {

            string projectJsonFile = Path.Combine(TestRootPath, "project.json");
            string projectLockJsonFile = Path.Combine(TestRootPath, "project.lock.json");
            var packageProjectJsonRestoreFlagContents =
                _packageProjectJsonRestoreFlagContents.Replace("#JsonFile#", projectJsonFile.Replace(@"\",@"\\"));
            var packageRestoreData = LoadPackageRestoreObject(GetTempFileName(), packageProjectJsonRestoreFlagContents);
            File.WriteAllText(projectJsonFile, _packageProjectJsonFileContents);

            CreateProjectJsonLockFile(projectLockJsonFile, new List<Tuple<string, string>>() { new Tuple<string, string>("Newtonsoft.Json", "6.0.3") });

            var packagePath =
                CreatePackagesFolder(
                    new List<Tuple<string, string>>() { new Tuple<string, string>("Newtonsoft.Json", "6.0.3") },@"\");
            var packages = (new NuGetProjectJsonParser()).GetPackages(packagePath, projectJsonFile, packageRestoreData).ToDictionary(i => $"{i.Id}.{i.Version}", i => i, StringComparer.OrdinalIgnoreCase);
            packages.Count.ShouldBe(1);
            packages.First().Value.Id.ShouldBe("Newtonsoft.Json");
            packages.First().Value.Version.ToString().ShouldBe("6.0.3");

            projectJsonFile = Path.Combine(TestRootPath, "foo.proj");
            packages = (new NuGetProjectJsonParser()).GetPackages(packagePath, projectJsonFile, packageRestoreData).ToDictionary(i => $"{i.Id}.{i.Version}", i => i, StringComparer.OrdinalIgnoreCase);
            packages.Count.ShouldBe(1);
            packages.First().Value.Id.ShouldBe("Newtonsoft.Json");
            packages.First().Value.Version.ToString().ShouldBe("6.0.3");
        }

        [Fact]
        public void VerifyPackageReferenceParserTest()
        {
            
            string projectAssetsJsonFile = Path.Combine(TestRootPath, "project.assets.json");
            var packageRestoreData = LoadPackageRestoreObject(GetTempFileName(), _packageReferenceRestoreFlagContents.Replace("#RestoreOutputPath#",TestRootPath.Replace(@"\", @"\\")));


            CreateProjectAssetsJsonFile(projectAssetsJsonFile, (new List<Tuple<string, string>>() { new Tuple<string, string>("Newtonsoft.Json", "6.0.3") }));

            var packagePath =
                CreatePackagesFolder(
                    new List<Tuple<string, string>>() { new Tuple<string, string>("Newtonsoft.Json", "6.0.3") }, @"\");
            var projectPackageReferenceFile = Path.Combine(TestRootPath, "foo.proj");
            var packages = (new NuGetPackageReferenceProjectParser(null)).GetPackages(packagePath, projectPackageReferenceFile, packageRestoreData).ToDictionary(i => $"{i.Id}.{i.Version}", i => i, StringComparer.OrdinalIgnoreCase);
            packages.Count.ShouldBe(1);
            packages.First().Value.Id.ShouldBe("Newtonsoft.Json");
            packages.First().Value.Version.ToString().ShouldBe("6.0.3");

        }

        [Fact]
        public void ValidatePackagesConfigNugetPropertyGeneratorTest()
        {
            var packageRestoreData = LoadPackageRestoreObject(GetTempFileName(), _packageConfigRestoreFlagContents);
            string packageConfigFile = Path.Combine(TestRootPath, "packages.config");
            File.WriteAllText(packageConfigFile, _packageConfigFileContents);
            string outputFile = Path.Combine(TestRootPath, "output.props");
            string expectedOutputContent = $@"<?xml version=""1.0"" encoding=""utf-8""?>
<Project ToolsVersion=""4.0"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
  <PropertyGroup>
    <MSBuildAllProjects>$(MSBuildAllProjects);$(MSBuildThisFileFullPath)</MSBuildAllProjects>
    <NuGetPath_Newtonsoft_Json>{TestRootPath}\packages\Newtonsoft.Json.6.0.1</NuGetPath_Newtonsoft_Json>
    <NuGetVersion_Newtonsoft_Json>6.0.1</NuGetVersion_Newtonsoft_Json>
  </PropertyGroup>
  <ItemGroup>
    <CBTNuGetPackageDir Include=""{TestRootPath}\packages\Newtonsoft.Json.6.0.1"" />
  </ItemGroup>
</Project>";

            var packagePath =
                CreatePackagesFolder(
                    new List<Tuple<string, string>>() { new Tuple<string, string>("Newtonsoft.Json", "6.0.3") }, @"\");
            (new NuGetPropertyGenerator(null, packageConfigFile)).Generate(outputFile, "NuGetVersion_", "NuGetPath_", "c:\\foo", packagePath,
                packageRestoreData).ShouldBe(true);
            File.Exists(outputFile).ShouldBe(true);
            File.ReadAllText(outputFile).NormalizeNewLine().ShouldBe(expectedOutputContent.NormalizeNewLine());

        }

        [Fact]
        public void ValidatePackageReferenceNugetPropertyGeneratorTest()
        {
            string projectAssetsJsonFile = Path.Combine(TestRootPath, "project.assets.json");
            var packageRestoreData = LoadPackageRestoreObject(GetTempFileName(), _packageReferenceRestoreFlagContents.Replace("#RestoreOutputPath#", TestRootPath.Replace(@"\", @"\\")));
            var projectPackageReferenceFile = Path.Combine(TestRootPath, "foo.proj");

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

            CreateProjectAssetsJsonFile(projectAssetsJsonFile, (new List<Tuple<string, string>>() { new Tuple<string, string>("Newtonsoft.Json", "6.0.3") }));

            var packagePath =
                CreatePackagesFolder(
                    new List<Tuple<string, string>>() { new Tuple<string, string>("Newtonsoft.Json", "6.0.3") }, @"\");
            (new NuGetPropertyGenerator(null, projectPackageReferenceFile)).Generate(outputFile, "NuGetVersion_", "NuGetPath_", "c:\\foo", packagePath,
                packageRestoreData).ShouldBe(true);
            File.Exists(outputFile).ShouldBe(true);
            File.ReadAllText(outputFile).NormalizeNewLine().ShouldBe(expectedOutputContent.NormalizeNewLine());
        }

        [Fact]
        public void ValidateProjectJsonNugetPropertyGeneratorTest()
        {
            string projectJsonFile = Path.Combine(TestRootPath, "project.json");
            string projectLockJsonFile = Path.Combine(TestRootPath, "project.lock.json");

            var packageProjectJsonRestoreFlagContents =
                _packageProjectJsonRestoreFlagContents.Replace("#JsonFile#", projectJsonFile.Replace(@"\", @"\\"));
            var packageRestoreData = LoadPackageRestoreObject(GetTempFileName(), packageProjectJsonRestoreFlagContents);
            File.WriteAllText(projectJsonFile, _packageProjectJsonFileContents);

            CreateProjectJsonLockFile(projectLockJsonFile, new List<Tuple<string, string>>() { new Tuple<string, string>("Newtonsoft.Json", "6.0.3") });

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

            var packagePath =
                CreatePackagesFolder(
                    new List<Tuple<string, string>>() { new Tuple<string, string>("Newtonsoft.Json", "6.0.3") }, @"\");
            (new NuGetPropertyGenerator(null, projectJsonFile)).Generate(outputFile, "NuGetVersion_", "NuGetPath_", "c:\\foo", packagePath,
                packageRestoreData).ShouldBe(true);
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
                Targets = new List<LockFileTarget>()
                {
                    new LockFileTarget()
                    {
                        TargetFramework = new NuGetFramework(".NETFramework,Version=v4.5"),
                        Libraries = packageList.Distinct().Select(i => new LockFileTargetLibrary(){Name = i.Item1, Version = new NuGetVersion(i.Item2), Type = "package"}).ToList(),
                    }
                }
            });
        }

        private PackageRestoreData LoadPackageRestoreObject(string restoreFlag, string restoreContent)
        {
            string packageJsonFlagFile = restoreFlag;
            File.WriteAllText(packageJsonFlagFile, restoreContent);
            GenerateNuGetProperties genTask = new GenerateNuGetProperties { AssetsFile = packageJsonFlagFile };
            return genTask.GetPackageRestoreData();
        }

        private string CreatePackagesFolder(IList<Tuple<string,string>> dummyPackages, string idAndVersionDivider=".")
        {
            var packageFolder = Path.Combine(TestRootPath, "packages");
            Directory.CreateDirectory(packageFolder);
            foreach (var pkg in dummyPackages)
            {
                var dummyPackageFolder = Path.Combine(packageFolder, $"{pkg.Item1}{idAndVersionDivider}{pkg.Item2}");
                if (!Directory.Exists(dummyPackageFolder))
                {
                    Directory.CreateDirectory(dummyPackageFolder);
                }
            }
            return packageFolder;
        }
    }
}
