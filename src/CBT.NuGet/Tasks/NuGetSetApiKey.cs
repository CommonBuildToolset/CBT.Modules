using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace CBT.NuGet.Tasks
{
    public sealed class NuGetSetApiKey : CommandBase
    {
        [Required]
        public string ApiKey { get; set; }

        public string Source { get; set; }

        protected override void GenerateCommandLineCommands(CommandLineBuilder commandLineBuilder)
        {
            commandLineBuilder.AppendSwitch("setApiKey");

            commandLineBuilder.AppendFileNameIfNotNull(ApiKey);

            commandLineBuilder.AppendSwitchIfNotNull("-Source ", Source);
        }
    }
}