using System;

namespace Newtonsoft.Json
{
    public enum NullValueHandling
    {
        Include,
        Ignore
    }

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public sealed class JsonPropertyAttribute : Attribute
    {
        public JsonPropertyAttribute(string name)
        {
            Name = name;
        }

        public string Name { get; }
        public NullValueHandling NullValueHandling { get; set; }
    }

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public sealed class JsonIgnoreAttribute : Attribute
    {
    }
}
