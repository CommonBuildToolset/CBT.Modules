using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Shouldly;

namespace NuGet.Deterministic.UnitTests
{
    public static class ExtensionMethods
    {
        public static T ShouldNotBeNull<T>(this T actual)
        {
            ShouldBeNullExtensions.ShouldNotBeNull(actual);

            return actual;
        }
    }
}
