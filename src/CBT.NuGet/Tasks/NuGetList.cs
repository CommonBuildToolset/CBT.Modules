using CBT.NuGet.Internal;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace CBT.NuGet.Tasks
{
    public sealed class NuGetList : CommandBase
    {
        private static readonly Regex MultiLineNuGetPackageIdRegex = new Regex(@"^(?<id>[^\s]+)$");
        private static readonly Regex MultiLineNuGetPackageVersionRegex = new Regex(@"^\s(?<version>\d*\.\d*\..+)$");
        private static readonly Regex SingleLineNuGetPackageRegex = new Regex(@"^(?<id>[^\s]+)\s(?<version>\d*\.\d*\..+)$");
        private readonly IList<ITaskItem> _packages = new List<ITaskItem>();

        private ITaskItem _lastTaskItem;

        public bool AllVersions { get; set; }

        public bool IncludeDelisted { get; set; }

        [Output]
        public ITaskItem[] Packages
        {
            get { return _packages.ToArray(); }
        }

        public bool Prerelease { get; set; }

        public string SearchTerms { get; set; }

        public ITaskItem[] Source { get; set; }

        protected override void GenerateCommandLineCommands(CommandLineBuilder commandLineBuilder)
        {
            commandLineBuilder.AppendSwitch("list");

            commandLineBuilder.AppendFileNameIfNotNull(SearchTerms);

            commandLineBuilder.AppendSwitchIfAny("-Source ", Source);

            commandLineBuilder.AppendSwitchIfTrue("-AllVersions", AllVersions);

            commandLineBuilder.AppendSwitchIfTrue("-Prerelease", Prerelease);

            commandLineBuilder.AppendSwitchIfTrue("-IncludeDelisted", IncludeDelisted);
        }

        protected override void LogEventsFromTextOutput(string singleLine, MessageImportance messageImportance)
        {
            if (CommandVerbosity != CommandVerbosity.Detailed)
            {
                var match = SingleLineNuGetPackageRegex.Match(singleLine);

                if (match.Success)
                {
                    _packages.Add(new TaskItem(String.Format("{0}.{1}", match.Groups["id"].Value, match.Groups["version"].Value), new Dictionary<string, string>
                    {
                        {"Id", match.Groups["id"].Value},
                        {"Version", match.Groups["version"].Value},
                    }));
                }
            }
            else
            {
                if (_lastTaskItem == null)
                {
                    var match = MultiLineNuGetPackageIdRegex.Match(singleLine);

                    if (match.Success)
                    {
                        _lastTaskItem = new TaskItem(match.Groups["id"].Value, new Dictionary<string, string>
                        {
                            {"Id", match.Groups["id"].Value}
                        });
                    }
                }
                else
                {
                    if (String.Empty.Equals(singleLine))
                    {
                        _packages.Add(_lastTaskItem);

                        _lastTaskItem = null;
                    }
                    else
                    {
                        var match = MultiLineNuGetPackageVersionRegex.Match(singleLine);

                        if (match.Success)
                        {
                            _lastTaskItem.ItemSpec = String.Format("{0}.{1}", _lastTaskItem.ItemSpec, match.Groups["version"].Value);
                            _lastTaskItem.SetMetadata("Version", match.Groups["version"].Value);
                        }
                        else
                        {
                            _lastTaskItem.SetMetadata("Description", singleLine.Trim());
                        }
                    }
                }
            }

            base.LogEventsFromTextOutput(singleLine, messageImportance);
        }
    }
}