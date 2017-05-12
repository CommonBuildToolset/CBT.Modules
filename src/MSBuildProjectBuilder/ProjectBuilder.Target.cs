using Microsoft.Build.Construction;
using System;
using System.Collections.Generic;

namespace Microsoft.MSBuildProjectBuilder
{
    public partial class ProjectBuilder
    {
        public ProjectBuilder AddTarget(string name, string condition = null, string label = null, string afterTargets = null, string beforeTargets = null, string dependsOnTargets = null, string inputs = null, string outputs = null, bool keepDuplicates = false)
        {
            ProjectTargetElement newTarget = ProjectRoot.CreateTargetElement(name);
            newTarget.AfterTargets = afterTargets ?? string.Empty;
            newTarget.BeforeTargets = beforeTargets ?? string.Empty;
            newTarget.DependsOnTargets = dependsOnTargets ?? string.Empty;
            newTarget.Inputs = inputs ?? string.Empty;
            newTarget.Outputs = outputs ?? string.Empty;
            newTarget.KeepDuplicateOutputs = (keepDuplicates)? "true" : string.Empty;
            newTarget.Label = label ?? string.Empty;
            newTarget.Condition = condition ?? string.Empty;

            ProjectRoot.AppendChild(newTarget);
            _lastGroupContainer = newTarget;
            _lastTargetElement = newTarget;
            _lastPropertyGroupElement = null;
            _lastItemGroupElement = null;
            return this;
        }

        public ProjectBuilder ExitTarget()
        {
            if (_lastGroupContainer is ProjectTargetElement)
            {
                _lastItemGroupElement = null;
                _lastPropertyGroupElement = null;
                _lastGroupContainer = ProjectRoot;
            }
            return this;
        }
    }
}
