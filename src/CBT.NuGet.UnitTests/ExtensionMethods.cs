using System;
using CBT.NuGet.Internal;
using Shouldly;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace CBT.NuGet.UnitTests
{
    public static class ExtensionMethods
    {
        internal static void PackagesToAggregateShouldBe(this ICollection<AggregatePackage.PackageOperation> aPC, ICollection<AggregatePackage.PackageOperation> aggregatePackages)
        {
            aPC.Count.ShouldBe(aggregatePackages.Count);

            var enumeratorThis = aPC.GetEnumerator();
            var enumeratorPassed = aggregatePackages.GetEnumerator();
            enumeratorThis.MoveNext();
            enumeratorPassed.MoveNext();
            do
            {
                enumeratorThis.Current.ShouldNotBe(null);
                enumeratorPassed.Current.ShouldNotBe(null);

                enumeratorThis.Current.Operation.ShouldBe(enumeratorPassed.Current.Operation);
                enumeratorThis.Current.Folder.ShouldBe(enumeratorPassed.Current.Folder);
                Directory.Exists(enumeratorThis.Current.Folder).ShouldBe(true);

                enumeratorThis.MoveNext();
                enumeratorPassed.MoveNext();
            } while (enumeratorThis.Current != null && enumeratorPassed.Current != null);
        }

        internal static void AggregatePackageShouldBe(this AggregatePackage aP, string outPropertyID, ICollection<AggregatePackage.PackageOperation> packageOperations)
        {
            aP.OutPropertyId.ShouldBe(outPropertyID);
            aP.PackagesToAggregate.PackagesToAggregateShouldBe(packageOperations);
        }
        internal static string NormalizeNewLine(this String str)
        {
            return Regex.Replace(str, @"\r\n?|\n", Environment.NewLine);
        }
    }
}
