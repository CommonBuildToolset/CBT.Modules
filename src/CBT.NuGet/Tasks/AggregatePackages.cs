using CBT.NuGet.Internal;
using Microsoft.Build.Construction;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using static CBT.NuGet.Internal.AggregatePackage;

namespace CBT.NuGet.Tasks
{
    /// <summary>
    /// Generate NuGet properties.
    ///
    /// Generate properties that contain the path and version of a given nuget package.
    /// </summary>
    public sealed class AggregatePackages : Task
    {
        /// <summary>
        /// Gets or sets the msbuild item of that contains the packages to aggregate.
        /// Example Input:
        /// <PropertyGroup>
        /// <!-- MSBuild property to set/override. -->
        /// adds the folder to the aggregate unless folder starts with ! then it removes it.
        /// | separates list of folders used for aggregation.
        ///   <CBTAggregatePackage>NuGetPath_LSBuild_Corext=$(NuGetPath_LSBuild_Corext)|$(NuGetPath_Microsoft_LSBuild_Extensions_SQLIS)|!$(NuGetPath_Microsoft_LSBuild_Excluded_Extensions_SQLIS);MyAggPkg=$(NuGetPath_Azure_Corext)|$(NuGetPath_Azure_Corext_AGG)</CBTAggregatePackage>
        /// </PropertyGroup>
        /// </summary>
        [Required]
        public string PackagesToAggregate { get; set; }

        /// <summary>
        /// Gets or sets the full path of the props file that is written to.
        /// </summary>
        [Required]
        public string PropsFile { get; set; }

        /// <summary>
        /// Gets or sets the root path of the packages to be aggregated.
        /// </summary>
        [Required]
        public string AggregateDestRoot { get; set; }

        /// <summary>
        /// Gets or sets the root paths of folders that are considered to be immutable and that the content will never change for that unique folder name.  Example a nuget package.
        /// </summary>
        [Required]
        public string ImmutableRoots { get; set; }

        public override bool Execute()
        {

            IDictionary<string, string> propertiesToCreate = new Dictionary<string, string>();

            foreach (var pkg in ParsePackagesToAggregate())
            {
                try
                {
                    if (!CreateAggregatePackage(pkg))
                    {
                        Log.LogError("Failed to create aggregate package {0} for input of {1}", pkg.OutPropertyId, PackagesToAggregate);
                    }
                }
                catch (DirectoryNotFoundException)
                {
                    Log.LogError("Aggregate package {0} not found after aggregation.", pkg.OutPropertyValue);
                }
                if (propertiesToCreate.ContainsKey(pkg.OutPropertyId))
                {
                    Log.LogWarning("Duplicate Aggregate package {0} specified.  Using first defined.", pkg.OutPropertyId);
                    continue;
                }
                propertiesToCreate.Add(pkg.OutPropertyId, pkg.OutPropertyValue);
            }

            try
            {
                CreatePropsFile(propertiesToCreate, PropsFile);
            }
            catch (Exception e)
            {
                Log.LogErrorFromException(e);
            }

            if (Log.HasLoggedErrors)
            {
                Log.LogError("Define aggregate packages in the format of 'MYAGGPROPERTY=c:\\pkg1|c:\\pkg2|!c:\\pkg3;MYAAGPROPERTY2=c:\\pkg1|c:\\pkg2' where ; seperates aggregate packages and | seperates paths to be aggregated for a package and ! denotes content that should be excluded from the aggregate.");
            }

            return !Log.HasLoggedErrors;
        }

        public bool Execute(string aggregateDestRoot, string packagesToAggregate, string propsFile, string immutableRoots)
        {
            BuildEngine = new CBTBuildEngine();
            AggregateDestRoot = aggregateDestRoot;
            PackagesToAggregate = packagesToAggregate;
            PropsFile = propsFile;
            ImmutableRoots = immutableRoots;
            return Execute();
        }

        internal void CreatePropsFile(IDictionary<string, string> propertyPairs, string propsFile)
        {
            ProjectRootElement project = ProjectRootElement.Create();

            ProjectPropertyGroupElement propertyGroup = project.AddPropertyGroup();
            propertyGroup.SetProperty("MSBuildAllProjects", "$(MSBuildAllProjects);$(MSBuildThisFileFullPath)");

            foreach (var kvp in propertyPairs)
            {
                propertyGroup.SetProperty(kvp.Key, kvp.Value);
            }

            project.Save(propsFile);
        }

        internal IEnumerable<AggregatePackage> ParsePackagesToAggregate()
        {
            // foo=pkg|pkg2|!pkg3;foo2=pkg|pkg2|!pkg3

            foreach (var item in PackagesToAggregate.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                                                   .Select(i => i.Trim())
                                                   .Where(i => !String.IsNullOrWhiteSpace(i))
                                                   .Select(i => i.Split(new[] { '=' }, 2, StringSplitOptions.RemoveEmptyEntries))
                                                   .Select(i => new
                                                   {
                                                       PropertyName = i.First(),
                                                       Options = i.Length == 2 ? i.Last() : null
                                                   })
            )
            {
                IList<PackageOperation> packageOperations = ParsePackageOperations(item.Options).ToList();

                if (packageOperations.Count == 0)
                {
                    // Invalid item because nothing is on the right side of the equal sign or there was no equal sign
                    Log.LogError($"No valid paths were found to aggregate for '{item.PropertyName}' with options '{item.Options}'");
                    continue;
                }

                yield return new AggregatePackage(item.PropertyName, packageOperations, AggregateDestRoot, ImmutableRoots);
            }
        }

        private IEnumerable<PackageOperation> ParsePackageOperations(string options)
        {
            // pkg1|pkg2|!pkg

            if (String.IsNullOrWhiteSpace(options))
            {
                yield break;
            }

            foreach (var option in options.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries)
                                                   .Select(i => i.Trim())
                                                   .Where(i => !String.IsNullOrWhiteSpace(i)))
            {
                AggregateOperation aggregateOperation = option.First() == (char)AggregateOperation.Remove ? AggregateOperation.Remove : AggregateOperation.Add;

                string folder = option.TrimStart((char)AggregateOperation.Remove).Trim();

                if (!Directory.Exists(folder))
                {
                    Log.LogError($"Path to aggregate '{folder}' does not exist.");
                    continue;
                }

                yield return new PackageOperation { Operation = aggregateOperation, Folder = folder };
            }
        }

        internal bool CreateAggregatePackage(AggregatePackage package)
        {
            // The assumption is that in order for the source of an aggregate to change the package source folder must of changed for a new version.
            // And therefor it is assumed this never needs to be regenerated (assumed corruption would be cleaned up manually).
            if (Directory.Exists(package.OutPropertyValue))
            {
                Log.LogMessage(MessageImportance.Low, $"{package.OutPropertyValue} already created. Skipping");
                return true;
            }

            using (var mutex = new Mutex(false, FileUtilities.ComputeMutexName(package.OutPropertyValue)))
            {
                bool owner = false;
                try
                {
                    var outTmpDir = package.OutPropertyValue + ".tmp";
                    FileUtilities.AcquireMutex(mutex);
                    owner = true;
                    // check again to see if aggregate package is already created while waiting.
                    if (Directory.Exists(package.OutPropertyValue))
                    {
                        Log.LogMessage(MessageImportance.Low, $"{package.OutPropertyValue} already created. Skipping");
                        return true;
                    }

                    if (Directory.Exists(outTmpDir))
                    {
                        Log.LogMessage(MessageImportance.Low, $"{outTmpDir} not cleaned up from previous build cleaning now.");
                        Directory.Delete(outTmpDir, true);
                    }
                    foreach (var srcPkg in package.PackagesToAggregate)
                    {
                        if (srcPkg.Operation.Equals(AggregatePackage.AggregateOperation.Add))
                        {
                            Log.LogMessage(MessageImportance.Low, $"Adding {srcPkg.Folder} to aggregate of {package.OutPropertyValue}");
                            FileUtilities.DirectoryCopy(srcPkg.Folder, outTmpDir, true, true);
                        }
                        if (srcPkg.Operation.Equals(AggregatePackage.AggregateOperation.Remove))
                        {
                            Log.LogMessage(MessageImportance.Low, $"Removing {srcPkg.Folder} from aggregate of {package.OutPropertyValue}");
                            FileUtilities.DirectoryRemove(srcPkg.Folder, outTmpDir, true);
                        }
                    }
                    Directory.Move(outTmpDir, package.OutPropertyValue);
                }
                finally
                {
                    if (owner)
                    {
                        mutex.ReleaseMutex();
                    }
                }
            }
            return Directory.Exists(package.OutPropertyValue);
        }

    }
}
