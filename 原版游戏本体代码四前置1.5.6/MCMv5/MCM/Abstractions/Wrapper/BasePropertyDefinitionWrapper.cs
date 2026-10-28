using System;

namespace MCM.Abstractions.Wrapper;

public abstract class BasePropertyDefinitionWrapper : IPropertyDefinitionBase
{
	public string DisplayName { get; }

	public int Order { get; }

	public bool RequireRestart { get; }

	public string HintText { get; }

	protected BasePropertyDefinitionWrapper(object @object)
	{
		Type type = @object.GetType();
		DisplayName = (type.GetProperty("DisplayName")?.GetValue(@object) as string) ?? "ERROR";
		Order = (type.GetProperty("Order")?.GetValue(@object) as int?) ?? (-1);
		RequireRestart = (type.GetProperty("RequireRestart")?.GetValue(@object) as bool?) ?? true;
		HintText = (type.GetProperty("HintText")?.GetValue(@object) as string) ?? "ERROR";
	}
}
