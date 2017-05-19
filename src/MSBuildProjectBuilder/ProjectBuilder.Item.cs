using System.IO;
using Microsoft.Build.Construction;
using System.Linq;

namespace Microsoft.MSBuildProjectBuilder
{
    public partial class ProjectBuilder
    {

        public ProjectBuilder AddItem(params Item[] items)
        {
            if (items == null || items.Length == 0)
            {
                // if call .AddItem() but not specifying an item just return;
                return this;
            }

            // If no item group is created then create it automatically.
            if (_lastItemGroupElement == null)
            {
                AddItemGroup();
            }
            _lastItemElements.Clear();
            foreach (var item in items)
            {
                ProjectItemElement projectItem = ProjectRoot.CreateItemElement(item.Name);
                projectItem.Include = item.Value;
                projectItem.Label = item.Label;
                projectItem.Condition = item.Condition;

                _lastItemGroupElement.AppendChild(projectItem);
                if (item.Metadata != null && item.Metadata.Length > 0)
                {
                    AddMetadataToItem(projectItem, item.Metadata);
                }
                _lastItemElements.Add(projectItem);
            }
            return this;
        }

        public ProjectBuilder WithItemMetadata(params ItemMetadata[] metadatas)
        {
            foreach (var pI in _lastItemElements.AsEnumerable())
            {
                AddMetadataToItem(pI, metadatas);
            }
            return this;
        }

        public ProjectBuilder AddProjectReference(params ProjectBuilder[] projects)
        {
            foreach (ProjectBuilder project in projects)
            {
                AddItem(new Item("ProjectReference", project.FullPath))
                    .WithItemMetadata($"Name={Path.GetFileNameWithoutExtension(project.FullPath)}");
            }

            return this;
        }

        private void AddMetadataToItem(ProjectItemElement item, ItemMetadata[] metadata)
        {
            foreach (var meta in metadata)
            {
                ProjectMetadataElement metaDataElement = ProjectRoot.CreateMetadataElement(meta.Name);
                metaDataElement.Value = meta.Value;
                metaDataElement.Condition = meta.Condition;
                metaDataElement.Label = meta.Label;
                item.AppendChild(metaDataElement);
            }
        }
    }
}
