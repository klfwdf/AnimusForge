using System.Collections.Generic;
using TaleWorlds.InputSystem;

namespace TaleWorlds.MountAndBlade;

public class GenericPanelGameKeyCategory : GameKeyContext
{
	public const string CategoryId = "GenericPanelGameKeyCategory";

	public const string Exit = "Exit";

	public const string Confirm = "Confirm";

	public const string ResetChanges = "Reset";

	public const string ToggleEscapeMenu = "ToggleEscapeMenu";

	public const string SwitchToPreviousTab = "SwitchToPreviousTab";

	public const string SwitchToNextTab = "SwitchToNextTab";

	public const string GiveAll = "GiveAll";

	public const string TakeAll = "TakeAll";

	public const string Randomize = "Randomize";

	public const string Start = "Start";

	public const string Delete = "Delete";

	public const string SelectProfile = "SelectProfile";

	public const string Play = "Play";

	public static GenericPanelGameKeyCategory Current { get; private set; }

	public GenericPanelGameKeyCategory(string categoryId = "GenericPanelGameKeyCategory")
		: base(categoryId, 116)
	{
		Current = this;
		RegisterHotKeys();
		RegisterGameKeys();
		RegisterGameAxisKeys();
	}

	private void RegisterHotKeys()
	{
		List<Key> keys = new List<Key>
		{
			new Key(InputKey.Escape),
			new Key(InputKey.ControllerRRight)
		};
		List<Key> keys2 = new List<Key>
		{
			new Key(InputKey.Enter),
			new Key(InputKey.NumpadEnter),
			new Key(InputKey.ControllerRLeft)
		};
		List<Key> keys3 = new List<Key>
		{
			new Key(InputKey.ControllerRUp)
		};
		List<Key> keys4 = new List<Key>
		{
			new Key(InputKey.Escape),
			new Key(InputKey.ControllerROption)
		};
		List<Key> keys5 = new List<Key>
		{
			new Key(InputKey.Q),
			new Key(InputKey.ControllerLBumper)
		};
		List<Key> keys6 = new List<Key>
		{
			new Key(InputKey.E),
			new Key(InputKey.ControllerRBumper)
		};
		List<Key> keys7 = new List<Key>
		{
			new Key(InputKey.D),
			new Key(InputKey.ControllerRTrigger)
		};
		List<Key> keys8 = new List<Key>
		{
			new Key(InputKey.A),
			new Key(InputKey.ControllerLTrigger)
		};
		List<Key> keys9 = new List<Key>
		{
			new Key(InputKey.R),
			new Key(InputKey.ControllerRUp)
		};
		List<Key> keys10 = new List<Key>
		{
			new Key(InputKey.ControllerROption)
		};
		List<Key> keys11 = new List<Key>
		{
			new Key(InputKey.Delete),
			new Key(InputKey.ControllerRUp)
		};
		List<Key> keys12 = new List<Key>
		{
			new Key(InputKey.Escape),
			new Key(InputKey.ControllerRUp)
		};
		List<Key> keys13 = new List<Key>
		{
			new Key(InputKey.Enter),
			new Key(InputKey.NumpadEnter),
			new Key(InputKey.ControllerRDown)
		};
		RegisterHotKey(new HotKey("Exit", "GenericPanelGameKeyCategory", keys));
		RegisterHotKey(new HotKey("Confirm", "GenericPanelGameKeyCategory", keys2));
		RegisterHotKey(new HotKey("Reset", "GenericPanelGameKeyCategory", keys3));
		RegisterHotKey(new HotKey("ToggleEscapeMenu", "GenericPanelGameKeyCategory", keys4));
		RegisterHotKey(new HotKey("SwitchToPreviousTab", "GenericPanelGameKeyCategory", keys5));
		RegisterHotKey(new HotKey("SwitchToNextTab", "GenericPanelGameKeyCategory", keys6));
		RegisterHotKey(new HotKey("GiveAll", "GenericPanelGameKeyCategory", keys7));
		RegisterHotKey(new HotKey("TakeAll", "GenericPanelGameKeyCategory", keys8));
		RegisterHotKey(new HotKey("Randomize", "GenericPanelGameKeyCategory", keys9));
		RegisterHotKey(new HotKey("Start", "GenericPanelGameKeyCategory", keys10));
		RegisterHotKey(new HotKey("Delete", "GenericPanelGameKeyCategory", keys11));
		RegisterHotKey(new HotKey("SelectProfile", "GenericPanelGameKeyCategory", keys12));
		RegisterHotKey(new HotKey("Play", "GenericPanelGameKeyCategory", keys13));
	}

	private void RegisterGameKeys()
	{
	}

	private void RegisterGameAxisKeys()
	{
	}
}
