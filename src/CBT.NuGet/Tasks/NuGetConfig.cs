using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;
using System.Linq;

namespace CBT.NuGet.Tasks
{
    public sealed class NuGetConfig : CommandBase
    {
        public bool AsPath { get; set; }

        public string Key { get; set; }

        public ITaskItem[] Set { get; set; }

        [Output]
        public string Value { get; set; }

        protected override void GenerateCommandLineCommands(CommandLineBuilder commandLineBuilder)
        {
            commandLineBuilder.AppendSwitch("config");

            commandLineBuilder.AppendFileNameIfNotNull(Key);

            commandLineBuilder.AppendSwitchIfAny("-Set ", Set);

            commandLineBuilder.AppendSwitchIfTrue("-AsPath", AsPath);
        }

        protected override void LogEventsFromTextOutput(string singleLine, MessageImportance messageImportance)
        {
            if ((Set == null || Set.Length == 0) && !String.IsNullOrWhiteSpace(singleLine) && !singleLine.StartsWith("WARNING", StringComparison.OrdinalIgnoreCase))
            {
                Value = singleLine;
            }

            base.LogEventsFromTextOutput(singleLine, messageImportance);
        }

        protected override bool ValidateParameters()
        {
            if ((Set == null || !Set.Any()) && String.IsNullOrWhiteSpace(Key))
            {
                Log.LogError("A value for the \"Set\" or \"Key\" properties must be supplied.");
                return false;
            }

            return base.ValidateParameters();
        }
    }
}