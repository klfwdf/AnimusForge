using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.Scoreboard;

public class MissionScoreboardStatItemVM : ViewModel
{
	private string _item;

	private string _headerID = "";

	private MissionScoreboardPlayerVM _belongedPlayer;

	[DataSourceProperty]
	public string Item
	{
		get
		{
			return _item;
		}
		set
		{
			if (value != _item)
			{
				_item = value;
				OnPropertyChangedWithValue(value, "Item");
			}
		}
	}

	[DataSourceProperty]
	public string HeaderID
	{
		get
		{
			return _headerID;
		}
		set
		{
			if (value != _headerID)
			{
				_headerID = value;
				OnPropertyChangedWithValue(value, "HeaderID");
			}
		}
	}

	[DataSourceProperty]
	public MissionScoreboardPlayerVM BelongedPlayer
	{
		get
		{
			return _belongedPlayer;
		}
		set
		{
			if (value != _belongedPlayer)
			{
				_belongedPlayer = value;
				OnPropertyChangedWithValue(value, "BelongedPlayer");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<MissionScoreboardMVPItemVM> MVPBadges => BelongedPlayer.MVPBadges;

	public MissionScoreboardStatItemVM(MissionScoreboardPlayerVM belongedPlayer, string headerID, string item)
	{
		Item = item;
		HeaderID = headerID;
		BelongedPlayer = belongedPlayer;
	}
}
