using System.Collections.Generic;
using MCM.Abstractions;
using MCM.Abstractions.FluentBuilder;
using MCM.Abstractions.FluentBuilder.Models;
using MCM.Abstractions.Wrapper;
using MCM.Common;

namespace MCM.Implementation.FluentBuilder.Models;

internal sealed class DefaultSettingsPropertyGroupToggleBuilder : BaseDefaultSettingsPropertyBuilder<ISettingsPropertyGroupToggleBuilder>, ISettingsPropertyGroupToggleBuilder, ISettingsPropertyBuilder<ISettingsPropertyGroupToggleBuilder>, ISettingsPropertyBuilder, IPropertyDefinitionGroupToggle, IPropertyDefinitionBase
{
	public bool IsToggle { get; } = true;

	internal DefaultSettingsPropertyGroupToggleBuilder(string id, string name, IRef @ref)
		: base(id, name, @ref)
	{
		base.SettingsPropertyBuilder = this;
	}

	public override IEnumerable<IPropertyDefinitionBase> GetDefinitions()
	{
		return new IPropertyDefinitionBase[2]
		{
			new PropertyDefinitionGroupToggleWrapper(this),
			new PropertyDefinitionWithIdWrapper(this)
		};
	}
}
