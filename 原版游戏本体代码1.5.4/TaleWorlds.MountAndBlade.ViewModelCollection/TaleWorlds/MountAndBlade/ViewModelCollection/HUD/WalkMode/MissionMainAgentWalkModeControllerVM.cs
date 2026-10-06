using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.MountAndBlade.ViewModelCollection.HUD.WalkMode;

public class MissionMainAgentWalkModeControllerVM : ViewModel
{
	public delegate bool GetIsWalkModeActivatedDelegate();

	public delegate void SetIsWalkModeActivatedDelegate(bool value);

	public delegate bool GetCanChangeWalkModeActivatedDelegate();

	private MBBindingList<WalkModeItemVM> _controlModes;

	private WalkModeItemVM _lastUsedItem;

	private bool _isEnabled;

	[DataSourceProperty]
	public MBBindingList<WalkModeItemVM> ControlModes
	{
		get
		{
			return _controlModes;
		}
		set
		{
			if (value != _controlModes)
			{
				_controlModes = value;
				OnPropertyChangedWithValue(value, "ControlModes");
			}
		}
	}

	[DataSourceProperty]
	public WalkModeItemVM LastUsedItem
	{
		get
		{
			return _lastUsedItem;
		}
		set
		{
			if (value != _lastUsedItem)
			{
				_lastUsedItem = value;
				OnPropertyChangedWithValue(value, "LastUsedItem");
			}
		}
	}

	[DataSourceProperty]
	public bool IsEnabled
	{
		get
		{
			return _isEnabled;
		}
		set
		{
			if (value != _isEnabled)
			{
				_isEnabled = value;
				OnPropertyChangedWithValue(value, "IsEnabled");
			}
		}
	}

	public MissionMainAgentWalkModeControllerVM()
	{
		ControlModes = new MBBindingList<WalkModeItemVM>();
		RefreshValues();
	}

	public override void OnFinalize()
	{
		base.OnFinalize();
		ControlModes.ApplyActionOnAllItems(delegate(WalkModeItemVM o)
		{
			o.OnFinalize();
		});
		ControlModes.Clear();
	}

	public void AddWalkMode(string typeId, TextObject name, GetIsWalkModeActivatedDelegate getIsActive, SetIsWalkModeActivatedDelegate setIsActive, GetCanChangeWalkModeActivatedDelegate canChangeActive, HotKey hotKey, bool isHotkeyConsoleOnly)
	{
		WalkModeItemVM walkModeItemVM = new WalkModeItemVM(typeId, name, getIsActive, setIsActive, canChangeActive, OnItemToggled);
		walkModeItemVM.SetToggleInputKey(hotKey, isHotkeyConsoleOnly);
		ControlModes.Add(walkModeItemVM);
	}

	public void AddWalkMode(string typeId, TextObject name, GetIsWalkModeActivatedDelegate getIsActive, SetIsWalkModeActivatedDelegate setIsActive, GetCanChangeWalkModeActivatedDelegate canChangeActive, GameKey hotKey, bool isHotkeyConsoleOnly)
	{
		WalkModeItemVM walkModeItemVM = new WalkModeItemVM(typeId, name, getIsActive, setIsActive, canChangeActive, OnItemToggled);
		walkModeItemVM.SetToggleInputKey(hotKey, isHotkeyConsoleOnly);
		ControlModes.Add(walkModeItemVM);
	}

	private void OnItemToggled(WalkModeItemVM item)
	{
		LastUsedItem = item;
	}

	public void SetEnabled(bool isEnabled)
	{
		IsEnabled = isEnabled;
		if (isEnabled)
		{
			for (int i = 0; i < ControlModes.Count; i++)
			{
				ControlModes[i].OnEnabled();
			}
		}
	}
}
