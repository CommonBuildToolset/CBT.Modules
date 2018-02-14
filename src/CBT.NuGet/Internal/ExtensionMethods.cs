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
        public static string GetMd5Hash(this string input)
        {
            using (MD5 md5 = MD5.Create())
            {
                StringBuilder sb = new StringBuilder();
                foreach (byte hashByte in md5.ComputeHash(Encoding.UTF8.GetBytes(input.ToUpperInvariant())))
                {
                    sb.Append(hashByte.ToString("X2"));
                }
                return sb.ToString();
            }
        }
    }
}