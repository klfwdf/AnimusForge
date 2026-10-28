namespace Bannerlord.ModuleManager;

internal record DependentModule
{
	public string Id { get; set; } = string.Empty;

	public Bannerlord.ModuleManager.ApplicationVersion Version { get; set; } = Bannerlord.ModuleManager.ApplicationVersion.Empty;

	public bool IsOptional { get; set; }

	public DependentModule()
	{
	}

	public DependentModule(string id, Bannerlord.ModuleManager.ApplicationVersion version, bool isOptional)
	{
		Id = id;
		Version = version;
		IsOptional = isOptional;
	}
}
