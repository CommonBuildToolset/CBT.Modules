using CBT.NuGet.Internal;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace CBT.NuGet.Tasks
{
    /// <summary>
    /// Generate NuGet properties.
    ///
    /// Generate properties that contain the path and version of a given nuget package.
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
            BuildEngine = new CBTBuildEngine();

            if (NuGetRestore.IsFileUpToDate(PropsFile, Inputs))
            {
                return true;
            }

            NuGetPropertyGenerator nuGetPropertyGenerator = new NuGetPropertyGenerator(PackageRestoreFile);

            Log.LogMessage(MessageImportance.Low, "Generating MSBuild property file '{0}' for NuGet packages", PropsFile);

            nuGetPropertyGenerator.Generate(PropsFile, PropertyVersionNamePrefix, PropertyPathNamePrefix, PropertyPathValuePrefix);

            return true;
        }

        public bool Execute(string packageRestoreFile, string[] inputs, string propsFile, string propertyVersionNamePrefix, string propertyPathNamePrefix, string propertyPathValuePrefix)
        {

            PackageRestoreFile = packageRestoreFile;
            Inputs = inputs;
            PropsFile = propsFile;
            PropertyVersionNamePrefix = propertyVersionNamePrefix;
            PropertyPathNamePrefix = propertyPathNamePrefix;
            PropertyPathValuePrefix = propertyPathValuePrefix;

            return Execute();
        }

    }
}
