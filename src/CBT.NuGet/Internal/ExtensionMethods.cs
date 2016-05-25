using System;
using System.Collections.Generic;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace CBT.NuGet.Internal
{
    internal static class ExtensionMethods
    {
        public static void AppendSwitchIfAny(this CommandLineBuilder commandLineBuilder, string switchName, IEnumerable<ITaskItem> items)
        {
            if (items != null)
            {
                foreach (var item in items)
                {
                    commandLineBuilder.AppendSwitchIfNotNull(switchName, item.ItemSpec);
                }
            }
        }

        public static void AppendSwitchIfNotNullOrWhiteSpace(this CommandLineBuilder commandLineBuilder, string switchName, string parameter)
        {
            if (!String.IsNullOrWhiteSpace(parameter))
            {
                commandLineBuilder.AppendSwitchIfNotNull(switchName, parameter);
            }
        }

        public static void AppendSwitchIfTrue(this CommandLineBuilder commandLineBuilder, string switchName, bool value)
        {
            if (value)
            {
                commandLineBuilder.AppendSwitch(switchName);
            }
        }
    }
}