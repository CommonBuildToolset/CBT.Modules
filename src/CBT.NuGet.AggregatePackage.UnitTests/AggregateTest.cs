using Microsoft.MSBuildProjectBuilder;
using Shouldly;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using Xunit;

namespace CBT.NuGet.AggregatePackage.UnitTests
{
    public class AggregateTest : IDisposable
    {
        private readonly string _testRootFolder;

        private readonly string _enlistmentRoot;

        private readonly string _repo;

        private string _testProject = String.Empty;

        private string _aggregatePropsInNupkg = string.Empty;

        private readonly string _destPackagesPath;

        private IDictionary<string, string> _installedNupkgs;

        public AggregateTest()
        {
            _testRootFolder = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            _enlistmentRoot = Path.Combine(_testRootFolder, "enl1");
            _repo = Path.Combine(Directory.GetCurrentDirectory(), "repo");
            _destPackagesPath = Path.Combine(_enlistmentRoot, "packages");
        }

        public void Dispose()
        {
            // Dispose of project collection.
            Helper._projectCollectionLazy.Value.Dispose();
            Directory.Delete(_testRootFolder, true);
        }

        private IDictionary<string, string> InstallNupkgFiles(string repo, string destinationRoot, string filePattern = "*.nupkg")
        {
            IDictionary<string, string> pkgInstalls = new Dictionary<string, string>();
            foreach (var sourceNupkg in Directory.EnumerateFiles(repo, filePattern))
            {
                var destPath = Path.Combine(destinationRoot, Path.GetFileNameWithoutExtension(sourceNupkg));
                ZipFile.ExtractToDirectory(sourceNupkg, destPath);
                var nuspec = Directory.EnumerateFiles(destPath, "*.nuspec", SearchOption.TopDirectoryOnly).SingleOrDefault();
                var packageID = Path.GetFileNameWithoutExtension(nuspec);

                pkgInstalls.Add(packageID, destPath);
            }
            return pkgInstalls;
        }

        private void CreateTestProject(string testProject)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(testProject));
            ProjectBuilder.Create()
                    .AddProperty("CBTEnablePackageRestore=true")
                    .AddProperty("Platform=AnyCPU", "Configuration=Debug", "OutputPath=bin")
                    .AddProperty($"CBTNuGetTasksAssemblyPath={Assembly.GetExecutingAssembly().Location}")
                    .AddProperty($"CBTNuGetTasksAssemblyName=$(CBTNuGetTasksAssemblyPath.GetType().Assembly.GetType(\'System.AppDomain\').GetProperty(\'CurrentDomain\').GetValue(null).SetData(\'CBT_NUGET_ASSEMBLY_PATH\', $(CBTNuGetTasksAssemblyPath)))$(CBTNuGetTasksAssemblyPath.GetType().Assembly.GetType(\'System.AppDomain\').GetProperty(\'CurrentDomain\').GetValue(null).SetData(\'CBT_NUGET_ASSEMBLY\', $(CBTNuGetTasksAssemblyPath.GetType().Assembly.GetType(\'System.AppDomain\').GetProperty(\'CurrentDomain\').GetValue(null).Load($(CBTNuGetTasksAssemblyPath.GetType().Assembly.GetType(\'System.IO.File\').GetMethod(\'ReadAllBytes\').Invoke(null, $([System.IO.Directory]::GetFiles($([System.IO.Path]::GetDirectoryName($(CBTNuGetTasksAssemblyPath))), $([System.IO.Path]::GetFileName($(CBTNuGetTasksAssemblyPath)))))))))))$(CBTNuGetTasksAssemblyPath.GetType().Assembly.GetType(\'System.AppDomain\').GetProperty(\'CurrentDomain\').GetValue(null).GetData(\'CBT_NUGET_ASSEMBLY\'))")
                    .AddProperty("CBTAggregatePackage=NuGetPath_Microsoft_Build=src1|scr2")
                    .AddProperty($"NuGetPackagesPath={_destPackagesPath}")
                    .AddProperty($@"IntermediateOutputPath={_enlistmentRoot}\obj\$(Configuration)\$(Platform)\AggregatePackage.proj")
                    .AddImport($@"{_installedNupkgs["CBT.NuGet.AggregatePackage"]}\build\After.CBT.NuGet.props")
                    .AddImport($@"{_installedNupkgs["CBT.NuGet.AggregatePackage"]}\build\After.Microsoft.Common.targets")
                    .AddTarget("Build")
                    .Save(testProject);
        }

        private void CreateAggregateImportProject(string project)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(project));
            ProjectBuilder.Create()
                    .AddProperty("CBTAggregatePackage=NuGetPath_Microsoft_Build=src1|src2")
                    .Save(project);
        }

        // This method is creating a mocked up enlistment for the unittest to run.
        // Creates the test project
        // Creates a CBT.AggregatePackages.props import to be used by the aggregate module.
        // replaces the AggregatePackages property hack with a stubbed out version since that build task is tested in the NuGet unittest project.
        // replaces the build task located in the restore target with a stubbed out task.

        private void SetupTestEnlistment(IDictionary<string, string> installedNupkgs)
        {
            _testProject = Path.Combine(_enlistmentRoot, "src", "AggregatePackage.proj");
            CreateTestProject(_testProject);

            string aggregateImport = Path.Combine(_enlistmentRoot, "src", "CBT.AggregatePackages.props");
            CreateAggregateImportProject(aggregateImport);

            _aggregatePropsInNupkg = Path.Combine(installedNupkgs["CBT.NuGet.AggregatePackage"], "build", "After.CBT.NuGet.props");
            File.WriteAllText(_aggregatePropsInNupkg, File.ReadAllText(_aggregatePropsInNupkg).Replace("CBT.NuGet.Tasks.AggregatePackages", "CBT.NuGet.AggregatePackage.UnitTests.FakeAggregateTrue"));

            var aMC_AP = Path.Combine(installedNupkgs["CBT.NuGet.AggregatePackage"], "build", "After.Microsoft.Common.targets");
            File.WriteAllText(aMC_AP, File.ReadAllText(aMC_AP).Replace("CBT.NuGet.Tasks.AggregatePackages", "CBT.NuGet.AggregatePackage.UnitTests.FakeAggregateFalse"));
            File.WriteAllText(aMC_AP, File.ReadAllText(aMC_AP).Replace("<AggregatePackages", "<FakeAggregateFalse"));
        }


        [Fact]
        public void AggregatePackageInvokeFail()
        {
            _installedNupkgs = InstallNupkgFiles(_repo, _destPackagesPath, "CBT.NuGet.AggregatePackage.*.nupkg");
            SetupTestEnlistment(_installedNupkgs);

            // modify module to fake agg expansion failure.
            File.WriteAllText(_aggregatePropsInNupkg, File.ReadAllText(_aggregatePropsInNupkg).Replace("CBT.NuGet.AggregatePackage.UnitTests.FakeAggregateTrue", "CBT.NuGet.AggregatePackage.UnitTests.FakeAggregateFalse"));

            var expectedItems = new List<Item>();
            expectedItems.Add(new Item("CBTParseError", "Aggregate packages were not generated and the build cannot continue.  Refer to other errors for more information.", null, null, new ItemMetadata("Code", "CBT.NuGet.AggregatePackage.1000")));
            Helper.RunTest(Helper.TestType.Simple, _testProject, null, null, null, expectedItems);
        }

        [Fact]
        public void AggregatePackagePropertySet()
        {
            _installedNupkgs = InstallNupkgFiles(_repo, _destPackagesPath, "CBT.NuGet.AggregatePackage.*.nupkg");
            SetupTestEnlistment(_installedNupkgs);

            var expectedEvalutedProperties = new List<Property>
            {
                new Property("CBTEnableAggregatePackageRestore", "true"),
                new Property("CBTAggregateDestPackageRoot", $@"{_destPackagesPath}\.agg"),
                new Property("CBTNuGetAggregatePackageImmutableRoots", $"{_destPackagesPath}"),
                new Property("CBTNuGetAggregatePackagePropertyFile", $@"{_enlistmentRoot}\obj\Debug\AnyCPU\AggregatePackage.proj\AggregatePackages.props"),
                new Property("RestoreNuGetPackagesDependsOn", ";AggregateNuGetPackages"),
                new Property("CBTAggregatePackageImport", $@"{_enlistmentRoot}\src\CBT.AggregatePackages.props"),
                new Property("CBTAggregatePackage", "NuGetPath_Microsoft_Build=src1|scr2"),
                new Property("CBTNuGetAggregatePackageGenerated", "True")
            };

            Helper.RunTest(Helper.TestType.Simple, _testProject, null, expectedEvalutedProperties);
        }

        [Fact]
        public void AggregatePackageTargetRun()
        {
            _installedNupkgs = InstallNupkgFiles(_repo, _destPackagesPath, "CBT.NuGet.AggregatePackage.*.nupkg");
            SetupTestEnlistment(_installedNupkgs);

            var expectedMessage = new List<ExpectedOutputMessage>
            {
                new ExpectedOutputMessage(MessageType.Error, new List<string> {"Fake failure"})
            };

            Helper.RunTest(Helper.TestType.Simple, _testProject, expectedMessage, null, null, null, "AggregateNuGetPackages");
        }

        [Fact]
        public void AggregatePackageExpectedImports()
        {
            _installedNupkgs = InstallNupkgFiles(_repo, _destPackagesPath, "CBT.NuGet.AggregatePackage.*.nupkg");
            SetupTestEnlistment(_installedNupkgs);

            var expectedImports = new List<Import>
            {
                new Import($@"{_installedNupkgs["CBT.NuGet.AggregatePackage"]}\build\After.CBT.NuGet.props"),
                new Import("$(CBTAggregatePackageImport)"),
                new Import($@"{_installedNupkgs["CBT.NuGet.AggregatePackage"]}\build\After.Microsoft.Common.targets")
            };

            Helper.RunTest(Helper.TestType.Simple, _testProject, null, null, expectedImports);
        }

        [Fact]
        public void VerifyNupkgPayload()
        {
            _installedNupkgs = InstallNupkgFiles(_repo, _destPackagesPath, "CBT.NuGet.AggregatePackage.*.nupkg");

            List<string> expectedPayload = new List<string>
            {
                $@"{_installedNupkgs["CBT.NuGet.AggregatePackage"]}\CBT.NuGet.AggregatePackage.nuspec",
                $@"{_installedNupkgs["CBT.NuGet.AggregatePackage"]}\build\After.CBT.NuGet.props",
                $@"{_installedNupkgs["CBT.NuGet.AggregatePackage"]}\build\After.Microsoft.Common.targets",
                $@"{_installedNupkgs["CBT.NuGet.AggregatePackage"]}\build\module.config"
            };

            List<string> nugetPkgPayload = Directory.GetFiles(_installedNupkgs["CBT.NuGet.AggregatePackage"], "*.*", SearchOption.AllDirectories).ToList();
            List<string> excludeFromPayload = Directory.GetFiles(_installedNupkgs["CBT.NuGet.AggregatePackage"], @"package\*.*", SearchOption.AllDirectories).ToList();
            excludeFromPayload.AddRange(Directory.GetFiles(_installedNupkgs["CBT.NuGet.AggregatePackage"], "[Content_Types].xml", SearchOption.TopDirectoryOnly).ToList());
            excludeFromPayload.AddRange(Directory.GetFiles(_installedNupkgs["CBT.NuGet.AggregatePackage"], @"_rels\*.*", SearchOption.AllDirectories).ToList());

            nugetPkgPayload.Except(excludeFromPayload).ShouldBe(expectedPayload, Case.Insensitive);
        }
    }
}
