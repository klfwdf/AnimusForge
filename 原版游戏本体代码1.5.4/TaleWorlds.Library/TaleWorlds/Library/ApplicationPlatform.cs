namespace TaleWorlds.Library;

public static class ApplicationPlatform
{
	public static EngineType CurrentEngine { get; private set; }

	public static Platform CurrentPlatform { get; private set; }

	public static Runtime CurrentRuntimeLibrary { get; private set; }

	public static void Initialize(EngineType engineType, Platform currentPlatform, Runtime currentRuntimeLibrary)
	{
		CurrentEngine = engineType;
		CurrentPlatform = currentPlatform;
		CurrentRuntimeLibrary = currentRuntimeLibrary;
	}

	public static bool IsPlatformWindows()
	{
		if (CurrentPlatform != Platform.WindowsEpic && CurrentPlatform != Platform.WindowsNoPlatform && CurrentPlatform != Platform.WindowsSteam && CurrentPlatform != Platform.WindowsGOG)
		{
			return CurrentPlatform == Platform.GDKDesktop;
		}
		return true;
	}

	public static bool IsPlatformConsole()
	{
		if (CurrentPlatform != Platform.Orbis)
		{
			return CurrentPlatform == Platform.Durango;
		}
		return true;
	}
}
