using System.Collections.Generic;
using MCM.Abstractions;
using MCM.Abstractions.FluentBuilder;
using MCM.Abstractions.FluentBuilder.Models;
using MCM.Abstractions.Wrapper;
using MCM.Common;

namespace MCM.Implementation.FluentBuilder.Models;

internal sealed class DefaultSettingsPropertyButtonBuilder : BaseDefaultSettingsPropertyBuilder<ISettingsPropertyButtonBuilder>, ISettingsPropertyButtonBuilder, ISettingsPropertyBuilder<ISettingsPropertyButtonBuilder>, ISettingsPropertyBuilder, IPropertyDefinitionButton, IPropertyDefinitionBase
{
	public string Content { get; }

	internal DefaultSettingsPropertyButtonBuilder(string id, string name, IRef @ref, string content)
		: base(id, name, @ref)
	{
		base.SettingsPropertyBuilder = this;
		Content = content;
	}

	public override IEnumerable<IPropertyDefinitionBase> GetDefinitions()
	{
		return new IPropertyDefinitionBase[2]
		{
			new PropertyDefinitionButtonWrapper(this),
			new PropertyDefinitionWithIdWrapper(this)
		};
	}
}
