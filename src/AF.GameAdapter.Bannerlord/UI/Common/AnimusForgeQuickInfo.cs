using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace AnimusForge;

internal static class AnimusForgeQuickInfo
{
	private const int TargetDurationMs = 3000;
	private const int NativeBaseDurationMs = 1000;
	private const int ExtraTimeInMs = TargetDurationMs - NativeBaseDurationMs;

	public static void Show(string message, BasicCharacterObject announcerCharacter = null, Equipment equipment = null, string soundEventPath = "")
	{
		ShowForDuration(message, TargetDurationMs, announcerCharacter, equipment, soundEventPath);
	}

	public static void Show(TextObject message, BasicCharacterObject announcerCharacter = null, Equipment equipment = null, string soundEventPath = "")
	{
		ShowForDuration(message, TargetDurationMs, announcerCharacter, equipment, soundEventPath);
	}

	public static void ShowForDuration(string message, int targetDurationMs, BasicCharacterObject announcerCharacter = null, Equipment equipment = null, string soundEventPath = "")
	{
		if (string.IsNullOrWhiteSpace(message))
		{
			return;
		}
		ShowForDuration(new TextObject(message), targetDurationMs, announcerCharacter, equipment, soundEventPath);
	}

	public static void ShowForDuration(TextObject message, int targetDurationMs, BasicCharacterObject announcerCharacter = null, Equipment equipment = null, string soundEventPath = "")
	{
		if (message == null)
		{
			return;
		}
		int extraTimeInMs = System.Math.Max(0, targetDurationMs - NativeBaseDurationMs);
		MBInformationManager.AddQuickInformation(message, extraTimeInMs, announcerCharacter, equipment, soundEventPath);
	}
}
