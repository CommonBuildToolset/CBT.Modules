using CBT.NuGet.Internal;
using CBT.NuGet.Tasks;
using Microsoft.MSBuildProjectBuilder;
using Shouldly;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace CBT.NuGet.UnitTests
{
    public class AggregatePackageTests : TestBase
    {
        private void CreateDummyPackage(string basePath, ICollection<string> filePaths)
        {
            foreach (var file in filePaths)
            {
                var fullFilePath = Path.Combine(basePath, file);
                var parent = Path.GetDirectoryName(fullFilePath);
                if (!Directory.Exists(parent))
                {
                    Directory.CreateDirectory(parent);
                }
                if (!File.Exists(fullFilePath))
                {
                    File.WriteAllText(fullFilePath, $"Dummy Contents {fullFilePath}");
                }
            }
        }

        [Fact]
        public void ParseAggregatePackageTest()
        {
            string pkg = Path.Combine(TestRootPath, "pkg");
            string pkg2 = Path.Combine(TestRootPath, "pkg2");
            string pkg3 = Path.Combine(TestRootPath, "pkg3");

            CreateDummyPackage(pkg, new[] { "fool.txt", "friend\\bat.txt", "cow.txt" });
            CreateDummyPackage(pkg2, new[] { "cammel.txt", "sour\\bat.txt", "cow.txt" });
            CreateDummyPackage(pkg3, new[] { "fool.txt", "sour\\bats.txt" });

            var aggPkgs = new AggregatePackages();
            aggPkgs.BuildEngine = new CBTBuildEngine();
            aggPkgs.PackagesToAggregate = $"foo={pkg}|{pkg2}|!{pkg3};foo2= \t {pkg}   |     {pkg2} | !  {pkg3}  \t";
            aggPkgs.AggregateDestRoot = Path.Combine(TestRootPath, ".agg");

            var parsedPackagesEnumerator = aggPkgs.ParsePackagesToAggregate().GetEnumerator();
            parsedPackagesEnumerator.MoveNext();
            var myPkg = parsedPackagesEnumerator.Current;
            var pkgsToAggShouldBe = new List<AggregatePackage.PackageOperation>()
                    {
                        new AggregatePackage.PackageOperation() { Operation = AggregatePackage.AggregateOperation.Add, Folder = pkg },
                        new AggregatePackage.PackageOperation() { Operation = AggregatePackage.AggregateOperation.Add, Folder = pkg2 },
                        new AggregatePackage.PackageOperation() { Operation = AggregatePackage.AggregateOperation.Remove, Folder = pkg3 }
                    };
            myPkg.AggregatePackageShouldBe("foo", pkgsToAggShouldBe);

            parsedPackagesEnumerator.MoveNext();
            myPkg = parsedPackagesEnumerator.Current;
            myPkg.AggregatePackageShouldBe("foo2", pkgsToAggShouldBe);
        }

        [Fact]
        public void CreateAggregatePackageTest()
        {

            string pkg = Path.Combine(TestRootPath, "pkg");
            string pkg2 = Path.Combine(TestRootPath, "pkg2");
            string pkg3 = Path.Combine(TestRootPath, "pkg3");

            CreateDummyPackage(pkg, new[] { "fool.txt", "friend\\bat.txt", "cow.txt" });
            CreateDummyPackage(pkg2, new[] { "cammel.txt", "sour\\bat.txt", "cow.txt" });
            CreateDummyPackage(pkg3, new[] { "fool.txt", "sour\\bats.txt" });

            var aggPkgs = new AggregatePackages();
            aggPkgs.BuildEngine = new CBTBuildEngine();
            aggPkgs.PackagesToAggregate = $"foo={pkg}|{pkg2}|!{pkg3};foo2={pkg}|!{pkg2}";
            aggPkgs.AggregateDestRoot = Path.Combine(TestRootPath, ".agg");

            var parsedPackagesEnumerator = aggPkgs.ParsePackagesToAggregate().GetEnumerator();
            parsedPackagesEnumerator.MoveNext();
            var myPkg = parsedPackagesEnumerator.Current;
            var pkgsToAggShouldBe = new List<AggregatePackage.PackageOperation>()
                    {
                        new AggregatePackage.PackageOperation() { Operation = AggregatePackage.AggregateOperation.Add, Folder = pkg },
                        new AggregatePackage.PackageOperation() { Operation = AggregatePackage.AggregateOperation.Add, Folder = pkg2 },
                        new AggregatePackage.PackageOperation() { Operation = AggregatePackage.AggregateOperation.Remove, Folder = pkg3 }
                    };
            myPkg.AggregatePackageShouldBe("foo", pkgsToAggShouldBe);

            aggPkgs.CreateAggregatePackage(myPkg);
            var expectedFileList = new[] { "cammel.txt", "cow.txt", "friend\\bat.txt", "sour\\bat.txt" };
            expectedFileList.ToList().ForEach(f => File.Exists(Path.Combine(myPkg.OutPropertyValue, f)).ShouldBe(true));
            var notExpectedFileList = new[] { "fool.txt", "sour\\bats.txt" };
            notExpectedFileList.ToList().ForEach(f => File.Exists(Path.Combine(myPkg.OutPropertyValue, f)).ShouldNotBe(true));
            File.ReadAllText(Path.Combine(myPkg.OutPropertyValue, "cow.txt")).ShouldBe($"Dummy Contents {pkg2}\\cow.txt");
            File.ReadAllText(Path.Combine(myPkg.OutPropertyValue, "friend\\bat.txt")).ShouldBe($"Dummy Contents {pkg}\\friend\\bat.txt");

            parsedPackagesEnumerator.MoveNext();
            myPkg = parsedPackagesEnumerator.Current;
            pkgsToAggShouldBe = new List<AggregatePackage.PackageOperation>()
                    {
                        new AggregatePackage.PackageOperation() { Operation = AggregatePackage.AggregateOperation.Add, Folder = pkg },
                        new AggregatePackage.PackageOperation() { Operation = AggregatePackage.AggregateOperation.Remove, Folder = pkg2 }
                    };
            myPkg.AggregatePackageShouldBe("foo2", pkgsToAggShouldBe);
            aggPkgs.CreateAggregatePackage(myPkg);
            expectedFileList = new[] { "fool.txt", "friend\\bat.txt" };
            expectedFileList.ToList().ForEach(f => File.Exists(Path.Combine(myPkg.OutPropertyValue, f)).ShouldBe(true));
            notExpectedFileList = new[] { "cammel.txt", "sour\\bat.txt", "cow.txt" };
            notExpectedFileList.ToList().ForEach(f => File.Exists(Path.Combine(myPkg.OutPropertyValue, f)).ShouldNotBe(true));

        }

        [Fact]
        public void WritePropsAggregatePackageTest()
        {
            string propsFile = Path.Combine(TestRootPath, "props", "foo.props");
            string propsFileExpect = Path.Combine(TestRootPath, "props", "fooExpected.props");
            Dictionary<string, string> props = new Dictionary<string, string>();
            props.Add("foo", "Myvalue");
            props.Add("foo2", "MyValue");
            var aggPkgs = new AggregatePackages();
            aggPkgs.CreatePropsFile(props, propsFile);
            var propsContents = File.ReadAllText(propsFile);
            var root = ProjectBuilder.Create()
                .AddProperty("MSBuildAllProjects=$(MSBuildAllProjects);$(MSBuildThisFileFullPath)", "foo=Myvalue", "foo2=MyValue")
                .Save(propsFileExpect);
            propsContents.ShouldBe(File.ReadAllText(propsFileExpect));
        }
    }
}
