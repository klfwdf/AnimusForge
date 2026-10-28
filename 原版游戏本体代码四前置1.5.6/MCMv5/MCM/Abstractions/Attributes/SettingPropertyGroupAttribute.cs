using System;

namespace MCM.Abstractions.Attributes;

[AttributeUsage(AttributeTargets.Property)]
public sealed class SettingPropertyGroupAttribute : Attribute, IPropertyGroupDefinition
{
	public static IPropertyGroupDefinition Default => new SettingPropertyGroupAttribute(SettingsPropertyGroupDefinition.DefaultGroupName);

	public string GroupName { get; }

	public int GroupOrder { get; set; }

	public SettingPropertyGroupAttribute(string groupName)
	{
		GroupName = groupName;
	}
}
