namespace Bannerlord.ModuleManager;

internal record DependentModuleMetadata
{
	public string Id { get; set; } = string.Empty;

	public Bannerlord.ModuleManager.LoadType LoadType { get; set; }

	public bool IsOptional { get; set; }

	public bool IsIncompatible { get; set; }

	public Bannerlord.ModuleManager.ApplicationVersion Version { get; set; } = Bannerlord.ModuleManager.ApplicationVersion.Empty;

	public Bannerlord.ModuleManager.ApplicationVersionRange VersionRange { get; set; } = Bannerlord.ModuleManager.ApplicationVersionRange.Empty;

	public DependentModuleMetadata()
	{
	}

	public DependentModuleMetadata(string id, Bannerlord.ModuleManager.LoadType loadType, bool isOptional, bool isIncompatible, Bannerlord.ModuleManager.ApplicationVersion version, Bannerlord.ModuleManager.ApplicationVersionRange versionRange)
	{
		Id = id;
		LoadType = loadType;
		IsOptional = isOptional;
		IsIncompatible = isIncompatible;
		Version = version;
		VersionRange = versionRange;
	}

	public static string GetLoadType(Bannerlord.ModuleManager.LoadType loadType)
	{
		return loadType switch
		{
			Bannerlord.ModuleManager.LoadType.None => "", 
			Bannerlord.ModuleManager.LoadType.LoadAfterThis => "Before       ", 
			Bannerlord.ModuleManager.LoadType.LoadBeforeThis => "After        ", 
			_ => "ERROR        ", 
		};
	}

	public static string GetVersion(Bannerlord.ModuleManager.ApplicationVersion? av)
	{
		if ((object)av == null || !av.IsSameWithChangeSet(Bannerlord.ModuleManager.ApplicationVersion.Empty))
		{
			return $" {av}";
		}
		return "";
	}

	public static string GetVersionRange(Bannerlord.ModuleManager.ApplicationVersionRange? avr)
	{
		if (!(avr == Bannerlord.ModuleManager.ApplicationVersionRange.Empty))
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
