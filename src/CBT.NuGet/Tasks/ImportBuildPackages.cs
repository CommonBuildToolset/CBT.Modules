using CBT.NuGet.Internal;
using Microsoft.Build.Construction;
using Microsoft.Build.Framework;
using System;
using System.IO;
using System.Linq;

namespace CBT.NuGet.Tasks
{
    /// <summary>
    /// A class for creating imports to NuGet build packages as if they are modules.
    /// </summary>
    public sealed class ImportBuildPackages : SemaphoreTask
    {
        /// <summary>
        /// The path to the modules packages.config.
        /// </summary>
        public string ModulePackagesConfig { get; set; }

        /// <summary>
        /// The paths to modules.
        /// </summary>
        public string[] ModulePaths { get; set; }

        /// <summary>
        /// The path to the .props file to create.
        /// </summary>
        public string PropsFile { get; set; }

        /// <summary>
        /// The path to the .targets file to create.
        /// </summary>
        public string TargetsFile { get; set; }

        protected override string SemaphoreName => PropsFile;

        public bool Execute(string modulePackagesConfig, string propsFile, string targetsFile, string[] inputs, string[] modulePaths)
        {
            try
            {
                BuildEngine = new CBTBuildEngine();

                Log.LogMessage(MessageImportance.Low, "Generate module build package imports:");
                Log.LogMessage(MessageImportance.Low, $"  ModulePackagesConfig = {modulePackagesConfig}");
                Log.LogMessage(MessageImportance.Low, $"  PropsFile = {propsFile}");
                Log.LogMessage(MessageImportance.Low, $"  TargetsFile = {targetsFile}");
                Log.LogMessage(MessageImportance.Low, $"  Inputs = {String.Join(";", inputs)}");
                Log.LogMessage(MessageImportance.Low, $"  ModulePaths = {String.Join(";", modulePaths)}");

                if (NuGetRestore.IsFileUpToDate(Log, propsFile, inputs) && NuGetRestore.IsFileUpToDate(Log, targetsFile, inputs))
                {
                    Log.LogMessage(MessageImportance.Low, $"Module build package import files '{propsFile}' and '{targetsFile}' are up-to-date");
                    return true;
                }

                ModulePackagesConfig = modulePackagesConfig;
                PropsFile = propsFile;
                TargetsFile = targetsFile;
                ModulePaths = modulePaths;

                return Execute();
            }
            catch (Exception e)
            {
                Log.LogError(e.ToString());
                return false;
            }
        }

        public override void Run()
        {
            Log.LogMessage("Generating module build package imports");

            ProjectRootElement propsProject = ProjectRootElement.Create(PropsFile);
            ProjectRootElement targetsProject = ProjectRootElement.Create(TargetsFile);

            ProjectPropertyGroupElement propertyGroup = propsProject.AddPropertyGroup();

            foreach (BuildPackageInfo buildPackageInfo in ModulePaths.Select(BuildPackageInfo.FromModulePath).Where(i => i != null))
            {
                if (buildPackageInfo.PropsPath == null && buildPackageInfo.TargetsPath == null)
                {
                    Log.LogMessage(MessageImportance.Low, $"  Skipping '{buildPackageInfo.Id}' because it is not a standard NuGet build package.");
                    continue;
                }

                // If this is a cbt module do not auto import props or targets.
                if (File.Exists(Path.Combine(Path.GetDirectoryName(buildPackageInfo.PropsPath ?? buildPackageInfo.TargetsPath), "module.config")))
                {
                    Log.LogMessage(MessageImportance.Low, $"  Skipping '{buildPackageInfo.Id}' because it is a CBT Module.");
                    continue;
                }

                ProjectPropertyElement enableProperty = propertyGroup.AddProperty(buildPackageInfo.EnablePropertyName, "false");
                enableProperty.Condition = $" '$({buildPackageInfo.EnablePropertyName})' == '' ";

                ProjectPropertyElement runProperty = propertyGroup.AddProperty(buildPackageInfo.RunPropertyName, "true");
                runProperty.Condition = $" '$({buildPackageInfo.RunPropertyName})' == '' ";

                if (File.Exists(buildPackageInfo.PropsPath))
                {
                    ProjectImportElement import = propsProject.AddImport(buildPackageInfo.PropsPath);

                    import.Condition = $" '$({buildPackageInfo.EnablePropertyName})' == 'true' And '$({buildPackageInfo.RunPropertyName})' == 'true' ";
                }

                if (File.Exists(buildPackageInfo.TargetsPath))
                {
                    ProjectImportElement import = targetsProject.AddImport(buildPackageInfo.TargetsPath);

                    import.Condition = $" '$({buildPackageInfo.EnablePropertyName})' == 'true' And '$({buildPackageInfo.RunPropertyName})' == 'true' ";
                }

                Log.LogMessage($"  Generated imports for '{buildPackageInfo.Id}'.");
            }

            propsProject.Save();
            targetsProject.Save();
        }


        private class BuildPackageInfo
        {
            private BuildPackageInfo()
            {
            }

            public string EnablePropertyName { get; private set; }

            public string Id { get; private set; }

            public string PropsPath { get; private set; }

            public string RunPropertyName { get; private set; }

            public string TargetsPath { get; private set; }

            public static BuildPackageInfo FromModulePath(string modulePath)
            {
                string[] parts = modulePath.Split(new[] {'='}, 2, StringSplitOptions.RemoveEmptyEntries);

                if (parts.Length != 2 || String.IsNullOrWhiteSpace(parts[0]) || String.IsNullOrWhiteSpace(parts[1]))
                {
                    return null;
                }

                string id = parts[0];
                string path = parts[1];

                FileInfo propsFile = new FileInfo(Path.Combine(path, "build", $"{id}.props"));
                FileInfo targetsFile = new FileInfo(Path.Combine(path, "build", $"{id}.targets"));

                BuildPackageInfo buildPackageInfo = new BuildPackageInfo
                {
                    Id = parts[0],
                    PropsPath = propsFile.Exists ? propsFile.FullName : null,
                    TargetsPath = targetsFile.Exists ? targetsFile.FullName : null,
                    EnablePropertyName = $"Enable{id.Replace(".", "_")}",
                    RunPropertyName = $"Run{id.Replace(".", "_")}",
                };

                return buildPackageInfo;
            }
        }
    }
}