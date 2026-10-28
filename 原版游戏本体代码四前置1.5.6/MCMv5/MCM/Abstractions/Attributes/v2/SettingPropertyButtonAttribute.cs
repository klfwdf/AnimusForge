using System;

namespace MCM.Abstractions.Attributes.v2;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public sealed class SettingPropertyButtonAttribute : BaseSettingPropertyAttribute, IPropertyDefinitionButton, IPropertyDefinitionBase
{
	public string Content { get; set; } = string.Empty;

	public SettingPropertyButtonAttribute(string displayName, int order = -1, bool requireRestart = true, string hintText = "")
		: base(displayName, order, requireRestart, hintText)
	{
	}
}
