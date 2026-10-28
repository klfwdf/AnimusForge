using System.Collections.Generic;
using MCM.Abstractions;
using MCM.Abstractions.FluentBuilder;
using MCM.Abstractions.FluentBuilder.Models;
using MCM.Abstractions.Wrapper;
using MCM.Common;

namespace MCM.Implementation.FluentBuilder.Models;

internal sealed class DefaultSettingsPropertyBoolBuilder : BaseDefaultSettingsPropertyBuilder<ISettingsPropertyBoolBuilder>, ISettingsPropertyBoolBuilder, ISettingsPropertyBuilder<ISettingsPropertyBoolBuilder>, ISettingsPropertyBuilder, IPropertyDefinitionBool, IPropertyDefinitionBase
{
	internal DefaultSettingsPropertyBoolBuilder(string id, string name, IRef @ref)
		: base(id, name, @ref)
	{
		base.SettingsPropertyBuilder = this;
	}

	public override IEnumerable<IPropertyDefinitionBase> GetDefinitions()
	{
		return new IPropertyDefinitionBase[2]
		{
			new PropertyDefinitionBoolWrapper(this),
			new PropertyDefinitionWithIdWrapper(this)
		};
	}
}
