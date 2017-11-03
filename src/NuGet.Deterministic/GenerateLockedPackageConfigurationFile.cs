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
using System.Reflection;

namespace NuGet.Tasks.Deterministic
{
    /// <summary>
    /// Generate NuGet properties.
    ///
    /// Generate properties that contain the path and version of a given nuget package.
    /// </summary>
    public sealed class GenerateLockedPackageConfigurationFile : Task
    {
        public const string HashfileMetadataName = "HashFile";

        public const string Sha512MetadataName = "Sha512";

        public const string VersionMetadataName = "Version";

        public const string PathMetadataName = "Path";

        public const string IncludeAssetsMetadataName = "IncludeAssets";

        public const string PrivateAssetsMetdataName = "PrivateAssets";


        /// <summary>
        /// Stores a list of assembly search paths where dependencies should be searched for.
        /// </summary>
        private readonly ICollection<string> _assemblySearchPaths = new List<string>();

        /// <summary>
        /// Stores a list of loaded assemblies in the event that the same assembly is requested multiple times.
        /// </summary>
        private readonly IDictionary<AssemblyName, Assembly> _loadedAssemblies = new Dictionary<AssemblyName, Assembly>();

        public GenerateLockedPackageConfigurationFile()
        {
            SetAssemblyResolver();
        }

        /// <summary>
        /// Gets or sets the full path of the msbuild properties file to create.
        /// </summary>
        [Required]
        public string GeneratedOutputPropsFile { get; set; }

        /// <summary>
        /// Gets or sets the full path of the NuGet assets file (project.assets.json).
        /// </summary>
        [Required]
        public string NuGetAssetsFilePath { get; set; }

        /// <summary>
        /// Gets or sets a true/false flag to determine if the GeneratedOutputPropsFile should be overwritten. Defaults to false which means to update without deletes.
        /// </summary>
        public bool OverwritePropsFile { get; set; }

        /// <summary>
        /// Gets or sets the list of packages to exclude from being made deterministic.
        /// </summary>
        public ITaskItem[] PackagesToExclude { get; set; }

        public override bool Execute()
        {
            if (!TryCreateProject(out ProjectRootElement project))
            {
                return false;
            }

            FileInfo fileInfo = new FileInfo(GeneratedOutputPropsFile);

            if (fileInfo.Exists && fileInfo.IsReadOnly)
            {
                // Some source control systems mark files as read-only
                //
                fileInfo.IsReadOnly = false;
            }

            fileInfo.Directory?.Create();

            project.Save(GeneratedOutputPropsFile);

            return !Log.HasLoggedErrors && File.Exists(GeneratedOutputPropsFile);
        }

        /// <summary>
        /// Creates an MSBuild properties file that specifies the full closure of packages with locked versions.
        /// </summary>
        /// <returns>A <see cref="ProjectRootElement"/> object that can be saved.</returns>
        internal bool TryCreateProject(out ProjectRootElement project)
        {
            project = null;

            if (!File.Exists(NuGetAssetsFilePath))
            {
                Log.LogError($"NuGet assets file '{NuGetAssetsFilePath}' does not exist.");
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

            project.AddProperty("NuGetDeterministicPropsWasImported", "true");

            LockFile lockFile = LockFileUtilities.GetLockFile(NuGetAssetsFilePath, NullLogger.Instance);

            bool crossTargeting = lockFile.Targets.Count > 1;

            foreach (LockFileTarget target in lockFile.Targets)
            {
                TargetFrameworkInformation targetFramework = lockFile.PackageSpec.TargetFrameworks.FirstOrDefault(i => i.FrameworkName.Equals(target.TargetFramework));

                if (targetFramework == null)
                {
                    continue;
                }

                ProjectItemGroupElement itemGroupElement = project.AddItemGroup();

                if (crossTargeting)
                {
                    itemGroupElement.Condition = $" '$(TargetFramework)' == '{target.TargetFramework.GetShortFolderName()}' ";
                }

                foreach (LockFileTargetLibrary targetLibrary in target.Libraries)
                {
                    LockFileLibrary library = lockFile.Libraries.FirstOrDefault(i => i.Name.Equals(targetLibrary.Name));

                    if (library == null)
                    {
                        continue;
                    }

                    // Skip any packages listed in the exclusion list.
                    if (packagesToExclude.Contains(library.Name))
                    {
                        continue;
                    }

                    Dictionary<string, string> metadata = new Dictionary<string, string>
                    {
                        {VersionMetadataName, $"[{library.Version}]"},
                        {Sha512MetadataName, library.Sha512},
                        {PathMetadataName, library.Path},
                        {HashfileMetadataName, library.Files.First(i => i.EndsWith("nupkg.sha512"))},
                    };

                    LibraryDependency libraryDependency = targetFramework.Dependencies.FirstOrDefault(i => i.Name.Equals(targetLibrary.Name));

                    if (libraryDependency != null)
                    {
                        metadata.Add(IncludeAssetsMetadataName, libraryDependency.IncludeType.ToString("F").Replace(", ", ";"));
                        metadata.Add(PrivateAssetsMetdataName, libraryDependency.SuppressParent.ToString("F").Replace(", ", ";"));
                    }

                    itemGroupElement.AddItem("PackageReference", targetLibrary.Name, metadata.ToList());
                }
            }

            return true;
        }

        private Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            AssemblyName assemblyName = new AssemblyName(args.Name);

            // Return the assembly if its already been loaded
            //
            if (_loadedAssemblies.ContainsKey(assemblyName))
            {
                return _loadedAssemblies[assemblyName];
            }

            // Return the first assembly search path that contains the requested assembly
            //
            string assemblyPath = _assemblySearchPaths.Select(i => Path.Combine(i, $"{assemblyName.Name}.dll")).FirstOrDefault(File.Exists);

            if (assemblyPath != null)
            {
                // Load the assembly and keep it in the list of loaded assemblies
                //
                _loadedAssemblies[assemblyName] = Assembly.Load(File.ReadAllBytes(assemblyPath));

                return _loadedAssemblies[assemblyName];
            }

            return null;
        }

        private void SetAssemblyResolver()
        {
            string executingAssemblyLocation = Assembly.GetExecutingAssembly().Location;

            if (!String.IsNullOrWhiteSpace(executingAssemblyLocation))
            {
                // When loading an assembly from a byte[], the Assembly.Location is not set so it shouldn't be considered
                //
                _assemblySearchPaths.Add(Path.GetDirectoryName(executingAssemblyLocation));
            }

            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
        }
    }
}