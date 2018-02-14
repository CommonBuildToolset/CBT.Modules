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
            if (File.Exists(PropsFile) && File.Exists(TargetsFile) && IsFileUpToDate(propsFile, inputs) && IsFileUpToDate(targetsFile, inputs))
            {
                return true;
            }

            BuildEngine = new CBTBuildEngine();

            ModulePackagesConfig = modulePackagesConfig;
            PropsFile = propsFile;
            TargetsFile = targetsFile;
            ModulePaths = modulePaths;

            return Execute();
        }

        public override void Run()
        {
            Log.LogMessage("Creating NuGet build package imports");

            ProjectRootElement propsProject = ProjectRootElement.Create(PropsFile);
            ProjectRootElement targetsProject = ProjectRootElement.Create(TargetsFile);

            ProjectPropertyGroupElement propertyGroup = propsProject.AddPropertyGroup();

            foreach (BuildPackageInfo buildPackageInfo in ModulePaths.Select(BuildPackageInfo.FromModulePath).Where(i => i != null))
            {
                // If this is a cbt module do not auto import props or targets.
                if (File.Exists(Path.Combine(Path.GetDirectoryName(buildPackageInfo.PropsPath), "module.config")))
                {
                    Log.LogMessage(MessageImportance.Low, $"Not auto importing {buildPackageInfo.Id} package because it is a CBT Module.");
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

                Log.LogMessage($"  {buildPackageInfo.Id}");
            }

            propsProject.Save();
            targetsProject.Save();
        }

        protected override bool BeforeRun()
        {
            // Do not do any work if the props file already exists
            return !File.Exists(PropsFile) || !File.Exists(TargetsFile);
        }

        /// <summary>
        /// Determines if a file is up-to-date in relation to the specified paths.
        /// </summary>
        /// <param name="input">The file to check if it is out-of-date.</param>
        /// <param name="outputs">The list of files to check against.</param>
        /// <returns><code>true</code> if the file does not exist or it is older than any of the other files.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="input"/> is <code>null</code>.</exception>
        private static bool IsFileUpToDate(string input, params string[] outputs)
        {
            if (String.IsNullOrWhiteSpace(input))
            {
                throw new ArgumentNullException(nameof(input));
            }

            if (!File.Exists(input) || outputs == null || outputs.Length == 0)
            {
                return false;
            }

            long lastWriteTime = File.GetLastWriteTimeUtc(input).Ticks;

            return outputs.All(output => File.Exists(output) && File.GetLastWriteTimeUtc(output).Ticks <= lastWriteTime);
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

                string propsPath = Path.Combine(path, "build", $"{id}.props");
                string targetsPath = Path.Combine(path, "build", $"{id}.targets");

                if (!File.Exists(propsPath) && !File.Exists(targetsPath))
                {
                    return null;
                }

                BuildPackageInfo buildPackageInfo = new BuildPackageInfo
                {
                    Id = parts[0],
                    PropsPath = propsPath,
                    TargetsPath = targetsPath,
                    EnablePropertyName = $"Enable{id.Replace(".", "_")}",
                    RunPropertyName = $"Run{id.Replace(".", "_")}",
                };

                return buildPackageInfo;
            }
        }
    }
}