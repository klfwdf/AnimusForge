using TaleWorlds.Core;
using TaleWorlds.InputSystem;
using TaleWorlds.Localization;

namespace TaleWorlds.MountAndBlade;

public static class GameKeyTextExtensions
{
	public static TextObject GetHotKeyGameText(this GameTextManager gameTextManager, string categoryName, string hotKeyId)
	{
		return gameTextManager.GetHotKeyGameTextFromKeyID(HotKeyManager.GetHotKeyId(categoryName, hotKeyId));
	}

	public static TextObject GetHotKeyGameText(this GameTextManager gameTextManager, string categoryName, int gameKeyId)
	{
		return gameTextManager.GetHotKeyGameTextFromKeyID(HotKeyManager.GetHotKeyId(categoryName, gameKeyId));
	}

	public static TextObject GetHotKeyGameTextFromKeyID(this GameTextManager gameTextManager, string keyId)
	{
		return gameTextManager.FindText("str_game_key_text", keyId.ToLower());
	}
}
