using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Generic;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade.Diamond;
using TaleWorlds.PlayerServices;

namespace TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.Lobby.Clan;

public class MPLobbyClanRosterVM : ViewModel
{
	private class MemberComparer : IComparer<MPLobbyClanMemberItemVM>
	{
		public int Compare(MPLobbyClanMemberItemVM x, MPLobbyClanMemberItemVM y)
		{
			if (y.Rank != x.Rank)
			{
				return y.Rank.CompareTo(x.Rank);
			}
			return y.IsOnline.CompareTo(x.IsOnline);
		}
	}

	private bool _isClanLeader;

	private bool _isClanOfficer;

	private MemberComparer _memberComparer;

	private bool _isSelected;

	private bool _isMemberActionsActive;

	private bool _isPrivilegedMember;

	private string _rosterText;

	private string _nameText;

	private string _badgeText;

	private string _statusText;

	private MBBindingList<MPLobbyClanMemberItemVM> _membersList;

	private MBBindingList<StringPairItemWithActionVM> _memberActionsList;

	private HintViewModel _promoteToClanOfficerHint;

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
			}
		}
	}

	[DataSourceProperty]
	public bool IsMemberActionsActive
	{
		get
		{
			return _isMemberActionsActive;
		}
		set
		{
			if (value != _isMemberActionsActive)
			{
				_isMemberActionsActive = value;
				OnPropertyChangedWithValue(value, "IsMemberActionsActive");
			}
		}
	}

	[DataSourceProperty]
	public bool IsPrivilegedMember
	{
		get
		{
			return _isPrivilegedMember;
		}
		set
		{
			if (value != _isPrivilegedMember)
			{
				_isPrivilegedMember = value;
				OnPropertyChangedWithValue(value, "IsPrivilegedMember");
			}
		}
	}

	[DataSourceProperty]
	public string RosterText
	{
		get
		{
			return _rosterText;
		}
		set
		{
			if (value != _rosterText)
			{
				_rosterText = value;
				OnPropertyChangedWithValue(value, "RosterText");
			}
		}
	}

	[DataSourceProperty]
	public string NameText
	{
		get
		{
			return _nameText;
		}
		set
		{
			if (value != _nameText)
			{
				_nameText = value;
				OnPropertyChangedWithValue(value, "NameText");
			}
		}
	}

	[DataSourceProperty]
	public string BadgeText
	{
		get
		{
			return _badgeText;
		}
		set
		{
			if (value != _badgeText)
			{
				_badgeText = value;
				OnPropertyChangedWithValue(value, "BadgeText");
			}
		}
	}

	[DataSourceProperty]
	public string StatusText
	{
		get
		{
			return _statusText;
		}
		set
		{
			if (value != _statusText)
			{
				_statusText = value;
				OnPropertyChangedWithValue(value, "StatusText");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<MPLobbyClanMemberItemVM> MembersList
	{
		get
		{
			return _membersList;
		}
		set
		{
			if (value != _membersList)
			{
				_membersList = value;
				OnPropertyChangedWithValue(value, "MembersList");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<StringPairItemWithActionVM> MemberActionsList
	{
		get
		{
			return _memberActionsList;
		}
		set
		{
			if (value != _memberActionsList)
			{
				_memberActionsList = value;
				OnPropertyChangedWithValue(value, "MemberActionsList");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel PromoteToClanOfficerHint
	{
		get
		{
			return _promoteToClanOfficerHint;
		}
		set
		{
			if (value != _promoteToClanOfficerHint)
			{
				_promoteToClanOfficerHint = value;
				OnPropertyChangedWithValue(value, "PromoteToClanOfficerHint");
			}
		}
	}

	public MPLobbyClanRosterVM()
	{
		MembersList = new MBBindingList<MPLobbyClanMemberItemVM>();
		MemberActionsList = new MBBindingList<StringPairItemWithActionVM>();
		_memberComparer = new MemberComparer();
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		RosterText = new TextObject("{=oyVeCtlg}Roster").ToString();
		NameText = new TextObject("{=PDdh1sBj}Name").ToString();
		BadgeText = new TextObject("{=4PrfimcK}Badge").ToString();
		StatusText = new TextObject("{=DXczLzml}Status").ToString();
		PromoteToClanOfficerHint = new HintViewModel(new TextObject("{=oeSrXaKt}You need to demote one of the officers"));
	}

	public void RefreshClanInformation(ClanHomeInfo info)
	{
		if (info == null || info.ClanInfo == null)
		{
			return;
		}
		_isClanLeader = NetworkMain.GameClient.IsClanLeader;
		_isClanOfficer = NetworkMain.GameClient.IsClanOfficer;
		MembersList.Clear();
		ClanPlayer[] players = info.ClanInfo.Players;
		foreach (ClanPlayer member in players)
		{
			if (!MultiplayerPlayerHelper.IsBlocked(member.PlayerId))
			{
				ClanPlayerInfo clanPlayerInfo = info.ClanPlayerInfos.First((ClanPlayerInfo clanPlayerInfo2) => clanPlayerInfo2.PlayerId.Equals(member.PlayerId));
				if (clanPlayerInfo != null)
				{
					bool isOnline = clanPlayerInfo.State == AnotherPlayerState.AtLobby || clanPlayerInfo.State == AnotherPlayerState.InMultiplayerGame || clanPlayerInfo.State == AnotherPlayerState.InParty;
					MembersList.Add(new MPLobbyClanMemberItemVM(member, isOnline, clanPlayerInfo.ActiveBadgeId, clanPlayerInfo.State, ExecutePopulateActionsList));
				}
			}
		}
		MembersList.Sort(_memberComparer);
		IsPrivilegedMember = NetworkMain.GameClient.IsClanLeader || NetworkMain.GameClient.IsClanOfficer;
	}

	public void OnPlayerNameUpdated(string playerName)
	{
		for (int i = 0; i < MembersList.Count; i++)
		{
			MPLobbyClanMemberItemVM mPLobbyClanMemberItemVM = MembersList[i];
			if (mPLobbyClanMemberItemVM.Id == NetworkMain.GameClient.PlayerID)
			{
				mPLobbyClanMemberItemVM.UpdateNameAndAvatar(forceUpdate: true);
			}
		}
	}

	private void ExecutePopulateActionsList(MPLobbyClanMemberItemVM member)
	{
		MemberActionsList.Clear();
		if (NetworkMain.GameClient.PlayerID != member.Id)
		{
			if (_isClanLeader)
			{
				MemberActionsList.Add(new StringPairItemWithActionVM(ExecutePromoteToClanLeader, new TextObject("{=GRpGNYHW}Promote To Clan Leader").ToString(), "PromoteToClanLeader", member));
				if (NetworkMain.GameClient.IsPlayerClanOfficer(member.Id))
				{
					MemberActionsList.Add(new StringPairItemWithActionVM(ExecuteDemoteFromClanOfficer, new TextObject("{=gowlLS2b}Demote From Clan Officer").ToString(), "DemoteFromClanOfficer", member));
				}
				else
				{
					StringPairItemWithActionVM stringPairItemWithActionVM = new StringPairItemWithActionVM(ExecutePromoteToClanOfficer, new TextObject("{=BXI1ObU8}Promote To Clan Officer").ToString(), "PromoteToClanOfficer", member);
					if (NetworkMain.GameClient.PlayersInClan.Count((ClanPlayer m) => m.Role == ClanPlayerRole.Officer) == Parameters.ClanOfficerCount)
					{
						stringPairItemWithActionVM.IsEnabled = false;
						stringPairItemWithActionVM.Hint = PromoteToClanOfficerHint;
					}
					MemberActionsList.Add(stringPairItemWithActionVM);
				}
			}
			if ((_isClanOfficer || _isClanLeader) && !NetworkMain.GameClient.IsPlayerClanLeader(member.Id) && (!_isClanOfficer || !NetworkMain.GameClient.IsPlayerClanOfficer(member.Id)))
			{
				MemberActionsList.Add(new StringPairItemWithActionVM(ExecuteKickFromClan, new TextObject("{=S8pZEPni}Kick From Clan").ToString(), "KickFromClan", member));
			}
			if (NetworkMain.GameClient.FriendInfos.All((FriendInfo f) => f.Id != member.Id))
			{
				MemberActionsList.Add(new StringPairItemWithActionVM(ExecuteRequestFriendship, GameTexts.FindText("str_mp_scoreboard_context_request_friendship").ToString(), "RequestFriendship", member));
			}
			else
			{
				MemberActionsList.Add(new StringPairItemWithActionVM(ExecuteTerminateFriendship, new TextObject("{=2YIVRuRa}Remove From Friends").ToString(), "TerminateFriendship", member));
			}
			if (NetworkMain.GameClient.SupportedFeatures.SupportsFeatures(Features.Party))
			{
				MemberActionsList.Add(new StringPairItemWithActionVM(ExecuteInviteToParty, new TextObject("{=RzROgBkv}Invite To Party").ToString(), "InviteToParty", member));
			}
			MultiplayerPlayerContextMenuHelper.AddLobbyViewProfileOptions(member, MemberActionsList);
		}
		if (MemberActionsList.Count > 0)
		{
			IsMemberActionsActive = false;
			IsMemberActionsActive = true;
		}
	}

	private void ExecuteRequestFriendship(object memberObj)
	{
		MPLobbyClanMemberItemVM mPLobbyClanMemberItemVM = memberObj as MPLobbyClanMemberItemVM;
		bool dontUseNameForUnknownPlayer = BannerlordConfig.EnableGenericNames && !NetworkMain.GameClient.IsKnownPlayer(mPLobbyClanMemberItemVM.Id);
		NetworkMain.GameClient.AddFriend(mPLobbyClanMemberItemVM.Id, dontUseNameForUnknownPlayer);
	}

	private void ExecuteTerminateFriendship(object memberObj)
	{
		MPLobbyClanMemberItemVM mPLobbyClanMemberItemVM = memberObj as MPLobbyClanMemberItemVM;
		NetworkMain.GameClient.RemoveFriend(mPLobbyClanMemberItemVM.Id);
	}

	private void ExecutePromoteToClanLeader(object memberObj)
	{
		MPLobbyClanMemberItemVM member = memberObj as MPLobbyClanMemberItemVM;
		GameTexts.SetVariable("MEMBER_NAME", member.Name);
		string titleText = new TextObject("{=GRpGNYHW}Promote To Clan Leader").ToString();
		string text = new TextObject("{=Z0TW2cub}Are you sure want to promote {MEMBER_NAME} as clan leader? You will lose your leadership.").ToString();
		InformationManager.ShowInquiry(new InquiryData(titleText, text, isAffirmativeOptionShown: true, isNegativeOptionShown: true, GameTexts.FindText("str_yes").ToString(), GameTexts.FindText("str_no").ToString(), delegate
		{
			PromoteToClanLeader(member.Id);
		}, null));
	}

	private void ExecutePromoteToClanOfficer(object memberObj)
	{
		MPLobbyClanMemberItemVM member = memberObj as MPLobbyClanMemberItemVM;
		GameTexts.SetVariable("MEMBER_NAME", member.Name);
		string titleText = new TextObject("{=BXI1ObU8}Promote To Clan Officer").ToString();
		string text = new TextObject("{=MS4Ng2iw}Are you sure want to promote {MEMBER_NAME} as clan officer?").ToString();
		InformationManager.ShowInquiry(new InquiryData(titleText, text, isAffirmativeOptionShown: true, isNegativeOptionShown: true, GameTexts.FindText("str_yes").ToString(), GameTexts.FindText("str_no").ToString(), delegate
		{
			PromoteToClanOfficer(member.Id);
		}, null));
	}

	private void ExecuteDemoteFromClanOfficer(object memberObj)
	{
		MPLobbyClanMemberItemVM member = memberObj as MPLobbyClanMemberItemVM;
		GameTexts.SetVariable("MEMBER_NAME", member.Name);
		string titleText = new TextObject("{=gowlLS2b}Demote From Clan Officer").ToString();
		string text = new TextObject("{=pSb1P6ZA}Are you sure want to demote {MEMBER_NAME} from clan officers?").ToString();
		InformationManager.ShowInquiry(new InquiryData(titleText, text, isAffirmativeOptionShown: true, isNegativeOptionShown: true, GameTexts.FindText("str_yes").ToString(), GameTexts.FindText("str_no").ToString(), delegate
		{
			DemoteFromClanOfficer(member.Id);
		}, null));
	}

	private void ExecuteKickFromClan(object memberObj)
	{
		MPLobbyClanMemberItemVM member = memberObj as MPLobbyClanMemberItemVM;
		GameTexts.SetVariable("MEMBER_NAME", member.Name);
		string titleText = new TextObject("{=S8pZEPni}Kick From Clan").ToString();
		string text = new TextObject("{=L6eaNe2q}Are you sure want to kick {MEMBER_NAME} from clan?").ToString();
		InformationManager.ShowInquiry(new InquiryData(titleText, text, isAffirmativeOptionShown: true, isNegativeOptionShown: true, GameTexts.FindText("str_yes").ToString(), GameTexts.FindText("str_no").ToString(), delegate
		{
			KickFromClan(member.Id);
		}, null));
	}

	private void ExecuteInviteToParty(object memberObj)
	{
		MPLobbyClanMemberItemVM mPLobbyClanMemberItemVM = memberObj as MPLobbyClanMemberItemVM;
		bool dontUseNameForUnknownPlayer = BannerlordConfig.EnableGenericNames && !NetworkMain.GameClient.IsKnownPlayer(mPLobbyClanMemberItemVM.Id);
		NetworkMain.GameClient.InviteToParty(mPLobbyClanMemberItemVM.Id, dontUseNameForUnknownPlayer);
	}

	private void ExecuteViewProfile(object memberObj)
	{
		(memberObj as MPLobbyClanMemberItemVM).ExecuteShowProfile();
	}

	private async void PromoteToClanLeader(PlayerId playerId)
	{
		bool dontUseNameForUnknownPlayer = BannerlordConfig.EnableGenericNames && !NetworkMain.GameClient.IsKnownPlayer(playerId);
		await NetworkMain.GameClient.PromoteToClanLeader(playerId, dontUseNameForUnknownPlayer);
	}

	private void PromoteToClanOfficer(PlayerId playerId)
	{
		bool dontUseNameForUnknownPlayer = BannerlordConfig.EnableGenericNames && !NetworkMain.GameClient.IsKnownPlayer(playerId);
		NetworkMain.GameClient.AssignAsClanOfficer(playerId, dontUseNameForUnknownPlayer);
	}

	private void DemoteFromClanOfficer(PlayerId playerId)
	{
		NetworkMain.GameClient.RemoveClanOfficerRoleForPlayer(playerId);
	}

	private void KickFromClan(PlayerId playerId)
	{
		NetworkMain.GameClient.KickFromClan(playerId);
	}
}
