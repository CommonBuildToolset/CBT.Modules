using Microsoft.Build.Construction;

namespace Microsoft.MSBuildProjectBuilder
{
    public partial class ProjectBuilder
    {

        private ProjectPropertyGroupElement CreatePropertyGroup(string condition, string label)
        {
            if (ProjectRoot == null)
            {
                Create();
            }
            ProjectPropertyGroupElement propertyGroup = ProjectRoot.CreatePropertyGroupElement();
            if (!string.IsNullOrWhiteSpace(condition))
            {
                propertyGroup.Condition = condition;
            }
            if (!string.IsNullOrWhiteSpace(label))
            {
                propertyGroup.Label = label;
            }
            return propertyGroup;
        }

        public ProjectBuilder AddPropertyGroup(string condition, string label, out ProjectPropertyGroupElement propertyGroup)
        {
            propertyGroup = CreatePropertyGroup(condition, label);
            ProjectRoot.AppendChild(propertyGroup);
            return this;
        }

        public ProjectBuilder AddPropertyGroupAfterElement(string condition, string label, ProjectElement insertAfterElement, out ProjectPropertyGroupElement propertyGroup)
        {
            propertyGroup = CreatePropertyGroup(condition, label);
            ProjectRoot.InsertAfterChild(propertyGroup, insertAfterElement);
            return this;
        }

        public ProjectBuilder AddPropertyGroupBeforeElement(string condition, string label, ProjectElement insertBeforeElement, out ProjectPropertyGroupElement propertyGroup)
        {
            propertyGroup = CreatePropertyGroup(condition, label);
            ProjectRoot.InsertBeforeChild(propertyGroup, insertBeforeElement);
            return this;
        }
    }
}
