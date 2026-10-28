using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.Multiplayer.Admin;

namespace TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.AdminPanel;

public class MultiplayerAdminPanelStringOptionVM : MultiplayerAdminPanelOptionBaseVM
{
	private new readonly IAdminPanelOption<string> _option;

	private bool _isStringOption;

	private string _text;

	[DataSourceProperty]
	public bool IsStringOption
	{
		get
		{
			return _isStringOption;
		}
		set
		{
			if (value != _isStringOption)
			{
				_isStringOption = value;
				OnPropertyChangedWithValue(value, "IsStringOption");
			}
		}
	}

	[DataSourceProperty]
	public string Text
	{
		get
		{
			return _text;
		}
		set
		{
			if (value != _text)
			{
				_text = value;
				OnPropertyChangedWithValue(value, "Text");
				_option?.SetValue(value);
			}
		}
	}

	public MultiplayerAdminPanelStringOptionVM(IAdminPanelOption<string> option)
		: base(option)
	{
		_option = option;
		Text = _option.GetValue();
		IsStringOption = true;
	}

	public override void UpdateValues()
	{
		base.UpdateValues();
		Text = _option.GetValue();
	}
}
