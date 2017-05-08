using Microsoft.Build.Construction;
using System.Collections.Generic;

namespace Microsoft.MSBuildProjectBuilder
{
    public partial class ProjectBuilder
    {
        public ProjectRootElement ProjectRoot { get; private set; }

        private ICollection<ProjectItemElement> _lastItemElements = new List<ProjectItemElement>();

        private ProjectItemGroupElement _lastItemGroupElement = null;

        private ProjectElement _lastGroupContainer = null;

    }

}
