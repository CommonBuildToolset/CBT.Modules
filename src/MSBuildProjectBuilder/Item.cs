using Microsoft.Build.Construction;

namespace Microsoft.MSBuildProjectBuilder
{
    public partial class ProjectBuilder
    {

        private ProjectItemElement CreateItem(string name, string include, string condition, string label)
        {
            ProjectItemElement item = ProjectRoot.CreateItemElement(name);
            if (!string.IsNullOrWhiteSpace(include))
            {
                item.Include = include;
            }
            if (!string.IsNullOrWhiteSpace(label))
            {
                item.Label = label;
            }
            if (!string.IsNullOrWhiteSpace(condition))
            {
                item.Condition = condition;
            }
            return item;
        }

        public ProjectBuilder AddItem(string name, string include, string condition, string label, ProjectItemGroupElement projectItemGroup, out ProjectItemElement projectItem)
        {
            if (projectItemGroup == null)
            {
                AddItemGroup(null, null, out projectItemGroup);
            }
            projectItem = CreateItem(name, include, condition, label);
            projectItemGroup.AppendChild(projectItem);
            return this;
        }

        public ProjectBuilder AddItem(string name, string include, string condition = null, string label = null, ProjectItemGroupElement itemGroup = null)
        {
            ProjectItemElement pie = null;
            AddItem(name, include, condition, label, itemGroup, out pie);
            return this;
        }

        public ProjectBuilder AddItemAfterItemElement(string name, string include, string condition, string label, ProjectItemElement insertAfterElement, out ProjectItemElement projectItem)
        {
            projectItem = CreateItem(name, include, condition, label);
            insertAfterElement.Parent.InsertAfterChild(projectItem, insertAfterElement);
            return this;
        }

        public ProjectBuilder AddItemBeforeItemElement(string name, string include, string condition, string label, ProjectItemElement insertBeforeElement, out ProjectItemElement projectItem)
        {
            projectItem = CreateItem(name, include, condition, label);
            insertBeforeElement.Parent.InsertBeforeChild(projectItem, insertBeforeElement);
            return this;
        }
    }
}
