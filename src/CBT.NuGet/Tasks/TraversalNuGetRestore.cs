using CBT.NuGet.Internal;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace CBT.NuGet.Tasks
{
    public sealed class TraversalNuGetRestore : NuGetRestore
    {
        public string GlobalProperties { get; set; }

        [Required]
        public string MSBuildToolsVersion { get; set; }

        [Required]
        public string Project { get; set; }

        public override bool Execute()
        {
            MSBuildProjectLoader projectLoader = new MSBuildProjectLoader(GlobalProperties.Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries).Where(i => !String.IsNullOrWhiteSpace(i)).Select(i => i.Trim().Split(new[] { '=' }, 2, StringSplitOptions.RemoveEmptyEntries)).ToDictionary(i => i.First(), i => i.Last()), MSBuildToolsVersion, Log, ProjectLoadSettings.IgnoreMissingImports);

            Log.LogMessage(MessageImportance.Normal, $"Loading project references for '{Project}'...");
            ProjectCollection projectCollection = projectLoader.LoadProjectsAndReferences(new[] { Project });

            if (Log.HasLoggedErrors)
            {
                return false;
            }

            Log.LogMessage(MessageImportance.Normal, $"Loaded '{projectCollection.LoadedProjects.Count}' projects");

            var projectsWithPackagesConfig = projectCollection.LoadedProjects.Select(i => new
            {
                Project = i,
                PackageConfig = Path.Combine(i.DirectoryPath, "packages.config"),
            }).Where(i => System.IO.File.Exists(i.PackageConfig)).ToList();

            Log.LogMessage(MessageImportance.Low, "Aggregating packages...");

            List<Tuple<string, string>> aggregatedPackages = projectsWithPackagesConfig.AsParallel().SelectMany(i => GetPackages(i.PackageConfig)).Distinct().OrderBy(i => i.Item1).ToList();

            if (aggregatedPackages.Count == 0)
            {
                Log.LogMessage(MessageImportance.Low, "No packages were found to aggregate");

                return true;
            }

            WriteAggregatePackagesConfig(File, aggregatedPackages);

            Log.LogMessage(MessageImportance.Low, $"Successfully aggregated packages to '{File}'");

            bool ret = base.Execute();

            if (ret)
            {
                foreach (Project project in projectsWithPackagesConfig.Select(i => i.Project))
                {
                    string restoreMarkerPath = project.GetPropertyValue("CBTNuGetPackagesRestoredMarker");

                    if (!String.IsNullOrWhiteSpace(restoreMarkerPath))
                    {
                        Log.LogMessage(MessageImportance.Low, $"Creating '{restoreMarkerPath}' for project '{project.FullPath}'");

                        Directory.CreateDirectory(Path.GetDirectoryName(restoreMarkerPath));

                        System.IO.File.WriteAllText(restoreMarkerPath, String.Empty);
                    }
                }
            }

            return ret && !Log.HasLoggedErrors;
        }

        public bool Execute(string file, string msBuildVersion, string packagesDirectory, bool requireConsent, string solutionDirectory, bool disableParallelProcessing, string[] fallbackSources, bool noCache, string packageSaveMode, string[] sources, string configFile, bool nonInteractive, string verbosity, int timeout, string toolPath, bool enableOptimization, string markerPath, string[] inputs, string msbuildToolsVersion, string project, string globalProperties, string msbuildPath, string additionalArguments)
        {
            MSBuildToolsVersion = msbuildToolsVersion;
            Project = project;
            GlobalProperties = globalProperties;

            return base.Execute(file, msBuildVersion, packagesDirectory, requireConsent, solutionDirectory, disableParallelProcessing, fallbackSources, noCache, packageSaveMode, sources, configFile, nonInteractive, verbosity, timeout, toolPath, enableOptimization, markerPath, inputs, msbuildPath, additionalArguments);
        }

        private static IEnumerable<Tuple<string, string>> GetPackages(string packagesConfigPath)
        {
            XDocument document = XDocument.Load(packagesConfigPath);

            if (document.Root != null)
            {
                foreach (var item in document.Root.Elements("package").Select(i => new
                {
                    Id = i.Attribute("id") == null ? null : i.Attribute("id")?.Value,
                    Version = i.Attribute("version") == null ? null : i.Attribute("version")?.Value,
                }))
                {
                    // Skip packages that are missing an 'id' or 'version' attribute or if they specified value is an empty string
                    //
                    if (item.Id == null || item.Version == null ||
                        String.IsNullOrWhiteSpace(item.Id) ||
                        String.IsNullOrWhiteSpace(item.Version))
                    {
                        continue;
                    }

                    yield return new Tuple<string, string>(item.Id, item.Version);
                }
            }
        }

        private static void WriteAggregatePackagesConfig(string path, IEnumerable<Tuple<string, string>> packageInfos)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));

            new XDocument(new XElement("packages", packageInfos.Select(packageInfo => new XElement("package", new XAttribute("id", packageInfo.Item1), new XAttribute("version", packageInfo.Item2))))).Save(path);
        }
    }
}