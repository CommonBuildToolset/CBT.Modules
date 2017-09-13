using Microsoft.Build.Construction;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using NuGet.Common;
using NuGet.ProjectModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace CBT.NuGet.Tasks
{
    /// <summary>
    /// Generate NuGet properties.
    ///
    /// Generate properties that contain the path and version of a given nuget package.
    /// </summary>
    public sealed class GenerateLockedPackageConfigurationFile : Task
    {

        /// <summary>
        /// Stores a list of assembly search paths where dependencies should be searched for.
        /// </summary>
        private readonly ICollection<string> _assemblySearchPaths = new List<string>();

        /// <summary>
        /// Stores a list of loaded assemblies in the event that the same assembly is requested multiple times.
        /// </summary>
        private readonly IDictionary<AssemblyName, Assembly> _loadedAssemblies = new Dictionary<AssemblyName, Assembly>();

        /// <summary>
        /// Gets or sets the full path of the msbuild properties file to create.
        /// </summary>
        [Required]
        public string GeneratedOutputPropsFile { get; set; }

        /// <summary>
        /// Gets or sets a true/false flag to determine if the GeneratedOutputPropsFile should be overwritten. Defaults to false which means to update without deletes.
        /// </summary>
        public bool OverwritePropsFile { get; set; }

        /// <summary>
        /// Gets or sets the full path of the nuget assets file that is read from the project to.
        /// </summary>
        [Required]
        public string NuGetAssetsFilePath { get; set; }

        /// <summary>
        /// Gets or sets the list of packages to exclude from being made deterministic.
        /// </summary>
        public ITaskItem[] PackagesToExclude { get; set; }

        public GenerateLockedPackageConfigurationFile()
        {
            SetAssemblyResolver();
        }

        
        public override bool Execute()
        {
            ProjectRootElement project = null;
            ProjectItemGroupElement itemGroup = null;

            if (!File.Exists(NuGetAssetsFilePath))
            {
                Log.LogError($"NuGet assets file {NuGetAssetsFilePath} does not exist.");
                return false;
            }
            if (File.Exists(GeneratedOutputPropsFile) && !OverwritePropsFile)
            {
                project = ProjectRootElement.Open(GeneratedOutputPropsFile);
                itemGroup = project?.ItemGroups.LastOrDefault();
            }
            // project is null if the file does not exist or OverwritePropsFile is true.
            if (project == null)
            {
                project = ProjectRootElement.Create();
                ProjectPropertyGroupElement propertyGroup = project.AddPropertyGroup();
                propertyGroup.AddProperty("NuGetDeterministicPropsWasImported", "true");
            }
            if (itemGroup == null)
            {
                itemGroup = project.AddItemGroup();
            }
            LockFile lockFile = LockFileUtilities.GetLockFile(NuGetAssetsFilePath, NullLogger.Instance);

            foreach (var package in lockFile.Libraries.Where(p => p.Type.Equals("package")))
            {
                // should item group be conditioned or items or metadata?  Perhaps item condition should be considered and compared as well as an item could be conditioned.  Consider the below scenarios.  Since we are only parsing the assets file we need to consider the project file entries.
                // <PackageReference Include="foo" Version="1.2.3" Condition="bar"/>
                // <PackageReference Include="foo">
                //    <version>1.2.3</version>
                //    <version Condition="bar">1.2.3</version>
                // </PackageReference>
                // What about dependencies of packages that are conditioned? they should be conditioned as well.

                // Skip any packages listed in the exclusion list.
                if (PackagesToExclude != null && PackagesToExclude.Length > 0 && PackagesToExclude
                    .Any(i => i.ItemSpec.Equals(package.Name, StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }
                ProjectItemElement item =
                    project.Items.SingleOrDefault(i => i.Include.Equals(package.Name, StringComparison.OrdinalIgnoreCase));
                if (item == null)
                {
                    item = itemGroup.AddItem("PackageReference", package.Name);
                }

                AddMetadataToProject(item, package, "version", $"[{package.Version}]");
                AddMetadataToProject(item, package, "sha512", $"{package.Sha512}");
                AddMetadataToProject(item, package, "path", $"{package.Path}");
                AddMetadataToProject(item, package, "hashFileName", $"{package.Path.Replace("/", ".")}.nupkg.sha512");

            }
            project.Save(GeneratedOutputPropsFile);
            return File.Exists(GeneratedOutputPropsFile);
        }

        private static void AddMetadataToProject(ProjectItemElement item, LockFileLibrary package, string name, string value)
        {
            ProjectMetadataElement metadata =
                item.Metadata.SingleOrDefault(m => m.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (metadata == null)
            {
                item.AddMetadata(name, value);
            }
            else
            {
                metadata.Value = value;
            }
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
    }
}
