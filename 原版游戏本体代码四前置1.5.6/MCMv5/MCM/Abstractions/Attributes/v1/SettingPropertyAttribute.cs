using System;

namespace MCM.Abstractions.Attributes.v1;

[AttributeUsage(AttributeTargets.Property)]
public sealed class SettingPropertyAttribute : BaseSettingPropertyAttribute, IPropertyDefinitionWithMinMax
{
	public decimal MinValue { get; }

	public decimal MaxValue { get; }

	public SettingPropertyAttribute(string displayName)
		: base(displayName)
	{
	}

	public SettingPropertyAttribute(string displayName, float minValue, float maxValue)
		: base(displayName)
	{
		MinValue = (decimal)minValue;
		MaxValue = (decimal)maxValue;
	}

	public SettingPropertyAttribute(string displayName, int minValue, int maxValue)
		: base(displayName)
	{
		MinValue = minValue;
		MaxValue = maxValue;
	}

	public SettingPropertyAttribute(string displayName, decimal minValue, decimal maxValue)
		: base(displayName)
	{
		MinValue = minValue;
		MaxValue = maxValue;
	}
}
