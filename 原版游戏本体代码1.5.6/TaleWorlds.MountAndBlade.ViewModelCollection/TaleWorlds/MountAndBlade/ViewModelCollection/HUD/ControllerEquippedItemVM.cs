using System;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.ViewModelCollection.Input;

namespace TaleWorlds.MountAndBlade.ViewModelCollection.HUD;

public class ControllerEquippedItemVM : EquipmentActionItemVM
{
	private InputKeyItemVM _shortcutKey;

	private float _dropProgress;

	[DataSourceProperty]
	public InputKeyItemVM ShortcutKey
	{
		get
		{
			return _shortcutKey;
		}
		set
		{
			if (value != _shortcutKey)
			{
				_shortcutKey = value;
				OnPropertyChangedWithValue(value, "ShortcutKey");
			}
		}
	}

	[DataSourceProperty]
	public float DropProgress
	{
		get
		{
			return _dropProgress;
		}
		set
		{
			if (value != _dropProgress)
			{
				_dropProgress = value;
				OnPropertyChangedWithValue(value, "DropProgress");
			}
		}
	}

	public ControllerEquippedItemVM(string item, string itemTypeAsString, object identifier, HotKey key, Action<EquipmentActionItemVM> onSelection)
		: base(item, itemTypeAsString, identifier, onSelection)
	{
		if (key != null)
		{
			ShortcutKey = InputKeyItemVM.CreateFromHotKey(key, isConsoleOnly: true);
		}
	}

	public override void OnFinalize()
	{
		base.OnFinalize();
		ShortcutKey?.OnFinalize();
		ShortcutKey = null;
	}
}
