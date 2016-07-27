using CBT.NuGet.Internal;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace CBT.NuGet.Tasks
{
    /// <summary>
    /// Deletes a package from the server.
    /// </summary>
    public sealed class NuGetDelete : CommandBase
    {
        /// <summary>
        /// Gets or sets the API key for the server.
        /// </summary>
        public string ApiKey { get; set; }

        /// <summary>
        /// Gets or sets a value indicating if a prompt should be shown before deleting.
        /// </summary>
        public bool NoPrompt { get; set; }

        /// <summary>
        /// Gets or sets the server URL.
        /// </summary>
        public string Source { get; set; }

        /// <summary>
        /// Gets or sets the ID of the package.
        /// </summary>
        [Required]
        public string PackageId { get; set; }

        /// <summary>
        /// Gets or sets the version of the package.
        /// </summary>
        [Required]
        public string PackageVersion { get; set; }

        protected override void GenerateCommandLineCommands(CommandLineBuilder commandLineBuilder)
        {
            commandLineBuilder.AppendSwitch("delete");

            commandLineBuilder.AppendFileNameIfNotNull(PackageId);

            commandLineBuilder.AppendFileNameIfNotNull(PackageVersion);

            commandLineBuilder.AppendSwitchIfNotNullOrWhiteSpace("-Source", Source);

            commandLineBuilder.AppendSwitchIfNotNullOrWhiteSpace("-ApiKey ", ApiKey);

            commandLineBuilder.AppendSwitchIfTrue("-NoPrompt", NoPrompt);
        }
    }
}