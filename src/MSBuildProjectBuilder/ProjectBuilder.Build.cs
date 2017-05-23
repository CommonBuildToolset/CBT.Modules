using Microsoft.Build.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.MSBuildProjectBuilder
{
    public partial class ProjectBuilder
    {
        /// <summary>
        /// Stores the logged events from the most recent build.
        /// </summary>
        private TestLogger _buildLog;

        /// <summary>
        /// Keeps track of whether or not a build is in progress.
        /// </summary>
        private bool _isBuildInProgress;

        /// <summary>
        /// Gets the result of the last build.  If no build was performed, then <code>null</code> is returned.
        /// </summary>
        public bool? LastBuildResult { get; private set; }

        /// <summary>
        /// Gets the errors logged during the last build.
        /// </summary>
        public IEnumerable<BuildErrorEventArgs> LoggedErrors => _buildLog?.Events.Where(i => i is BuildErrorEventArgs).Cast<BuildErrorEventArgs>();

        /// <summary>
        /// Gets all events logged during the last build.
        /// </summary>
        public IReadOnlyCollection<BuildEventArgs> LoggedEvents => _buildLog?.Events;

        /// <summary>
        /// Gets all messages logged during the last build.
        /// </summary>
        public IEnumerable<BuildMessageEventArgs> LoggedMessages => _buildLog?.Events.Where(i => i is BuildMessageEventArgs).Cast<BuildMessageEventArgs>();

        /// <summary>
        /// Gets the text of all messages logged during the last build.
        /// </summary>
        public IEnumerable<string> LoggedMessageText => LoggedMessages.Select(i => i.Message);

        /// <summary>
        /// Gets all warnings logged during the last build.
        /// </summary>
        public IEnumerable<BuildWarningEventArgs> LoggedWarnings => _buildLog?.Events.Where(i => i is BuildWarningEventArgs).Cast<BuildWarningEventArgs>();

        /// <summary>
        /// Builds the current project.
        /// </summary>
        /// <param name="targets">An optional list of targets to build.</param>
        /// <returns>The current <see cref="ProjectBuilder"/> object.</returns>
        public ProjectBuilder Build(params string[] targets)
        {
            if (_isBuildInProgress)
            {
                throw new NotSupportedException("A build has already been started and only one build can be running at a time");
            }

            _isBuildInProgress = true;

            _buildLog = new TestLogger();

            LastBuildResult = Project.Build(targets, new[] {_buildLog});

            _isBuildInProgress = false;

            return this;
        }

        /// <summary>
        /// An implementation of <see cref="ILogger"/> which stores logged events during a build.
        /// </summary>
        private class TestLogger : ILogger
        {
            // Stores all logged events
            private readonly List<BuildEventArgs> _events = new List<BuildEventArgs>();

            /// <summary>
            /// Gets the logged events.
            /// </summary>
            public IReadOnlyCollection<BuildEventArgs> Events => _events.AsReadOnly();

            /// <inheritdoc />
            public string Parameters { get; set; }

            /// <inheritdoc />
            public LoggerVerbosity Verbosity { get; set; }

            /// <inheritdoc />
            public void Initialize(IEventSource eventSource)
            {
                eventSource.AnyEventRaised += (sender, args) => _events.Add(args);
            }

            /// <inheritdoc />
            public void Shutdown()
            {
            }
        }
    }
}