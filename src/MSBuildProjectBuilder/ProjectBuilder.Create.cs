
namespace Microsoft.MSBuildProjectBuilder
{
    public partial class ProjectBuilder
    {
        public static ProjectBuilder Create(string toolsVersion = null, string defaultTargets = null, string initialTargets = null, string label = null)
        {
            return new ProjectBuilder(toolsVersion, defaultTargets, initialTargets, label);
        }
    }
}
