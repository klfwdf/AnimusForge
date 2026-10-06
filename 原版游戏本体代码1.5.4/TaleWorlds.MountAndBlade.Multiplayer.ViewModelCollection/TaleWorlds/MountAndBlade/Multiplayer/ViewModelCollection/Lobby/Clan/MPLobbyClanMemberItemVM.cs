using System;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Generic;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade.Diamond;
using TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.Lobby.Friends;
using TaleWorlds.PlayerServices;

namespace TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.Lobby.Clan;

public class MPLobbyClanMemberItemVM : MPLobbyPlayerBaseVM
{
	private ClanPlayer _member;

	private Action<MPLobbyClanMemberItemVM> _executeActivate;

	private bool _isOnline;

	private bool _isClanLeader;

	private string _notEligibleInfo;

	private string _inviteAcceptInfo;

	private int _rank;

	private MBBindingList<StringPairItemWithActionVM> _userActionsList;

	private HintViewModel _rankHint;

	public PlayerId Id { get; private set; }

	[DataSourceProperty]
	public bool IsOnline
	{
		get
		{
			return _isOnline;
		}
		set
		{
			if (value != _isOnline)
			{
				_isOnline = value;
				OnPropertyChangedWithValue(value, "IsOnline");
			}
		}
	}

	[DataSourceProperty]
	public bool IsClanLeader
	{
		get
		{
			return _isClanLeader;
		}
		set
		{
			if (value != _isClanLeader)
			{
				_isClanLeader = value;
				OnPropertyChangedWithValue(value, "IsClanLeader");
			}
		}
	}

	[DataSourceProperty]
	public string NotEligibleInfo
	{
		get
		{
			return _notEligibleInfo;
		}
		set
		{
			if (value != _notEligibleInfo)
			{
				_notEligibleInfo = value;
				OnPropertyChangedWithValue(value, "NotEligibleInfo");
			}
		}
	}

	[DataSourceProperty]
	public string InviteAcceptInfo
	{
		get
		{
			return _inviteAcceptInfo;
		}
		set
		{
			if (value != _inviteAcceptInfo)
			{
				_inviteAcceptInfo = value;
				OnPropertyChangedWithValue(value, "InviteAcceptInfo");
			}
		}
	}

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

	[DataSourceProperty]
	public MBBindingList<StringPairItemWithActionVM> UserActionsList
	{
		get
		{
			return _userActionsList;
		}
		set
		{
			if (value != _userActionsList)
			{
				_userActionsList = value;
				OnPropertyChangedWithValue(value, "UserActionsList");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel RankHint
	{
		get
		{
			return _rankHint;
		}
		set
		{
			if (value != _rankHint)
			{
				_rankHint = value;
				OnPropertyChangedWithValue(value, "RankHint");
			}
		}
	}

	public MPLobbyClanMemberItemVM(PlayerId playerId)
		: base(playerId)
	{
		Id = playerId;
		RefreshValues();
	}

	public MPLobbyClanMemberItemVM(ClanPlayer member, bool isOnline, string selectedBadgeID, AnotherPlayerState state, Action<MPLobbyClanMemberItemVM> executeActivate = null)
		: base(member.PlayerId)
	{
		_member = member;
		Id = _member.PlayerId;
		IsOnline = isOnline;
		_executeActivate = executeActivate;
		base.SelectedBadgeID = selectedBadgeID;
		if (isOnline)
		{
			base.StateText = GameTexts.FindText("str_multiplayer_lobby_state", state.ToString()).ToString();
		}
		IsClanLeader = _member.Role == ClanPlayerRole.Leader;
		Rank = (int)_member.Role;
		RankHint = new HintViewModel();
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		NotEligibleInfo = "";
		ClanPlayer member = _member;
		if (member != null && member.Role == ClanPlayerRole.Leader)
		{
			RankHint.HintText = new TextObject("{=SrfYbg3x}Leader");
			return;
		}
		ClanPlayer member2 = _member;
		if (member2 != null && member2.Role == ClanPlayerRole.Officer)
		{
			RankHint.HintText = new TextObject("{=ZYF2t1VI}Officer");
		}
	}

	public void SetNotEligibleInfo(PlayerNotEligibleError notEligibleError)
	{
		string text = "";
		switch (notEligibleError)
		{
		case PlayerNotEligibleError.AlreadyInClan:
			text = new TextObject("{=zEMWM4h3}Already In a Clan").ToString();
			break;
		case PlayerNotEligibleError.NotAtLobby:
			text = new TextObject("{=hPbppi6E}Not At The Lobby").ToString();
			break;
		case PlayerNotEligibleError.DoesNotSupportFeature:
			text = new TextObject("{=MsokbMx2}Does not support Clan feature").ToString();
			break;
		}
		if (string.IsNullOrEmpty(NotEligibleInfo))
		{
			NotEligibleInfo = text;
			return;
		}
		GameTexts.SetVariable("LEFT", NotEligibleInfo);
		GameTexts.SetVariable("RIGHT", text);
		NotEligibleInfo = GameTexts.FindText("str_LEFT_comma_RIGHT").ToString();
	}

	private void ExecuteSelection()
	{
		_executeActivate?.Invoke(this);
	}
}
