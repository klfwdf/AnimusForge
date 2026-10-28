using System;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.Diamond;

namespace TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.Scoreboard;

public class MissionScoreboardPlayerVM : MPPlayerVM
{
	private const string BadgeHeaderID = "badge";

	private readonly MissionPeer _lobbyPeer;

	private readonly Action<MissionScoreboardPlayerVM> _executeActivate;

	private readonly ChatBox _chatBox;

	private int _ping;

	private bool _isPlayer;

	private bool _isVoiceMuted;

	private bool _isTextMuted;

	private MBBindingList<MissionScoreboardStatItemVM> _stats;

	private MBBindingList<MissionScoreboardMVPItemVM> _mvpBadges;

	public int Score { get; private set; }

	public bool IsBot { get; private set; }

	public bool IsMine
	{
		get
		{
			if (_lobbyPeer != null)
			{
				return _lobbyPeer.IsMine;
			}
			return false;
		}
	}

	public bool IsTeammate
	{
		get
		{
			if (_lobbyPeer != null)
			{
				return _lobbyPeer.Team.IsPlayerTeam;
			}
			return false;
		}
	}

	[DataSourceProperty]
	public int Ping
	{
		get
		{
			return _ping;
		}
		set
		{
			if (value != _ping)
			{
				_ping = value;
				OnPropertyChangedWithValue(value, "Ping");
			}
		}
	}

	[DataSourceProperty]
	public bool IsPlayer
	{
		get
		{
			return _isPlayer;
		}
		set
		{
			if (value != _isPlayer)
			{
				_isPlayer = value;
				OnPropertyChangedWithValue(value, "IsPlayer");
			}
		}
	}

	[DataSourceProperty]
	public bool IsVoiceMuted
	{
		get
		{
			return _isVoiceMuted;
		}
		set
		{
			if (value != _isVoiceMuted)
			{
				_isVoiceMuted = value;
				OnPropertyChangedWithValue(value, "IsVoiceMuted");
			}
		}
	}

	[DataSourceProperty]
	public bool IsTextMuted
	{
		get
		{
			return _isTextMuted;
		}
		set
		{
			if (value != _isTextMuted)
			{
				_isTextMuted = value;
				OnPropertyChangedWithValue(value, "IsTextMuted");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<MissionScoreboardStatItemVM> Stats
	{
		get
		{
			return _stats;
		}
		set
		{
			if (value != _stats)
			{
				_stats = value;
				OnPropertyChangedWithValue(value, "Stats");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<MissionScoreboardMVPItemVM> MVPBadges
	{
		get
		{
			return _mvpBadges;
		}
		set
		{
			if (value != _mvpBadges)
			{
				_mvpBadges = value;
				OnPropertyChangedWithValue(value, "MVPBadges");
			}
		}
	}

	public MissionScoreboardPlayerVM(MissionPeer peer, string[] attributes, string[] headerIDs, int score, Action<MissionScoreboardPlayerVM> executeActivate)
		: base(peer)
	{
		_chatBox = Game.Current.GetGameHandler<ChatBox>();
		_executeActivate = executeActivate;
		_lobbyPeer = peer;
		Stats = new MBBindingList<MissionScoreboardStatItemVM>();
		for (int i = 0; i < attributes.Length; i++)
		{
			Stats.Add(new MissionScoreboardStatItemVM(this, headerIDs[i], ""));
		}
		UpdateAttributes(attributes, score);
		IsPlayer = IsMine;
		MVPBadges = new MBBindingList<MissionScoreboardMVPItemVM>();
		base.Peer.SetMuted(PermaMuteList.IsPlayerMuted(peer.Peer.Id));
		UpdateIsMuted();
	}

	public MissionScoreboardPlayerVM(string[] attributes, string[] headerIDs, int score, Action<MissionScoreboardPlayerVM> executeActivate)
		: base((Agent)null)
	{
		_executeActivate = executeActivate;
		Stats = new MBBindingList<MissionScoreboardStatItemVM>();
		for (int i = 0; i < attributes.Length; i++)
		{
			Stats.Add(new MissionScoreboardStatItemVM(this, headerIDs[i], ""));
		}
		UpdateAttributes(attributes, score);
		IsBot = true;
		IsPlayer = false;
		base.IsDead = false;
	}

	public void Tick(float dt)
	{
		if (!IsBot)
		{
			base.IsDead = _lobbyPeer == null || !_lobbyPeer.IsControlledAgentActive;
		}
	}

	public void UpdateAttributes(string[] attributes, int score)
	{
		if (Stats.Count == attributes.Length)
		{
			for (int i = 0; i < attributes.Length; i++)
			{
				Stats[i].Item = attributes[i] ?? string.Empty;
			}
		}
		Score = score;
	}

	public void ExecuteSelection()
	{
		_executeActivate?.Invoke(this);
	}

	public void UpdateIsMuted()
	{
		bool flag = PermaMuteList.IsPlayerMuted(_lobbyPeer.Peer.Id);
		IsTextMuted = flag || _chatBox.IsPlayerMuted(_lobbyPeer.Peer.Id);
		IsVoiceMuted = flag || base.Peer.IsMutedFromGameOrPlatform;
	}

	public void SetMVPBadgeCount(int badgeCount)
	{
		MVPBadges.Clear();
		for (int i = 0; i < badgeCount; i++)
		{
			MVPBadges.Add(new MissionScoreboardMVPItemVM());
		}
	}
}
