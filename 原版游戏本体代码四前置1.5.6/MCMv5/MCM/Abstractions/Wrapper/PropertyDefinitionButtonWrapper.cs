using System;
using HarmonyLib.BUTR.Extensions;

namespace MCM.Abstractions.Wrapper;

public sealed class PropertyDefinitionButtonWrapper : BasePropertyDefinitionWrapper, IPropertyDefinitionButton, IPropertyDefinitionBase
{
	public string Content { get; }

	public PropertyDefinitionButtonWrapper(object @object)
		: base(@object)
	{
		Type type = @object.GetType();
		Content = (AccessTools2.Property(type, "Content")?.GetValue(@object) as string) ?? string.Empty;
	}
}
