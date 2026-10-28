using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.Diamond.MultiplayerBadges;

namespace TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.Lobby.AfterBattle;

public class MPAfterBattleBadgeRewardItemVM : MPAfterBattleRewardItemVM
{
	private string _badgeID;

	[DataSourceProperty]
	public string BadgeID
	{
		get
		{
			return _badgeID;
		}
		set
		{
			if (value != _badgeID)
			{
				_badgeID = value;
				OnPropertyChangedWithValue(value, "BadgeID");
			}
		}
	}

	public MPAfterBattleBadgeRewardItemVM(Badge badge)
	{
		base.Type = 1;
		base.Name = badge.Name.ToString();
		BadgeID = badge.StringId;
		RefreshValues();
	}
}
