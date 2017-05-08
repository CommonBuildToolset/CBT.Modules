using Microsoft.Build.Construction;
using System;
using System.Collections.Generic;

namespace Microsoft.MSBuildProjectBuilder
{
    public partial class ProjectBuilder
    {

        public ProjectBuilder AddItemGroup(string condition = null, string label = null)
        {
            // If no project is created then create it automatically.
            if (ProjectRoot == null)
            {
                Create();
            }
            ProjectItemGroupElement newItemGroup = ProjectRoot.CreateItemGroupElement();
            newItemGroup.Label = label;
            newItemGroup.Condition = condition;

            Dictionary<Type, Action> @switch = new Dictionary<Type, Action> {
                    { typeof(ProjectRootElement), () => (_lastGroupContainer as ProjectRootElement).AppendChild(newItemGroup) },
                    { typeof(ProjectTargetElement), () => (_lastGroupContainer as ProjectTargetElement).AppendChild(newItemGroup) },
                };

            Action action;
            if (@switch.TryGetValue(_lastGroupContainer.GetType(), out action))
            {
                action();
                _lastItemGroupElement = newItemGroup;
            }
            return this;
        }
    }
}
