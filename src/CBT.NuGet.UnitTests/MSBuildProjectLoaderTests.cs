using CBT.NuGet.Internal;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Microsoft.MSBuildProjectBuilder;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using CBT.UnitTests.Common;
using Xunit;

namespace CBT.NuGet.UnitTests
{
    public class MSBuildProjectLoaderTests : TestBase
    {
        private const string MSBuildToolsVersion = "4.0";

        private readonly MockBuildEngine _buildEngine = new MockBuildEngine();
        private readonly Lazy<TaskLoggingHelper> _logLazy;

        public MSBuildProjectLoaderTests()
        {
            _logLazy = new Lazy<TaskLoggingHelper>(() => new TaskLoggingHelper(_buildEngine, "TaskName"), isThreadSafe: true);
        }

        public TaskLoggingHelper Log => _logLazy.Value;

        [Fact]
        public void ArgumentNullException_Log()
        {
            ArgumentNullException exception = Should.Throw<ArgumentNullException>(() =>
            {
                MSBuildProjectLoader unused = new MSBuildProjectLoader(globalProperties: null, toolsVersion: null, log: null);
            });

            exception.ParamName.ShouldBe("log");
        }

        [Fact]
        public void BuildFailsIfError()
        {
            var dirsProj = ProjectBuilder
                .Create()
                .AddProperty("IsTraversal=true")
                .AddItem("ProjectFile=does not exist")
                .Save(GetTempFileName());

            MSBuildProjectLoader loader = new MSBuildProjectLoader(null, MSBuildToolsVersion, Log);
            loader.LoadProjectsAndReferences(new[] {dirsProj.FullPath});

            Log.HasLoggedErrors.ShouldBe(true);

            _buildEngine.LoggedEvents.Count.ShouldBe(1);
            BuildErrorEventArgs errorEvent = _buildEngine.LoggedEvents.FirstOrDefault() as BuildErrorEventArgs;

            errorEvent.ShouldNotBeNull();

            errorEvent?.Message.ShouldStartWith("The project file could not be loaded. Could not find file ");
        }

        [Fact]
        public void GlobalPropertiesSetCorrectly()
        {
            Dictionary<string, string> expectedGlobalProperties = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                {"Property1", "1A836FEB3ABA43B183034DFDD5C4E375"},
                {"Property2", "CEEC5C9FF0F344DAA32A0F545460EB2C"}
            };

            var projectA = ProjectBuilder
                .Create()
                .Save(GetTempFileName());

            MSBuildProjectLoader loader = new MSBuildProjectLoader(expectedGlobalProperties, MSBuildToolsVersion, Log);

            ProjectCollection projectCollection = loader.LoadProjectsAndReferences(new[] {projectA.FullPath});

            projectCollection.GlobalProperties.ShouldBe(expectedGlobalProperties);
        }

        [Fact]
        public void ProjectReferencesWork()
        {
            var projectB = ProjectBuilder.Create()
                .Save(GetTempFileName());

            var projectA = ProjectBuilder
                .Create()
                .AddProjectReference(projectB)
                .Save(GetTempFileName());

            MSBuildProjectLoader loader = new MSBuildProjectLoader(null, MSBuildToolsVersion, Log);

            ProjectCollection projectCollection = loader.LoadProjectsAndReferences(new[] {projectA.FullPath});

            projectCollection.LoadedProjects.Select(i => i.FullPath).ShouldBe(new[] {projectA.FullPath, projectB.FullPath});
        }

        [Fact]
        public void TraversalReferencesWork()
        {
            var projectB = ProjectBuilder.Create()
                .Save(GetTempFileName());

            var projectA = ProjectBuilder
                .Create()
                .AddProjectReference(projectB)
                .Save(GetTempFileName());

            var dirsProj = ProjectBuilder
                .Create()
                .AddProperty("IsTraversal=true")
                .AddItem($"ProjectFile={projectA.FullPath}")
                .Save(GetTempFileName());

            MSBuildProjectLoader loader = new MSBuildProjectLoader(null, MSBuildToolsVersion, Log);

            ProjectCollection projectCollection = loader.LoadProjectsAndReferences(new[] {dirsProj.FullPath});

            projectCollection.LoadedProjects.Select(i => i.FullPath).ShouldBe(new[] {dirsProj.FullPath, projectA.FullPath, projectB.FullPath});
        }
    }
}