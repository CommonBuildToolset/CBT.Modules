using Microsoft.Build.Construction;
using System.Linq;

namespace Microsoft.MSBuildProjectBuilder
{
    public partial class ProjectBuilder
    {

        public ProjectBuilder AddImport(params Import[] imports)
        {
            if (imports == null || imports.Length == 0)
            {
                // if call .AddImport() but not specifying an item just return;
                return this;
            }

            foreach (var import in imports)
            {
                ProjectImportElement importElement = ProjectRoot.CreateImportElement(import.Project);
                importElement.Condition = import.Condition;
                importElement.Label = import.Label;
                ProjectRoot.AppendChild(importElement);
            }
            return this;
        }
    }
}
