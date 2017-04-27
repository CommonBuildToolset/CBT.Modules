using CBT.NuGet.Internal;
using Microsoft.Build.Construction;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System.Collections.Generic;
using System.IO;

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
        /// * adds the files to the aggregate
        /// | removes the files from the aggregate
        ///   <CBTAggregatePackage>NugetPath_LSBuild_Corext=$(NugetPath_LSBuild_Corext)*$(NugetPath_Microsoft_LSBuild_Extensions_SQLIS)|$(NugetPath_Microsoft_LSBuild_Excluded_Extensions_SQLIS);MyAggPkg=$(NugetPath_Azure_Corext)*$(NugetPath_Azure_Corext_AGG)</CBTAggregatePackage>
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
            BuildEngine = new CBTBuildEngine();
            Dictionary<string, string> propertiesToCreate = new Dictionary<string, string>();

            foreach (var aggPkg in PackagesToAggregate.Split(';'))
            {
                if (string.IsNullOrWhiteSpace(aggPkg))
                {
                    continue;
                }
                var pkg = AggregatePackage.ParseIntoPackage(aggPkg, AggregateDestRoot);
                if (pkg == null)
                {
                    Log.LogError("Failed to parse aggregate package format * adds content to aggregate and | removes content if it exist from aggregate.  Must be in the format of\nMyFirstPackage=e:\\pkgdir\\package.1.2.3*e:\\pkgdir\\packageB.1.4.5|e:\\pkgdir\\packageExclusionsA.1.4.5;MySecondPackage=e:\\pkgdir\\packageC.1.2.3*e:\\pkgdir\\packageD.4.5.6");
                    return false;
                }

                try
                {
                    if (!pkg.CreateAggregatePackage())
                    {
                        Log.LogError("Failed to create aggregate package {0} for input of {1}", pkg.OutPropertyId, aggPkg);
                        return false;
                    }
                }
                catch (DirectoryNotFoundException exp)
                {
                    Log.LogError(exp.Message);
                    throw;
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

    }
}
