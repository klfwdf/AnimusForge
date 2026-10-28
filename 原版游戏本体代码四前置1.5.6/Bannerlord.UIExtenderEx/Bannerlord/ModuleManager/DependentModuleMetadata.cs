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
		return loadType switch
		{
			LoadType.None => "", 
			LoadType.LoadAfterThis => "Before       ", 
			LoadType.LoadBeforeThis => "After        ", 
			_ => "ERROR        ", 
		};
	}

	public static string GetVersion(ApplicationVersion? av)
	{
		if ((object)av == null || !av.IsSameWithChangeSet(ApplicationVersion.Empty))
		{
			return $" {av}";
		}
		return "";
	}

	public static string GetVersionRange(ApplicationVersionRange? avr)
	{
		if (!(avr == ApplicationVersionRange.Empty))
		{
			return $" {avr}";
		}
		return "";
	}

	public static string GetOptional(bool isOptional)
	{
		if (!isOptional)
		{
			return "";
		}
		return " Optional";
	}

	public static string GetIncompatible(bool isOptional)
	{
		if (!isOptional)
		{
			return "";
		}
		return "Incompatible ";
	}

	public override string ToString()
	{
		return GetLoadType(LoadType) + GetIncompatible(IsIncompatible) + Id + GetVersion(Version) + GetVersionRange(VersionRange) + GetOptional(IsOptional);
	}
}
