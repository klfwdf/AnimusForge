using System;

namespace MCM.Abstractions.Wrapper;

public sealed class PropertyGroupDefinitionWrapper : IPropertyGroupDefinition
{
	public string GroupName { get; }

	public int GroupOrder { get; }

	public PropertyGroupDefinitionWrapper(object @object)
	{
		Type type = @object.GetType();
		GroupName = (type.GetProperty("GroupName")?.GetValue(@object) as string) ?? "ERROR";
		GroupOrder = (type.GetProperty("GroupOrder")?.GetValue(@object) as int?) ?? (-1);
	}
}
