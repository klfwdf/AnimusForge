using System;

namespace MCM.Abstractions.Attributes;

public abstract class BaseSettingPropertyAttribute : Attribute, IPropertyDefinitionBase
{
	public string DisplayName { get; }

	public int Order { get; set; }

	public bool RequireRestart { get; set; }

	public string HintText { get; set; }

	protected BaseSettingPropertyAttribute(string displayName, int order = -1, bool requireRestart = true, string hintText = "")
	{
		DisplayName = displayName;
		Order = order;
		RequireRestart = requireRestart;
		HintText = hintText;
	}
}
