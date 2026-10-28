using System;

namespace MCM.Abstractions.Attributes.v2;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public sealed class SettingPropertyFloatingIntegerAttribute : BaseSettingPropertyAttribute, IPropertyDefinitionWithMinMax, IPropertyDefinitionWithFormat, IPropertyDefinitionWithCustomFormatter
{
	public decimal MinValue { get; }

	public decimal MaxValue { get; }

	public string ValueFormat { get; }

	public Type? CustomFormatter { get; set; }

	public SettingPropertyFloatingIntegerAttribute(string displayName, float minValue, float maxValue, string valueFormat = "0.00")
		: base(displayName)
	{
		MinValue = (decimal)minValue;
		MaxValue = (decimal)maxValue;
		ValueFormat = valueFormat;
	}
}
