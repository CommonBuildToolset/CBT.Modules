using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace CBT.NuGet
{
    public abstract class DownloadCommandBase : CommandBase
    {
        public bool DisableParallelProcessing { get; set; }

        public ITaskItem[] FallbackSource { get; set; }

        public bool NoCache { get; set; }

        public string PackageSaveMode { get; set; }

        public ITaskItem[] Source { get; set; }

        protected override void GenerateCommandLineCommands(CommandLineBuilder commandLineBuilder)
        {
            if (Source != null)
            {
                foreach (var item in Source)
                {
                    commandLineBuilder.AppendSwitchIfNotNull("-Source ", item.ItemSpec);
                }
            }

            if (FallbackSource != null)
            {
                foreach (var item in FallbackSource)
                {
                    commandLineBuilder.AppendSwitchIfNotNull("-FallbackSource ", item.ItemSpec);
                }
            }

            if (NoCache)
            {
                commandLineBuilder.AppendSwitch("-NoCache");
            }

            if (DisableParallelProcessing)
            {
                commandLineBuilder.AppendSwitch("-DisableParallelProcessing");
            }

            commandLineBuilder.AppendSwitchIfNotNull("-PackageSaveMode ", PackageSaveMode);
        }
    }
}