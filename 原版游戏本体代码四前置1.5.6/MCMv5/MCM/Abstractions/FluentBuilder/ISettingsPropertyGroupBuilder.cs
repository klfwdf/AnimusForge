using System;
using System.Collections.Generic;
using MCM.Abstractions.FluentBuilder.Models;
using MCM.Common;

namespace MCM.Abstractions.FluentBuilder;

public interface ISettingsPropertyGroupBuilder
{
	Dictionary<string, ISettingsPropertyBuilder> Properties { get; }

	ISettingsPropertyGroupBuilder SetGroupOrder(int value);

	ISettingsPropertyGroupBuilder AddToggle(string id, string name, IRef @ref, Action<ISettingsPropertyGroupToggleBuilder>? builder);

	ISettingsPropertyGroupBuilder AddBool(string id, string name, IRef @ref, Action<ISettingsPropertyBoolBuilder>? builder);

	ISettingsPropertyGroupBuilder AddDropdown(string id, string name, int selectedIndex, IRef @ref, Action<ISettingsPropertyDropdownBuilder>? builder);

	ISettingsPropertyGroupBuilder AddInteger(string id, string name, int minValue, int maxValue, IRef @ref, Action<ISettingsPropertyIntegerBuilder>? builder);

	ISettingsPropertyGroupBuilder AddFloatingInteger(string id, string name, float minValue, float maxValue, IRef @ref, Action<ISettingsPropertyFloatingIntegerBuilder>? builder);

	ISettingsPropertyGroupBuilder AddText(string id, string name, IRef @ref, Action<ISettingsPropertyTextBuilder>? builder);

	ISettingsPropertyGroupBuilder AddButton(string id, string name, IRef @ref, string content, Action<ISettingsPropertyButtonBuilder>? builder);

	ISettingsPropertyGroupBuilder AddCustom<TSettingsPropertyBuilder>(ISettingsPropertyBuilder<TSettingsPropertyBuilder> builder) where TSettingsPropertyBuilder : ISettingsPropertyBuilder;

	IPropertyGroupDefinition GetPropertyGroupDefinition();
}
