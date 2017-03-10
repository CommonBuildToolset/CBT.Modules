using CBT.NuGet.Internal;
using Microsoft.Build.Evaluation;
using Shouldly;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace CBT.NuGet.UnitTests
{
    public class MSBuildProjectLoaderTests : IDisposable
    {
        private const string MSBuildToolsVersion = "4.0";
        private readonly string _basePath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

        private readonly string _dirsAPath;
        private readonly string _projectAPath;
        private readonly string _projectBPath;

        public MSBuildProjectLoaderTests()
        {
            _dirsAPath = Path.Combine(_basePath, "dirsA.proj");
            _projectAPath = Path.Combine(_basePath, "ProjectA.proj");
            _projectBPath = Path.Combine(_basePath, "ProjectB.proj");

            MSBuildProjectHelper.CreateProject(_projectAPath,
                new[]
                {
                    MSBuildProjectHelper.CreateProject(_projectBPath).FullPath
                });

            MSBuildProjectHelper.CreateTraversalProject(_dirsAPath,
                new[]
                {
                    _projectAPath
                });
        }

        public void Dispose()
        {
            Directory.Delete(_basePath, recursive: true);
        }

        [Fact]
        public void GlobalPropertiesSetCorrectly()
        {
            Dictionary<string, string> expectedGlobalProperties = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                {"Property1", "1A836FEB3ABA43B183034DFDD5C4E375"},
                {"Property2", "CEEC5C9FF0F344DAA32A0F545460EB2C"}
            };

            MSBuildProjectLoader loader = new MSBuildProjectLoader(expectedGlobalProperties, MSBuildToolsVersion, ProjectLoadSettings.Default);

            ProjectCollection projectCollection = loader.LoadProjectsAndReferences(new[] {_projectAPath});

            projectCollection.GlobalProperties.ShouldBe(expectedGlobalProperties);
        }

        [Fact]
        public void ProjectReferencesWork()
        {
            MSBuildProjectLoader loader = new MSBuildProjectLoader(null, MSBuildToolsVersion);

            ProjectCollection projectCollection = loader.LoadProjectsAndReferences(new[] {_projectAPath});

            projectCollection.LoadedProjects.Select(i => i.FullPath).ShouldBe(new[] {_projectAPath, _projectBPath});
        }

        [Fact]
        public void TraversalReferencesWork()
        {
            MSBuildProjectLoader loader = new MSBuildProjectLoader(null, MSBuildToolsVersion);

            ProjectCollection projectCollection = loader.LoadProjectsAndReferences(new[] {_dirsAPath});

            projectCollection.LoadedProjects.Select(i => i.FullPath).ShouldBe(new[] {_dirsAPath, _projectAPath, _projectBPath});
        }
    }
}