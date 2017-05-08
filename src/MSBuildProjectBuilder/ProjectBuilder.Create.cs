using Microsoft.Build.Construction;

namespace Microsoft.MSBuildProjectBuilder
{
    public partial class ProjectBuilder
    {
        public ProjectBuilder Create(string fileName = null, string toolsVersion = null, string defaultTargets = null, string initialTargets = null, string label = null)
        {
            ProjectRoot = string.IsNullOrWhiteSpace(fileName) ? ProjectRootElement.Create() : ProjectRootElement.Create(fileName);
            ProjectRoot.DefaultTargets = defaultTargets ?? ProjectRoot.DefaultTargets;
            ProjectRoot.InitialTargets = initialTargets ?? ProjectRoot.InitialTargets;
            ProjectRoot.ToolsVersion = toolsVersion ?? ProjectRoot.ToolsVersion;
            ProjectRoot.Label = label ?? string.Empty;
            _lastGroupContainer = ProjectRoot;
            return this;
        }
    }
}
