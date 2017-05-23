using Microsoft.Build.Framework;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace CBT.NuGet.UnitTests
{
    public class TestBuildEngine : IBuildEngine
    {
        private readonly ConcurrentBag<BuildEventArgs> _events = new ConcurrentBag<BuildEventArgs>();

        public int ColumnNumberOfTaskNode => 0;

        public bool ContinueOnError => false;

        public int LineNumberOfTaskNode => 0;

        public IReadOnlyCollection<BuildEventArgs> LoggedEvents => _events.ToList().AsReadOnly();

        public string ProjectFileOfTaskNode => null;

        public bool BuildProjectFile(string projectFileName, string[] targetNames, IDictionary globalProperties, IDictionary targetOutputs)
        {
            throw new NotSupportedException();
        }

        public void LogCustomEvent(CustomBuildEventArgs e) => _events.Add(e);

        public void LogErrorEvent(BuildErrorEventArgs e) => _events.Add(e);

        public void LogMessageEvent(BuildMessageEventArgs e) => _events.Add(e);

        public void LogWarningEvent(BuildWarningEventArgs e) => _events.Add(e);
    }
}