using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.ViewModelCollection.GameOptions;

namespace TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.Lobby.HostGame.HostGameOptions;

public class InputHostGameOptionDataVM : GenericHostGameOptionDataVM
{
	private string _text;

	private bool _isPassword;

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
				base.OptionType.SetValue(value);
				NotifyValueChanged();
			}
		}
	}

	[DataSourceProperty]
	public bool IsPassword
	{
		get
		{
			return _isPassword;
		}
		set
		{
			if (value != _isPassword)
			{
				_isPassword = value;
				OnPropertyChangedWithValue(value, "IsPassword");
			}
		}
	}

	public InputHostGameOptionDataVM(MultiplayerOptions.OptionType optionType, int preferredIndex)
		: base(OptionsVM.OptionsDataType.InputOption, optionType, preferredIndex)
	{
		IsPassword = base.Presentation.IsPassword;
		RefreshData();
	}

	public override void RefreshData()
	{
		string strValue = base.OptionType.GetStrValue();
		Text = strValue;
	}
}
