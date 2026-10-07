namespace MCM.Abstractions.Attributes
{
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class SettingPropertyGroupAttribute : Attribute
    {
        public string Name { get; }
        public SettingPropertyGroupAttribute(string name) => Name = name;
    }
}

namespace MCM.Abstractions.Attributes.v2
{
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class SettingPropertyBoolAttribute : Attribute
    {
        public string Name { get; }
        public int Order { get; set; }
        public bool RequireRestart { get; set; }
        public string HintText { get; set; }
        public SettingPropertyBoolAttribute(string name) => Name = name;
    }
}

namespace MCM.Abstractions.Base.Global
{
    public static class GlobalSettings<T>
    {
        public static T Loaded;
        public static bool ThrowOnRead;
        public static T Instance => ThrowOnRead ? throw new InvalidOperationException("fixture MCM unavailable") : Loaded;
    }
}
