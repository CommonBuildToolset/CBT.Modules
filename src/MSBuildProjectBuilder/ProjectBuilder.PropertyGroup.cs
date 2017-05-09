using Microsoft.Build.Construction;
using System;
using System.Collections.Generic;

namespace Microsoft.MSBuildProjectBuilder
{
    public partial class ProjectBuilder
    {
        public ProjectBuilder AddPropertyGroup(string condition = null, string label = null)
        {
            ProjectPropertyGroupElement newPropertyGroup = ProjectRoot.CreatePropertyGroupElement();
            newPropertyGroup.Label = label;
            newPropertyGroup.Condition = condition;

            Dictionary<Type, Action> @switch = new Dictionary<Type, Action> {
                    { typeof(ProjectRootElement), () => (_lastGroupContainer as ProjectRootElement).AppendChild(newPropertyGroup) },
                    { typeof(ProjectTargetElement), () => (_lastGroupContainer as ProjectTargetElement).AppendChild(newPropertyGroup) },
                };

            Action action;
            if (@switch.TryGetValue(_lastGroupContainer.GetType(), out action))
            {
                action();
                _lastPropertyGroupElement = newPropertyGroup;
            }
            return this;
        }
    }
}
