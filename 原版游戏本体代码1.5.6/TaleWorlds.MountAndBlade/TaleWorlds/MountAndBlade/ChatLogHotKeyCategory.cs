using System.Collections.Generic;
using TaleWorlds.InputSystem;

namespace TaleWorlds.MountAndBlade;

public sealed class ChatLogHotKeyCategory : GameKeyContext
{
	public const string CategoryId = "ChatLogHotKeyCategory";

	public const int InitiateAllChat = 6;

	public const int InitiateTeamChat = 7;

	public const int FinalizeChat = 8;

	public const string CycleChatTypes = "CycleChatTypes";

	public const string FinalizeChatAlternative = "FinalizeChatAlternative";

	public const string SendMessage = "SendMessage";

	public ChatLogHotKeyCategory()
		: base("ChatLogHotKeyCategory", 116)
	{
		RegisterHotKeys();
		RegisterGameKeys();
		RegisterGameAxisKeys();
	}

	private void RegisterHotKeys()
	{
		List<Key> keys = new List<Key>
		{
			new Key(InputKey.Tab),
			new Key(InputKey.ControllerRUp)
		};
		List<Key> list = new List<Key>
		{
			new Key(InputKey.NumpadEnter)
		};
		list.Add(new Key(InputKey.ControllerLOption));
		List<Key> keys2 = new List<Key>
		{
			new Key(InputKey.ControllerRLeft)
		};
		RegisterHotKey(new HotKey("CycleChatTypes", "ChatLogHotKeyCategory", keys));
		RegisterHotKey(new HotKey("FinalizeChatAlternative", "ChatLogHotKeyCategory", list));
		RegisterHotKey(new HotKey("SendMessage", "ChatLogHotKeyCategory", keys2));
	}

	private void RegisterGameKeys()
	{
		RegisterGameKey(new GameKey(6, "InitiateAllChat", "ChatLogHotKeyCategory", InputKey.T, GameKeyMainCategories.ChatCategory));
		RegisterGameKey(new GameKey(7, "InitiateTeamChat", "ChatLogHotKeyCategory", InputKey.Y, GameKeyMainCategories.ChatCategory));
		RegisterGameKey(new GameKey(8, "FinalizeChat", "ChatLogHotKeyCategory", InputKey.Enter, InputKey.ControllerLOption, GameKeyMainCategories.ChatCategory));
	}

	private void RegisterGameAxisKeys()
	{
	}
}
