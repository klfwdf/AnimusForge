namespace TaleWorlds.Engine;

public static class LoadingWindow
{
	public static bool IsLoadingWindowActive { get; private set; }

	public static ILoadingWindowManager LoadingWindowManager { get; private set; }

	static LoadingWindow()
	{
	}

	public static void InitializeWith<T>() where T : class, ILoadingWindowManager, new()
	{
		Destroy();
		LoadingWindowManager = new T();
		LoadingWindowManager.Initialize();
		if (IsLoadingWindowActive)
		{
			LoadingWindowManager.EnableLoadingWindow();
		}
	}

	public static void Destroy()
	{
		LoadingWindowManager?.DisableLoadingWindow();
		LoadingWindowManager?.Destroy();
		LoadingWindowManager = null;
	}

	public static void DisableGlobalLoadingWindow()
	{
		if (LoadingWindowManager != null)
		{
			if (IsLoadingWindowActive)
			{
				LoadingWindowManager.DisableLoadingWindow();
				Utilities.DisableGlobalLoadingWindow();
				Utilities.OnLoadingWindowDisabled();
			}
			IsLoadingWindowActive = false;
			Utilities.DebugSetGlobalLoadingWindowState(newState: false);
		}
	}

	public static void EnableGlobalLoadingWindow()
	{
		if (LoadingWindowManager != null)
		{
			IsLoadingWindowActive = true;
			Utilities.DebugSetGlobalLoadingWindowState(newState: true);
			if (IsLoadingWindowActive)
			{
				LoadingWindowManager.EnableLoadingWindow();
				Utilities.OnLoadingWindowEnabled();
			}
		}
	}

	public static void SetCurrentModeIsMultiplayer(bool isMultiplayer)
	{
		LoadingWindowManager?.SetCurrentModeIsMultiplayer(isMultiplayer);
	}
}
