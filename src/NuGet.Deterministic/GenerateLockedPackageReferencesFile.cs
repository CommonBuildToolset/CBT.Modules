using Microsoft.Build.Construction;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using NuGet.Common;
using NuGet.LibraryModel;
using NuGet.ProjectModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace NuGet.Tasks.Deterministic
{
    /// <summary>
    /// Generate NuGet properties.
    ///
    /// Generate properties that contain the path and version of a given nuget package.
    /// </summary>
    public sealed class GenerateLockedPackageReferencesFile : TaskBase
    {
        public const string HashfileMetadataName = "HashFile";
        public const string IncludeAssetsMetadataName = "IncludeAssets";
        public const string IsImplicitlyDefinedMetadataName = "IsImplicitlyDefined";
        public const string PackagePathMetadataName = "Path";
        public const string PrivateAssetsMetdataName = "PrivateAssets";
        public const string Sha512MetadataName = "Sha512";
        public const string VersionMetadataName = "Version";

        public bool ExcludeImplicitReferences { get; set; }

        /// <summary>
        /// Gets or sets the list of packages to exclude from being made deterministic.
        /// </summary>
        public ITaskItem[] PackagesToExclude { get; set; }

        /// <summary>
        /// Gets or sets the full path of the NuGet assets file (project.assets.json).
        /// </summary>
        [Required]
        public string ProjectAssetsFile { get; set; }

        /// <summary>
        /// Gets or sets the full path of the msbuild properties file to create.
        /// </summary>
        [Required]
        public string PropsFile { get; set; }

        public override bool Execute()
        {
            if (!TryCreateProject(out ProjectRootElement project))
            {
                return false;
            }

            FileInfo fileInfo = new FileInfo(PropsFile);

            if (fileInfo.Exists && fileInfo.IsReadOnly)
            {
                // Some source control systems mark files as read-only
                //
                fileInfo.IsReadOnly = false;
            }

            fileInfo.Directory?.Create();

            project.Save(PropsFile);

            AddFileWrite(PropsFile);

            return !Log.HasLoggedErrors && File.Exists(PropsFile);
        }

        /// <summary>
        /// Creates an MSBuild properties file that specifies the full closure of packages with locked versions.
        /// </summary>
        /// <returns>A <see cref="ProjectRootElement"/> object that can be saved.</returns>
        internal bool TryCreateProject(out ProjectRootElement project)
        {
            project = null;

            if (!File.Exists(ProjectAssetsFile))
            {
                Log.LogError($"NuGet assets file '{ProjectAssetsFile}' does not exist.");
                return false;
            }

            // should item group be conditioned or items or metadata?  Perhaps item condition should be considered and compared as well as an item could be conditioned.  Consider the below scenarios.  Since we are only parsing the assets file we need to consider the project file entries.
            // <PackageReference Include="foo" Version="1.2.3" Condition="bar"/>
            // <PackageReference Include="foo">
            //    <version>1.2.3</version>
            //    <version Condition="bar">1.2.3</version>
            // </PackageReference>
            // What about dependencies of packages that are conditioned? they should be conditioned as well.

            HashSet<string> packagesToExclude = new HashSet<string>(PackagesToExclude?.Select(i => i.ItemSpec).Distinct() ?? Enumerable.Empty<string>(), StringComparer.OrdinalIgnoreCase);

            project = ProjectRootElement.Create();

            project.ToolsVersion = String.Empty;

            ProjectPropertyElement wasImportedPropertyElement = project.AddProperty("NuGetDeterministicPropsWasImported", "true");

            LockFile lockFile = LockFileUtilities.GetLockFile(ProjectAssetsFile, NullLogger.Instance);

            bool crossTargeting = lockFile.PackageSpec.TargetFrameworks.Count > 1;

            foreach (TargetFrameworkInformation targetFramework in lockFile.PackageSpec.TargetFrameworks)
            {
                HashSet<LockFileLibrary> addedLibraries = new HashSet<LockFileLibrary>();

                ProjectItemGroupElement itemGroupElement = project.AddItemGroup();

                if (crossTargeting)
                {
                    itemGroupElement.Condition = $" '$(TargetFramework)' == '{targetFramework.FrameworkName.GetShortFolderName()}' ";
                }

                LockFileTarget target = lockFile.GetTarget(targetFramework.FrameworkName, runtimeIdentifier: null);

                bool addedImplicitReference = false;

                foreach (LibraryDependency libraryDependency in targetFramework.Dependencies.Where(i => !packagesToExclude.Contains(i.Name)))
                {
                    if (libraryDependency.AutoReferenced)
                    {
                        if (ExcludeImplicitReferences)
                        {
                            continue;
                        }
                        addedImplicitReference = true;
                    }

                    LockFileLibrary library = lockFile.GetLibrary(libraryDependency);

                    if (library.Type.Equals("project", StringComparison.OrdinalIgnoreCase))
                    {
                        // if a csproj name matches a package id then nuget swaps in the csproj output instead of the package.  Because of this we should skip adding the package as a locked package because it provides no value.
                        continue;
                    }

                    if (addedLibraries.Contains(library))
                    {
                        continue;
                    }

                    addedLibraries.Add(library);

                    LockFileTargetLibrary targetLibrary = target.GetTargetLibrary(libraryDependency.Name);

                    itemGroupElement.AddItem("PackageReference", targetLibrary.Name, GetPackageReferenceItemMetadata(library, libraryDependency));

                    foreach (LockFileLibrary dependency in targetLibrary.ResolveDependencies(lockFile, target).Where(i => !addedLibraries.Contains(i) && !packagesToExclude.Contains(i.Name)))
                    {
                        addedLibraries.Add(dependency);

                        itemGroupElement.AddItem("PackageReference", dependency.Name, GetPackageReferenceItemMetadata(dependency));
                    }
                }

                if (addedImplicitReference)
                {
                    ProjectPropertyElement disableImplicitFrameworkReferencesPropertyElement = project.AddProperty("DisableImplicitFrameworkReferences", "true");

                    if (crossTargeting)
                    {
                        disableImplicitFrameworkReferencesPropertyElement.Condition = $" '$(TargetFramework)' == '{targetFramework.FrameworkName.GetShortFolderName()}' ";
                    }
                }
            }

            ProjectImportElement beforeImportElement = project.CreateImportElement("Before.$(MSBuildThisFile)");
            project.InsertAfterChild(beforeImportElement, wasImportedPropertyElement.Parent);
            beforeImportElement.Condition = $"Exists('{beforeImportElement.Project}')";

            ProjectImportElement afterImportElement = project.AddImport("After.$(MSBuildThisFile)");
            afterImportElement.Condition = $"Exists('{afterImportElement.Project}')";

            return true;
        }

        private Dictionary<string, string> GetPackageReferenceItemMetadata(LockFileLibrary library, LibraryDependency libraryDependency = null)
        {
            if (library == null)
            {
                throw new ArgumentNullException(nameof(library));
            }

            Dictionary<string, string> metadata = new Dictionary<string, string>
            {
                {VersionMetadataName, $"[{library.Version}]"},
                {Sha512MetadataName, library.Sha512},
                {PackagePathMetadataName, library.Path},
                {HashfileMetadataName, library.Files.First(i => i.EndsWith("nupkg.sha512", StringComparison.OrdinalIgnoreCase)).ToLowerInvariant()},
            };

            if (libraryDependency != null)
            {
                if (libraryDependency.IncludeType != LibraryIncludeFlags.All)
                {
                    metadata.Add(IncludeAssetsMetadataName, libraryDependency.IncludeType.ToString("F").Replace(", ", ";"));
                }

                if (libraryDependency.SuppressParent != LibraryIncludeFlagUtils.DefaultSuppressParent)
                {
                    metadata.Add(PrivateAssetsMetdataName, libraryDependency.SuppressParent.ToString("F").Replace(", ", ";"));
                }

                if (libraryDependency.AutoReferenced)
                {
                    metadata.Add(IsImplicitlyDefinedMetadataName, "true");
                }
            }

            return metadata;
        }
    }
}