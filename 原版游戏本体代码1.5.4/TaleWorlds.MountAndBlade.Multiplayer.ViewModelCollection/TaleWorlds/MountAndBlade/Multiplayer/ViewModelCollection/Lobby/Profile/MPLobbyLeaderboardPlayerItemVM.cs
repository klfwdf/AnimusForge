using System;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.Diamond;
using TaleWorlds.MountAndBlade.Diamond.Lobby.LocalData;
using TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.Lobby.Friends;

namespace TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.Lobby.Profile;

public class MPLobbyLeaderboardPlayerItemVM : MPLobbyPlayerBaseVM
{
	public readonly MatchHistoryData MatchOfThePlayer;

	private readonly Action<MPLobbyLeaderboardPlayerItemVM> _onActivatePlayerActions;

	private int _rank;

	[DataSourceProperty]
	public int Rank
	{
		get
		{
			return _rank;
		}
		set
		{
			if (value != _rank)
			{
				_rank = value;
				OnPropertyChangedWithValue(value, "Rank");
			}
		}
	}

	public MPLobbyLeaderboardPlayerItemVM(int rank, PlayerLeaderboardData playerLeaderboardData, Action<MPLobbyLeaderboardPlayerItemVM> onActivatePlayerActions)
		: base(playerLeaderboardData.PlayerId, playerLeaderboardData.Name)
	{
		Rank = rank;
		base.Rating = playerLeaderboardData.Rating;
		base.RatingID = playerLeaderboardData.RankId;
		_onActivatePlayerActions = onActivatePlayerActions;
		RefreshValues();
	}

	private void ExecuteActivatePlayerActions()
	{
		_onActivatePlayerActions?.Invoke(this);
	}
}
