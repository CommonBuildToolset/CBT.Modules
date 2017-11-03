using CBT.UnitTests.Common;
using Microsoft.Build.Construction;
using Microsoft.Build.Framework;
using NuGet.Frameworks;
using NuGet.LibraryModel;
using NuGet.Packaging.Core;
using NuGet.ProjectModel;
using NuGet.Tasks.Deterministic;
using NuGet.Versioning;
using Shouldly;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace NuGet.Deterministic.UnitTests
{
    public class GenerateLockedPackageConfigurationFileTests : TestBase
    {
        private readonly Lazy<string> _assestsFilePathLazy;

        private readonly LockFile _lockFile = new LockFile
        {
            Libraries = new List<LockFileLibrary>
            {
                new LockFileLibrary
                {
                    Path = "packagea/1.0.0",
                    Name = "PackageA",
                    Version = new NuGetVersion(1, 0, 0),
                    Sha512 = "3473C4E0C7F5404F8F46311CA3631331",
                    Type = "package",
                    Files = new List<string>
                    {
                        "packagea.1.0.0.nupkg.sha512"
                    },
                },
                new LockFileLibrary
                {
                    Path = "packagex/7.7.7",
                    Name = "PackageX",
                    Version = new NuGetVersion(7, 7, 7),
                    Sha512 = "0B6DEE850AAB4EE786AD54B84D69DEAA",
                    Type = "package",
                    Files = new List<string>
                    {
                        "packagex.7.7.7.nupkg.sha512"
                    }
                }
            },
            Targets = new List<LockFileTarget>
            {
                new LockFileTarget
                {
                    TargetFramework = NuGetFramework.ParseFolder("net46"),
                    Libraries = new List<LockFileTargetLibrary>
                    {
                        new LockFileTargetLibrary
                        {
                            Name = "PackageA",
                            Dependencies = new List<PackageDependency>
                            {
                                new PackageDependency("PackageX", VersionRange.Parse("7.7.7"))
                            },
                            Type = "package",
                            Version = new NuGetVersion(1, 0, 0)
                        },
                        new LockFileTargetLibrary
                        {
                            Name = "PackageX",
                            Type = "package",
                            Version = new NuGetVersion(7, 7, 7),
                        }
                    }
                }
            },
            PackageSpec = new PackageSpec
            {
                Name = "foo",
                TargetFrameworks =
                {
                    new TargetFrameworkInformation
                    {
                        FrameworkName = NuGetFramework.ParseFolder("net46"),
                        Dependencies = new List<LibraryDependency>
                        {
                            new LibraryDependency
                            {
                                IncludeType = LibraryIncludeFlags.Runtime | LibraryIncludeFlags.ContentFiles,
                                SuppressParent = LibraryIncludeFlags.All,
                                LibraryRange = new LibraryRange("PackageA", VersionRange.Parse("1.0.0"), LibraryDependencyTarget.Package)
                            }
                        }
                    }
                }
            }
        };

        public GenerateLockedPackageConfigurationFileTests()
        {
            _assestsFilePathLazy = new Lazy<string>(() =>
            {
                string assetsFilePath = GetTempFileName();

                new LockFileFormat().Write(assetsFilePath, _lockFile);

                return assetsFilePath;
            });
        }

        [Fact]
        public void TryCreateProjectFailsIfAssetsFileDoesNotExist()
        {
            MockBuildEngine buildEngine = new MockBuildEngine();

            GenerateLockedPackageConfigurationFile generateLockedPackageConfigurationFile = new GenerateLockedPackageConfigurationFile
            {
                BuildEngine = buildEngine,
                NuGetAssetsFilePath = "File that does not exist",
            };

            generateLockedPackageConfigurationFile.TryCreateProject(out ProjectRootElement _).ShouldBeFalse();

            buildEngine.ErrorCount.ShouldBe(1);

            BuildErrorEventArgs buildError = buildEngine.Errors.Single();

            buildError.Message.ShouldBe($"NuGet assets file '{generateLockedPackageConfigurationFile.NuGetAssetsFilePath}' does not exist.");
        }

        [Fact]
        public void TryCreateProjectTest()
        {
            MockBuildEngine buildEngine = new MockBuildEngine();

            GenerateLockedPackageConfigurationFile generateLockedPackageConfigurationFile = new GenerateLockedPackageConfigurationFile
            {
                BuildEngine = buildEngine,
                NuGetAssetsFilePath = _assestsFilePathLazy.Value,
            };

            generateLockedPackageConfigurationFile.TryCreateProject(out ProjectRootElement project).ShouldBeTrue();

            ProjectPropertyGroupElement propertyGroupElement = project.PropertyGroups.ShouldHaveSingleItem();

            ProjectPropertyElement propertyElement = propertyGroupElement.Properties.Single();

            propertyElement.Name.ShouldBe("NuGetDeterministicPropsWasImported");

            propertyElement.Value.ShouldBe("true");

            ProjectItemGroupElement itemGroupElement = project.ItemGroups.ShouldHaveSingleItem();

            for (int i = 0; i < _lockFile.Targets[0].Libraries.Count; i++)
            {
                LockFileTargetLibrary lockFileTargetLibrary = _lockFile.Targets[0].Libraries[i];

                ProjectItemElement itemElement = itemGroupElement.Items.Skip(i).First();

                itemElement.Include.ShouldBe(lockFileTargetLibrary.Name);

                ProjectMetadataElement versionMetadataElement = itemElement.Metadata.FirstOrDefault(x => x.Name.Equals("Version"));

                versionMetadataElement.ShouldNotBeNull();

                versionMetadataElement.Value.ShouldBe($"[{lockFileTargetLibrary.Version.ToString()}]");
            }

            itemGroupElement.Items.Count.ShouldBe(_lockFile.Targets.Aggregate(0, (count, target) => target.Libraries.Count));
        }

        protected override void Dispose(bool disposing)
        {
            if (_assestsFilePathLazy.IsValueCreated)
            {
                File.Delete(_assestsFilePathLazy.Value);
            }
            base.Dispose(disposing);
        }
    }
}