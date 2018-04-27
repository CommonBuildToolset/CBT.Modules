using CBT.NuGet.Internal;
using Microsoft.Build.Framework;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace CBT.NuGet.Tasks
{
    /// <summary>
    /// Generate NuGet properties.
    ///
    /// Generate properties that contain the path and version of a given NuGet package.
    /// </summary>
    public sealed class GenerateNuGetProperties : SemaphoreTask
    {
        /// <summary>
        /// Stores a list of assembly search paths where dependencies should be searched for.
        /// </summary>
        private readonly ICollection<string> _assemblySearchPaths = new List<string>();

        /// <summary>
        /// Stores a list of loaded assemblies in the event that the same assembly is requested multiple times.
        /// </summary>
        private readonly IDictionary<AssemblyName, Assembly> _loadedAssemblies = new Dictionary<AssemblyName, Assembly>();

        private readonly CBTTaskLogHelper _log;

        public GenerateNuGetProperties()
        {
            _log = new CBTTaskLogHelper(this);

            string executingAssemblyLocation = Assembly.GetExecutingAssembly().Location;

            if (!String.IsNullOrWhiteSpace(executingAssemblyLocation))
            {
                // When loading an assembly from a byte[], the Assembly.Location is not set so it shouldn't be considered
                //
                _assemblySearchPaths.Add(Path.GetDirectoryName(executingAssemblyLocation));
            }

            if (AppDomain.CurrentDomain.GetData("CBT_NUGET_ASSEMBLY_PATH") != null)
            {
                // CBT.NuGet.props currently sets this value so we can determine where CBT.NuGet.dll is since its loaded as a byte[]
                //
                _assemblySearchPaths.Add(Path.GetDirectoryName(AppDomain.CurrentDomain.GetData("CBT_NUGET_ASSEMBLY_PATH").ToString()));
            }
        }

        /// <summary>
        /// Gets or sets the list of file inputs that should be considered for incrementality if the PropsFile exist and is older then the inputs this is considered a no-op.
        /// </summary>
        [Required]
        public string[] Inputs { get; set; }

        /// <summary>
        /// Gets or sets the NuGetPackagesPath.
        /// </summary>
        public string NuGetPackagesPath { get; set; }

        /// <summary>
        /// Gets or sets the packages.config or project.json file to be parsed by this command.
        /// </summary>
        [Required]
        public string PackageRestoreFile { get; set; }

        /// <summary>
        /// Gets or sets the property name prefix for the path variables. Properties are formed as ($(PropertyPathNamePrefix_)+PackageID.Replace('.','_')).
        /// </summary>
        [Required]
        public string PropertyPathNamePrefix { get; set; }

        /// <summary>
        /// Gets or sets the property name prefix for the version variables. Properties are formed as ($(PropertyVersionNamePrefix_)+PackageID.Replace('.','_')).
        /// </summary>
        [Required]
        public string PropertyVersionNamePrefix { get; set; }

        /// <summary>
        /// Gets or sets the full path of the props file that is written.
        /// </summary>
        [Required]
        public string PropsFile { get; set; }

        /// <summary>
        /// Gets or sets the assets file from the NuGet restore.
        /// </summary>
        public string RestoreInfoFile { get; set; }

        protected override string SemaphoreName => PropsFile;

        public bool Execute(string packageRestoreFile, string[] inputs, string propsFile, string propertyVersionNamePrefix, string propertyPathNamePrefix, string restoreInfoFile = "")
        {
            try
            {
                BuildEngine = new CBTBuildEngine();
                Log.LogMessage(MessageImportance.Low, "Generate NuGet packages properties:");
                Log.LogMessage(MessageImportance.Low, $"  Inputs = {String.Join(";", inputs)}");
                Log.LogMessage(MessageImportance.Low, $"  PackageRestoreFile = {packageRestoreFile}");
                Log.LogMessage(MessageImportance.Low, $"  PropertyPathNamePrefix = {propertyPathNamePrefix}");
                Log.LogMessage(MessageImportance.Low, $"  PropertyVersionNamePrefix = {propertyVersionNamePrefix}");
                Log.LogMessage(MessageImportance.Low, $"  PropsFile = {propsFile}");
                Log.LogMessage(MessageImportance.Low, $"  RestoreInfoFile = {restoreInfoFile}");

                if (NuGetRestore.IsFileUpToDate(Log, propsFile, inputs))
                {
                    Log.LogMessage(MessageImportance.Low, $"NuGet package properties file '{propsFile}' is up-to-date");
                    return true;
                }

                if (Directory.Exists(packageRestoreFile))
                {
                    Log.LogMessage(MessageImportance.Low, $"A directory with the name '{packageRestoreFile}' exist.  Please consider renaming this directory to avoid breaking NuGet convention.");
                    return true;
                }

                PackageRestoreFile = packageRestoreFile;
                Inputs = inputs;
                PropsFile = propsFile;
                PropertyVersionNamePrefix = propertyVersionNamePrefix;
                PropertyPathNamePrefix = propertyPathNamePrefix;
                RestoreInfoFile = restoreInfoFile;

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
            Log.LogMessage(MessageImportance.Low, "Generating MSBuild property file '{0}' for NuGet packages", PropsFile);

            NuGetPropertyGenerator nuGetPropertyGenerator = new NuGetPropertyGenerator(_log, PackageRestoreFile);

            nuGetPropertyGenerator.Generate(PropsFile, PropertyVersionNamePrefix, PropertyPathNamePrefix, GetPackageRestoreData());
        }

        /// <summary>
        /// Reads package restore data generated by the GenerateModuleAssetFlagFile target located in cbt\build.props
        /// </summary>
        /// <returns> a PackageRestoreData object based off the contents of the flag file.</returns>
        internal PackageRestoreData GetPackageRestoreData()
        {
            if (NuGetPackagesConfigParser.IsPackagesConfigFile(PackageRestoreFile))
            {
                return new PackageRestoreData
                {
                    // At the moment, NuGet sets the restore style to Unknown if its not PackageReference or project.json
                    RestoreProjectStyle = "Unknown"
                };
            }

            if (String.IsNullOrWhiteSpace(RestoreInfoFile) || !File.Exists(RestoreInfoFile))
            {
                Log.LogMessage(MessageImportance.Low, $"Package reference {RestoreInfoFile} not found.  Either you are not using PackageReference elements for your packages or you are using a version of nuget.exe prior to 4.x, or the project does not import CBT in some way.");
                return null;
            }

            return JsonConvert.DeserializeObject<PackageRestoreData>(File.ReadAllText(RestoreInfoFile));
        }
    }
}