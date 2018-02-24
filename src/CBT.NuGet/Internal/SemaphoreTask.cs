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
            // If BeforeRun() returns false, don't continue on
            if (!BeforeRun())
            {
                return !Log.HasLoggedErrors;
            }

            using (Semaphore semaphore = new Semaphore(0, 1, SemaphoreName.GetHash(), out bool releaseSemaphore))
            {
                try
                {
                    // releaseSemaphore is false if a new semaphore was not acquired
                    if (!releaseSemaphore)
                    {
                        // Wait for the semaphore
                        releaseSemaphore = semaphore.WaitOne(SemaphoreTimeout);

                        if (RunOnceOnly)
                        {
                            // Return if another thread did the work and the task is marked to only run once (the default)
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

        /// <summary>
        /// Runs before the semaphore is acquired and determines if remaining actions should be executed.
        /// </summary>
        /// <returns><code>true</code> if the semaphone should be acquired and the other actions should be executed, otherwise <code>false</code> to not execute.</returns>
        protected virtual bool BeforeRun()
        {
            return true;
        }
    }
}