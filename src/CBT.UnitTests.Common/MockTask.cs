using System;
using Microsoft.Build.Framework;

namespace CBT.UnitTests.Common
{
    public sealed class MockTask : ITask
    {
        public IBuildEngine BuildEngine { get; set; }

        public ITaskHost HostObject { get; set; }

        public bool Execute()
        {
            throw new NotSupportedException();
        }
    }
}