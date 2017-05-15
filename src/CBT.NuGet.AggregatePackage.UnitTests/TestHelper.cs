// <copyright>
//    Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace CBT.NuGet.AggregatePackage.UnitTests
{
    using Microsoft.Build.Evaluation;
    using Microsoft.Build.Framework;
    using Microsoft.MSBuildProjectBuilder;
    using Shouldly;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public enum MessageType
    {
        BuildSucceededMessage,
        BuildSucceededWarning,
        Message,
        Warning,
        Error,
        TargetFinished,
        TaskFinished
    }

    public class ExpectedOutputMessage
    {
        IList<string> messages;
        public ExpectedOutputMessage(MessageType messageType, IList<string> expectedOutputMessages)
        {
            EventType = messageType;
            messages = expectedOutputMessages;
        }

        public MessageType EventType { get; private set; }

        public IList<string> Messages
        {
            get
            {
                return messages;
            }
        }

        public bool ListItemsContainedIn(string fullOutputMessage)
        {
            return messages.Count == messages.Count(eOM => fullOutputMessage.IndexOf(eOM, StringComparison.OrdinalIgnoreCase) >= 0);
        }
    }
    static class Helper
    {
        internal static readonly Lazy<ProjectCollection> _projectCollectionLazy = new Lazy<ProjectCollection>(() => new ProjectCollection(), isThreadSafe: true);

        public enum TestType
        {
            Simple
        }

        public static void RunTest(TestType testType, string projectName, IList<ExpectedOutputMessage> expectedOutputMessages, ICollection<Property> expectedProperties = null, ICollection<Import> expectedImports = null, ICollection<Item> expectedItems = null, string target = "")
        {
            var logger = new TestLogger();
            var project = _projectCollectionLazy.Value.LoadProject(projectName);

            try
            {
                switch (testType)
                {
                    case TestType.Simple:
                        if (expectedOutputMessages != null && expectedOutputMessages.Any())
                        {
                            MessageTest(project, logger, expectedOutputMessages, target);
                        }
                        if (expectedProperties != null && expectedProperties.Any())
                        {
                            PropertiesTest(project, logger, expectedProperties, target);
                        }
                        if (expectedImports != null && expectedImports.Any())
                        {
                            ImportsTest(project, logger, expectedImports, target);
                        }
                        if (expectedItems != null && expectedItems.Any())
                        {
                            ItemsTest(project, logger, expectedItems, target);
                        }
                        return;
                    default:
                        throw new NotImplementedException("Test Type Not Expected.");
                }
            }
            finally
            {
                _projectCollectionLazy.Value.UnloadProject(project);
            }
        }

        private static void PropertiesTest(Project project, TestLogger logger, ICollection<Property> expectedProperties, string target = "")
        {
            project.AllEvaluatedProperties.PropertiesShouldContain(expectedProperties);
        }

        private static void ItemsTest(Project project, TestLogger logger, ICollection<Item> expectedItems, string target = "")
        {
            project.AllEvaluatedItems.ItemsShouldBe(expectedItems);
        }

        private static void ImportsTest(Project project, TestLogger logger, ICollection<Import> expectedImports, string target = "")
        {
            project.Imports.ImportsShouldBe(expectedImports);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity", Justification = "Simple enough.")]
        private static void MessageTest(Project project, TestLogger logger, IList<ExpectedOutputMessage> expectedOutputMessages, string target = "")
        {
            bool buildSuccessful = string.IsNullOrWhiteSpace(target) ? project.Build(logger) : project.Build(target, new List<ILogger> { logger });
            int lastFindLocation = 0;
            EventMessages foundEventArgs = new EventMessages { Matched = false, BuildEventArgs = null };
            for (var i = 0; i <= expectedOutputMessages.Count - 1; i++)
            {
                int findLocation = -1;
                switch (expectedOutputMessages[i].EventType)
                {
                    case MessageType.Error:
                        buildSuccessful.ShouldBe(false);
                        foundEventArgs = logger.Events.Where(evn => evn.Matched == false && evn.BuildEventArgs is BuildErrorEventArgs).Where(evn => expectedOutputMessages[i].ListItemsContainedIn(evn.BuildEventArgs.Message)).FirstOrDefault();
                        findLocation = logger.Events.IndexOf(foundEventArgs);
                        break;
                    case MessageType.BuildSucceededWarning:
                        buildSuccessful.ShouldBe(true);
                        goto case MessageType.Warning;
                    case MessageType.Warning:
                        foundEventArgs = logger.Events.Where(evn => evn.Matched == false && evn.BuildEventArgs is BuildWarningEventArgs).Where(evn => expectedOutputMessages[i].ListItemsContainedIn(evn.BuildEventArgs.Message)).FirstOrDefault();
                        findLocation = logger.Events.IndexOf(foundEventArgs);
                        break;
                    case MessageType.BuildSucceededMessage:
                        buildSuccessful.ShouldBe(true);
                        goto case MessageType.Message;
                    case MessageType.Message:
                        foundEventArgs = logger.Events.Where(evn => evn.Matched == false && evn.BuildEventArgs is BuildMessageEventArgs).Where(evn => expectedOutputMessages[i].ListItemsContainedIn(evn.BuildEventArgs.Message)).FirstOrDefault();
                        findLocation = logger.Events.IndexOf(foundEventArgs);
                        break;
                    case MessageType.TargetFinished:
                        foundEventArgs = logger.Events.Where(evn => evn.Matched == false && evn.BuildEventArgs is TargetFinishedEventArgs).Where(evn => expectedOutputMessages[i].ListItemsContainedIn(evn.BuildEventArgs.Message)).FirstOrDefault();
                        findLocation = logger.Events.IndexOf(foundEventArgs);
                        break;
                    case MessageType.TaskFinished:
                        foundEventArgs = logger.Events.Where(evn => evn.Matched == false && evn.BuildEventArgs is TaskFinishedEventArgs).Where(evn => expectedOutputMessages[i].ListItemsContainedIn(evn.BuildEventArgs.Message)).FirstOrDefault();
                        findLocation = logger.Events.IndexOf(foundEventArgs);
                        break;
                }

                // when message is not found.
                var messageNotFound = findLocation == -1;
                messageNotFound.ShouldBe(false, $"Message(s) of type {expectedOutputMessages[i].EventType} and value of \"{string.Join(",", expectedOutputMessages[i].Messages)}\" not found in build output.");
                // when message is found out of order.
                var messageFoundOutOfOrder = lastFindLocation >= findLocation;
                messageFoundOutOfOrder.ShouldBe(false, $"Message(s) found in unexpected order: \"{ string.Join(",", expectedOutputMessages[i].Messages)}\"");
                lastFindLocation = findLocation;
                // mark as found message. 
                logger.Events[findLocation] = new EventMessages { Matched = true, BuildEventArgs = foundEventArgs.BuildEventArgs };
            }
        }
    }
}
