using Microsoft.Build.Construction;

namespace Microsoft.MSBuildProjectBuilder
{
    public partial class ProjectBuilder
    {
        public ProjectRootElement ProjectRoot { get; private set; }

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
            if (!string.IsNullOrWhiteSpace(label))
            {
                ProjectRoot.Label = label;
            }
            if (!string.IsNullOrWhiteSpace(toolsVersion))
            {
                ProjectRoot.ToolsVersion = toolsVersion;
            }
            return this;
        }
    }
}
