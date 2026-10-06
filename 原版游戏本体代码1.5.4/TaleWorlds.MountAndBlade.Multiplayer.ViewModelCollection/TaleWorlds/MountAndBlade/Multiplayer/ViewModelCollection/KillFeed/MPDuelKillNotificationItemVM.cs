using System;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.ClassLoadout;

namespace TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.KillFeed;

public class MPDuelKillNotificationItemVM : ViewModel
{
	private Action<MPDuelKillNotificationItemVM> _onRemove;

	private bool _isEndOfDuel;

	private int _arenaType;

	private int _firstPlayerScore;

	private int _secondPlayerScore;

	private string _firstPlayerName;

	private string _secondPlayerName;

	private MPTeammateCompassTargetVM _firstPlayerCompassElement;

	private MPTeammateCompassTargetVM _secondPlayerCompassElement;

	[DataSourceProperty]
	public bool IsEndOfDuel
	{
		get
		{
			return _isEndOfDuel;
		}
		set
		{
			if (value != _isEndOfDuel)
			{
				_isEndOfDuel = value;
				OnPropertyChangedWithValue(value, "IsEndOfDuel");
			}
		}
	}

	[DataSourceProperty]
	public int ArenaType
	{
		get
		{
			return _arenaType;
		}
		set
		{
			if (value != _arenaType)
			{
				_arenaType = value;
				OnPropertyChangedWithValue(value, "ArenaType");
			}
		}
	}

	[DataSourceProperty]
	public int FirstPlayerScore
	{
		get
		{
			return _firstPlayerScore;
		}
		set
		{
			if (value != _firstPlayerScore)
			{
				_firstPlayerScore = value;
				OnPropertyChangedWithValue(value, "FirstPlayerScore");
			}
		}
	}

	[DataSourceProperty]
	public int SecondPlayerScore
	{
		get
		{
			return _secondPlayerScore;
		}
		set
		{
			if (value != _secondPlayerScore)
			{
				_secondPlayerScore = value;
				OnPropertyChangedWithValue(value, "SecondPlayerScore");
			}
		}
	}

	[DataSourceProperty]
	public string FirstPlayerName
	{
		get
		{
			return _firstPlayerName;
		}
		set
		{
			if (value != _firstPlayerName)
			{
				_firstPlayerName = value;
				OnPropertyChangedWithValue(value, "FirstPlayerName");
			}
		}
	}

	[DataSourceProperty]
	public string SecondPlayerName
	{
		get
		{
			return _secondPlayerName;
		}
		set
		{
			if (value != _secondPlayerName)
			{
				_secondPlayerName = value;
				OnPropertyChangedWithValue(value, "SecondPlayerName");
			}
		}
	}

	[DataSourceProperty]
	public MPTeammateCompassTargetVM FirstPlayerCompassElement
	{
		get
		{
			return _firstPlayerCompassElement;
		}
		set
		{
			if (value != _firstPlayerCompassElement)
			{
				_firstPlayerCompassElement = value;
				OnPropertyChangedWithValue(value, "FirstPlayerCompassElement");
			}
		}
	}

	[DataSourceProperty]
	public MPTeammateCompassTargetVM SecondPlayerCompassElement
	{
		get
		{
			return _secondPlayerCompassElement;
		}
		set
		{
			if (value != _secondPlayerCompassElement)
			{
				_secondPlayerCompassElement = value;
				OnPropertyChangedWithValue(value, "SecondPlayerCompassElement");
			}
		}
	}

	public MPDuelKillNotificationItemVM(MissionPeer firstPlayerPeer, MissionPeer secondPlayerPeer, int firstPlayerScore, int secondPlayerScore, TroopType arenaTroopType, Action<MPDuelKillNotificationItemVM> onRemove)
	{
		_onRemove = onRemove;
		ArenaType = (int)arenaTroopType;
		FirstPlayerScore = firstPlayerScore;
		SecondPlayerScore = secondPlayerScore;
		int intValue = MultiplayerOptions.OptionType.MinScoreToWinDuel.GetIntValue();
		IsEndOfDuel = FirstPlayerScore == intValue || SecondPlayerScore == intValue;
		InitProperties(firstPlayerPeer, secondPlayerPeer);
	}

	public void InitProperties(MissionPeer firstPlayerPeer, MissionPeer secondPlayerPeer)
	{
		TargetIconType peerIconType = GetPeerIconType(firstPlayerPeer);
		FirstPlayerName = firstPlayerPeer.DisplayedName;
		FirstPlayerCompassElement = new MPTeammateCompassTargetVM(peerIconType, Color.White.ToUnsignedInteger(), Color.White.ToUnsignedInteger(), Banner.CreateOneColoredEmptyBanner(0), isAlly: false);
		TargetIconType peerIconType2 = GetPeerIconType(secondPlayerPeer);
		SecondPlayerName = secondPlayerPeer.DisplayedName;
		SecondPlayerCompassElement = new MPTeammateCompassTargetVM(peerIconType2, Color.White.ToUnsignedInteger(), Color.White.ToUnsignedInteger(), Banner.CreateOneColoredEmptyBanner(0), isAlly: false);
	}

	private TargetIconType GetPeerIconType(MissionPeer peer)
	{
		return MultiplayerClassDivisions.GetMPHeroClassForPeer(peer)?.IconType ?? TargetIconType.None;
	}

	public void ExecuteRemove()
	{
		_onRemove(this);
	}
}
