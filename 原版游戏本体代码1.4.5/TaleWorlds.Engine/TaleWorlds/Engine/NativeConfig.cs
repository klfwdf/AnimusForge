using TaleWorlds.Engine.Options;

namespace TaleWorlds.Engine;

public static class NativeConfig
{
	public static bool CheatMode { get; private set; }

	public static bool IsDevelopmentMode { get; private set; }

	public static bool LocalizationDebugMode { get; private set; }

	public static bool GetUIDebugMode { get; private set; }

	public static bool DisableSound { get; private set; }

	public static bool EnableEditMode { get; private set; }

	public static bool TableauCacheEnabled => EngineApplicationInterface.IConfig.GetTableauCacheMode();

	public static bool DoLocalizationCheckAtStartup => EngineApplicationInterface.IConfig.GetDoLocalizationCheckAtStartup();

	public static bool EnableClothSimulation => EngineApplicationInterface.IConfig.GetEnableClothSimulation();

	public static int CharacterDetail => EngineApplicationInterface.IConfig.GetCharacterDetail();

	public static bool InvertMouse => EngineApplicationInterface.IConfig.GetInvertMouse();

	public static string LastOpenedScene => EngineApplicationInterface.IConfig.GetLastOpenedScene();

	public static int AutoSaveInMinutes => EngineApplicationInterface.IConfig.AutoSaveInMinutes();

	public static bool GetUIDoNotUseGeneratedPrefabs => EngineApplicationInterface.IConfig.GetUIDoNotUseGeneratedPrefabs();

	public static string DebugLoginUsername => EngineApplicationInterface.IConfig.GetDebugLoginUserName();

	public static string DebugLogicPassword => EngineApplicationInterface.IConfig.GetDebugLoginPassword();

	public static bool DisableGuiMessages => EngineApplicationInterface.IConfig.GetDisableGuiMessages();

	public static NativeOptions.ConfigQuality AutoGFXQuality => (NativeOptions.ConfigQuality)EngineApplicationInterface.IConfig.GetAutoGFXQuality();

	public static void OnConfigChanged()
	{
		CheatMode = EngineApplicationInterface.IConfig.GetCheatMode();
		IsDevelopmentMode = EngineApplicationInterface.IConfig.GetDevelopmentMode();
		GetUIDebugMode = EngineApplicationInterface.IConfig.GetUIDebugMode();
		LocalizationDebugMode = EngineApplicationInterface.IConfig.GetLocalizationDebugMode();
		EnableEditMode = EngineApplicationInterface.IConfig.GetEnableEditMode();
		DisableSound = EngineApplicationInterface.IConfig.GetDisableSound();
	}

	public static void SetAutoConfigWrtHardware()
	{
		EngineApplicationInterface.IConfig.SetAutoConfigWrtHardware();
	}
}
