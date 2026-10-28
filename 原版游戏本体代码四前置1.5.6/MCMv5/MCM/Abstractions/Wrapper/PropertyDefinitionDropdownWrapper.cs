using System;
using HarmonyLib.BUTR.Extensions;

namespace MCM.Abstractions.Wrapper;

public sealed class PropertyDefinitionDropdownWrapper : BasePropertyDefinitionWrapper, IPropertyDefinitionDropdown, IPropertyDefinitionBase
{
	public int SelectedIndex { get; }

	public PropertyDefinitionDropdownWrapper(object @object)
		: base(@object)
	{
		Type type = @object.GetType();
		SelectedIndex = (AccessTools2.Property(type, "SelectedIndex")?.GetValue(@object) as int?).GetValueOrDefault();
	}
}
