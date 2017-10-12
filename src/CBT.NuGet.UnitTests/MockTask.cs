using Microsoft.Build.Framework;
using System;

namespace CBT.NuGet.UnitTests
{
    internal sealed class MockTask : ITask
    {
        public IBuildEngine BuildEngine { get; set; }

        public ITaskHost HostObject { get; set; }

        public bool Execute()
        {
            throw new NotSupportedException();
        }
    }
}