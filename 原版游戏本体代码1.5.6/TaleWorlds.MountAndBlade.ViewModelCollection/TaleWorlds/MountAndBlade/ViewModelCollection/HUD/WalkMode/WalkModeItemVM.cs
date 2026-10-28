using System;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade.ViewModelCollection.Input;

namespace TaleWorlds.MountAndBlade.ViewModelCollection.HUD.WalkMode;

public class WalkModeItemVM : ViewModel
{
	private readonly MissionMainAgentWalkModeControllerVM.GetIsWalkModeActivatedDelegate _getIsActive;

	private readonly MissionMainAgentWalkModeControllerVM.SetIsWalkModeActivatedDelegate _setIsActive;

	private readonly MissionMainAgentWalkModeControllerVM.GetCanChangeWalkModeActivatedDelegate _canChangeActive;

	private readonly Action<WalkModeItemVM> _onToggle;

	private readonly TextObject _descriptionTextObj;

	private InputKeyItemVM _toggleInputKey;

	private bool _isActive;

	private bool _isDisabled;

	private string _description;

	private string _typeId;

	[DataSourceProperty]
	public InputKeyItemVM ToggleInputKey
	{
		get
		{
			return _toggleInputKey;
		}
		set
		{
			if (value != _toggleInputKey)
			{
				_toggleInputKey = value;
				OnPropertyChangedWithValue(value, "ToggleInputKey");
			}
		}
	}

	[DataSourceProperty]
	public bool IsActive
	{
		get
		{
			return _isActive;
		}
		set
		{
			if (value != _isActive)
			{
				_isActive = value;
				OnPropertyChangedWithValue(value, "IsActive");
			}
		}
	}

	[DataSourceProperty]
	public bool IsDisabled
	{
		get
		{
			return _isDisabled;
		}
		set
		{
			if (value != _isDisabled)
			{
				_isDisabled = value;
				OnPropertyChangedWithValue(value, "IsDisabled");
			}
		}
	}

	[DataSourceProperty]
	public string Description
	{
		get
		{
			return _description;
		}
		set
		{
			if (value != _description)
			{
				_description = value;
				OnPropertyChangedWithValue(value, "Description");
			}
		}
	}

	[DataSourceProperty]
	public string TypeId
	{
		get
		{
			return _typeId;
		}
		set
		{
			if (value != _typeId)
			{
				_typeId = value;
				OnPropertyChangedWithValue(value, "TypeId");
			}
		}
	}

	public WalkModeItemVM(string typeId, TextObject description, MissionMainAgentWalkModeControllerVM.GetIsWalkModeActivatedDelegate getIsActive, MissionMainAgentWalkModeControllerVM.SetIsWalkModeActivatedDelegate setIsActive, MissionMainAgentWalkModeControllerVM.GetCanChangeWalkModeActivatedDelegate canChangeActive, Action<WalkModeItemVM> onToggle)
	{
		_getIsActive = getIsActive;
		_setIsActive = setIsActive;
		_canChangeActive = canChangeActive;
		_descriptionTextObj = description;
		_onToggle = onToggle;
		IsActive = _getIsActive();
		TypeId = typeId;
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		Description = _descriptionTextObj.ToString();
	}

	public override void OnFinalize()
	{
		base.OnFinalize();
		ToggleInputKey?.OnFinalize();
	}

	public void OnEnabled()
	{
		IsActive = _getIsActive();
		IsDisabled = !_canChangeActive();
	}

	public void ToggleState()
	{
		IsDisabled = !_canChangeActive();
		if (!IsDisabled)
		{
			IsActive = !IsActive;
			_setIsActive(IsActive);
			_onToggle(this);
		}
	}

	public void SetToggleInputKey(HotKey hotKey, bool isHotKeyConsoleOnly)
	{
		ToggleInputKey?.OnFinalize();
		ToggleInputKey = InputKeyItemVM.CreateFromHotKey(hotKey, isHotKeyConsoleOnly);
	}

	public void SetToggleInputKey(GameKey gameKey, bool isHotKeyConsoleOnly)
	{
		ToggleInputKey?.OnFinalize();
		ToggleInputKey = InputKeyItemVM.CreateFromGameKey(gameKey, isHotKeyConsoleOnly);
	}
}
