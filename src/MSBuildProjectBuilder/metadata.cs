using Microsoft.Build.Construction;

namespace Microsoft.MSBuildProjectBuilder
{
    public partial class ProjectBuilder
    {

        private ProjectMetadataElement CreateMetadata(string name, string value, string condition, string label)
        {
            ProjectMetadataElement metadata = ProjectRoot.CreateMetadataElement(name);
            if (!string.IsNullOrWhiteSpace(value))
            {
                metadata.Value = value;
            }
            if (!string.IsNullOrWhiteSpace(label))
            {
                metadata.Label = label;
            }
            if (!string.IsNullOrWhiteSpace(condition))
            {
                metadata.Condition = condition;
            }
            return metadata;
        }

        public ProjectBuilder AddMetadata(string name, string value, string condition, string label, ProjectItemElement projectItem, out ProjectMetadataElement projectMetadata)
        {
            // We can either create a dummy item to add metdata to if the item doesn't exist or we can handle the error or let it blow up.  Right now opting for it blowing up.
            projectMetadata = CreateMetadata(name, value, condition, label);
            projectItem.AppendChild(projectMetadata);
            return this;
        }

        public ProjectBuilder AddMetadata(string name, string value = null, string condition = null, string label = null, ProjectItemElement item = null)
        {
            ProjectMetadataElement ppe = null;
            AddMetadata(name, value, condition, label, item, out ppe);
            return this;
        }

        public ProjectBuilder AddMetadataAfterMetadataElement(string name, string value, string condition, string label, ProjectMetadataElement insertAfterElement, out ProjectMetadataElement projectMetadata)
        {
            projectMetadata = CreateMetadata(name, value, condition, label);
            insertAfterElement.Parent.InsertAfterChild(projectMetadata, insertAfterElement);
            return this;
        }

        public ProjectBuilder AddMetadataBeforeMetadataElement(string name, string value, string condition, string label, ProjectMetadataElement insertBeforeElement, out ProjectMetadataElement projectMetadata)
        {
            projectMetadata = CreateMetadata(name, value, condition, label);
            insertBeforeElement.Parent.InsertBeforeChild(projectMetadata, insertBeforeElement);
            return this;
        }
    }
}
