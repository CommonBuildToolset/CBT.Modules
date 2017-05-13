// <copyright>
//    Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

// TODO: refactor into unittest helper
namespace CBT.NuGet.AggregatePackage.UnitTests
{
    using Microsoft.Build.Framework;
    using Microsoft.Build.Utilities;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;

    public class EventMessages
    {
        public bool Matched { get; set; }

        public LazyFormattedBuildEventArgs BuildEventArgs { get; set; }
    }

    public class TestLogger : Logger
    {
        List<EventMessages> events = new List<EventMessages>();
        public TestLogger()
        {
        }

        public IList<EventMessages> Events
        {
            get
            {
                return events;
            }
        }

        public override void Initialize(IEventSource eventSource)
        {
            if (eventSource == null)
            {
                throw new ArgumentNullException("eventSource");
            }
            eventSource.WarningRaised += EventSourceWarningRaised;
            eventSource.ErrorRaised += EventSourceErrorRaised;
            eventSource.TargetFinished += EventSourceOnTargetFinished;
            eventSource.MessageRaised += EventSourceMessageRaised;
            eventSource.TaskFinished += EventSourceOnTaskFinished;
        }

        public override void Shutdown()
        {
            events.ToList().ForEach(e => Trace.WriteLine(e.BuildEventArgs.Message));
        }

        void EventSourceMessageRaised(object sender, BuildMessageEventArgs e)
        {
            events.Add(new EventMessages { Matched = false, BuildEventArgs = e });
        }

        void EventSourceErrorRaised(object sender, BuildErrorEventArgs e)
        {
            events.Add(new EventMessages { Matched = false, BuildEventArgs = e });
        }

        void EventSourceWarningRaised(object sender, BuildWarningEventArgs e)
        {
            events.Add(new EventMessages { Matched = false, BuildEventArgs = e });
        }

        private void EventSourceOnTargetFinished(object sender, TargetFinishedEventArgs targetFinishedEventArgs)
        {
            events.Add(new EventMessages { Matched = false, BuildEventArgs = targetFinishedEventArgs });
        }

        private void EventSourceOnTaskFinished(object sender, TaskFinishedEventArgs taskFinishedEventArgs)
        {
            events.Add(new EventMessages { Matched = false, BuildEventArgs = taskFinishedEventArgs });
        }
    }
}
