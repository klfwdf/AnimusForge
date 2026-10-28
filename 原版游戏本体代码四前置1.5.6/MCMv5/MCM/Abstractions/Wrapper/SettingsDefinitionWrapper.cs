namespace MCM.Abstractions.Wrapper;

public sealed class SettingsDefinitionWrapper : SettingsDefinition
{
	private static string? GetSettingsId(object @object)
	{
		return @object.GetType().GetProperty("SettingsId")?.GetValue(@object) as string;
	}

	public SettingsDefinitionWrapper(object @object)
		: base(GetSettingsId(@object) ?? "ERROR")
	{
	}
}
