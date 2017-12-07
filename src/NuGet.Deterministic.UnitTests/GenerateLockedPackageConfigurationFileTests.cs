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
        private readonly List<string> _assetsFilesWritten = new List<string>();

        [Fact]
        public void MultipleTargetFramworks()
        {
            LockFile lockFile = GetLockFileWithMultipleTargetFrameworks();

            ProjectRootElement project = ValidateProject(lockFile);

            project.ItemGroups.Count.ShouldBe(lockFile.PackageSpec.TargetFrameworks.Count);

            foreach (LockFileTarget target in lockFile.Targets)
            {
                ProjectItemGroupElement itemGroupElement = project.ItemGroups.SingleOrDefault(i => i.Condition.Equals($" '$(TargetFramework)' == '{target.TargetFramework.GetShortFolderName()}' "));

                itemGroupElement.ShouldNotBeNull();

                itemGroupElement.Items.Count.ShouldBe(target.Libraries.Count);
            }
        }

        [Fact]
        public void SingleTargetFramework()
        {
            LockFile lockFile = GetLockFileWithSingleTargetFramework();

            ProjectRootElement project = ValidateProject(lockFile);

            ProjectItemGroupElement itemGroupElement = project.ItemGroups.ShouldHaveSingleItem();

            foreach (LockFileTargetLibrary targetLibrary in lockFile.Targets[0].Libraries)
            {
                ProjectItemElement itemElement = itemGroupElement.Items.FirstOrDefault(x => x.Include.Equals(targetLibrary.Name));

                itemElement.ShouldNotBeNull();

                ProjectMetadataElement versionMetadataElement = itemElement.Metadata.FirstOrDefault(x => x.Name.Equals("Version"));

                versionMetadataElement.ShouldNotBeNull();

                versionMetadataElement.Value.ShouldBe($"[{targetLibrary.Version.ToString()}]");
            }

            itemGroupElement.Items.Count.ShouldBe(lockFile.Targets.Aggregate(0, (count, target) => target.Libraries.Count));
        }

        [Fact]
        public void SingleTargetFrameworkExcludeImplicityReferences()
        {
            LockFile lockFile = GetLockFileWithSingleTargetFramework();

            ProjectRootElement project = ValidateProject(lockFile, task => { task.ExcludeImplicitReferences = true; });

            ProjectItemGroupElement itemGroupElement = project.ItemGroups.ShouldHaveSingleItem();

            itemGroupElement.Items.ShouldNotContain(i => i.Include.Equals("Package2"));
        }

        [Fact]
        public void SingleTargetFrameworkWithExcludes()
        {
            LockFile lockFile = GetLockFileWithSingleTargetFramework();

            ProjectRootElement project = ValidateProject(lockFile, task =>
            {
                task.PackagesToExclude = new ITaskItem[]
                {
                    new MockTaskItem("Package1"),
                    new MockTaskItem("Package2"),
                };
            });

            ProjectItemGroupElement itemGroupElement = project.ItemGroups.ShouldHaveSingleItem();

            ProjectItemElement itemElement = itemGroupElement.Items.ShouldHaveSingleItem();

            itemElement.Include.ShouldBe("Package3");
        }

        [Fact]
        public void SingleTargetFrameworkWithIncludeAssetsAndPrivateAssets()
        {
            LockFile lockFile = GetLockFileWithSingleTargetFramework();

            ProjectRootElement project = ValidateProject(lockFile);

            ProjectItemGroupElement itemGroupElement = project.ItemGroups.ShouldHaveSingleItem();

            itemGroupElement.Items.ShouldContain(i => i.Include.Equals("Package3"), 1);

            var itemElement = itemGroupElement.Items.Single(i => i.Include.Equals("Package3"));

            ProjectMetadataElement includeAssetsMetadataElement = itemElement.Metadata.FirstOrDefault(i => i.Name.Equals("IncludeAssets"));

            includeAssetsMetadataElement.ShouldNotBeNull();

            includeAssetsMetadataElement.Value.ShouldBe("Runtime;ContentFiles");

            ProjectMetadataElement privateAssetsMetadataElement = itemElement.Metadata.FirstOrDefault(i => i.Name.Equals("PrivateAssets"));

            privateAssetsMetadataElement.ShouldNotBeNull();

            privateAssetsMetadataElement.Value.ShouldBe("Native");
        }

        [Fact]
        public void TryCreateProjectFailsIfAssetsFileDoesNotExist()
        {
            MockBuildEngine buildEngine = new MockBuildEngine();

            GenerateLockedPackageReferencesFile generateLockedPackageReferencesFile = new GenerateLockedPackageReferencesFile
            {
                BuildEngine = buildEngine,
                ProjectAssetsFile = "File that does not exist",
            };

            generateLockedPackageReferencesFile.TryCreateProject(out ProjectRootElement _).ShouldBeFalse();

            buildEngine.ErrorCount.ShouldBe(1);

            BuildErrorEventArgs buildError = buildEngine.Errors.Single();

            buildError.Message.ShouldBe($"NuGet assets file '{generateLockedPackageReferencesFile.ProjectAssetsFile}' does not exist.");
        }

        protected override void Dispose(bool disposing)
        {
            foreach (string assetsFile in _assetsFilesWritten)
            {
                if (File.Exists(assetsFile))
                {
                    File.Delete(assetsFile);
                }
            }
            base.Dispose(disposing);
        }

        private string GetAssetsFilePath(LockFile lockFile)
        {
            string assetsFilePath = GetTempFileName();

            new LockFileFormat().Write(assetsFilePath, lockFile);

            _assetsFilesWritten.Add(assetsFilePath);

            return assetsFilePath;
        }

        private LockFile GetLockFileWithMultipleTargetFrameworks()
        {
            return new LockFile
            {
                Libraries = new List<LockFileLibrary>
                {
                    new LockFileLibrary
                    {
                        Path = "package1/1.0.0",
                        Name = "Package1",
                        Version = new NuGetVersion(1, 0, 0),
                        Sha512 = "3473C4E0C7F5404F8F46311CA3631331",
                        Type = "package",
                        Files = new List<string>
                        {
                            "package1.1.0.0.nupkg.sha512"
                        },
                    },
                    new LockFileLibrary
                    {
                        Path = "package2/2.0.0",
                        Name = "Package2",
                        Version = new NuGetVersion(2, 0, 0),
                        Sha512 = "64C01D6AD3DB49DFBE992B38B8466D15",
                        Type = "package",
                        Files = new List<string>
                        {
                            "package2.2.0.0.nupkg.sha512"
                        },
                    },
                    new LockFileLibrary
                    {
                        Path = "package2/2.1.0",
                        Name = "Package2",
                        Version = new NuGetVersion(2, 1, 0),
                        Sha512 = "64C01D6AD3DB49DFBE992B38B8466D15",
                        Type = "package",
                        Files = new List<string>
                        {
                            "package2.2.1.0.nupkg.sha512"
                        },
                    },

                    new LockFileLibrary
                    {
                        Path = "packagex/7.7.7",
                        Name = "PackageX",
                        Version = new NuGetVersion(7, 7, 7),
                        Sha512 = "4E17BFD496BD42F0948F3F9CB3C6DA04",
                        Type = "package",
                        Files = new List<string>
                        {
                            "packagex.7.7.7.nupkg.sha512"
                        },
                    },
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
                                Name = "Package1",
                                Dependencies = new List<PackageDependency>
                                {
                                    new PackageDependency("PackageX", VersionRange.Parse("7.7.7"))
                                },
                                Type = "package",
                                Version = new NuGetVersion(1, 0, 0)
                            },
                            new LockFileTargetLibrary
                            {
                                Name = "Package2",
                                Type = "package",
                                Version = new NuGetVersion(2, 0, 0)
                            },
                            new LockFileTargetLibrary
                            {
                                Name = "PackageX",
                                Type = "package",
                                Version = new NuGetVersion(7, 7, 7),
                            }
                        }
                    },
                    new LockFileTarget
                    {
                        TargetFramework = NuGetFramework.ParseFolder("netstandard1.3"),
                        Libraries = new List<LockFileTargetLibrary>
                        {
                            new LockFileTargetLibrary
                            {
                                Name = "Package2",
                                Type = "package",
                                Version = new NuGetVersion(2, 1, 0)
                            },
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
                                    LibraryRange = new LibraryRange("Package1", VersionRange.Parse("1.0.0"), LibraryDependencyTarget.Package)
                                },
                                new LibraryDependency
                                {
                                    LibraryRange = new LibraryRange("Package2", VersionRange.Parse("2.0.0"), LibraryDependencyTarget.Package),
                                },
                            }
                        },
                        new TargetFrameworkInformation
                        {
                            FrameworkName = NuGetFramework.ParseFolder("netstandard1.3"),
                            Dependencies = new List<LibraryDependency>
                            {
                                new LibraryDependency
                                {
                                    LibraryRange = new LibraryRange("Package2", VersionRange.Parse("2.1.0"), LibraryDependencyTarget.Package),
                                },
                            }
                        }
                    }
                }
            };
        }

        private LockFile GetLockFileWithSingleTargetFramework()
        {
            return new LockFile
            {
                Libraries = new List<LockFileLibrary>
                {
                    new LockFileLibrary
                    {
                        Path = "package1/1.0.0",
                        Name = "Package1",
                        Version = new NuGetVersion(1, 0, 0),
                        Sha512 = "3473C4E0C7F5404F8F46311CA3631331",
                        Type = "package",
                        Files = new List<string>
                        {
                            "package1.1.0.0.nupkg.sha512"
                        },
                    },
                    new LockFileLibrary
                    {
                        Path = "package2/2.0.0",
                        Name = "Package2",
                        Version = new NuGetVersion(2, 0, 0),
                        Sha512 = "64C01D6AD3DB49DFBE992B38B8466D15",
                        Type = "package",
                        Files = new List<string>
                        {
                            "package2.2.0.0.nupkg.sha512"
                        },
                    },
                    new LockFileLibrary
                    {
                        Path = "package3/3.0.0",
                        Name = "Package3",
                        Version = new NuGetVersion(3, 0, 0),
                        Sha512 = "35F31F00E1A246F2A9C7C04B06F11D8B",
                        Type = "package",
                        Files = new List<string>
                        {
                            "package3.3.0.0.nupkg.sha512"
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
                                Name = "Package1",
                                Dependencies = new List<PackageDependency>
                                {
                                    new PackageDependency("PackageX", VersionRange.Parse("7.7.7"))
                                },
                                Type = "package",
                                Version = new NuGetVersion(1, 0, 0)
                            },
                            new LockFileTargetLibrary
                            {
                                Name = "Package2",
                                Type = "package",
                                Version = new NuGetVersion(2, 0, 0)
                            },
                            new LockFileTargetLibrary
                            {
                                Name = "Package3",
                                Type = "package",
                                Version = new NuGetVersion(3, 0, 0)
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
                                    LibraryRange = new LibraryRange("Package1", VersionRange.Parse("1.0.0"), LibraryDependencyTarget.Package)
                                },
                                new LibraryDependency
                                {
                                    AutoReferenced = true,
                                    LibraryRange = new LibraryRange("Package2", VersionRange.Parse("2.0.0"), LibraryDependencyTarget.Package),
                                },
                                new LibraryDependency
                                {
                                    IncludeType = LibraryIncludeFlags.Runtime | LibraryIncludeFlags.ContentFiles,
                                    SuppressParent = LibraryIncludeFlags.Native,
                                    LibraryRange = new LibraryRange("Package3", VersionRange.Parse("3.0.0"), LibraryDependencyTarget.Package),
                                },
                            }
                        }
                    }
                }
            };
        }

        private ProjectRootElement ValidateProject(LockFile lockFile)
        {
            return ValidateProject(lockFile, null, out _);
        }

        private ProjectRootElement ValidateProject(LockFile lockFile, Action<GenerateLockedPackageReferencesFile> modifier)
        {
            return ValidateProject(lockFile, modifier, out _);
        }

        private ProjectRootElement ValidateProject(LockFile lockFile, Action<GenerateLockedPackageReferencesFile> modifier, out MockBuildEngine buildEngine)
        {
            buildEngine = new MockBuildEngine();

            GenerateLockedPackageReferencesFile task = new GenerateLockedPackageReferencesFile
            {
                BuildEngine = buildEngine,
                ProjectAssetsFile = GetAssetsFilePath(lockFile),
            };

            modifier?.Invoke(task);

            task.TryCreateProject(out ProjectRootElement project).ShouldBeTrue();

            ProjectPropertyGroupElement propertyGroupElement = project.PropertyGroups.ShouldHaveSingleItem();

            ProjectPropertyElement propertyElement = propertyGroupElement.Properties.FirstOrDefault(i => i.Name.Equals("NuGetDeterministicPropsWasImported"));

            propertyElement.ShouldNotBeNull();

            propertyElement.Value.ShouldBe("true");

            HashSet<string> excludes = new HashSet<string>(task.PackagesToExclude == null ? Enumerable.Empty<string>() : task.PackagesToExclude.Select(i => i.ItemSpec), StringComparer.OrdinalIgnoreCase);

            foreach (TargetFrameworkInformation targetFramework in lockFile.PackageSpec.TargetFrameworks)
            {
                if (!task.ExcludeImplicitReferences && targetFramework.Dependencies.Any(i => !excludes.Contains(i.Name) && i.AutoReferenced))
                {
                    ProjectPropertyElement disableImplicitFrameworkReferencesPropertyElement = lockFile.PackageSpec.TargetFrameworks.Count > 1 ? project.Properties.FirstOrDefault(i => i.Name.Equals("DisableImplicitFrameworkReferences") && i.Condition.Contains($"'{targetFramework.FrameworkName.GetShortFolderName()}'")) : project.Properties.FirstOrDefault(i => i.Name.Equals("DisableImplicitFrameworkReferences") && i.Condition.Equals(String.Empty));

                    disableImplicitFrameworkReferencesPropertyElement.ShouldNotBeNull();
                }
            }

            ProjectElement secondElement = project.Children.Skip(1).FirstOrDefault();

            ProjectImportElement beforeImportElement = secondElement.ShouldBeOfType<ProjectImportElement>();

            beforeImportElement.Project.ShouldBe("Before.$(MSBuildThisFile)");
            beforeImportElement.Condition.ShouldBe("Exists('Before.$(MSBuildThisFile)')");

            ProjectElement lastElement = project.Children.LastOrDefault();

            ProjectImportElement afterImportElement = lastElement.ShouldBeOfType<ProjectImportElement>();
            afterImportElement.Project.ShouldBe("After.$(MSBuildThisFile)");
            afterImportElement.Condition.ShouldBe("Exists('After.$(MSBuildThisFile)')");

            return project;
        }
    }
}