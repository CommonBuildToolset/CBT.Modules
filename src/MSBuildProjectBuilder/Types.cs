
namespace Microsoft.MSBuildProjectBuilder
{
    public class NameValuePair
    {
        public string Value { get; private set; }
        public string Name { get; private set; }

        public NameValuePair(string name, string value)
        {
            if (value == null)
            {
                value = string.Empty;
            }
            Value = value;
            Name = name;
        }
    }

    public class Item : NameValuePair
    {
        public Item(string name, string value) :
            base(name, value)
        { }
    }

    public class Metadata : NameValuePair
    {
        public Metadata(string name, string value) :
            base(name, value)
        { }
    }

    public class Property : NameValuePair
    {
        public Property(string name, string value) :
            base(name, value)
        { }
    }

}
