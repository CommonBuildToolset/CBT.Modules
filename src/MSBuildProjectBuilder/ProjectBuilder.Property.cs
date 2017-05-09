using Microsoft.Build.Construction;

namespace Microsoft.MSBuildProjectBuilder
{
    public partial class ProjectBuilder
    {
        public ProjectBuilder AddProperty(params Property[] properties)
        {
            if (properties == null || properties.Length == 0)
            {
                // if call .AddProperty() but not specifying a property just return;
                return this;
            }

            // If no property group is created then create it automatically.
            if (_lastPropertyGroupElement == null)
            {
                AddPropertyGroup();
            }
            _lastPropertyElements.Clear();
            foreach (var property in properties)
            {
                ProjectPropertyElement projectProperty = ProjectRoot.CreatePropertyElement(property.Name);
                projectProperty.Value = property.Value;
                projectProperty.Label = property.Label;
                projectProperty.Condition = property.Condition;

                _lastPropertyGroupElement.AppendChild(projectProperty);
                _lastPropertyElements.Add(projectProperty);
            }
            return this;
        }
    }
}
