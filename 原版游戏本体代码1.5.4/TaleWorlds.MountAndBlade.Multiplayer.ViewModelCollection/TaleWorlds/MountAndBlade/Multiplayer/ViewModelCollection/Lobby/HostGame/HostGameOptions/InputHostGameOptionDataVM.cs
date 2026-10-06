using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.ViewModelCollection.GameOptions;

namespace TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.Lobby.HostGame.HostGameOptions;

public class InputHostGameOptionDataVM : GenericHostGameOptionDataVM
{
	private string _text;

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
			}
		}
	}

	public InputHostGameOptionDataVM(MultiplayerOptions.OptionType optionType, int preferredIndex)
		: base(OptionsVM.OptionsDataType.InputOption, optionType, preferredIndex)
	{
		RefreshData();
	}

	public override void RefreshData()
	{
		string strValue = base.OptionType.GetStrValue();
		Text = strValue;
	}
}
