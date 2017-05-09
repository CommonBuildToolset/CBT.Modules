
namespace Microsoft.MSBuildProjectBuilder
{
    public partial class ProjectBuilder
    {
        public ProjectBuilder Save(string fileName)
        {
            ProjectRoot.Save(fileName);
            return this;
        }
    }
}
