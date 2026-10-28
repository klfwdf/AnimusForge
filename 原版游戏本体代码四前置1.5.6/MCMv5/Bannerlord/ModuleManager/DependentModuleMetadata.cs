namespace Bannerlord.ModuleManager;

internal record DependentModuleMetadata
{
	public string Id { get; set; } = string.Empty;

	public LoadType LoadType { get; set; }

	public bool IsOptional { get; set; }

	public bool IsIncompatible { get; set; }

	public ApplicationVersion Version { get; set; } = ApplicationVersion.Empty;

	public ApplicationVersionRange VersionRange { get; set; } = ApplicationVersionRange.Empty;

	public DependentModuleMetadata()
	{
	}

	public DependentModuleMetadata(string id, LoadType loadType, bool isOptional, bool isIncompatible, ApplicationVersion version, ApplicationVersionRange versionRange)
	{
		Id = id;
		LoadType = loadType;
		IsOptional = isOptional;
		IsIncompatible = isIncompatible;
		Version = version;
		VersionRange = versionRange;
	}

	public static string GetLoadType(LoadType loadType)
	{
		if (1 == 0)
		{
		}
		string result = loadType switch
		{
			LoadType.None => "", 
			LoadType.LoadAfterThis => "Before       ", 
			LoadType.LoadBeforeThis => "After        ", 
			_ => "ERROR        ", 
		};
		if (1 == 0)
		{
		}
		return result;
	}

	public static string GetVersion(ApplicationVersion? av)
	{
		return ((object)av != null && av.IsSameWithChangeSet(ApplicationVersion.Empty)) ? "" : $" {av}";
	}

	public static string GetVersionRange(ApplicationVersionRange? avr)
	{
		return (avr == ApplicationVersionRange.Empty) ? "" : $" {avr}";
	}

	public static string GetOptional(bool isOptional)
	{
		return isOptional ? " Optional" : "";
	}

	public static string GetIncompatible(bool isOptional)
	{
		return isOptional ? "Incompatible " : "";
	}

	public override string ToString()
	{
		return GetLoadType(LoadType) + GetIncompatible(IsIncompatible) + Id + GetVersion(Version) + GetVersionRange(VersionRange) + GetOptional(IsOptional);
	}
}
