using System;

using System.Text.RegularExpressions;

namespace MSBuildProjectBuilder.UnitTest
{
    public static class Extensions
    {
        public static string NormalizeNewLine(this String str)
        {
            return Regex.Replace(str, @"\r\n?|\n", Environment.NewLine);
        }
    }
}
