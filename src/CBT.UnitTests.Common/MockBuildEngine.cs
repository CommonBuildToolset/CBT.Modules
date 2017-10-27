using Microsoft.Build.Framework;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace CBT.UnitTests.Common
{
    public sealed class MockBuildEngine : IBuildEngine
    {
        // ReSharper disable once CollectionNeverQueried.Local
        private readonly ConcurrentBag<BuildEventArgs> _events = new ConcurrentBag<BuildEventArgs>();

        private int _customEventsCount;
        private int _errorsCount;
        private int _messagesCount;
        private int _warningsCount;
        public int ColumnNumberOfTaskNode => 0;

        public bool ContinueOnError => false;

        public int ErrorCount => _errorsCount;
        public IEnumerable<BuildErrorEventArgs> Errors => _events.OfType<BuildErrorEventArgs>();

        public int LineNumberOfTaskNode => 0;

        public IReadOnlyCollection<BuildEventArgs> LoggedEvents => _events.ToList().AsReadOnly();

        public string ProjectFileOfTaskNode => null;

        public IEnumerable<BuildWarningEventArgs> Warnings => _events.OfType<BuildWarningEventArgs>();

        public int WarningsCount => _warningsCount;

        public bool BuildProjectFile(string projectFileName, string[] targetNames, IDictionary globalProperties, IDictionary targetOutputs)
        {
            throw new NotSupportedException();
        }

        public void LogCustomEvent(CustomBuildEventArgs e)
        {
            _events.Add(e);
            Interlocked.Increment(ref _customEventsCount);
        }

        public void LogErrorEvent(BuildErrorEventArgs e)
        {
            _events.Add(e);

            Interlocked.Increment(ref _errorsCount);
        }

        public void LogMessageEvent(BuildMessageEventArgs e)
        {
            _events.Add(e);

            Interlocked.Increment(ref _messagesCount);
        }

        public void LogWarningEvent(BuildWarningEventArgs e)
        {
            _events.Add(e);

            Interlocked.Increment(ref _warningsCount);
        }
    }
}