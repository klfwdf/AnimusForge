using System.IO;
using System.Reflection;

namespace TaleWorlds.Library;

public static class BasePath
{
	public static string Name
	{
		get
		{
			if (ApplicationPlatform.CurrentEngine == EngineType.UnrealEngine)
			{
				return Path.GetFullPath(Path.GetDirectoryName(typeof(BasePath).Assembly.Location) + "/../../");
			}
			if (ApplicationPlatform.CurrentPlatform == Platform.Orbis)
			{
				return "/app0/";
			}
			if (ApplicationPlatform.CurrentPlatform == Platform.Durango)
			{
				return "/";
			}
			if (ApplicationPlatform.CurrentPlatform == Platform.Web)
			{
				return Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) + "/../../";
			}
			return "../../";
		}
	}
}
