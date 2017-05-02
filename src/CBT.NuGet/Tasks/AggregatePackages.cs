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
        ///   <CBTAggregatePackage>NugetPath_LSBuild_Corext=$(NugetPath_LSBuild_Corext)|$(NugetPath_Microsoft_LSBuild_Extensions_SQLIS)|!$(NugetPath_Microsoft_LSBuild_Excluded_Extensions_SQLIS);MyAggPkg=$(NugetPath_Azure_Corext)|$(NugetPath_Azure_Corext_AGG)</CBTAggregatePackage>
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

        public override bool Execute()
        {
            // Property=path*package1!package2
            // Property=path,pkg1,!pkg2;
            
            Dictionary<string, string> propertiesToCreate = new Dictionary<string, string>();

            foreach (var pkg in ParsePackagesToAggregate())
            {
                try
                {
                    if (!CreateAggregatePackage(pkg))
                    {
                        Log.LogError("Failed to create aggregate package {0} for input of {1}", pkg.OutPropertyId, PackagesToAggregate);
                        return false;
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

            if (!CreatePropsFile(propertiesToCreate, PropsFile))
            {
                return false;
            }

            return true;
        }

        public bool Execute(string aggregateDestRoot, string packagesToAggregate, string propsFile)
        {
            BuildEngine = new CBTBuildEngine();
            AggregateDestRoot = aggregateDestRoot;
            PackagesToAggregate = packagesToAggregate;
            PropsFile = propsFile;
            return Execute();
        }
        
        private bool CreatePropsFile(Dictionary<string,string> propertyPairs, string propsFile)
        {
            ProjectRootElement project = ProjectRootElement.Create();

            ProjectPropertyGroupElement propertyGroup = project.AddPropertyGroup();
            propertyGroup.SetProperty("MSBuildAllProjects", "$(MSBuildAllProjects);$(MSBuildThisFileFullPath)");

            foreach (var kvp in propertyPairs)
            {
                propertyGroup.SetProperty(kvp.Key, kvp.Value);
            }

            project.Save(propsFile);

            return File.Exists(propsFile);
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
                if(String.IsNullOrWhiteSpace(item.Options))
                {
                    // Invalid item because nothing is on the right side of the equal sign or there was no equal sign
                    continue;
                }

                yield return new AggregatePackage(item.PropertyName, ParseAggregateOptions(item.Options).ToList(), AggregateDestRoot);
            }
        }

        internal IEnumerable<PackageOperations> ParseAggregateOptions(string option)
        {
            // pkg1|pkg2|!pkg

            foreach (var folder in option.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries)
                                                   .Select(i => i.Trim())
                                                   .Where(i => !String.IsNullOrWhiteSpace(i)))
            {
                AggregateOperation aggregateOperation = folder.First() == '!' ? AggregateOperation.Remove : AggregateOperation.Add;

                yield return new PackageOperations { Operation = aggregateOperation, Folder = folder.TrimStart('!').Trim() };
            }
        }

        internal bool CreateAggregatePackage(AggregatePackage package)
        {
            // The assumption is that in order for the source of an aggregate to change the package source folder must of changed for a new version.
            // And therefor it is assumed this never needs to be regenerated (assumped corruption would be cleaned up manually).
            // This is a flawed assumption if someone passes a non nuget package folder to aggregate.  Must consider what to do.
            if (Directory.Exists(package.OutPropertyValue))
            {
                Log.LogMessage(MessageImportance.Low, "{0} already created. Skipping", package.OutPropertyValue);
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
                        Log.LogMessage(MessageImportance.Low, "{0} already created. Skipping", package.OutPropertyValue);
                        return true;
                    }

                    if (Directory.Exists(outTmpDir))
                    {
                        Log.LogMessage(MessageImportance.Low, "{0} not cleaned up from previous build cleaning now.", outTmpDir);
                        Directory.Delete(outTmpDir, true);
                    }
                    foreach (var srcPkg in package.PackagesToAggregate)
                    {
                        if (srcPkg.Operation.Equals(AggregatePackage.AggregateOperation.Add))
                        {
                            Log.LogMessage(MessageImportance.Low, "Adding {0} to aggregate of {1}", srcPkg.Folder, package.OutPropertyValue);
                            FileUtilities.DirectoryCopy(srcPkg.Folder, outTmpDir, true, true);
                        }
                        if (srcPkg.Operation.Equals(AggregatePackage.AggregateOperation.Remove))
                        {
                            Log.LogMessage(MessageImportance.Low, "Removing {0} from aggregate of {1}", srcPkg.Folder, package.OutPropertyValue);
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
