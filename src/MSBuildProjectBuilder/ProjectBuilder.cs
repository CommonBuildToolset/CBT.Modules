using Microsoft.Build.Construction;
using System.Collections.Generic;

namespace Microsoft.MSBuildProjectBuilder
{
    public partial class ProjectBuilder
    {
        public ProjectRootElement ProjectRoot { get; private set; }

        private ICollection<ProjectElement> lastElements = new List<ProjectElement>();

    }
}
