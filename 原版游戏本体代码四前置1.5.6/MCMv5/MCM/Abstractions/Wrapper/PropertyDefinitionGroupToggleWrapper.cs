using System;
using HarmonyLib.BUTR.Extensions;

namespace MCM.Abstractions.Wrapper;

public sealed class PropertyDefinitionGroupToggleWrapper : BasePropertyDefinitionWrapper, IPropertyDefinitionGroupToggle, IPropertyDefinitionBase
{
	public bool IsToggle { get; }

	public PropertyDefinitionGroupToggleWrapper(object @object)
		: base(@object)
	{
		Type type = @object.GetType();
		IsToggle = AccessTools2.Property(type, "IsToggle")?.GetValue(@object) as bool? == true;
	}
}
