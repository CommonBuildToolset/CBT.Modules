using Microsoft.Build.Construction;
using System;
using System.Linq;

namespace Microsoft.MSBuildProjectBuilder
{
    public partial class ProjectBuilder
    {
        public ProjectBuilder AddItem(params Item[] items)
        {
            if (items == null || items.Length == 0)
            {
                throw new ArgumentException("At least one Item must be specified to be added.");
            }

            // If no project is created then create it automatically.
            if (!lastElements.Any())
            {
                Create();
            }

            ProjectElement lE = null;
            do
            {
                lE = lastElements.FirstOrDefault();
                ProjectItemGroupElement itemGroup = lE as ProjectItemGroupElement;
                if (itemGroup != null)
                {
                    lastElements.Clear();
                    foreach (var item in items)
                    {
                        ProjectItemElement projectItem = ProjectRoot.CreateItemElement(item.Name);
                        projectItem.Include = item.Value;
                        itemGroup.AppendChild(projectItem);
                        lastElements.Add(projectItem);
                    }
                    return this;
                }
                // if at root of project or root of target or root of when block create empty item group.
                if ((lE is ProjectRootElement) || (lE is ProjectTargetElement))
                {
                    AddItemGroup();
                }
            }
            while (lE != null && (lE is ProjectItemGroupElement) == false);

            throw new InvalidOperationException("Invalid project object, AddItem failed."); ;
        }
    }
}
