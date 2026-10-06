using System;
using TaleWorlds.Engine.InputSystem;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.ModuleManager;
using TaleWorlds.ScreenSystem;

namespace TaleWorlds.Engine;

public static class EngineController
{
	public static event Action ConfigChange;

	public static event Action<bool> OnConstrainedStateChanged;

	public static event Action OnDLCInstalledCallback;

	public static event Action OnDLCLoadedCallback;

	internal static void OnApplicationTick(float dt)
	{
		Input.Update();
		Screen.Update();
	}

	[EngineCallback(null, false)]
	internal static void Initialize()
	{
		IInputContext debugInput = null;
		Input.Initialize(new EngineInputManager(), debugInput);
		Common.PlatformFileHelper = new PlatformFileHelperPC(Utilities.GetApplicationName());
	}

	[EngineCallback(null, false)]
	internal static void OnConfigChange()
	{
		NativeConfig.OnConfigChanged();
		if (EngineController.ConfigChange != null)
		{
			EngineController.ConfigChange();
		}
	}

	[EngineCallback(null, false)]
	internal static void OnConstrainedStateChange(bool isConstrained)
	{
		EngineController.OnConstrainedStateChanged?.Invoke(isConstrained);
	}

	[EngineCallback(null, false)]
	internal static void OnDLCInstalled()
	{
		EngineController.OnDLCInstalledCallback?.Invoke();
	}

	[EngineCallback(null, false)]
	internal static void OnDLCLoaded()
	{
		EngineController.OnDLCLoadedCallback?.Invoke();
	}

	[EngineCallback(null, false)]
	public static string GetVersionStr()
	{
		return ApplicationVersion.FromParametersFile().ToString();
	}

	[EngineCallback(null, false)]
	public static string GetApplicationPlatformName()
	{
		return ApplicationPlatform.CurrentPlatform.ToString();
	}

	[EngineCallback(null, false)]
	public static string GetModulesVersionStr()
	{
		string text = "";
		foreach (ModuleInfo module in ModuleHelper.GetModules())
		{
			text = string.Concat(text, module.Name, "#", module.Version, "\n");
		}
		return text;
	}

	[EngineCallback(null, false)]
	internal static void OnControllerDisconnection()
	{
		ScreenManager.OnControllerDisconnect();
	}
}
