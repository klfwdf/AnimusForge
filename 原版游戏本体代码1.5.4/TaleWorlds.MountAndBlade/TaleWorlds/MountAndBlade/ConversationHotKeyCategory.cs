using System.Collections.Generic;
using TaleWorlds.InputSystem;

namespace TaleWorlds.MountAndBlade;

public sealed class ConversationHotKeyCategory : GameKeyContext
{
	public const string CategoryId = "ConversationHotKeyCategory";

	public const string ContinueKey = "ContinueKey";

	public const string ContinueClick = "ContinueClick";

	public ConversationHotKeyCategory()
		: base("ConversationHotKeyCategory", 116)
	{
		RegisterHotKeys();
		RegisterGameKeys();
		RegisterGameAxisKeys();
	}

	private void RegisterHotKeys()
	{
		List<Key> keys = new List<Key>
		{
			new Key(InputKey.Space),
			new Key(InputKey.Enter),
			new Key(InputKey.NumpadEnter)
		};
		RegisterHotKey(new HotKey("ContinueKey", "ConversationHotKeyCategory", keys));
		List<Key> keys2 = new List<Key>
		{
			new Key(InputKey.LeftMouseButton),
			new Key(InputKey.ControllerRDown)
		};
		RegisterHotKey(new HotKey("ContinueClick", "ConversationHotKeyCategory", keys2));
	}

	private void RegisterGameKeys()
	{
	}

	private void RegisterGameAxisKeys()
	{
	}
}
