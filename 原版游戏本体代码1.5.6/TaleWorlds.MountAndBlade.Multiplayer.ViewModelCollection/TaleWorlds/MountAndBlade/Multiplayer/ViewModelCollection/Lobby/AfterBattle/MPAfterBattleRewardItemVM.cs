using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.Lobby.AfterBattle;

public abstract class MPAfterBattleRewardItemVM : ViewModel
{
	public enum RewardType
	{
		Loot,
		Badge
	}

	private int _type;

	private string _name;

	public int Type
	{
		get
		{
			return _type;
		}
		set
		{
			if (value != _type)
			{
				_type = value;
				OnPropertyChangedWithValue(value, "Type");
			}
		}
	}

	public string Name
	{
		get
		{
			return _name;
		}
		set
		{
			if (value != _name)
			{
				_name = value;
				OnPropertyChangedWithValue(value, "Name");
			}
		}
	}
}
