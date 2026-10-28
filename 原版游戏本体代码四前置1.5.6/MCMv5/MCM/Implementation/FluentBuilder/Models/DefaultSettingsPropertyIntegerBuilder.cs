using System;
using System.Collections.Generic;
using MCM.Abstractions;
using MCM.Abstractions.FluentBuilder;
using MCM.Abstractions.FluentBuilder.Models;
using MCM.Abstractions.Wrapper;
using MCM.Common;

namespace MCM.Implementation.FluentBuilder.Models;

internal sealed class DefaultSettingsPropertyIntegerBuilder : BaseDefaultSettingsPropertyBuilder<ISettingsPropertyIntegerBuilder>, ISettingsPropertyIntegerBuilder, ISettingsPropertyBuilder<ISettingsPropertyIntegerBuilder>, ISettingsPropertyBuilder, IPropertyDefinitionWithMinMax, IPropertyDefinitionWithFormat, IPropertyDefinitionWithActionFormat
{
	public decimal MinValue { get; }

	public decimal MaxValue { get; }

	public string ValueFormat { get; private set; } = string.Empty;

	public Func<object, string>? ValueFormatFunc { get; private set; }

	internal DefaultSettingsPropertyIntegerBuilder(string id, string name, int minValue, int maxValue, IRef @ref)
		: base(id, name, @ref)
	{
		base.SettingsPropertyBuilder = this;
		MinValue = minValue;
		MaxValue = maxValue;
	}

	public ISettingsPropertyBuilder AddValueFormat(string value)
	{
		if (ValueFormatFunc != null)
		{
			throw new InvalidOperationException("AddActionValueFormat was already called!");
		}
		ValueFormat = value;
		return base.SettingsPropertyBuilder;
	}

	public override IEnumerable<IPropertyDefinitionBase> GetDefinitions()
	{
		return new IPropertyDefinitionBase[5]
		{
			new PropertyDefinitionWithMinMaxWrapper(this),
			new PropertyDefinitionWithFormatWrapper(this),
			new PropertyDefinitionWithActionFormatWrapper(this),
			new PropertyDefinitionWithCustomFormatterWrapper(this),
			new PropertyDefinitionWithIdWrapper(this)
		};
	}
}
