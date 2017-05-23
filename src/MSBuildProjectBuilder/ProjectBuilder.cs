using Microsoft.Build.Construction;
using Microsoft.Build.Evaluation;
using System;
using System.Collections.Generic;

namespace Microsoft.MSBuildProjectBuilder
{
    public partial class ProjectBuilder
    {
        private readonly ICollection<ProjectItemElement> _lastItemElements = new List<ProjectItemElement>();
        private readonly ICollection<ProjectPropertyElement> _lastPropertyElements = new List<ProjectPropertyElement>();
        private readonly Lazy<Project> _projectLazy;

        private ProjectElement _lastGroupContainer;
        private ProjectItemGroupElement _lastItemGroupElement;
        private ProjectPropertyGroupElement _lastPropertyGroupElement;
        private ProjectTargetElement _lastTargetElement;

        private ProjectBuilder(string toolsVersion, string defaultTargets, string initialTargets, string label)
        {
            ProjectRoot = ProjectRootElement.Create();
            ProjectRoot.DefaultTargets = defaultTargets ?? ProjectRoot.DefaultTargets;
            ProjectRoot.InitialTargets = initialTargets ?? ProjectRoot.InitialTargets;
            ProjectRoot.ToolsVersion = toolsVersion ?? ProjectRoot.ToolsVersion;
            ProjectRoot.Label = label ?? string.Empty;
            _lastGroupContainer = ProjectRoot;

            _projectLazy = new Lazy<Project>(() => new Project(ProjectRoot), isThreadSafe: true);
        }

        /// <summary>
        /// Gets the full path to the project file.
        /// </summary>
        public string FullPath => ProjectRoot.FullPath;

        public Project Project
        {
            get
            {
                Project project = _projectLazy.Value;

                project.ReevaluateIfNecessary();

                return project;
            }
        }

        public ProjectRootElement ProjectRoot { get; }
    }
}