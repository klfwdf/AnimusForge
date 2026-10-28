using System;
using HarmonyLib.BUTR.Extensions;

namespace MCM.Abstractions.Wrapper;

public sealed class PropertyDefinitionWithCustomFormatterWrapper : BasePropertyDefinitionWrapper, IPropertyDefinitionWithCustomFormatter
{
	public Type? CustomFormatter { get; }

	public PropertyDefinitionWithCustomFormatterWrapper(object @object)
		: base(@object)
	{
		Type type = @object.GetType();
		CustomFormatter = AccessTools2.Property(type, "CustomFormatter", logErrorInTrace: false)?.GetValue(@object) as Type;
	}
}
