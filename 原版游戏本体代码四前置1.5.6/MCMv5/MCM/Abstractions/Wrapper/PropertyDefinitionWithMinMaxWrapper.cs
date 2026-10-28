using System;
using HarmonyLib.BUTR.Extensions;

namespace MCM.Abstractions.Wrapper;

public sealed class PropertyDefinitionWithMinMaxWrapper : BasePropertyDefinitionWrapper, IPropertyDefinitionWithMinMax
{
	public decimal MinValue { get; }

	public decimal MaxValue { get; }

	public PropertyDefinitionWithMinMaxWrapper(object @object)
		: base(@object)
	{
		Type type = @object.GetType();
		MinValue = (AccessTools2.Property(type, "MinValue")?.GetValue(@object) as decimal?).GetValueOrDefault();
		MaxValue = (AccessTools2.Property(type, "MaxValue")?.GetValue(@object) as decimal?).GetValueOrDefault();
	}
}
