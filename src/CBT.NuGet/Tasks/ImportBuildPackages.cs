using CBT.NuGet.Internal;
using Microsoft.Build.Construction;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;
using System.IO;
using System.Linq;
using System.Threading;

namespace CBT.NuGet.Tasks
{
    /// <summary>
    /// A class for creating imports to NuGet build packages as if they are modules.
    /// </summary>
    public sealed class ImportBuildPackages : Task
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
        /// The path to the .targets file to create.
        /// </summary>
        public string TargetsFile { get; set; }

        public override bool Execute()
        {
            if (File.Exists(TargetsFile))
            {
                return true;
            }

            string semaphoreName = TargetsFile.ToUpper().GetHashCode().ToString("X");

            bool releaseSemaphore;

            using (Semaphore semaphore = new Semaphore(0, 1, semaphoreName, out releaseSemaphore))
            {
                try
                {
                    if (!releaseSemaphore)
                    {
                        releaseSemaphore = semaphore.WaitOne(TimeSpan.FromMinutes(5));

                        return releaseSemaphore;
                    }

                    return GenerateBuildPackageImportFile();
                }
                finally
                {
                    if (releaseSemaphore)
                    {
                        semaphore.Release();
                    }
                }
            }
        }

        public bool Execute(string modulePackagesConfig, string targetsFile, string[] inputs, string[] modulePaths)
        {
            if (File.Exists(TargetsFile) && IsFileUpToDate(targetsFile, inputs))
            {
                return true;
            }

            BuildEngine = new CBTBuildEngine();

            ModulePackagesConfig = modulePackagesConfig;
            TargetsFile = targetsFile;
            ModulePaths = modulePaths;

            return Execute();
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

        private bool GenerateBuildPackageImportFile()
        {
            Log.LogMessage("Creating NuGet build package imports");

            ProjectRootElement targetsProject = ProjectRootElement.Create(TargetsFile);

            foreach (BuildPackageInfo buildPackageInfo in ModulePaths.Select(BuildPackageInfo.FromModulePath).Where(i => i != null))
            {

                if (File.Exists(buildPackageInfo.TargetsPath))
                {
                    // Because PackageId.props is auto imported as a CBT module by cbt.core.dll we need to autoimport PackageId.targets.
                    ProjectImportElement import = targetsProject.AddImport(buildPackageInfo.TargetsPath);
                }
            }

            targetsProject.Save();

            return true;
        }

        private class BuildPackageInfo
        {
            private BuildPackageInfo()
            {
            }

            public string Id { get; private set; }

            public string TargetsPath { get; private set; }

            public static BuildPackageInfo FromModulePath(string modulePath)
            {
                string[] parts = modulePath.Split(new[] { '=' }, 2, StringSplitOptions.RemoveEmptyEntries);

                if (parts.Length != 2 || String.IsNullOrWhiteSpace(parts[0]) || String.IsNullOrWhiteSpace(parts[1]))
                {
                    return null;
                }
                string id = parts[0];
                string path = parts[1];

                string targetsPath = Path.Combine(path, "build", $"{id}.targets");

                if (!File.Exists(targetsPath))
                {
                    return null;
                }

                BuildPackageInfo buildPackageInfo = new BuildPackageInfo
                {
                    Id = parts[0],
                    TargetsPath = targetsPath,
                };

                return buildPackageInfo;
            }
        }
    }
}