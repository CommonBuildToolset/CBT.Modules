using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System.Collections.Generic;

namespace CBT.NuGet.Tasks
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

        public static void AppendSwitchIfTrue(this CommandLineBuilder commandLineBuilder, string switchName, bool value)
        {
            if (value)
            {
                commandLineBuilder.AppendSwitch(switchName);
            }
        }
    }
}