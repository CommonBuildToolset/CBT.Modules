using Microsoft.Build.Construction;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.MSBuildProjectBuilder
{
    public partial class ProjectBuilder
    {
        public ProjectBuilder AddMetadata(params Metadata[] metadatas)
        {

            if (metadatas == null || metadatas.Length == 0)
            {
                throw new ArgumentException("At least one metadata must be specified to be added.");
            }

            ICollection<ProjectElement> elementsToModify = lastElements;
            // Test last elements if item then process list.  If not item then walk up to find first item then process.  Example scenario: .metadata(...).metadata(...)
            // since lastElements will contain elements of all the same type grab any of them to test.
            var lastItem = elementsToModify.FirstOrDefault();
            if ((lastItem is ProjectItemElement) == false)
            {
                while (lastItem != null && (lastItem is ProjectItemElement) == false)
                {
                    lastItem = lastItem.Parent;
                }
                if (lastItem == null)
                {
                    throw new InvalidOperationException("AddMetadata may only be called on a type that has an ancestor of Item.");
                }
                elementsToModify = new List<ProjectElement>() { lastItem };
            }

            ICollection<ProjectElement> metadataElements = new List<ProjectElement>();
            foreach (var projectElement in elementsToModify.AsEnumerable())
            {
                ProjectItemElement projectItemElement= (projectElement as ProjectItemElement);
                if (projectItemElement != null)
                {
                    foreach (var meta in metadatas)
                    {
                        metadataElements.Add(projectItemElement.AddMetadata(meta.Name, meta.Value));
                    }
                }
                else
                {
                    // elementsToModify should only contain ItemElements so this should not be reached.  If not something went horribly wrong.
                    throw new InvalidOperationException("AddMetadata may only be called on a type that has an ancestor of Item.");
                }
            }
            lastElements = metadataElements;

            return this;
        }
    }
}
