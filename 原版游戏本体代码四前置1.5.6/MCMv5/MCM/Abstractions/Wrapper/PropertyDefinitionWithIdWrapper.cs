using System;
using HarmonyLib.BUTR.Extensions;

namespace MCM.Abstractions.Wrapper;

public sealed class PropertyDefinitionWithIdWrapper : BasePropertyDefinitionWrapper, IPropertyDefinitionWithId
{
	public string Id { get; }

	public PropertyDefinitionWithIdWrapper(object @object)
		: base(@object)
	{
		Type type = @object.GetType();
		Id = (AccessTools2.Property(type, "Id")?.GetValue(@object) as string) ?? string.Empty;
	}
}
