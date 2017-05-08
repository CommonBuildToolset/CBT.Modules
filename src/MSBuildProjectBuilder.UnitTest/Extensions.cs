using Microsoft.Build.Construction;
using Shouldly;
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

        public static void RawXmlShouldBe(this ProjectRootElement pre, string xml)
        {
            pre.RawXml.NormalizeNewLine()
                .ShouldBe(xml.NormalizeNewLine());
        }
    }
}
