using CBT.NuGet.Internal;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;
using System.Diagnostics;
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
    public class NuGetRestore : DownloadCommandBase
    {
        /// <summary>
        /// Gets or sets additional NuGet restore arguments.
        /// </summary>
        public string AdditionalArguments { get; set; }

        [Required]
        public string File { get; set; }

        /// <summary>
        /// Gets or sets the path of MSBuild to be used with this command. Supported value is a path to msbuild.exe
        /// </summary>
        public string MSBuildPath { get; set; }

        /// <summary>
        /// Gets or sets the version of MSBuild to be used with this command. Supported values are 4, 12, 14. By default the MSBuild in your path is picked, otherwise it defaults to the highest installed version of MSBuild.
        /// </summary>
        public string MsBuildVersion { get; set; }

        /// <summary>
        /// Gets or sets the packages folder.
        /// </summary>
        public string PackagesDirectory { get; set; }

        /// <summary>
        /// Gets or sets timeout in seconds for resolving project to project references.
        /// </summary>
        public string Project2ProjectTimeOut { get; set; }

        /// <summary>
        /// Gets or sets a value indicating if package restore consent is granted before installing a package.
        /// </summary>
        public bool RequireConsent { get; set; }

        /// <summary>
        /// Gets or sets the solution directory. Not valid when restoring packages for a solution.
        /// </summary>
        public string SolutionDirectory { get; set; }

        public static bool IsFileUpToDate(TaskLoggingHelper log, string input, params string[] outputs)
        {
            if (String.IsNullOrWhiteSpace(input))
            {
                throw new ArgumentNullException(nameof(input));
            }

            if (!System.IO.File.Exists(input))
            {
                log.LogMessage(MessageImportance.Low, $"File '{input}' is not up-to-date because it does not exist.");
                return false;
            }
            if (outputs == null || outputs.Length == 0)
            {
                log.LogMessage(MessageImportance.Low, $"File '{input}' is not up-to-date because no outputs were specified.");
                return false;
            }

            DateTime lastWriteTime = System.IO.File.GetLastWriteTimeUtc(input);

            foreach (var output in outputs.Where(i => !String.IsNullOrWhiteSpace(i)))
            {
                if (!System.IO.File.Exists(output))
                {
                    log.LogMessage(MessageImportance.Low, $"File '{input}' is not up-to-date because the output file '{output}' does not exist.");
                    return false;
                }

                var outputLastWriteTime = System.IO.File.GetLastWriteTimeUtc(output);

                if (outputLastWriteTime.Ticks > lastWriteTime.Ticks)
                {
                    log.LogMessage(MessageImportance.Low, $"File '{input}' is not up-to-date because the output file '{output}' is newer ({lastWriteTime:O} > {outputLastWriteTime:O}).");
                    return false;
                }
            }
            return true;
        }

        public override bool Execute()
        {
            // NuGet now evaluates msbuild projects during restore, CBTEnablePackageRestore must be set to prevent NuGet from infinite looping.
            // CBTModulesRestored must be set so on it's evaluation it knows to import the generated module imports so it evaluates the proper full closure of the project.
            EnvironmentVariables = new[]
            {
                "CBTEnablePackageRestore=false",
                "CBTModulesRestored=true"
            };

            // Do restoration in a semaphore to prevent NuGet restore having locking issues
            //
            string semaphoreName = File.ToUpper().GetHashCode().ToString("X");

            using (Semaphore semaphore = new Semaphore(0, 1, semaphoreName, out bool releaseSemaphore))
            {
                try
                {
                    if (!releaseSemaphore)
                    {
                        releaseSemaphore = semaphore.WaitOne(TimeSpan.FromMinutes(30));

                        return releaseSemaphore;
                    }

                    bool ret = base.Execute();

                    if (ret)
                    {
                        ExecutePostRestore();
                    }

                    return ret && !Log.HasLoggedErrors;
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

        public bool Execute(string file, string msBuildVersion, bool requireConsent, bool disableParallelProcessing, string[] fallbackSources, bool noCache, string packageSaveMode, string[] sources, string configFile, bool nonInteractive, string verbosity, int timeout, string toolPath, bool enableOptimization, string markerPath, string[] inputs, string msBuildPath, string additionalArguments)
        {
            if (BuildEngine == null)
            {
                BuildEngine = new CBTBuildEngine();
            }

            Log.LogMessage(MessageImportance.Low, "Restore NuGet Packages:");
            Log.LogMessage(MessageImportance.Low, $"  File = {file}");
            Log.LogMessage(MessageImportance.Low, $"  MSBuildVersion = {msBuildVersion}");
            Log.LogMessage(MessageImportance.Low, $"  MSBuildPath = {msBuildPath}");
            Log.LogMessage(MessageImportance.Low, $"  RequireConsent = {requireConsent}");
            Log.LogMessage(MessageImportance.Low, $"  DisableParallelProcessing = {disableParallelProcessing}");
            Log.LogMessage(MessageImportance.Low, $"  FallbackSources = {String.Join(";", fallbackSources)}");
            Log.LogMessage(MessageImportance.Low, $"  NoCache = {noCache}");
            Log.LogMessage(MessageImportance.Low, $"  PackageSaveMode = {packageSaveMode}");
            Log.LogMessage(MessageImportance.Low, $"  Sources = {String.Join(";", sources)}");
            Log.LogMessage(MessageImportance.Low, $"  ConfigFile = {configFile}");
            Log.LogMessage(MessageImportance.Low, $"  NonInteractive = {nonInteractive}");
            Log.LogMessage(MessageImportance.Low, $"  Verbosity = {verbosity}");
            Log.LogMessage(MessageImportance.Low, $"  Timeout = {timeout}");
            Log.LogMessage(MessageImportance.Low, $"  ToolPath = {toolPath}");
            Log.LogMessage(MessageImportance.Low, $"  EnableOptimization = {enableOptimization}");
            Log.LogMessage(MessageImportance.Low, $"  MarkerPath = {markerPath}");
            Log.LogMessage(MessageImportance.Low, $"  Inputs = {String.Join(";", inputs)}");
            Log.LogMessage(MessageImportance.Low, $"  AdditionalArguments = {additionalArguments}");

            if (enableOptimization && IsFileUpToDate(Log, markerPath, inputs))
            {
                Log.LogMessage(MessageImportance.Low, "NuGet packages are up-to-date");
                return true;
            }

            if (Directory.Exists(file))
            {
                Log.LogMessage(MessageImportance.Low, $"A directory with the name '{file}' exist.  Please consider renaming this directory to avoid breaking nuget convention.");
                return true;
            }
            File = file;
            MsBuildVersion = msBuildVersion;
            MSBuildPath = msBuildPath;
            RequireConsent = RequireConsent;
            DisableParallelProcessing = disableParallelProcessing;
            FallbackSource = fallbackSources.Any() ? fallbackSources.Where(i => !String.IsNullOrWhiteSpace(i)).Select(i => new TaskItem(i)).Cast<ITaskItem>().ToArray() : null;
            NoCache = noCache;
            PackageSaveMode = !String.IsNullOrWhiteSpace(packageSaveMode) ? packageSaveMode : null;
            Source = sources.Any() ? sources.Where(i => !String.IsNullOrWhiteSpace(i)).Select(i => new TaskItem(i)).Cast<ITaskItem>().ToArray() : null;
            ConfigFile = !String.IsNullOrWhiteSpace(configFile) ? configFile : null;
            NonInteractive = nonInteractive;
            Verbosity = verbosity;
            AdditionalArguments = additionalArguments;

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

                if (enableOptimization && !String.IsNullOrWhiteSpace(markerPath))
                {
                    GenerateNuGetOptimizationFile(markerPath);
                }
            }
            catch (Exception e)
            {
                Log.LogErrorFromException(e);
            }

            return ret;
        }

        /// <summary>
        /// Executes any post-restore actions after a successful restore.
        /// </summary>
        protected virtual void ExecutePostRestore()
        {
        }

        protected override void GenerateCommandLineCommands(CommandLineBuilder commandLineBuilder)
        {
            commandLineBuilder.AppendSwitch("restore");

            commandLineBuilder.AppendFileNameIfNotNull(File);

            commandLineBuilder.AppendSwitchIfTrue("-RequireConsent", RequireConsent);

            commandLineBuilder.AppendSwitchIfNotNullOrWhiteSpace("-PackagesDirectory ", PackagesDirectory);

            commandLineBuilder.AppendSwitchIfNotNullOrWhiteSpace("-SolutionDirectory ", SolutionDirectory);

            commandLineBuilder.AppendSwitchIfNotNullOrWhiteSpace("-MSBuildVersion ", MsBuildVersion);

            if (IsNuGetVersion4OrGreater())
            {
                commandLineBuilder.AppendSwitchIfNotNullOrWhiteSpace("-MSBuildPath ", MSBuildPath);
            }

            commandLineBuilder.AppendSwitchIfNotNullOrWhiteSpace("-Project2ProjectTimeOut ", Project2ProjectTimeOut);

            commandLineBuilder.AppendTextUnquoted(AdditionalArguments);

            base.GenerateCommandLineCommands(commandLineBuilder);
        }

        protected void GenerateNuGetOptimizationFile(string markerPath)
        {
            string semaphoreName = markerPath.ToUpper().GetHashCode().ToString("X");

            using (Semaphore semaphore = new Semaphore(0, 1, semaphoreName, out bool releaseSemaphore))
            {
                try
                {
                    if (!releaseSemaphore)
                    {
                        releaseSemaphore = semaphore.WaitOne(TimeSpan.FromMinutes(30));

                        return;
                    }

                    string dir = Path.GetDirectoryName(markerPath);

                    if (!String.IsNullOrWhiteSpace(dir))
                    {
                        Log.LogMessage(MessageImportance.Low, "Creating marker file for NuGet package restore optimization: '{0}'", markerPath);

                        Directory.CreateDirectory(dir);

                        System.IO.File.WriteAllText(markerPath, String.Empty);
                    }
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

        /// <summary>
        /// Determines if the version of NuGet.exe is 4.0 or greater.
        /// </summary>
        /// <returns><code>true</code> if the version of NuGet.exe is 4.0 or greater, otherwise <code>false</code>.</returns>
        private bool IsNuGetVersion4OrGreater()
        {
            try
            {
                if (!System.IO.File.Exists(ToolPath))
                {
                    return false;
                }

                FileVersionInfo fileVersionInfo = FileVersionInfo.GetVersionInfo(ToolPath);

                return fileVersionInfo.ProductMajorPart >= 4;
            }
            catch (Exception e)
            {
                Log.LogWarning($"Failed to determine the version of assembly '{ToolPath}'.  Ensure that path is a valid NuGet.exe.  The error was: {Environment.NewLine}{e}");

                return false;
            }
        }
    }
}