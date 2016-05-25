using CBT.NuGet.Internal;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;
using System.IO;
using System.Linq;
using System.Threading;

namespace CBT.NuGet.Tasks
{
    /// <summary>
    /// Restores NuGet packages.
    ///
    /// If a solution is specified, this command restores NuGet packages that are installed in the solution and in projects contained in the solution. Otherwise, the command restores packages listed in the specified packages.config file, Microsoft Build project, or project.json file.
    /// </summary>
    public sealed class NuGetRestore : DownloadCommandBase
    {
        [Required]
        public string File { get; set; }

        /// <summary>
        /// Gets or sets the version of MSBuild to be used with this command. Supported values are 4, 12, 14. By default the MSBuild in your path is picked, otherwise it defaults to the highest installed version of MSBuild.
        /// </summary>
        public string MsBuildVersion { get; set; }

        /// <summary>
        /// Gets or sets the packages folder.
        /// </summary>
        public string PackagesDirectory { get; set; }

        /// <summary>
        /// Gets or sets a value indicating if package restore consent is granted before installing a package.
        /// </summary>
        public bool RequireConsent { get; set; }

        /// <summary>
        /// Gets or sets the solution directory. Not valid when restoring packages for a solution.
        /// </summary>
        public string SolutionDirectory { get; set; }

        public override bool Execute()
        {
            string mutextName = PackagesDirectory.ToUpper().GetHashCode().ToString("X");

            using (Mutex mutex = new Mutex(false, mutextName))
            {
                if (!mutex.WaitOne(TimeSpan.FromMinutes(30)))
                {
                    return false;
                }

                try
                {
                    return base.Execute();
                }
                finally
                {
                    mutex.ReleaseMutex();
                }
            }
        }

        public bool Execute(string file, string msBuildVersion, string packagesDirectory, bool requireConsent, string solutionDirectory, bool disableParallelProcessing, string[] fallbackSources, bool noCache, string packageSaveMode, string[] sources, string configFile, bool nonInteractive, string verbosity, int timeout, string toolPath, bool disableOptimization, string markerPath, string[] inputs)
        {
            BuildEngine = new CBTBuildEngine();

            if (!disableOptimization && IsFileUpToDate(markerPath, inputs))
            {
                Log.LogMessage(MessageImportance.Low, "NuGet packages are up-to-date");

                return true;
            }

            File = file;
            MsBuildVersion = msBuildVersion;
            PackagesDirectory = packagesDirectory;
            RequireConsent = RequireConsent;
            SolutionDirectory = !String.IsNullOrWhiteSpace(solutionDirectory) ? solutionDirectory : null;
            DisableParallelProcessing = disableParallelProcessing;
            FallbackSource = fallbackSources.Any() ? fallbackSources.Where(i => !String.IsNullOrWhiteSpace(i)).Select(i => new TaskItem(i)).Cast<ITaskItem>().ToArray() : null;
            NoCache = noCache;
            PackageSaveMode = !String.IsNullOrWhiteSpace(packageSaveMode) ? packageSaveMode : null;
            Source = sources.Any() ? sources.Where(i => !String.IsNullOrWhiteSpace(i)).Select(i => new TaskItem(i)).Cast<ITaskItem>().ToArray() : null;
            ConfigFile = !String.IsNullOrWhiteSpace(configFile) ? configFile : null;
            NonInteractive = nonInteractive;
            Verbosity = verbosity;

            if (timeout > 0)
            {
                Timeout = timeout;
            }

            if (!String.IsNullOrEmpty(toolPath))
            {
                ToolPath = toolPath;
            }

            bool ret = false;

            try
            {
                ret = Execute();

                if (!disableOptimization)
                {
                    Log.LogMessage(MessageImportance.Low, "Creating marker file for NuGet package restore optimization: '{0}'", markerPath);

                    string dir = Path.GetDirectoryName(markerPath);

                    using (Mutex mutex = new Mutex(false, markerPath.ToUpper().GetHashCode().ToString("X")))
                    {
                        if (!mutex.WaitOne(TimeSpan.FromMinutes(30)))
                        {
                            return false;
                        }

                        if (!System.IO.File.Exists(markerPath))
                        {
                            if (!String.IsNullOrWhiteSpace(dir) && !Directory.Exists(dir))
                            {
                                Directory.CreateDirectory(dir);
                            }

                            System.IO.File.WriteAllText(markerPath, String.Empty);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Log.LogErrorFromException(e);
            }

            return ret;
        }

        public bool GenerateNuGetProperties(string file, string[] inputs, string propsFile, string propertyNamePrefix, string propertyValuePrefix)
        {
            BuildEngine = new CBTBuildEngine();

            if (IsFileUpToDate(propsFile, inputs))
            {
                return true;
            }

            NuGetPropertyGenerator nuGetPropertyGenerator = new NuGetPropertyGenerator(file);

            Log.LogMessage(MessageImportance.Low, "Generating MSBuild property file '{0}' for NuGet packages", propsFile);

            nuGetPropertyGenerator.Generate(propsFile, propertyNamePrefix, propertyValuePrefix);

            return true;
        }

        protected override void GenerateCommandLineCommands(CommandLineBuilder commandLineBuilder)
        {
            commandLineBuilder.AppendSwitch("restore");

            commandLineBuilder.AppendFileNameIfNotNull(File);

            commandLineBuilder.AppendSwitchIfTrue("-RequireConsent", RequireConsent);

            commandLineBuilder.AppendSwitchIfNotNullOrWhiteSpace("-PackagesDirectory ", PackagesDirectory);

            commandLineBuilder.AppendSwitchIfNotNullOrWhiteSpace("-SolutionDirectory ", SolutionDirectory);

            commandLineBuilder.AppendSwitchIfNotNullOrWhiteSpace("-MSBuildVersion ", MsBuildVersion);

            base.GenerateCommandLineCommands(commandLineBuilder);
        }

        private static bool IsFileUpToDate(string output, params string[] inputs)
        {
            if (String.IsNullOrWhiteSpace(output))
            {
                throw new ArgumentNullException("output");
            }

            if (!System.IO.File.Exists(output) || inputs == null || inputs.Length == 0)
            {
                return false;
            }

            long lastWriteTime = System.IO.File.GetLastWriteTimeUtc(output).Ticks;

            return inputs.Where(i => !String.IsNullOrWhiteSpace(i)).All(i => System.IO.File.Exists(i) && System.IO.File.GetLastWriteTimeUtc(i).Ticks <= lastWriteTime);
        }
    }
}