using CBT.UnitTests.Common;
using Microsoft.Build.Framework;
using NuGet.Tasks.Deterministic;
using Shouldly;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace NuGet.Deterministic.UnitTests
{
    public class ValidateNuGetPackageHashesTests : TestBase
    {
        [Fact]
        public void FailsWhenHashesDoNotMatch()
        {
            ITaskItem[] packageReferences =
            {
                new MockTaskItem("MyPackage")
                {
                    {GenerateLockedPackageReferencesFile.HashfileMetadataName, "mypackage.1.0.0.nupkg.sha512"},
                    {GenerateLockedPackageReferencesFile.PackagePathMetadataName, "mypackage/1.0.0"},
                    {GenerateLockedPackageReferencesFile.Sha512MetadataName, "oPK8YOt8mZVSBTr8NG6ae74m4SmmM/mFRLeD7xs4/oYnq4AeoDyjFct3p6dEZO03l/eaRaZJ6ATecE+AifO0/w=="},
                    {GenerateLockedPackageReferencesFile.VersionMetadataName, "1.0.0"},
                },
            };

            ITaskItem[] packageReferencesWithNonMatchingHash =
            {
                new MockTaskItem("MyPackage")
                {
                    {GenerateLockedPackageReferencesFile.HashfileMetadataName, "mypackage.1.0.0.nupkg.sha512"},
                    {GenerateLockedPackageReferencesFile.PackagePathMetadataName, "mypackage/1.0.0"},
                    {GenerateLockedPackageReferencesFile.Sha512MetadataName, "Does not match"},
                    {GenerateLockedPackageReferencesFile.VersionMetadataName, "1.0.0"},
                },
            };

            string packageFolders = CreateNuGetPackageRoot(packageReferencesWithNonMatchingHash.Select(i => new PackageReferenceTaskItem(i)));

            Execute(packageReferences, packageFolders, out MockBuildEngine buildEngine).ShouldBeFalse();

            BuildErrorEventArgs actual = buildEngine.Errors.ShouldHaveSingleItem();

            actual.Code.ShouldBe("ND1003");

            actual.Message.ShouldStartWith("The package reference 'MyPackage' has an expected hash of 'oPK8YOt8mZVSBTr8NG6ae74m4SmmM/mFRLeD7xs4/oYnq4AeoDyjFct3p6dEZO03l/eaRaZJ6ATecE+AifO0/w==' which does not match the hash of the package 'Does not match' according to NuGet's hash file '");
        }

        [Fact]
        public void FailsWhenHashFileDoesNotExist()
        {
            ITaskItem[] packageReferences =
            {
                new MockTaskItem("MyPackage")
                {
                    {GenerateLockedPackageReferencesFile.HashfileMetadataName, "mypackage.1.0.0.nupkg.sha512"},
                    {GenerateLockedPackageReferencesFile.PackagePathMetadataName, "mypackage/1.0.0"},
                    {GenerateLockedPackageReferencesFile.Sha512MetadataName, "oPK8YOt8mZVSBTr8NG6ae74m4SmmM/mFRLeD7xs4/oYnq4AeoDyjFct3p6dEZO03l/eaRaZJ6ATecE+AifO0/w=="},
                    {GenerateLockedPackageReferencesFile.VersionMetadataName, "1.0.0"},
                },
            };

            string packageFolders = CreateNuGetPackageRoot(packageReferences.Select(i => new PackageReferenceTaskItem(i)), writeHashFiles: false);

            Execute(packageReferences, packageFolders, out MockBuildEngine buildEngine).ShouldBeFalse();

            BuildErrorEventArgs actual = buildEngine.Errors.ShouldHaveSingleItem();

            actual.Code.ShouldBe("ND1002");

            actual.Message.ShouldStartWith("The package 'MyPackage' does not have a hash file at '");
        }

        [Fact]
        public void FailsWhenPackageDirectoryNotFound()
        {
            ITaskItem[] packageReferences =
            {
                new MockTaskItem("MyPackage")
                {
                    {GenerateLockedPackageReferencesFile.HashfileMetadataName, "mypackage.1.0.0.nupkg.sha512"},
                    {GenerateLockedPackageReferencesFile.PackagePathMetadataName, "mypackage/1.0.0"},
                    {GenerateLockedPackageReferencesFile.Sha512MetadataName, "oPK8YOt8mZVSBTr8NG6ae74m4SmmM/mFRLeD7xs4/oYnq4AeoDyjFct3p6dEZO03l/eaRaZJ6ATecE+AifO0/w=="},
                    {GenerateLockedPackageReferencesFile.VersionMetadataName, "1.0.0"},
                },
            };

            Execute(packageReferences, "Directory that does not exist", out MockBuildEngine buildEngine).ShouldBeFalse();

            BuildErrorEventArgs actual = buildEngine.Errors.ShouldHaveSingleItem();

            actual.Code.ShouldBe("ND1001");

            actual.Message.ShouldBe("The package 'MyPackage' does not exist in any of the specified package folders 'Directory that does not exist'.  Ensure that the packages for this project have been restored and were not deleted.");
        }

        [Fact]
        public void SuccessfullyValidatesHashes()
        {
            ITaskItem[] packageReferences =
            {
                new MockTaskItem("MyPackage")
                {
                    {GenerateLockedPackageReferencesFile.HashfileMetadataName, "mypackage.1.0.0.nupkg.sha512"},
                    {GenerateLockedPackageReferencesFile.PackagePathMetadataName, "mypackage/1.0.0"},
                    {GenerateLockedPackageReferencesFile.Sha512MetadataName, "oPK8YOt8mZVSBTr8NG6ae74m4SmmM/mFRLeD7xs4/oYnq4AeoDyjFct3p6dEZO03l/eaRaZJ6ATecE+AifO0/w=="},
                    {GenerateLockedPackageReferencesFile.VersionMetadataName, "1.0.0"},
                },
                new MockTaskItem("Another.Package")
                {
                    {GenerateLockedPackageReferencesFile.HashfileMetadataName, "another.package.2.0.0.nupkg.sha512"},
                    {GenerateLockedPackageReferencesFile.PackagePathMetadataName, "another.package/1.0.0"},
                    {GenerateLockedPackageReferencesFile.Sha512MetadataName, "a522a7eea77149ed88d988f9711be640"},
                    {GenerateLockedPackageReferencesFile.VersionMetadataName, "2.0.0"},
                },
            };

            string packageFolders = CreateNuGetPackageRoot(packageReferences.Select(i => new PackageReferenceTaskItem(i)));

            Execute(packageReferences, packageFolders, out MockBuildEngine buildEngine).ShouldBeTrue();

            buildEngine.WarningsCount.ShouldBe(0, $"{String.Join($"{Environment.NewLine}    ", buildEngine.Warnings.Select(i => $"warning {i.Code}: {i.Message}"))}");
            buildEngine.ErrorCount.ShouldBe(0, $"{String.Join($"{Environment.NewLine}    ", buildEngine.Errors.Select(i => $"error {i.Code}: {i.Message}"))}");
        }

        private string CreateNuGetPackageRoot(IEnumerable<PackageReferenceTaskItem> packageReferences, bool writeHashFiles = true)
        {
            string packageRoot = Path.Combine(TestRootPath, "packages");

            Directory.CreateDirectory(packageRoot);

            foreach (PackageReferenceTaskItem packageReference in packageReferences)
            {
                string packageFolder = Path.Combine(packageRoot, packageReference.PackagePath);

                if (!Directory.Exists(packageFolder))
                {
                    Directory.CreateDirectory(packageFolder);

                    if (writeHashFiles)
                    {
                        File.WriteAllText(Path.Combine(packageFolder, packageReference.Hashfile), packageReference.Sha512);
                    }
                }
            }

            return packageRoot;
        }

        private bool Execute(ITaskItem[] packageReferences, string packageFolders, out MockBuildEngine buildEngine)
        {
            buildEngine = new MockBuildEngine();

            ValidateNuGetPackageHashes task = new ValidateNuGetPackageHashes
            {
                BuildEngine = buildEngine,
                PackageReferences = packageReferences,
                PackageFolders = packageFolders,
            };

            return task.Execute();
        }
    }
}