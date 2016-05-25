using CBT.NuGet.Internal;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System.Text.RegularExpressions;

namespace CBT.NuGet.Tasks
{
    public sealed class NuGetPack : CommandBase
    {
        private static readonly Regex PackageCreatedRegex = new Regex(@"^Successfully created package '(?<package>.+)'\.$");

        public string BasePath { get; set; }

        public bool Build { get; set; }

        public ITaskItem[] Exclude { get; set; }

        public bool ExcludeEmptyDirectories { get; set; }

        [Required]
        public ITaskItem File { get; set; }

        public bool IncludeReferencedProjects { get; set; }

        public string MinClientVersion { get; set; }

        public string MsBuildVersion { get; set; }

        public bool NoDefaultExcludes { get; set; }

        public bool NoPackageAnalysis { get; set; }

        public string OutputDirectory { get; set; }

        [Output]
        public ITaskItem Package { get; set; }

        public ITaskItem[] Properties { get; set; }

        public bool Symbols { get; set; }

        public bool Tool { get; set; }

        public bool Verbose { get; set; }

        public string Version { get; set; }

        protected override void GenerateCommandLineCommands(CommandLineBuilder commandLineBuilder)
        {
            commandLineBuilder.AppendSwitch("pack");

            commandLineBuilder.AppendFileNameIfNotNull(File);

            commandLineBuilder.AppendSwitchIfNotNullOrWhiteSpace("-OutputDirectory ", OutputDirectory);

            commandLineBuilder.AppendSwitchIfNotNullOrWhiteSpace("-BasePath ", BasePath);

            commandLineBuilder.AppendSwitchIfTrue("-Verbose", Verbose);

            commandLineBuilder.AppendSwitchIfNotNullOrWhiteSpace("-Version ", Version);

            commandLineBuilder.AppendSwitchIfAny("-Exclude ", Exclude);

            commandLineBuilder.AppendSwitchIfTrue("-Symbols", Symbols);

            commandLineBuilder.AppendSwitchIfTrue("-Tool", Tool);

            commandLineBuilder.AppendSwitchIfTrue("-Build", Build);

            commandLineBuilder.AppendSwitchIfTrue("-NoDefaultExcludes", NoDefaultExcludes);

            commandLineBuilder.AppendSwitchIfTrue("-NoPackageAnalysis", NoPackageAnalysis);

            commandLineBuilder.AppendSwitchIfTrue("-ExcludeEmptyDirectories", ExcludeEmptyDirectories);

            commandLineBuilder.AppendSwitchIfTrue("-IncludeReferencedProjects", IncludeReferencedProjects);

            commandLineBuilder.AppendSwitchIfAny("-Properties ", Properties);

            commandLineBuilder.AppendSwitchIfNotNullOrWhiteSpace("-MinClientVersion ", MinClientVersion);

            commandLineBuilder.AppendSwitchIfNotNullOrWhiteSpace("-MSBuildVersion ", MsBuildVersion);
        }

        protected override void LogEventsFromTextOutput(string singleLine, MessageImportance messageImportance)
        {
            var match = PackageCreatedRegex.Match(singleLine);

            if (match.Success)
            {
                Package = new TaskItem(match.Groups["package"].Value);
            }

            base.LogEventsFromTextOutput(singleLine, messageImportance);
        }
    }
}