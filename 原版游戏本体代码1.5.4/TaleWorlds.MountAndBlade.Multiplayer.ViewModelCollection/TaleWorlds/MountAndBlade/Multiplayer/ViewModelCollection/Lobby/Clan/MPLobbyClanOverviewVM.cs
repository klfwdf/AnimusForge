using System;
using System.Threading.Tasks;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade.Diamond;
using TaleWorlds.ObjectSystem;
using TaleWorlds.PlatformService;

namespace TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.Lobby.Clan;

public class MPLobbyClanOverviewVM : ViewModel
{
	private readonly Action _openInviteClanMemberPopup;

	private bool _isSelected;

	private bool _isLeader;

	private bool _isPrivilegedMember;

	private bool _areActionButtonsEnabled;

	private bool _doesHaveDescription;

	private bool _doesHaveAnnouncements;

	private string _nameText;

	private string _membersText;

	private string _changeSigilText;

	private string _changeFactionText;

	private string _leaveText;

	private string _disbandText;

	private string _factionCultureID;

	private string _informationText;

	private string _announcementsText;

	private string _clanDescriptionText;

	private string _noDescriptionText;

	private string _noAnnouncementsText;

	private string _titleText;

	private Color _cultureColor1;

	private Color _cultureColor2;

	private BannerImageIdentifierVM _sigilImage;

	private BannerImageIdentifierVM _factionBanner;

	private MPLobbyClanChangeSigilPopupVM _changeSigilPopup;

	private MPLobbyClanChangeFactionPopupVM _changeFactionPopup;

	private MBBindingList<MPLobbyClanAnnouncementVM> _announcementsList;

	private MPLobbyClanSendPostPopupVM _sendAnnouncementPopup;

	private MPLobbyClanSendPostPopupVM _setClanInformationPopup;

	private HintViewModel _cantLeaveHint;

	private HintViewModel _inviteMembersHint;

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
				OnPropertyChanged("IsSelected");
			}
		}
	}

	[DataSourceProperty]
	public bool IsLeader
	{
		get
		{
			return _isLeader;
		}
		set
		{
			if (value != _isLeader)
			{
				_isLeader = value;
				OnPropertyChanged("IsLeader");
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
				OnPropertyChanged("IsPrivilegedMember");
			}
		}
	}

	[DataSourceProperty]
	public bool AreActionButtonsEnabled
	{
		get
		{
			return _areActionButtonsEnabled;
		}
		set
		{
			if (value != _areActionButtonsEnabled)
			{
				_areActionButtonsEnabled = value;
				OnPropertyChanged("AreActionButtonsEnabled");
			}
		}
	}

	[DataSourceProperty]
	public bool DoesHaveDescription
	{
		get
		{
			return _doesHaveDescription;
		}
		set
		{
			if (value != _doesHaveDescription)
			{
				_doesHaveDescription = value;
				OnPropertyChanged("DoesHaveDescription");
			}
		}
	}

	[DataSourceProperty]
	public bool DoesHaveAnnouncements
	{
		get
		{
			return _doesHaveAnnouncements;
		}
		set
		{
			if (value != _doesHaveAnnouncements)
			{
				_doesHaveAnnouncements = value;
				OnPropertyChanged("DoesHaveAnnouncements");
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
				OnPropertyChanged("NameText");
			}
		}
	}

	[DataSourceProperty]
	public string MembersText
	{
		get
		{
			return _membersText;
		}
		set
		{
			if (value != _membersText)
			{
				_membersText = value;
				OnPropertyChanged("MembersText");
			}
		}
	}

	[DataSourceProperty]
	public string ChangeSigilText
	{
		get
		{
			return _changeSigilText;
		}
		set
		{
			if (value != _changeSigilText)
			{
				_changeSigilText = value;
				OnPropertyChanged("ChangeSigilText");
			}
		}
	}

	[DataSourceProperty]
	public string ChangeFactionText
	{
		get
		{
			return _changeFactionText;
		}
		set
		{
			if (value != _changeFactionText)
			{
				_changeFactionText = value;
				OnPropertyChanged("ChangeFactionText");
			}
		}
	}

	[DataSourceProperty]
	public string LeaveText
	{
		get
		{
			return _leaveText;
		}
		set
		{
			if (value != _leaveText)
			{
				_leaveText = value;
				OnPropertyChanged("LeaveText");
			}
		}
	}

	[DataSourceProperty]
	public string DisbandText
	{
		get
		{
			return _disbandText;
		}
		set
		{
			if (value != _disbandText)
			{
				_disbandText = value;
				OnPropertyChanged("DisbandText");
			}
		}
	}

	[DataSourceProperty]
	public string FactionCultureID
	{
		get
		{
			return _factionCultureID;
		}
		set
		{
			if (value != _factionCultureID)
			{
				_factionCultureID = value;
				OnPropertyChanged("FactionCultureID");
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

	[DataSourceProperty]
	public string InformationText
	{
		get
		{
			return _informationText;
		}
		set
		{
			if (value != _informationText)
			{
				_informationText = value;
				OnPropertyChanged("InformationText");
			}
		}
	}

	[DataSourceProperty]
	public string AnnouncementsText
	{
		get
		{
			return _announcementsText;
		}
		set
		{
			if (value != _announcementsText)
			{
				_announcementsText = value;
				OnPropertyChanged("AnnouncementsText");
			}
		}
	}

	[DataSourceProperty]
	public string ClanDescriptionText
	{
		get
		{
			return _clanDescriptionText;
		}
		set
		{
			if (value != _clanDescriptionText)
			{
				_clanDescriptionText = value;
				OnPropertyChanged("ClanDescriptionText");
			}
		}
	}

	[DataSourceProperty]
	public string NoDescriptionText
	{
		get
		{
			return _noDescriptionText;
		}
		set
		{
			if (value != _noDescriptionText)
			{
				_noDescriptionText = value;
				OnPropertyChanged("NoDescriptionText");
			}
		}
	}

	[DataSourceProperty]
	public string NoAnnouncementsText
	{
		get
		{
			return _noAnnouncementsText;
		}
		set
		{
			if (value != _noAnnouncementsText)
			{
				_noAnnouncementsText = value;
				OnPropertyChanged("NoAnnouncementsText");
			}
		}
	}

	[DataSourceProperty]
	public string TitleText
	{
		get
		{
			return _titleText;
		}
		set
		{
			if (value != _titleText)
			{
				_titleText = value;
				OnPropertyChanged("TitleText");
			}
		}
	}

	[DataSourceProperty]
	public BannerImageIdentifierVM SigilImage
	{
		get
		{
			return _sigilImage;
		}
		set
		{
			if (value != _sigilImage)
			{
				_sigilImage = value;
				OnPropertyChanged("SigilImage");
			}
		}
	}

	[DataSourceProperty]
	public BannerImageIdentifierVM FactionBanner
	{
		get
		{
			return _factionBanner;
		}
		set
		{
			if (value != _factionBanner)
			{
				_factionBanner = value;
				OnPropertyChanged("FactionBanner");
			}
		}
	}

	[DataSourceProperty]
	public MPLobbyClanChangeSigilPopupVM ChangeSigilPopup
	{
		get
		{
			return _changeSigilPopup;
		}
		set
		{
			if (value != _changeSigilPopup)
			{
				_changeSigilPopup = value;
				OnPropertyChanged("ChangeSigilPopup");
			}
		}
	}

	[DataSourceProperty]
	public MPLobbyClanChangeFactionPopupVM ChangeFactionPopup
	{
		get
		{
			return _changeFactionPopup;
		}
		set
		{
			if (value != _changeFactionPopup)
			{
				_changeFactionPopup = value;
				OnPropertyChanged("ChangeFactionPopup");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<MPLobbyClanAnnouncementVM> AnnouncementsList
	{
		get
		{
			return _announcementsList;
		}
		set
		{
			if (value != _announcementsList)
			{
				_announcementsList = value;
				OnPropertyChanged("AnnouncementsList");
			}
		}
	}

	[DataSourceProperty]
	public MPLobbyClanSendPostPopupVM SendAnnouncementPopup
	{
		get
		{
			return _sendAnnouncementPopup;
		}
		set
		{
			if (value != _sendAnnouncementPopup)
			{
				_sendAnnouncementPopup = value;
				OnPropertyChanged("SendAnnouncementPopup");
			}
		}
	}

	[DataSourceProperty]
	public MPLobbyClanSendPostPopupVM SetClanInformationPopup
	{
		get
		{
			return _setClanInformationPopup;
		}
		set
		{
			if (value != _setClanInformationPopup)
			{
				_setClanInformationPopup = value;
				OnPropertyChanged("SetClanInformationPopup");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel CantLeaveHint
	{
		get
		{
			return _cantLeaveHint;
		}
		set
		{
			if (value != _cantLeaveHint)
			{
				_cantLeaveHint = value;
				OnPropertyChanged("CantLeaveHint");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel InviteMembersHint
	{
		get
		{
			return _inviteMembersHint;
		}
		set
		{
			if (value != _inviteMembersHint)
			{
				_inviteMembersHint = value;
				OnPropertyChanged("InviteMembersHint");
			}
		}
	}

	public MPLobbyClanOverviewVM(Action openInviteClanMemberPopup)
	{
		_openInviteClanMemberPopup = openInviteClanMemberPopup;
		AnnouncementsList = new MBBindingList<MPLobbyClanAnnouncementVM>();
		ChangeSigilPopup = new MPLobbyClanChangeSigilPopupVM();
		ChangeFactionPopup = new MPLobbyClanChangeFactionPopupVM();
		SendAnnouncementPopup = new MPLobbyClanSendPostPopupVM(MPLobbyClanSendPostPopupVM.PostPopupMode.Announcement);
		SetClanInformationPopup = new MPLobbyClanSendPostPopupVM(MPLobbyClanSendPostPopupVM.PostPopupMode.Information);
		AreActionButtonsEnabled = true;
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		ChangeSigilText = new TextObject("{=7R0i82Nw}Change Sigil").ToString();
		ChangeFactionText = new TextObject("{=aGGq9lJT}Change Culture").ToString();
		LeaveText = new TextObject("{=3sRdGQou}Leave").ToString();
		DisbandText = new TextObject("{=xXSFaGW8}Disband").ToString();
		InformationText = new TextObject("{=SyklU5aP}Information").ToString();
		AnnouncementsText = new TextObject("{=JY2pBVHQ}Announcements").ToString();
		NoAnnouncementsText = new TextObject("{=0af2iQvw}Clan doesn't have any announcements").ToString();
		NoDescriptionText = new TextObject("{=NwiYsUwm}Clan doesn't have a description").ToString();
		TitleText = new TextObject("{=r223yChR}Overview").ToString();
		CantLeaveHint = new HintViewModel(new TextObject("{=76HlhP7r}You have to give leadership to another member to leave"));
		InviteMembersHint = new HintViewModel(new TextObject("{=tSMckUw3}Invite Members"));
	}

	public async Task RefreshClanInformation(ClanHomeInfo info)
	{
		if (info == null || info.ClanInfo == null)
		{
			CloseAllPopups();
			return;
		}
		ClanInfo clanInfo = info.ClanInfo;
		GameTexts.SetVariable("STR", clanInfo.Tag);
		string clanTagInBrackets = new TextObject("{=uTXYEAOg}[{STR}]").ToString();
		GameTexts.SetVariable("STR1", await PlatformServices.FilterString(clanInfo.Name, new TextObject("{=wNUcqcJP}Clan Name").ToString()));
		GameTexts.SetVariable("STR2", clanTagInBrackets);
		NameText = GameTexts.FindText("str_STR1_space_STR2").ToString();
		GameTexts.SetVariable("LEFT", new TextObject("{=lBn2pSBL}Members").ToString());
		GameTexts.SetVariable("RIGHT", clanInfo.Players.Length);
		MembersText = GameTexts.FindText("str_LEFT_colon_RIGHT").ToString();
		SigilImage = new BannerImageIdentifierVM(new Banner(clanInfo.Sigil), nineGrid: true);
		FactionCultureID = clanInfo.Faction;
		BasicCultureObject basicCultureObject = MBObjectManager.Instance.GetObject<BasicCultureObject>(FactionCultureID);
		CultureColor1 = Color.FromUint(basicCultureObject?.Color ?? 0);
		CultureColor2 = Color.FromUint(basicCultureObject?.Color2 ?? 0);
		if (NetworkMain.GameClient != null)
		{
			IsLeader = NetworkMain.GameClient.IsClanLeader;
			IsPrivilegedMember = IsLeader || NetworkMain.GameClient.IsClanOfficer;
		}
		else
		{
			Debug.FailedAssert("Game client is destroyed while updating clan home info", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection\\Lobby\\Clan\\MPLobbyClanOverviewVM.cs", "RefreshClanInformation", 89);
			Debug.Print("Game client is destroyed while updating clan home info");
			IsLeader = false;
			IsPrivilegedMember = false;
		}
		FactionBanner = new BannerImageIdentifierVM(basicCultureObject.Banner, nineGrid: true);
		ClanDescriptionText = clanInfo.InformationText;
		DoesHaveDescription = true;
		if (string.IsNullOrEmpty(clanInfo.InformationText))
		{
			DoesHaveDescription = false;
		}
		AnnouncementsList.Clear();
		ClanAnnouncement[] announcements = clanInfo.Announcements;
		ClanAnnouncement[] array = announcements;
		foreach (ClanAnnouncement clanAnnouncement in array)
		{
			AnnouncementsList.Add(new MPLobbyClanAnnouncementVM(clanAnnouncement.AuthorId, clanAnnouncement.Announcement, clanAnnouncement.CreationTime, clanAnnouncement.Id, IsPrivilegedMember));
		}
		DoesHaveAnnouncements = true;
		if (announcements.IsEmpty())
		{
			DoesHaveAnnouncements = false;
		}
	}

	private void ExecuteDisbandClan()
	{
		string titleText = new TextObject("{=oFWcihyW}Disband Clan").ToString();
		string text = new TextObject("{=vW1VgmaP}Are you sure want to disband your clan?").ToString();
		InformationManager.ShowInquiry(new InquiryData(titleText, text, isAffirmativeOptionShown: true, isNegativeOptionShown: true, GameTexts.FindText("str_yes").ToString(), GameTexts.FindText("str_no").ToString(), DisbandClan, null));
	}

	private void DisbandClan()
	{
		NetworkMain.GameClient.DestroyClan();
	}

	private void ExecuteLeaveClan()
	{
		string titleText = new TextObject("{=4ZE6i9nW}Leave Clan").ToString();
		string text = new TextObject("{=67hsZZor}Are you sure want to leave your clan?").ToString();
		InformationManager.ShowInquiry(new InquiryData(titleText, text, isAffirmativeOptionShown: true, isNegativeOptionShown: true, GameTexts.FindText("str_yes").ToString(), GameTexts.FindText("str_no").ToString(), LeaveClan, null));
	}

	private void LeaveClan()
	{
		NetworkMain.GameClient.KickFromClan(NetworkMain.GameClient.PlayerID);
	}

	private void ExecuteOpenChangeSigilPopup()
	{
		ChangeSigilPopup.ExecuteOpenPopup();
	}

	private void ExecuteCloseChangeSigilPopup()
	{
		ChangeSigilPopup.ExecuteClosePopup();
	}

	private void ExecuteOpenChangeFactionPopup()
	{
		ChangeFactionPopup.ExecuteOpenPopup();
	}

	private void ExecuteCloseChangeFactionPopup()
	{
		ChangeFactionPopup.ExecuteClosePopup();
	}

	private void ExecuteOpenSendAnnouncementPopup()
	{
		SendAnnouncementPopup.ExecuteOpenPopup();
	}

	private void ExecuteCloseSendAnnouncementPopup()
	{
		SendAnnouncementPopup.ExecuteClosePopup();
	}

	private void ExecuteOpenSetClanInformationPopup()
	{
		SetClanInformationPopup.ExecuteOpenPopup();
	}

	private void ExecuteCloseSetClanInformationPopup()
	{
		SetClanInformationPopup.ExecuteClosePopup();
	}

	private void ExecuteOpenInviteClanMemberPopup()
	{
		_openInviteClanMemberPopup?.Invoke();
	}

	private void CloseAllPopups()
	{
		ChangeSigilPopup.ExecuteClosePopup();
		ChangeFactionPopup.ExecuteClosePopup();
		SendAnnouncementPopup.ExecuteClosePopup();
		SetClanInformationPopup.ExecuteClosePopup();
	}
}
