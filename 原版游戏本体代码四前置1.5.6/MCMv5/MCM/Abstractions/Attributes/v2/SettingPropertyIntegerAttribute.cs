using System;

namespace MCM.Abstractions.Attributes.v2;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public sealed class SettingPropertyIntegerAttribute : BaseSettingPropertyAttribute, IPropertyDefinitionWithMinMax, IPropertyDefinitionWithFormat, IPropertyDefinitionWithCustomFormatter
{
	public decimal MinValue { get; }

	public decimal MaxValue { get; }

	public string ValueFormat { get; }

	public Type? CustomFormatter { get; set; }

	public SettingPropertyIntegerAttribute(string displayName, int minValue, int maxValue, string valueFormat = "0")
		: base(displayName)
	{
		MinValue = minValue;
		MaxValue = maxValue;
		ValueFormat = valueFormat;
	}
}
