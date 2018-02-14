using Microsoft.Build.Utilities;
using System;
using System.Threading;

namespace CBT.NuGet.Internal
{
    public abstract class SemaphoreTask : Task
    {
        /// <summary>
        /// Gets or sets a value indicating if the task should only run once.  The default is <code>true</code>.
        /// Set this to <code>false</code> to have the task run multiple times but only have on thread running at one time.
        /// </summary>
        protected virtual bool RunOnceOnly { get; } = true;

        protected abstract string SemaphoreName { get; }

        protected virtual TimeSpan SemaphoreTimeout { get; } = TimeSpan.FromMinutes(30);

        public override bool Execute()
        {
            using (Semaphore semaphore = new Semaphore(0, 1, SemaphoreName.GetMd5Hash(), out bool releaseSemaphore))
            {
                try
                {
                    if (!releaseSemaphore)
                    {
                        releaseSemaphore = semaphore.WaitOne(SemaphoreTimeout);

                        if (RunOnceOnly)
                        {
                            return releaseSemaphore;
                        }
                    }

                    Run();
                }
                finally
                {
                    if (releaseSemaphore)
                    {
                        semaphore.Release();
                    }
                }
            }

            return !Log.HasLoggedErrors;
        }

        public abstract void Run();
    }
}