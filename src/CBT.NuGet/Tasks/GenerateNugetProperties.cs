using CBT.NuGet.Internal;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;
using System.Diagnostics;

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


        public override bool Execute()
        {
            Log.LogMessage(MessageImportance.Low, "Generating MSBuild property file '{0}' for NuGet packages", PropsFile);

            NuGetPropertyGenerator nuGetPropertyGenerator = new NuGetPropertyGenerator(PackageRestoreFile);

            nuGetPropertyGenerator.Generate(PropsFile, PropertyVersionNamePrefix, PropertyPathNamePrefix, PropertyPathValuePrefix);

            return true;
        }

        public bool Execute(string packageRestoreFile, string[] inputs, string propsFile, string propertyVersionNamePrefix, string propertyPathNamePrefix, string propertyPathValuePrefix)
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

                return Execute();
            }
            catch (Exception e)
            {
                Trace.TraceError(e.ToString());
                return false;
            }
        }

    }
}
