using System;
using HarmonyLib.BUTR.Extensions;

namespace MCM.Abstractions.Wrapper;

public sealed class PropertyDefinitionWithFormatWrapper : BasePropertyDefinitionWrapper, IPropertyDefinitionWithFormat
{
	public string ValueFormat { get; }

	public PropertyDefinitionWithFormatWrapper(object @object)
		: base(@object)
	{
		Type type = @object.GetType();
		ValueFormat = (AccessTools2.Property(type, "ValueFormat")?.GetValue(@object) as string) ?? string.Empty;
	}
}
