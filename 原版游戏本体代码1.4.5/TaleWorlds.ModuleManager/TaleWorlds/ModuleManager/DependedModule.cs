using TaleWorlds.Library;

namespace TaleWorlds.ModuleManager;

public struct DependedModule
{
	public string ModuleId { get; private set; }

	public ApplicationVersion Version { get; private set; }

	public bool IsOptional { get; private set; }

	public DependedModule(string moduleId, ApplicationVersion version, bool isOptional = false)
	{
		ModuleId = moduleId;
		Version = version;
		IsOptional = isOptional;
	}

	public void UpdateVersionChangeSet()
	{
		Version = new ApplicationVersion(Version.ApplicationVersionType, Version.Major, Version.Minor, Version.Revision, 114896);
	}
}
