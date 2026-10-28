using TaleWorlds.Library;

namespace TaleWorlds.Core;

public static class FilePaths
{
	public const string SaveDirectoryName = "Game Saves";

	public const string RecordingsDirectoryName = "Recordings";

	public const string StatisticsDirectoryName = "Statistics";

	public static PlatformDirectoryPath SavePath => new PlatformDirectoryPath(PlatformFileType.User, "Game Saves");

	public static PlatformDirectoryPath RecordingsPath => new PlatformDirectoryPath(PlatformFileType.User, "Recordings");

	public static PlatformDirectoryPath StatisticsPath => new PlatformDirectoryPath(PlatformFileType.User, "Statistics");
}
