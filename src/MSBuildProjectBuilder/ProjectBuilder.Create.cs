using Microsoft.Build.Construction;

namespace Microsoft.MSBuildProjectBuilder
{
    public partial class ProjectBuilder
    {
        public ProjectBuilder Create(string fileName = null, string toolsVersion = null, string defaultTargets = null, string initialTargets = null, string label = null)
        {
            ProjectRoot = string.IsNullOrWhiteSpace(fileName) ? ProjectRootElement.Create() : ProjectRootElement.Create(fileName);
            // do not overwrite default values unless set.
            if (!string.IsNullOrWhiteSpace(defaultTargets))
            {
                ProjectRoot.DefaultTargets = defaultTargets;
            }
            if (!string.IsNullOrWhiteSpace(initialTargets))
            {
                ProjectRoot.InitialTargets = initialTargets;
            }
            if (!string.IsNullOrWhiteSpace(toolsVersion))
            {
                ProjectRoot.ToolsVersion = toolsVersion;
            }

            lastElements.Clear();
            lastElements.Add(ProjectRoot);
            return this;
        }
    }
}
