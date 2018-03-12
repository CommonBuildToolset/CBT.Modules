using CBT.NuGet.Internal;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CBT.NuGet.Tasks
{
    public sealed class TraversalNuGetRestore : NuGetRestore
    {
        private bool _enableOptimization = true;
        private ProjectCollection _projectCollection;
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

            MSBuildProjectLoader projectLoader = new MSBuildProjectLoader(GlobalProperties.Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries).Where(i => !String.IsNullOrWhiteSpace(i)).Select(i => i.Trim().Split(new[] { '=' }, 2, StringSplitOptions.RemoveEmptyEntries)).ToDictionary(i => i.First(), i => i.Last()), MSBuildToolsVersion, Log, ProjectLoadSettings.IgnoreMissingImports);

            Log.LogMessage(MessageImportance.Normal, $"Loading project references for '{Project}'...");
            _projectCollection = projectLoader.LoadProjectsAndReferences(new[] { Project });

            _enableOptimization = enableOptimization;

            if (Log.HasLoggedErrors)
            {
                return false;
            }

            Log.LogMessage(MessageImportance.Normal, $"Loaded '{_projectCollection.LoadedProjects.Count}' projects");

            if (!TryWriteSolutionFile(_projectCollection, projectLoader.ProjectsLoadedFromTraversal))
            {
                return false;
            }

            bool ret = Execute(file, msBuildVersion, requireConsent, disableParallelProcessing, fallbackSources, noCache, packageSaveMode, sources, configFile, nonInteractive, verbosity, timeout, toolPath, enableOptimization, markerPath, inputs, msbuildPath, additionalArguments);

            return ret && !Log.HasLoggedErrors;
        }

        protected override void ExecutePostRestore()
        {
            if (_enableOptimization)
            {
                foreach (Project loadedProject in _projectCollection.LoadedProjects)
                {
                    string restoreMarkerPath = loadedProject.GetPropertyValue("CBTNuGetPackagesRestoredMarker");

                    if (!String.IsNullOrWhiteSpace(restoreMarkerPath))
                    {
                        GenerateNuGetOptimizationFile(restoreMarkerPath);
                    }

                    restoreMarkerPath = loadedProject.GetPropertyValue("CBTNuGetTraversalPackagesRestoredMarker");

                    if (!String.IsNullOrWhiteSpace(restoreMarkerPath))
                    {
                        GenerateNuGetOptimizationFile(restoreMarkerPath);
                    }
                }
            }

            base.ExecutePostRestore();
        }

        private bool TryWriteSolutionFile(ProjectCollection projectCollection, HashSet<string> projectsLoadedFromTraversal)
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

                    // don't warn on first project of collection as it is the entry project.
                    // don't warn on projects that set SuppressProjectNotInTraversalWarnings to true.
                    if (!project.FullPath.Equals(projectCollection.LoadedProjects.FirstOrDefault()?.FullPath, StringComparison.OrdinalIgnoreCase)
                        &&!project.AllEvaluatedProperties.Any(p => p.Name.Equals("SuppressProjectNotInTraversalWarnings", StringComparison.OrdinalIgnoreCase) && p.EvaluatedValue.Equals("true", StringComparison.OrdinalIgnoreCase))
                        && !projectsLoadedFromTraversal.Contains(project.FullPath))
                    {
                        Log.LogWarning("TraversalParse", "CBT.NuGet.1003", "ProjectNotInTraversal", "project.FullPath", 0, 0, 0, 0, message: $"Project {project.FullPath} is not detected in traversal graph (dirs.proj) but is being pulled into build graph through (ProjectReference).  Add missing project to traversal graph to ensure proper functionality.  You can set property SuppressProjectNotInTraversalWarnings to true in this project to suppress and ignore these warnings.");
                    }
                }
            }

            return true;
        }
    }
}