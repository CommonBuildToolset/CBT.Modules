using CBT.NuGet.Internal;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Newtonsoft.Json;

namespace CBT.NuGet.Tasks
{
    /// <summary>
    /// Generate NuGet properties.
    ///
    /// Generate properties that contain the path and version of a given NuGet package.
    /// </summary>
    public sealed class GenerateNuGetProperties : Task
    {
        /// <summary>
        /// Gets or sets the packages.config or project.json file to be parsed by this command.
        /// </summary>
        [Required]
        public string PackageRestoreFile { get; set; }

        /// <summary>
        /// Gets or sets the list of file inputs that should be considered for incrementality if the PropsFile exist and is older then the inputs this is considered a no-op.
        /// </summary>
        [Required]
        public string[] Inputs { get; set; }

        /// <summary>
        /// Gets or sets the full path of the props file that is written.
        /// </summary>
        [Required]
        public string PropsFile { get; set; }

        /// <summary>
        /// Gets or sets the property name prefix for the version variables. Properties are formed as ($(PropertyVersionNamePrefix_)+PackageID.Replace('.','_')).
        /// </summary>
        [Required]
        public string PropertyVersionNamePrefix { get; set; }

        /// <summary>
        /// Gets or sets the property name prefix for the path variables. Properties are formed as ($(PropertyPathNamePrefix_)+PackageID.Replace('.','_')).
        /// </summary>
        [Required]
        public string PropertyPathNamePrefix { get; set; }

        /// <summary>
        /// Gets or sets the property value prefix for the path variables. Property values are formed as ($(PropertyPathValuePrefix_)+PackageInstallRelativePath).
        /// </summary>
        [Required]
        public string PropertyPathValuePrefix { get; set; }

        /// <summary>
        /// Gets or sets the NuGetPackagesPath.
        /// </summary>
        public string NuGetPackagesPath { get; set; }

        /// <summary>
        /// Gets or sets the assets file from the nuget restore.
        /// </summary>
        public string AssetsFile { get; set; }

        private readonly CBTTaskLogHelper _log;


        public GenerateNuGetProperties()
        {
            _log = new CBTTaskLogHelper(this);
        }

        public override bool Execute()
        {
            // Tell appdomain to load nuget references and newtonsoft.json from current directory.
            UpdateAssemblySearchPaths();
            Log.LogMessage(MessageImportance.Low, "Generating MSBuild property file '{0}' for NuGet packages", PropsFile);
            NuGetPropertyGenerator nuGetPropertyGenerator = new NuGetPropertyGenerator(_log, PackageRestoreFile);
            nuGetPropertyGenerator.Generate(PropsFile, PropertyVersionNamePrefix, PropertyPathNamePrefix, PropertyPathValuePrefix, NuGetPackagesPath, GetPackageRestoreData());
            return true;
        }

        public bool Execute(string packageRestoreFile, string[] inputs, string propsFile, string propertyVersionNamePrefix, string propertyPathNamePrefix, string propertyPathValuePrefix, string nuGetPackagesPath, string assetsFile = "")
        {
            try
            {
                BuildEngine = new CBTBuildEngine();
                Log.LogMessage(MessageImportance.Low, "Generate NuGet packages properties:");
                Log.LogMessage(MessageImportance.Low, $"  PackageRestoreFile = {packageRestoreFile}");
                Log.LogMessage(MessageImportance.Low, $"  Inputs = {String.Join(";", inputs)}");
                Log.LogMessage(MessageImportance.Low, $"  PropsFile = {propsFile}");
                Log.LogMessage(MessageImportance.Low, $"  PropertyVersionNamePrefix = {propertyVersionNamePrefix}");
                Log.LogMessage(MessageImportance.Low, $"  PropertyPathNamePrefix = {propertyPathNamePrefix}");
                Log.LogMessage(MessageImportance.Low, $"  PropertyPathValuePrefix = {propertyPathValuePrefix}");
                Log.LogMessage(MessageImportance.Low, $"  NuGetPackagesPath = {nuGetPackagesPath}");
                Log.LogMessage(MessageImportance.Low, $"  AssetsFile = {assetsFile}");

                if (NuGetRestore.IsFileUpToDate(Log, propsFile, inputs))
                {
                    Log.LogMessage(MessageImportance.Low, $"NuGet package properties file '{propsFile}' is up-to-date");
                    return true;
                }

                PackageRestoreFile = packageRestoreFile;
                Inputs = inputs;
                PropsFile = propsFile;
                PropertyVersionNamePrefix = propertyVersionNamePrefix;
                PropertyPathNamePrefix = propertyPathNamePrefix;
                PropertyPathValuePrefix = propertyPathValuePrefix;
                AssetsFile = assetsFile;
                NuGetPackagesPath = nuGetPackagesPath;

                return Execute();
            }
            catch (Exception e)
            {
                Log.LogError(e.ToString());
                Trace.TraceError(e.ToString());
                return false;
            }
        }

        /// <summary>
        /// Reads package restore data generated by the GenerateModuleAssetFlagFile target located in cbt\build.props
        /// </summary>
        /// <returns> a PackageRestoreData object based off the contents of the flag file.</returns>
        internal PackageRestoreData GetPackageRestoreData()
        {
            if (string.IsNullOrWhiteSpace(AssetsFile) || !File.Exists(AssetsFile))
            {
                Log.LogMessage(MessageImportance.Low, $"Package reference {AssetsFile} not found.  Either you are not using PackageReference elements for your packages or you are using a version of nuget.exe prior to 4.x, or the project does not import CBT in some way.");
                return null;
            }
            return JsonConvert.DeserializeObject<PackageRestoreData>(File.ReadAllText(AssetsFile));
        }

        private void UpdateAssemblySearchPaths()
        {
            AppDomain.CurrentDomain.AssemblyResolve += LoadFromSameFolder;
        }
        static Assembly LoadFromSameFolder(object sender, ResolveEventArgs args)
        {
            string folderPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string assemblyPath = Path.Combine(folderPath, new AssemblyName(args.Name).Name + ".dll");
            if (!File.Exists(assemblyPath))
            {
                return null;
            }

            Assembly assembly = Assembly.LoadFrom(assemblyPath);
            return assembly;
        }
    }
}
