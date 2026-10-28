using System.Collections.Generic;
using MCM.Abstractions;
using MCM.Abstractions.FluentBuilder;
using MCM.Abstractions.FluentBuilder.Models;
using MCM.Abstractions.Wrapper;
using MCM.Common;

namespace MCM.Implementation.FluentBuilder.Models;

internal sealed class DefaultSettingsPropertyDropdownBuilder : BaseDefaultSettingsPropertyBuilder<ISettingsPropertyDropdownBuilder>, ISettingsPropertyDropdownBuilder, ISettingsPropertyBuilder<ISettingsPropertyDropdownBuilder>, ISettingsPropertyBuilder, IPropertyDefinitionDropdown, IPropertyDefinitionBase
{
	public int SelectedIndex { get; }

	internal DefaultSettingsPropertyDropdownBuilder(string id, string name, int selectedIndex, IRef @ref)
		: base(id, name, @ref)
	{
		base.SettingsPropertyBuilder = this;
		SelectedIndex = selectedIndex;
	}

	public override IEnumerable<IPropertyDefinitionBase> GetDefinitions()
	{
		return new IPropertyDefinitionBase[2]
		{
			new PropertyDefinitionDropdownWrapper(this),
			new PropertyDefinitionWithIdWrapper(this)
		};
	}
}
