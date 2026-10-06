using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade.Missions.Multiplayer;

namespace TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.TeamSelection;

public class TeamSelectTeamInstanceVM : ViewModel
{
	private const int MaxFriendAvatarCount = 6;

	public readonly Team Team;

	public readonly Action<Team> _onSelect;

	private readonly List<MPPlayerVM> _friends;

	private MissionScoreboardComponent _missionScoreboardComponent;

	private MissionScoreboardComponent.MissionScoreboardSide _missionScoreboardSide;

	private readonly BasicCultureObject _culture;

	private bool _isDisabled;

	private string _displayedPrimary;

	private string _displayedSecondary;

	private string _displayedSecondarySub;

	private string _lockText;

	private string _cultureId;

	private int _score;

	private BannerImageIdentifierVM _banner;

	private MBBindingList<MPPlayerVM> _friendAvatars;

	private bool _hasExtraFriends;

	private bool _isAttacker;

	private bool _isSiege;

	private string _friendsExtraText;

	private HintViewModel _friendsExtraHint;

	private Color _cultureColor1;

	private Color _cultureColor2;

	[DataSourceProperty]
	public string CultureId
	{
		get
		{
			return _cultureId;
		}
		set
		{
			if (_cultureId != value)
			{
				_cultureId = value;
				OnPropertyChangedWithValue(value, "CultureId");
			}
		}
	}

	[DataSourceProperty]
	public int Score
	{
		get
		{
			return _score;
		}
		set
		{
			if (value != _score)
			{
				_score = value;
				OnPropertyChangedWithValue(value, "Score");
			}
		}
	}

	[DataSourceProperty]
	public bool IsDisabled
	{
		get
		{
			return _isDisabled;
		}
		set
		{
			if (_isDisabled != value)
			{
				_isDisabled = value;
				OnPropertyChangedWithValue(value, "IsDisabled");
			}
		}
	}

	[DataSourceProperty]
	public bool IsAttacker
	{
		get
		{
			return _isAttacker;
		}
		set
		{
			if (_isAttacker != value)
			{
				_isAttacker = value;
				OnPropertyChangedWithValue(value, "IsAttacker");
			}
		}
	}

	[DataSourceProperty]
	public bool IsSiege
	{
		get
		{
			return _isSiege;
		}
		set
		{
			if (_isSiege != value)
			{
				_isSiege = value;
				OnPropertyChangedWithValue(value, "IsSiege");
			}
		}
	}

	[DataSourceProperty]
	public string DisplayedPrimary
	{
		get
		{
			return _displayedPrimary;
		}
		set
		{
			_displayedPrimary = value;
			OnPropertyChangedWithValue(value, "DisplayedPrimary");
		}
	}

	[DataSourceProperty]
	public string DisplayedSecondary
	{
		get
		{
			return _displayedSecondary;
		}
		set
		{
			_displayedSecondary = value;
			OnPropertyChangedWithValue(value, "DisplayedSecondary");
		}
	}

	[DataSourceProperty]
	public string DisplayedSecondarySub
	{
		get
		{
			return _displayedSecondarySub;
		}
		set
		{
			_displayedSecondarySub = value;
			OnPropertyChangedWithValue(value, "DisplayedSecondarySub");
		}
	}

	[DataSourceProperty]
	public string LockText
	{
		get
		{
			return _lockText;
		}
		set
		{
			_lockText = value;
			OnPropertyChangedWithValue(value, "LockText");
		}
	}

	[DataSourceProperty]
	public BannerImageIdentifierVM Banner
	{
		get
		{
			return _banner;
		}
		set
		{
			if (value != _banner && (value == null || _banner == null || _banner.Id != value.Id))
			{
				_banner = value;
				OnPropertyChangedWithValue(value, "Banner");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<MPPlayerVM> FriendAvatars
	{
		get
		{
			return _friendAvatars;
		}
		set
		{
			if (_friendAvatars != value)
			{
				_friendAvatars = value;
				OnPropertyChangedWithValue(value, "FriendAvatars");
			}
		}
	}

	[DataSourceProperty]
	public bool HasExtraFriends
	{
		get
		{
			return _hasExtraFriends;
		}
		set
		{
			if (_hasExtraFriends != value)
			{
				_hasExtraFriends = value;
				OnPropertyChangedWithValue(value, "HasExtraFriends");
			}
		}
	}

	[DataSourceProperty]
	public string FriendsExtraText
	{
		get
		{
			return _friendsExtraText;
		}
		set
		{
			if (_friendsExtraText != value)
			{
				_friendsExtraText = value;
				OnPropertyChangedWithValue(value, "FriendsExtraText");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel FriendsExtraHint
	{
		get
		{
			return _friendsExtraHint;
		}
		set
		{
			if (_friendsExtraHint != value)
			{
				_friendsExtraHint = value;
				OnPropertyChangedWithValue(value, "FriendsExtraHint");
			}
		}
	}

	[DataSourceProperty]
	public Color CultureColor1
	{
		get
		{
			return _cultureColor1;
		}
		set
		{
			if (value != _cultureColor1)
			{
				_cultureColor1 = value;
				OnPropertyChangedWithValue(value, "CultureColor1");
			}
		}
	}

	[DataSourceProperty]
	public Color CultureColor2
	{
		get
		{
			return _cultureColor2;
		}
		set
		{
			if (value != _cultureColor2)
			{
				_cultureColor2 = value;
				OnPropertyChangedWithValue(value, "CultureColor2");
			}
		}
	}

	public TeamSelectTeamInstanceVM(MissionScoreboardComponent missionScoreboardComponent, Team team, BasicCultureObject culture, Banner banner, Action<Team> onSelect, MultiplayerBattleColors.MultiplayerCultureColorInfo cultureColors)
	{
		Team = team;
		_onSelect = onSelect;
		_culture = culture;
		IsSiege = Mission.Current?.HasMissionBehavior<MissionMultiplayerSiegeClient>() ?? false;
		if (Team != null && Team.Side != BattleSideEnum.None)
		{
			_missionScoreboardComponent = missionScoreboardComponent;
			_missionScoreboardComponent.OnRoundPropertiesChanged += UpdateTeamScores;
			_missionScoreboardSide = _missionScoreboardComponent.Sides.FirstOrDefault((MissionScoreboardComponent.MissionScoreboardSide s) => s != null && s.Side == Team.Side);
			IsAttacker = Team.Side == BattleSideEnum.Attacker;
			UpdateTeamScores();
		}
		CultureId = ((culture == null) ? "" : culture.StringId);
		if (team == null)
		{
			IsDisabled = true;
		}
		Banner = new BannerImageIdentifierVM(banner, nineGrid: true);
		CultureColor1 = cultureColors.Color1;
		CultureColor2 = cultureColors.Color2;
		_friends = new List<MPPlayerVM>();
		FriendAvatars = new MBBindingList<MPPlayerVM>();
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		DisplayedPrimary = ((_culture == null) ? new TextObject("{=pSheKLB4}Spectator").ToString() : _culture.Name.ToString());
	}

	public override void OnFinalize()
	{
		if (_missionScoreboardComponent != null)
		{
			_missionScoreboardComponent.OnRoundPropertiesChanged -= UpdateTeamScores;
		}
		_missionScoreboardComponent = null;
		_missionScoreboardSide = null;
		base.OnFinalize();
	}

	private void UpdateTeamScores()
	{
		if (_missionScoreboardSide != null)
		{
			Score = _missionScoreboardSide.SideScore;
		}
	}

	public void RefreshFriends(IEnumerable<MissionPeer> friends)
	{
		List<MissionPeer> list = friends.ToList();
		List<MPPlayerVM> list2 = new List<MPPlayerVM>();
		foreach (MPPlayerVM friend in _friends)
		{
			if (!list.Contains(friend.Peer))
			{
				list2.Add(friend);
			}
		}
		foreach (MPPlayerVM item in list2)
		{
			_friends.Remove(item);
		}
		List<MissionPeer> list3 = _friends.Select((MPPlayerVM x) => x.Peer).ToList();
		foreach (MissionPeer item2 in list)
		{
			if (!list3.Contains(item2))
			{
				_friends.Add(new MPPlayerVM(item2));
			}
		}
		FriendAvatars.Clear();
		MBStringBuilder mBStringBuilder = default(MBStringBuilder);
		mBStringBuilder.Initialize(16, "RefreshFriends");
		for (int num = 0; num < _friends.Count; num++)
		{
			if (num < 6)
			{
				FriendAvatars.Add(_friends[num]);
			}
			else
			{
				mBStringBuilder.AppendLine(_friends[num].Peer.DisplayedName);
			}
		}
		int num2 = _friends.Count - 6;
		if (num2 > 0)
		{
			HasExtraFriends = true;
			TextObject textObject = new TextObject("{=hbwp3g3k}+{FRIEND_COUNT} {newline} {?PLURAL}friends{?}friend{\\?}");
			textObject.SetTextVariable("FRIEND_COUNT", num2);
			textObject.SetTextVariable("PLURAL", (num2 != 1) ? 1 : 0);
			FriendsExtraText = textObject.ToString();
			FriendsExtraHint = new HintViewModel(textObject);
		}
		else
		{
			mBStringBuilder.Release();
			HasExtraFriends = false;
			FriendsExtraText = "";
		}
	}

	public void SetIsDisabled(bool isCurrentTeam, bool disabledForBalance)
	{
		IsDisabled = isCurrentTeam || disabledForBalance;
		if (isCurrentTeam)
		{
			LockText = new TextObject("{=SoQcsslF}CURRENT TEAM").ToString();
		}
		else if (disabledForBalance)
		{
			LockText = new TextObject("{=qe46yXVJ}LOCKED FOR BALANCE").ToString();
		}
	}

	public void ExecuteSelectTeam()
	{
		if (_onSelect != null)
		{
			_onSelect(Team);
		}
	}
}
