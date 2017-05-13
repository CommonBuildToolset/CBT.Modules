using Microsoft.Build.Evaluation;
using Microsoft.MSBuildProjectBuilder;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CBT.NuGet.AggregatePackage.UnitTests
{
    static class Extensions
    {
        internal static void PropertiesShouldContain(this ICollection<ProjectProperty> pP, ICollection<Property> expectedProperties)
        {
            foreach (var property in expectedProperties)
            {
                var foundProperty = pP.Where(i => i.Name.Equals(property.Name, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
                foundProperty.ShouldNotBe(null, $"Property by name of {property.Name} was not found in project.");
                foundProperty.EvaluatedValue.ShouldBe(property.Value, $"Property by name of {property.Name} does not have expected Evaluated value.");
            }
        }
        internal static void ItemsShouldBe(this ICollection<ProjectItem> eI, ICollection<Item> itemList)
        {
            eI.Count.ShouldBe(itemList.Count, "Amount of expected items do not match");

            var enumeratorThis = eI.GetEnumerator();
            var enumeratorPassed = itemList.GetEnumerator();
            enumeratorThis.MoveNext();
            enumeratorPassed.MoveNext();
            do
            {
                enumeratorThis.Current.ShouldNotBe(null);
                enumeratorPassed.Current.ShouldNotBe(null);

                enumeratorThis.Current.ItemType.ShouldBe(enumeratorPassed.Current.Name, "Expected Item type does not match");
                enumeratorThis.Current.EvaluatedInclude.ShouldBe(enumeratorPassed.Current.Value, "Expected Item type does not match");
                foreach (var meta in enumeratorPassed.Current.Metadata)
                {
                    enumeratorThis.Current.HasMetadata(meta.Name).ShouldBe(true);
                    enumeratorThis.Current.GetMetadata(meta.Name).EvaluatedValue.ShouldBe(meta.Value);
                }
                enumeratorThis.MoveNext();
                enumeratorPassed.MoveNext();
            } while (enumeratorThis.Current != null && enumeratorPassed.Current != null);
        }

        internal static void ImportsShouldBe(this IList<ResolvedImport> rI, ICollection<Import> importList)
        {
            rI.Count.ShouldBe(importList.Count, "Amount of expected imports do not match");

            var enumeratorThis = rI.GetEnumerator();
            var enumeratorPassed = importList.GetEnumerator();
            enumeratorThis.MoveNext();
            enumeratorPassed.MoveNext();
            do
            {
                enumeratorThis.Current.ImportedProject.ShouldNotBe(null);
                enumeratorPassed.Current.ShouldNotBe(null);

                enumeratorThis.Current.ImportingElement.Project.ShouldBe(enumeratorPassed.Current.Project, "Expected Import does not match");

                enumeratorThis.MoveNext();
                enumeratorPassed.MoveNext();
            } while (enumeratorThis.Current.ImportedProject != null && enumeratorPassed.Current != null);
        }
    }
}
