using System.Collections.Generic;
using TaleWorlds.InputSystem;

namespace TaleWorlds.MountAndBlade;

public sealed class PartyHotKeyCategory : GameKeyContext
{
	public const string CategoryId = "PartyHotKeyCategory";

	public const string TakeAllTroops = "TakeAllTroops";

	public const string GiveAllTroops = "GiveAllTroops";

	public const string TakeAllPrisoners = "TakeAllPrisoners";

	public const string GiveAllPrisoners = "GiveAllPrisoners";

	public const string PopupItemPrimaryAction = "PopupItemPrimaryAction";

	public const string PopupItemSecondaryAction = "PopupItemSecondaryAction";

	public const string OpenUpgradePopup = "OpenUpgradePopup";

	public const string OpenRecruitPopup = "OpenRecruitPopup";

	public PartyHotKeyCategory()
		: base("PartyHotKeyCategory", 116)
	{
		RegisterHotKeys();
		RegisterGameKeys();
		RegisterGameAxisKeys();
	}

	private void RegisterHotKeys()
	{
		List<Key> keys = new List<Key>
		{
			new Key(InputKey.Q),
			new Key(InputKey.ControllerLTrigger)
		};
		List<Key> keys2 = new List<Key>
		{
			new Key(InputKey.E),
			new Key(InputKey.ControllerRTrigger)
		};
		List<Key> keys3 = new List<Key>
		{
			new Key(InputKey.A),
			new Key(InputKey.ControllerLBumper)
		};
		List<Key> keys4 = new List<Key>
		{
			new Key(InputKey.D),
			new Key(InputKey.ControllerRBumper)
		};
		List<Key> keys5 = new List<Key>
		{
			new Key(InputKey.ControllerLBumper)
		};
		List<Key> keys6 = new List<Key>
		{
			new Key(InputKey.ControllerRBumper)
		};
		List<Key> keys7 = new List<Key>
		{
			new Key(InputKey.ControllerLThumb)
		};
		List<Key> keys8 = new List<Key>
		{
			new Key(InputKey.ControllerRThumb)
		};
		RegisterHotKey(new HotKey("TakeAllTroops", "PartyHotKeyCategory", keys));
		RegisterHotKey(new HotKey("GiveAllTroops", "PartyHotKeyCategory", keys2));
		RegisterHotKey(new HotKey("TakeAllPrisoners", "PartyHotKeyCategory", keys3));
		RegisterHotKey(new HotKey("GiveAllPrisoners", "PartyHotKeyCategory", keys4));
		RegisterHotKey(new HotKey("OpenUpgradePopup", "PartyHotKeyCategory", keys7));
		RegisterHotKey(new HotKey("OpenRecruitPopup", "PartyHotKeyCategory", keys8));
		RegisterHotKey(new HotKey("PopupItemPrimaryAction", "PartyHotKeyCategory", keys5));
		RegisterHotKey(new HotKey("PopupItemSecondaryAction", "PartyHotKeyCategory", keys6));
	}

	private void RegisterGameKeys()
	{
	}

	private void RegisterGameAxisKeys()
	{
	}
}
