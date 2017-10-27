using CBT.NuGet.Internal;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Framework;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace CBT.NuGet.Tasks
{
    public sealed class TraversalNuGetRestore : NuGetRestore
    {
        public string GlobalProperties { get; set; }

        [Required]
        public string MSBuildToolsVersion { get; set; }

        [Required]
        public string Project { get; set; }

        public bool Execute(string file, string msBuildVersion, bool requireConsent, bool disableParallelProcessing, string[] fallbackSources, bool noCache, string packageSaveMode, string[] sources, string configFile, bool nonInteractive, string verbosity, int timeout, string toolPath, bool enableOptimization, string markerPath, string[] inputs, string msbuildToolsVersion, string project, string globalProperties, string msbuildPath, string additionalArguments)
        {
            if (BuildEngine == null)
            {
                BuildEngine = new CBTBuildEngine();
            }
            
            MSBuildToolsVersion = msbuildToolsVersion;
            Project = project;
            GlobalProperties = globalProperties;
            File = file;

            if (enableOptimization && IsFileUpToDate(Log, markerPath, inputs))
            {
                Log.LogMessage(MessageImportance.Low, "Traversal NuGet packages are up-to-date");
                return true;
            }

            MSBuildProjectLoader projectLoader = new MSBuildProjectLoader(GlobalProperties.Split(new[] {';', ','}, StringSplitOptions.RemoveEmptyEntries).Where(i => !String.IsNullOrWhiteSpace(i)).Select(i => i.Trim().Split(new[] {'='}, 2, StringSplitOptions.RemoveEmptyEntries)).ToDictionary(i => i.First(), i => i.Last()), MSBuildToolsVersion, Log, ProjectLoadSettings.IgnoreMissingImports);

            Log.LogMessage(MessageImportance.Normal, $"Loading project references for '{Project}'...");
            ProjectCollection projectCollection = projectLoader.LoadProjectsAndReferences(new[] {Project});

            if (Log.HasLoggedErrors)
            {
                return false;
            }

            Log.LogMessage(MessageImportance.Normal, $"Loaded '{projectCollection.LoadedProjects.Count}' projects");

            if (!TryWriteSolutionFile(projectCollection))
            {
                return false;
            }

            bool ret = Execute(file, msBuildVersion, requireConsent, disableParallelProcessing, fallbackSources, noCache, packageSaveMode, sources, configFile, nonInteractive, verbosity, timeout, toolPath, enableOptimization, markerPath, inputs, msbuildPath, additionalArguments);

            if (ret)
            {
                foreach (Project loadedProject in projectCollection.LoadedProjects)
                {
                    string restoreMarkerPath = loadedProject.GetPropertyValue("CBTNuGetPackagesRestoredMarker");

                    if (!String.IsNullOrWhiteSpace(restoreMarkerPath))
                    {
                        Log.LogMessage(MessageImportance.Low, $"Creating '{restoreMarkerPath}' for project '{loadedProject.FullPath}'");

                        Directory.CreateDirectory(Path.GetDirectoryName(restoreMarkerPath));

                        System.IO.File.WriteAllText(restoreMarkerPath, String.Empty);
                    }
                }
            }

            return ret && !Log.HasLoggedErrors;
        }

        private bool TryWriteSolutionFile(ProjectCollection projectCollection)
        {
            string folder = $"{Path.GetDirectoryName(File)}{Path.DirectorySeparatorChar}";
            Uri fromUri = new Uri(folder);
            Directory.CreateDirectory(folder);
            using (var writer = System.IO.File.CreateText(File))
            {
                writer.WriteLine("Microsoft Visual Studio Solution File, Format Version 12.00");

                foreach (var project in projectCollection.LoadedProjects)
                {
                    Uri toUri = new Uri(project.FullPath, UriKind.Absolute);

                    string relativePath = Uri.UnescapeDataString(fromUri.MakeRelativeUri(toUri).ToString()).Replace('/', Path.DirectorySeparatorChar);

                    writer.WriteLine($"Project(\"\") = \"\", \"{relativePath}\", \"\"");
                    writer.WriteLine("EndProject");
                }
            }

            return true;
        }
    }
}