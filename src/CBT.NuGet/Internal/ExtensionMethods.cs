using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace CBT.NuGet.Internal
{
    internal static class ExtensionMethods
    {
        private static Lazy<SHA256> _hasherLazy = new Lazy<SHA256>(SHA256.Create, isThreadSafe: true);

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

        /// <summary>
        /// Gets a case-insensitive MD5 hash of the current string.
        /// </summary>
        public static string GetHash(this string input, string prefix = null)
        {
            if (prefix != null)
            {
                return $"{prefix}{Convert.ToBase64String(_hasherLazy.Value.ComputeHash(Encoding.UTF8.GetBytes(input.ToUpperInvariant())))}";
            }
            else
            {
                return Convert.ToBase64String(_hasherLazy.Value.ComputeHash(Encoding.UTF8.GetBytes(input.ToUpperInvariant())));
            }
        }
    }
}