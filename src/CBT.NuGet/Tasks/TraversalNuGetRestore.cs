using CBT.NuGet.Internal;
using Microsoft.Build.Construction;
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

        public bool Execute(string file, string projectsFile, string msBuildVersion, bool requireConsent, bool disableParallelProcessing, string[] fallbackSources, bool noCache, string packageSaveMode, string[] sources, string configFile, bool nonInteractive, string verbosity, int timeout, string toolPath, bool enableOptimization, string markerPath, string[] inputs, string msbuildToolsVersion, string project, string globalProperties, string msbuildPath, string additionalArguments)
        {
            if (BuildEngine == null)
            {
                BuildEngine = new CBTBuildEngine();
            }

            MSBuildToolsVersion = msbuildToolsVersion;
            Project = project;
            GlobalProperties = globalProperties;
            File = file;

            FileInfo projectsFileInfo = !String.IsNullOrWhiteSpace(projectsFile) ? new FileInfo(projectsFile) : null;
            
            inputs = inputs.Concat(GetAllPathsFromProjectsFile(projectsFileInfo)).ToArray();

            MSBuildProjectLoader projectLoader = new MSBuildProjectLoader(GlobalProperties.Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries).Where(i => !String.IsNullOrWhiteSpace(i)).Select(i => i.Trim().Split(new[] { '=' }, 2, StringSplitOptions.RemoveEmptyEntries)).ToDictionary(i => i.First(), i => i.Last()), MSBuildToolsVersion, Log, ProjectLoadSettings.IgnoreMissingImports);

            // In the scenario where a project is deleted from the codebase the projectsFile contains invalid projects.  This causes a file load error.  So always delete file.
            if (System.IO.File.Exists(projectsFileInfo.FullName))
            {
                System.IO.File.Delete(projectsFileInfo.FullName);
            }

            Log.LogMessage(MessageImportance.Normal, $"Loading project references for '{Project}'...");
            _projectCollection = projectLoader.LoadProjectsAndReferences(new[] { Project });
            _enableOptimization = enableOptimization;

            Log.LogMessage(MessageImportance.Low, "10 Slowest Loading Projects:");
            foreach (var loadTimes in projectLoader.Statistics.ProjectLoadTimes.OrderByDescending(i => i.Value).Take(10))
            {
                Log.LogMessage(MessageImportance.Low, $"  {loadTimes.Key} {loadTimes.Value}");
            }

            if (Log.HasLoggedErrors)
            {
                return false;
            }

            Log.LogMessage(MessageImportance.Normal, $"Loaded '{_projectCollection.LoadedProjects.Count}' projects");

            if (!TryWriteSolutionFile(_projectCollection))
            {
                return false;
            }

            if (projectsFileInfo != null && !TryWriteProjectsFile(_projectCollection, projectsFileInfo))
            {
                return false;
            }

            // Always regenerate the dirs.proj.sln since a project can be deleted from the source tree.
            if (enableOptimization && IsFileUpToDate(Log, markerPath, inputs))
            {
                Log.LogMessage(MessageImportance.Low, "Traversal NuGet packages are up-to-date");
                return true;
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

        private IEnumerable<string> GetAllPathsFromProjectsFile(FileInfo projectsFileInfo)
        {
            if (projectsFileInfo == null || !projectsFileInfo.Exists)
            {
                yield break;
            }

            ProjectRootElement rootElement = ProjectRootElement.Open(projectsFileInfo.FullName);

            if (rootElement == null)
            {
                yield break;
            }

            foreach (ProjectItemElement projectItemElement in rootElement.Items.Where(i => i.ItemType.Equals("ProjectFile") || i.ItemType.Equals("TraversalFile")))
            {
                if (System.IO.File.Exists(projectItemElement.Include))
                {
                    yield return projectItemElement.Include;
                }
            }
        }

        private bool TryWriteProjectsFile(ProjectCollection projectCollection, FileInfo projectsFile)
        {
            Log.LogMessageFromText($"Generating file '{projectsFile.FullName}'", MessageImportance.Low);

            Directory.CreateDirectory(projectsFile.DirectoryName);

            ProjectRootElement rootElement = ProjectRootElement.Create(projectsFile.FullName);

            ProjectItemGroupElement projectFileItemGroup = rootElement.AddItemGroup();
            ProjectItemGroupElement traversalFileItemGroup = rootElement.AddItemGroup();

            foreach (Project project in projectCollection.LoadedProjects)
            {
                if (String.Equals(project.GetPropertyValue("IsTraversal"), "true", StringComparison.OrdinalIgnoreCase))
                {
                    traversalFileItemGroup.AddItem("TraversalFile", project.FullPath);
                }
                else
                {
                    projectFileItemGroup.AddItem("ProjectFile", project.FullPath);
                }
            }

            rootElement.Save();

            return true;
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