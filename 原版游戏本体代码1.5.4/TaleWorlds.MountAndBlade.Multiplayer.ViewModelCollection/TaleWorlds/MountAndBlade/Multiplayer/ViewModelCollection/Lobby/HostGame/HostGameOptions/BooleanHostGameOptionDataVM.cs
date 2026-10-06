using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.ViewModelCollection.GameOptions;

namespace TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.Lobby.HostGame.HostGameOptions;

public class BooleanHostGameOptionDataVM : GenericHostGameOptionDataVM
{
	private bool _isSelected;

	[DataSourceProperty]
	public bool IsSelected
	{
		get
		{
			return _isSelected;
		}
		set
		{
			if (value != _isSelected)
			{
				_isSelected = value;
				OnPropertyChangedWithValue(value, "IsSelected");
				base.OptionType.SetValue(value);
			}
		}
	}

	public BooleanHostGameOptionDataVM(MultiplayerOptions.OptionType optionType, int preferredIndex)
		: base(OptionsVM.OptionsDataType.BooleanOption, optionType, preferredIndex)
	{
		RefreshData();
	}

	public override void RefreshData()
	{
		IsSelected = base.OptionType.GetBoolValue();
	}
}
