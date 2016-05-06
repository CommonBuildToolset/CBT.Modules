using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace CBT.NuGet.Tasks
{
    /// <summary>
    /// Adds the given package to a hierarchical source. HTTP sources are not supported.
    /// </summary>
    public sealed class NuGetAdd : CommandBase
    {
        /// <summary>
        /// Gets or sets a value indicating if the package should also expanded.
        /// </summary>
        public bool Expand { get; set; }

        /// <summary>
        /// Gets or sets the package to be added.
        /// </summary>
        [Required]
        public string Package { get; set; }

        /// <summary>
        /// Gets or sets the package source, which is a folder or UNC share, to which the nupkg will be added. Http sources are not supported.
        /// </summary>
        [Required]
        public string Source { get; set; }

        protected override void GenerateCommandLineCommands(CommandLineBuilder commandLineBuilder)
        {
            commandLineBuilder.AppendSwitch("add");

            commandLineBuilder.AppendFileNameIfNotNull(Package);

            commandLineBuilder.AppendSwitchIfNotNull("-Source ", Source);

            commandLineBuilder.AppendSwitchIfTrue("-Expand", Expand);
        }
    }
}