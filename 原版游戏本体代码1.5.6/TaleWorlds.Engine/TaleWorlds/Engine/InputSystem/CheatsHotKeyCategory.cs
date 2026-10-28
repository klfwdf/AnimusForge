using TaleWorlds.InputSystem;

namespace TaleWorlds.Engine.InputSystem;

public class CheatsHotKeyCategory : GameKeyContext
{
	public const string CategoryId = "Cheats";

	public const string MissionScreenHotkeyIncreaseCameraSpeed = "MissionScreenHotkeyIncreaseCameraSpeed";

	public const string MissionScreenHotkeyDecreaseCameraSpeed = "MissionScreenHotkeyDecreaseCameraSpeed";

	public const string ResetCameraSpeed = "ResetCameraSpeed";

	public const string MissionScreenHotkeyIncreaseSlowMotionFactor = "MissionScreenHotkeyIncreaseSlowMotionFactor";

	public const string MissionScreenHotkeyDecreaseSlowMotionFactor = "MissionScreenHotkeyDecreaseSlowMotionFactor";

	public const string EnterSlowMotion = "EnterSlowMotion";

	public const string Pause = "Pause";

	public const string MissionScreenHotkeyHealYourSelf = "MissionScreenHotkeyHealYourSelf";

	public const string MissionScreenHotkeyHealYourHorse = "MissionScreenHotkeyHealYourHorse";

	public const string MissionScreenHotkeyKillEnemyAgent = "MissionScreenHotkeyKillEnemyAgent";

	public const string MissionScreenHotkeyKillAllEnemyAgents = "MissionScreenHotkeyKillAllEnemyAgents";

	public const string MissionScreenHotkeyKillEnemyHorse = "MissionScreenHotkeyKillEnemyHorse";

	public const string MissionScreenHotkeyKillAllEnemyHorses = "MissionScreenHotkeyKillAllEnemyHorses";

	public const string MissionScreenHotkeyKillFriendlyAgent = "MissionScreenHotkeyKillFriendlyAgent";

	public const string MissionScreenHotkeyKillAllFriendlyAgents = "MissionScreenHotkeyKillAllFriendlyAgents";

	public const string MissionScreenHotkeyKillFriendlyHorse = "MissionScreenHotkeyKillFriendlyHorse";

	public const string MissionScreenHotkeyKillAllFriendlyHorses = "MissionScreenHotkeyKillAllFriendlyHorses";

	public const string MissionScreenHotkeyKillYourSelf = "MissionScreenHotkeyKillYourSelf";

	public const string MissionScreenHotkeyKillYourHorse = "MissionScreenHotkeyKillYourHorse";

	public const string MissionScreenHotkeyGhostCam = "MissionScreenHotkeyGhostCam";

	public const string MissionScreenHotkeySwitchAgentToAi = "MissionScreenHotkeySwitchAgentToAi";

	public const string MissionScreenHotkeyControlFollowedAgent = "MissionScreenHotkeyControlFollowedAgent";

	public const string MissionScreenHotkeyTeleportMainAgent = "MissionScreenHotkeyTeleportMainAgent";

	public CheatsHotKeyCategory()
		: base("Cheats", 0)
	{
		RegisterCheatHotkey("Pause", InputKey.F11, HotKey.Modifiers.Control);
		RegisterCheatHotkey("MissionScreenHotkeyIncreaseCameraSpeed", InputKey.Up, HotKey.Modifiers.Control);
		RegisterCheatHotkey("MissionScreenHotkeyDecreaseCameraSpeed", InputKey.Down, HotKey.Modifiers.Control);
		RegisterCheatHotkey("ResetCameraSpeed", InputKey.MiddleMouseButton, HotKey.Modifiers.Control);
		RegisterCheatHotkey("MissionScreenHotkeyIncreaseSlowMotionFactor", InputKey.NumpadPlus, HotKey.Modifiers.Shift);
		RegisterCheatHotkey("MissionScreenHotkeyDecreaseSlowMotionFactor", InputKey.NumpadMinus, HotKey.Modifiers.Shift);
		RegisterCheatHotkey("EnterSlowMotion", InputKey.F9, HotKey.Modifiers.Control);
		RegisterCheatHotkey("MissionScreenHotkeySwitchAgentToAi", InputKey.F5, HotKey.Modifiers.Control);
		RegisterCheatHotkey("MissionScreenHotkeyControlFollowedAgent", InputKey.Numpad5, HotKey.Modifiers.Alt | HotKey.Modifiers.Control);
		RegisterCheatHotkey("MissionScreenHotkeyHealYourSelf", InputKey.H, HotKey.Modifiers.Control, HotKey.Modifiers.Shift);
		RegisterCheatHotkey("MissionScreenHotkeyHealYourHorse", InputKey.H, HotKey.Modifiers.Shift | HotKey.Modifiers.Control);
		RegisterCheatHotkey("MissionScreenHotkeyKillEnemyAgent", InputKey.F4, HotKey.Modifiers.Control);
		RegisterCheatHotkey("MissionScreenHotkeyKillAllEnemyAgents", InputKey.F4, HotKey.Modifiers.Alt | HotKey.Modifiers.Control);
		RegisterCheatHotkey("MissionScreenHotkeyKillEnemyHorse", InputKey.F4, HotKey.Modifiers.Shift | HotKey.Modifiers.Control);
		RegisterCheatHotkey("MissionScreenHotkeyKillAllEnemyHorses", InputKey.F4, HotKey.Modifiers.Shift | HotKey.Modifiers.Alt | HotKey.Modifiers.Control);
		RegisterCheatHotkey("MissionScreenHotkeyKillFriendlyAgent", InputKey.F2, HotKey.Modifiers.Control);
		RegisterCheatHotkey("MissionScreenHotkeyKillAllFriendlyAgents", InputKey.F2, HotKey.Modifiers.Alt | HotKey.Modifiers.Control);
		RegisterCheatHotkey("MissionScreenHotkeyKillFriendlyHorse", InputKey.F2, HotKey.Modifiers.Shift | HotKey.Modifiers.Control);
		RegisterCheatHotkey("MissionScreenHotkeyKillAllFriendlyHorses", InputKey.F2, HotKey.Modifiers.Shift | HotKey.Modifiers.Alt | HotKey.Modifiers.Control);
		RegisterCheatHotkey("MissionScreenHotkeyKillYourSelf", InputKey.F3, HotKey.Modifiers.Control);
		RegisterCheatHotkey("MissionScreenHotkeyKillYourHorse", InputKey.F3, HotKey.Modifiers.Shift | HotKey.Modifiers.Control);
		RegisterCheatHotkey("MissionScreenHotkeyGhostCam", InputKey.K, HotKey.Modifiers.Control);
		RegisterCheatHotkey("MissionScreenHotkeyTeleportMainAgent", InputKey.X, HotKey.Modifiers.Alt);
	}

	private void RegisterCheatHotkey(string id, InputKey hotkeyKey, HotKey.Modifiers modifiers, HotKey.Modifiers negativeModifiers = HotKey.Modifiers.None)
	{
		RegisterHotKey(new HotKey(id, "Cheats", hotkeyKey, modifiers, negativeModifiers));
	}
}
