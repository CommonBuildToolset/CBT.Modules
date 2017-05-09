using Microsoft.Build.Construction;

namespace Microsoft.MSBuildProjectBuilder
{
    public partial class ProjectBuilder
    {
        public static ProjectBuilder Create(string fileName = null, string toolsVersion = null, string defaultTargets = null, string initialTargets = null, string label = null)
        {
            return new ProjectBuilder(fileName, toolsVersion, defaultTargets, initialTargets, label);
        }
    }
}
