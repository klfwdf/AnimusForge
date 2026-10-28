using System.Collections.Generic;
using MCM.Abstractions;
using MCM.Abstractions.FluentBuilder;
using MCM.Common;

namespace MCM.Implementation.FluentBuilder.Models;

internal abstract class BaseDefaultSettingsPropertyBuilder<TSettingsPropertyBuilder> : ISettingsPropertyBuilder<TSettingsPropertyBuilder>, ISettingsPropertyBuilder, IPropertyDefinitionBase, IPropertyDefinitionWithId where TSettingsPropertyBuilder : ISettingsPropertyBuilder
{
	protected TSettingsPropertyBuilder SettingsPropertyBuilder { get; set; } = default(TSettingsPropertyBuilder);

	public string Name { get; }

	public string Id { get; }

	public IRef PropertyReference { get; }

	public string DisplayName => Name;

	public int Order { get; private set; }

	public bool RequireRestart { get; private set; }

	public string HintText { get; private set; } = string.Empty;

	protected BaseDefaultSettingsPropertyBuilder(string id, string name, IRef @ref)
	{
		Id = id;
		Name = name;
		PropertyReference = @ref;
	}

	public TSettingsPropertyBuilder SetOrder(int value)
	{
		Order = value;
		return SettingsPropertyBuilder;
	}

	public TSettingsPropertyBuilder SetRequireRestart(bool value)
	{
		RequireRestart = value;
		return SettingsPropertyBuilder;
	}

	public TSettingsPropertyBuilder SetHintText(string value)
	{
		HintText = value;
		return SettingsPropertyBuilder;
	}

	public abstract IEnumerable<IPropertyDefinitionBase> GetDefinitions();
}
