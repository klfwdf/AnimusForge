using System;
using System.Linq;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.Diamond.Lobby.LocalData;
using TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.Lobby.Friends;
using TaleWorlds.PlayerServices;

namespace TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.Lobby.Profile;

public class MPLobbyRecentGamePlayerItemVM : MPLobbyPlayerBaseVM
{
	public readonly MatchHistoryData MatchOfThePlayer;

	private readonly Action<MPLobbyRecentGamePlayerItemVM> _onActivatePlayerActions;

	private int _killCount;

	private int _deathCount;

	private int _assistCount;

	[DataSourceProperty]
	public int KillCount
	{
		get
		{
			return _killCount;
		}
		set
		{
			if (value != _killCount)
			{
				_killCount = value;
				OnPropertyChangedWithValue(value, "KillCount");
			}
		}
	}

	[DataSourceProperty]
	public int DeathCount
	{
		get
		{
			return _deathCount;
		}
		set
		{
			if (value != _deathCount)
			{
				_deathCount = value;
				OnPropertyChangedWithValue(value, "DeathCount");
			}
		}
	}

	[DataSourceProperty]
	public int AssistCount
	{
		get
		{
			return _assistCount;
		}
		set
		{
			if (value != _assistCount)
			{
				_assistCount = value;
				OnPropertyChangedWithValue(value, "AssistCount");
			}
		}
	}

	public MPLobbyRecentGamePlayerItemVM(PlayerId playerId, MatchHistoryData matchOfThePlayer, Action<MPLobbyRecentGamePlayerItemVM> onActivatePlayerActions)
		: base(playerId)
	{
		MatchOfThePlayer = matchOfThePlayer;
		_onActivatePlayerActions = onActivatePlayerActions;
		PlayerInfo playerInfo = MatchOfThePlayer.Players.FirstOrDefault((PlayerInfo p) => p.PlayerId == playerId.ToString());
		if (playerInfo != null)
		{
			KillCount = playerInfo.Kill;
			DeathCount = playerInfo.Death;
			AssistCount = playerInfo.Assist;
		}
		RefreshValues();
	}

	private void ExecuteActivatePlayerActions()
	{
		_onActivatePlayerActions?.Invoke(this);
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
	}
}
