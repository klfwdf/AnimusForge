using TaleWorlds.Library;

namespace TaleWorlds.Engine;

public static class EngineFilePaths
{
	public const string ConfigsDirectoryName = "Configs";

	public static PlatformDirectoryPath ConfigsPath => new PlatformDirectoryPath(PlatformFileType.User, "Configs");
}
