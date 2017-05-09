using Microsoft.Build.Construction;
using System.Collections.Generic;

namespace Microsoft.MSBuildProjectBuilder
{
    public partial class ProjectBuilder
    {
        public ProjectRootElement ProjectRoot { get; private set; }

        private ICollection<ProjectItemElement> _lastItemElements = new List<ProjectItemElement>();

        private ICollection<ProjectPropertyElement> _lastPropertyElements = new List<ProjectPropertyElement>();

        private ProjectItemGroupElement _lastItemGroupElement = null;

        private ProjectPropertyGroupElement _lastPropertyGroupElement = null;

        private ProjectElement _lastGroupContainer = null;

        private ProjectBuilder(string fileName, string toolsVersion, string defaultTargets, string initialTargets, string label)
        {
            ProjectRoot = string.IsNullOrWhiteSpace(fileName) ? ProjectRootElement.Create() : ProjectRootElement.Create(fileName);
            ProjectRoot.DefaultTargets = defaultTargets ?? ProjectRoot.DefaultTargets;
            ProjectRoot.InitialTargets = initialTargets ?? ProjectRoot.InitialTargets;
            ProjectRoot.ToolsVersion = toolsVersion ?? ProjectRoot.ToolsVersion;
            ProjectRoot.Label = label ?? string.Empty;
            _lastGroupContainer = ProjectRoot;
        }

    }

}
