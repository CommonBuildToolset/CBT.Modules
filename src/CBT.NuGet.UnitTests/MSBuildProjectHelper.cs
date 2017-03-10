using System.Collections.Generic;
using System.Linq;
using Microsoft.Build.Construction;

namespace CBT.NuGet.UnitTests
{
    internal static class MSBuildProjectHelper
    {
        public static ProjectRootElement CreateProject(string path)
        {
            return CreateProject(path, Enumerable.Empty<string>());
        }

        public static ProjectRootElement CreateProject(string path, IEnumerable<string> projectReferences, string itemName = "ProjectReference")
        {
            ProjectRootElement projectRootElement = ProjectRootElement.Create(path);

            ProjectItemGroupElement itemGroup = null;

            foreach (string projectReference in projectReferences)
            {
                if (itemGroup == null)
                {
                    itemGroup = projectRootElement.AddItemGroup();
                }

                ProjectItemElement projectReferenceItem = itemGroup.AddItem(itemName, projectReference);
            }

            projectRootElement.Save();

            return projectRootElement;
        }

        public static ProjectRootElement CreateTraversalProject(string path, IEnumerable<string> projectReferences)
        {
            ProjectRootElement projectRootElement = CreateProject(path, projectReferences, itemName: "ProjectFile");

            projectRootElement.AddPropertyGroup().SetProperty("IsTraversal", "true");

            projectRootElement.Save();

            return projectRootElement;
        }
    }
}