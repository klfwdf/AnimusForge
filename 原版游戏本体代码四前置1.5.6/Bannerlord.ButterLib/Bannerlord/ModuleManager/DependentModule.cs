namespace Bannerlord.ModuleManager;

internal record DependentModule
{
	public string Id { get; set; } = string.Empty;

	public ApplicationVersion Version { get; set; } = ApplicationVersion.Empty;

	public bool IsOptional { get; set; }

	public DependentModule()
	{
	}

	public DependentModule(string id, ApplicationVersion version, bool isOptional)
	{
		Id = id;
		Version = version;
		IsOptional = isOptional;
	}
}
