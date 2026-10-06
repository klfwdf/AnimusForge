using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.DedicatedCustomServer.ClientHelper;

internal static class ModLogger
{
	public static void Log(string message, int logLevel = 0, Debug.DebugColor color = Debug.DebugColor.Green)
	{
		Debug.Print("DCS Client Helper :: " + message, logLevel, color);
	}

	public static void Warn(string message)
	{
		Log(message, 0, Debug.DebugColor.Yellow);
	}
}
