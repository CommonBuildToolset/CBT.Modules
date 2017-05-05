
namespace Microsoft.MSBuildProjectBuilder
{
    public partial class ProjectBuilder
    {
        public ProjectBuilder WithLabel(string label)
        {
            foreach (var element in lastElements)
            {
                element.Label = label;
            }
            return this;
        }

        public ProjectBuilder WithCondition(string condition)
        {
            foreach (var element in lastElements)
            {
                element.Condition = condition;
            }
            return this;
        }
    }
}
