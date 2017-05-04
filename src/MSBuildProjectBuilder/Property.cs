using Microsoft.Build.Construction;

namespace Microsoft.MSBuildProjectBuilder
{
    public partial class ProjectBuilder
    {

        private ProjectPropertyElement CreateProperty(string name, string value, string condition, string label)
        {
            ProjectPropertyElement property = ProjectRoot.CreatePropertyElement(name);
            if (!string.IsNullOrWhiteSpace(value))
            {
                property.Value = value;
            }
            if (!string.IsNullOrWhiteSpace(label))
            {
                property.Label = label;
            }
            if (!string.IsNullOrWhiteSpace(condition))
            {
                property.Condition = condition;
            }
            return property;
        }

        public ProjectBuilder AddProperty(string name, string value, string condition, string label, ProjectPropertyGroupElement projectPropertyGroup, out ProjectPropertyElement projectProperty)
        {
            if (projectPropertyGroup == null)
            {
                AddPropertyGroup(null, null, out projectPropertyGroup);
            }
            projectProperty = CreateProperty(name, value, condition, label);
            projectPropertyGroup.AppendChild(projectProperty);
            return this;
        }

        public ProjectBuilder AddProperty(string name, string value = null, string condition = null, string label = null, ProjectPropertyGroupElement propertyGroup = null)
        {
            ProjectPropertyElement ppe = null;
            AddProperty(name, value, condition, label, propertyGroup, out ppe);
            return this;
        }

        public ProjectBuilder AddPropertyAfterPropertyElement(string name, string value, string condition, string label, ProjectPropertyElement insertAfterElement, out ProjectPropertyElement projectProperty)
        {
            projectProperty = CreateProperty(name, value, condition, label);
            insertAfterElement.Parent.InsertAfterChild(projectProperty, insertAfterElement);
            return this;
        }

        public ProjectBuilder AddPropertyBeforePropertyElement(string name, string value, string condition, string label, ProjectPropertyElement insertBeforeElement, out ProjectPropertyElement projectProperty)
        {
            projectProperty = CreateProperty(name, value, condition, label);
            insertBeforeElement.Parent.InsertBeforeChild(projectProperty, insertBeforeElement);
            return this;
        }
    }
}
