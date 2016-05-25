using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;

namespace CBT.NuGet
{
    /// <summary>
    /// Represents a base class for NuGet commands.
    /// </summary>
    public abstract class CommandBase : ToolTask
    {
        private CommandVerbosity _commandVerbosity = CommandVerbosity.None;

        protected CommandBase()
        {
            NonInteractive = true;
            LogStandardErrorAsError = true;
            StandardErrorImportance = StandardOutputImportance = MessageImportance.Normal.ToString();
        }

        /// <summary>
        /// Gets or sets the path to the NuGet configuration file. If not specified, file %AppData%\NuGet\NuGet.config is used as configuration file.
        /// </summary>
        public string ConfigFile { get; set; }

        /// <summary>
        /// Get or sets a value indicating if prompts for user input or confirmations should be disabled.
        /// </summary>
        public bool NonInteractive { get; set; }

        /// <summary>
        /// Gets or sets the verbosity of the output.
        /// </summary>
        public string Verbosity { get; set; }

        protected CommandVerbosity CommandVerbosity
        {
            get { return _commandVerbosity; }
        }

        protected override string ToolName
        {
            get { return "NuGet.exe"; }
        }

        public override bool Execute()
        {
            if (Timeout == 0)
            {
                // ToolTask does not treat 0 as infinite
                //
                Timeout = System.Threading.Timeout.Infinite;
            }

            return base.Execute();
        }

        protected override string GenerateCommandLineCommands()
        {
            CommandLineBuilder commandLineBuilder = new CommandLineBuilder();

            GenerateCommandLineCommands(commandLineBuilder);

            commandLineBuilder.AppendSwitchIfNotNull("-ConfigFile ", ConfigFile);

            if (_commandVerbosity != CommandVerbosity.None)
            {
                commandLineBuilder.AppendSwitchIfNotNull("-Verbosity ", _commandVerbosity.ToString());
            }

            if (NonInteractive)
            {
                commandLineBuilder.AppendSwitch("-NonInteractive");
            }

            return commandLineBuilder.ToString();
        }

        protected abstract void GenerateCommandLineCommands(CommandLineBuilder commandLineBuilder);

        protected override string GenerateFullPathToTool()
        {
            return ToolName;
        }

        protected override bool ValidateParameters()
        {
            if (!String.IsNullOrWhiteSpace(Verbosity) && !Enum.TryParse(Verbosity, out _commandVerbosity))
            {
                Log.LogError("Invalid value '{0}' for Verbosity.  Valid values are: '{1}' ", Verbosity, String.Join(", ", Enum.GetNames(typeof (CommandVerbosity))));

                return false;
            }

            return base.ValidateParameters();
        }
    }
}