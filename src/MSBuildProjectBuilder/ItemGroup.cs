using Microsoft.Build.Construction;

namespace Microsoft.MSBuildProjectBuilder
{
    public partial class ProjectBuilder
    {

        private ProjectItemGroupElement CreateItemGroup(string condition, string label)
        {
            if (ProjectRoot == null)
            {
                Create();
            }
            ProjectItemGroupElement itemGroup = ProjectRoot.CreateItemGroupElement();
            if (!string.IsNullOrWhiteSpace(condition))
            {
                itemGroup.Condition = condition;
            }
            if (!string.IsNullOrWhiteSpace(label))
            {
                itemGroup.Label = label;
            }
            return itemGroup;
        }

        public ProjectBuilder AddItemGroup(string condition, string label, out ProjectItemGroupElement itemGroup)
        {
            itemGroup = CreateItemGroup(condition, label);
            ProjectRoot.AppendChild(itemGroup);
            return this;
        }

        public ProjectBuilder AddItemGroupAfterElement(string condition, string label, ProjectElement insertAfterElement, out ProjectItemGroupElement itemGroup)
        {
            itemGroup = CreateItemGroup(condition, label);
            ProjectRoot.InsertAfterChild(itemGroup, insertAfterElement);
            return this;
        }

        public ProjectBuilder AddItemGroupBeforeElement(string condition, string label, ProjectElement insertBeforeElement, out ProjectItemGroupElement itemGroup)
        {
            itemGroup = CreateItemGroup(condition, label);
            ProjectRoot.InsertBeforeChild(itemGroup, insertBeforeElement);
            return this;
        }
    }
}
