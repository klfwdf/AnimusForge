using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.Multiplayer.Admin;

namespace TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.AdminPanel;

public class MultiplayerAdminPanelToggleOptionVM : MultiplayerAdminPanelOptionBaseVM
{
	private new readonly IAdminPanelOption<bool> _option;

	private bool _isToggleOption;

	private bool _toggleValue;

	[DataSourceProperty]
	public bool IsToggleOption
	{
		get
		{
			return _isToggleOption;
		}
		set
		{
			if (value != _isToggleOption)
			{
				_isToggleOption = value;
				OnPropertyChangedWithValue(value, "IsToggleOption");
			}
		}
	}

	[DataSourceProperty]
	public bool ToggleValue
	{
		get
		{
			return _toggleValue;
		}
		set
		{
			if (value != _toggleValue)
			{
				_toggleValue = value;
				OnPropertyChangedWithValue(value, "ToggleValue");
				_option.SetValue(value);
			}
		}
	}

	public MultiplayerAdminPanelToggleOptionVM(IAdminPanelOption<bool> option)
		: base(option)
	{
		_option = option;
		ToggleValue = _option.GetValue();
		IsToggleOption = true;
	}

	public override void UpdateValues()
	{
		base.UpdateValues();
		ToggleValue = _option.GetValue();
	}

	public void ExecuteToggle()
	{
		ToggleValue = !ToggleValue;
	}
}
