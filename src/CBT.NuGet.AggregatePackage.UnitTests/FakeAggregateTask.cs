
using Microsoft.Build.Utilities;

namespace CBT.NuGet.AggregatePackage.UnitTests
{
    public class FakeAggregateFalse : Task
    {
        public string PackagesToAggregate { get; set; }
        public string PropsFile { get; set; }
        public string AggregateDestRoot { get; set; }
        public string ImmutableRoots { get; set; }

        public override bool Execute()
        {
            Log.LogError("Fake failure");
            return false;
        }
        public bool Execute(string aggregateDestRoot, string packagesToAggregate, string propsFile, string immutableRoots)
        {
            return false;
        }
    }

    public class FakeAggregateTrue : Task
    {
        public string PackagesToAggregate { get; set; }
        public string PropsFile { get; set; }
        public string AggregateDestRoot { get; set; }
        public string ImmutableRoots { get; set; }

        public override bool Execute()
        {
            return true;
        }
        public bool Execute(string aggregateDestRoot, string packagesToAggregate, string propsFile, string immutableRoots)
        {
            return true;
        }
    }
}
