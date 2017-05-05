using Microsoft.Build.Construction;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.MSBuildProjectBuilder
{
    public partial class ProjectBuilder
    {
        public ProjectBuilder AddItemGroup()
        {
            // The assumption here is that the groups that this will apply to such as ProjectRootElement, ProjectTargetElement, ProjectWhenElement will only have been added as single items. 
            // Example .AddTarget("Name") and not .AddTarget(new[]{ new Target("Name1"),Target("Name2")})
            // .AddTarget and .AddWhen should not accept an array.  If that changes this will need to be reworked as it only processes the first valid item it finds.
            // If no project is created then create it automatically.
            if (!lastElements.Any())
            {
                Create();
            }

            ProjectElement lE = lastElements.FirstOrDefault();
            do
            {

                Dictionary<Type, Action> @switch = new Dictionary<Type, Action> {
                    { typeof(ProjectRootElement), () => lastElements.Add((lE as ProjectRootElement).AddItemGroup()) },
                    { typeof(ProjectTargetElement), () => lastElements.Add((lE as ProjectTargetElement).AddItemGroup()) },
                };
                Action action;
                if (@switch.TryGetValue(lE.GetType(), out action))
                {
                    lastElements.Clear();
                    action();
                    return this;
                }
                lE = lE.Parent;
            }
            while (lE != null);

            throw new InvalidOperationException("Invalid project object, AddItemGroup failed."); ;
        }
    }
}
