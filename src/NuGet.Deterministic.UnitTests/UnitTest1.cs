using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NuGet.Commands;
using NuGet.Common;
using NuGet.LibraryModel;
using NuGet.ProjectModel;

namespace NuGet.Deterministic.UnitTests
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestMethod1()
        {
            LockFile lockFile = LockFileUtilities.GetLockFile(@"D:\CommonBuildToolset\CloudBuild.Modules\src\CBT.CloudBuild\obj\project.assets.json", new NullLogger());

            foreach (LockFileLibrary lockFileLibrary in lockFile.Libraries.Where(p => p.Type.Equals("package")))
            {
                LibraryDependency libraryDependency = lockFile.PackageSpec.TargetFrameworks.Select(i => i.Dependencies.FirstOrDefault(x => x.Name.Equals(lockFileLibrary.Name))).FirstOrDefault();

                if (libraryDependency != null)
                {
                    string privateAssets = libraryDependency.SuppressParent.ToString();

                    string includeAssets = libraryDependency.IncludeType.ToString();
                }
            }
        }
    }
}
