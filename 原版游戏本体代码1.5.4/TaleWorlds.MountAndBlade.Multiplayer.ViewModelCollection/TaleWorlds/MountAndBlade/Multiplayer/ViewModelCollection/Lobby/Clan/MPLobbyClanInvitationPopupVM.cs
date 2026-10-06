using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade.Diamond;
using TaleWorlds.PlayerServices;

namespace TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.Lobby.Clan;

public class MPLobbyClanInvitationPopupVM : ViewModel
{
	public enum InvitationMode
	{
		Creation,
		Invitation
	}

	private InvitationMode _invitationMode;

	private bool _isEnabled;

	private bool _isCreation;

	private string _titleText;

	private string _clanNameAndTag;

	private string _inviteReceivedText;

	private string _withPlayersText;

	private string _wantToJoinText;

	private MBBindingList<MPLobbyClanMemberItemVM> _partyMembersList;

	[DataSourceProperty]
	public bool IsEnabled
	{
		get
		{
			return _isEnabled;
		}
		set
		{
			if (value != _isEnabled)
			{
				_isEnabled = value;
				OnPropertyChanged("IsEnabled");
			}
		}
	}

	[DataSourceProperty]
	public bool IsCreation
	{
		get
		{
			return _isCreation;
		}
		set
		{
			if (value != _isCreation)
			{
				_isCreation = value;
				OnPropertyChanged("IsCreation");
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
	public string ClanNameAndTag
	{
		get
		{
			return _clanNameAndTag;
		}
		set
		{
			if (value != _clanNameAndTag)
			{
				_clanNameAndTag = value;
				OnPropertyChanged("ClanNameAndTag");
			}
		}
	}

	[DataSourceProperty]
	public string InviteReceivedText
	{
		get
		{
			return _inviteReceivedText;
		}
		set
		{
			if (value != _inviteReceivedText)
			{
				_inviteReceivedText = value;
				OnPropertyChanged("InviteReceivedText");
			}
		}
	}

	[DataSourceProperty]
	public string WithPlayersText
	{
		get
		{
			return _withPlayersText;
		}
		set
		{
			if (value != _withPlayersText)
			{
				_withPlayersText = value;
				OnPropertyChanged("WithPlayersText");
			}
		}
	}

	[DataSourceProperty]
	public string WantToJoinText
	{
		get
		{
			return _wantToJoinText;
		}
		set
		{
			if (value != _wantToJoinText)
			{
				_wantToJoinText = value;
				OnPropertyChanged("WantToJoinText");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<MPLobbyClanMemberItemVM> PartyMembersList
	{
		get
		{
			return _partyMembersList;
		}
		set
		{
			if (value != _partyMembersList)
			{
				_partyMembersList = value;
				OnPropertyChanged("PartyMembersList");
			}
		}
	}

	public MPLobbyClanInvitationPopupVM()
	{
		PartyMembersList = new MBBindingList<MPLobbyClanMemberItemVM>();
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		TitleText = new TextObject("{=D9zIAw9y}Clan Invite").ToString();
		InviteReceivedText = new TextObject("{=wNAl9o4A}You received an invite from").ToString();
		WantToJoinText = new TextObject("{=qa9aOxLm}Do you want to join this clan?").ToString();
	}

	public void Open(string clanName, string clanTag, bool isCreation)
	{
		GameTexts.SetVariable("STR", clanTag);
		string content = new TextObject("{=uTXYEAOg}[{STR}]").ToString();
		GameTexts.SetVariable("STR1", clanName);
		GameTexts.SetVariable("STR2", content);
		ClanNameAndTag = GameTexts.FindText("str_STR1_space_STR2").ToString();
		PartyMembersList.Clear();
		IsCreation = isCreation;
		if (isCreation)
		{
			_invitationMode = InvitationMode.Creation;
			foreach (PartyPlayerInLobbyClient item in NetworkMain.GameClient.PlayersInParty)
			{
				if (item.PlayerId != NetworkMain.GameClient.PlayerID)
				{
					MPLobbyClanMemberItemVM mPLobbyClanMemberItemVM = new MPLobbyClanMemberItemVM(item.PlayerId);
					mPLobbyClanMemberItemVM.InviteAcceptInfo = new TextObject("{=c0ZdKSkn}Waiting").ToString();
					PartyMembersList.Add(mPLobbyClanMemberItemVM);
				}
			}
		}
		else
		{
			_invitationMode = InvitationMode.Invitation;
		}
		WithPlayersText = ((PartyMembersList.Count > 1) ? new TextObject("{=iCaRFZpG}along with these players").ToString() : string.Empty);
		IsEnabled = true;
	}

	public void Close()
	{
		IsEnabled = false;
	}

	public void UpdateConfirmation(PlayerId playerId, ClanCreationAnswer answer)
	{
		foreach (MPLobbyClanMemberItemVM partyMembers in PartyMembersList)
		{
			if (partyMembers.ProvidedID == playerId)
			{
				switch (answer)
				{
				case ClanCreationAnswer.Accepted:
					partyMembers.InviteAcceptInfo = new TextObject("{=JTMegIk4}Accepted").ToString();
					break;
				case ClanCreationAnswer.Declined:
					partyMembers.InviteAcceptInfo = new TextObject("{=FgaORzy5}Declined").ToString();
					break;
				}
			}
		}
	}

	private async void ExecuteAcceptInvitation()
	{
		if (_invitationMode == InvitationMode.Creation)
		{
			NetworkMain.GameClient.AcceptClanCreationRequest();
			IsEnabled = false;
		}
		else if (await NetworkMain.GameClient.AcceptClanInvitation())
		{
			IsEnabled = false;
		}
	}

	private void ExecuteDeclineInvitation()
	{
		IsEnabled = false;
		if (_invitationMode == InvitationMode.Creation)
		{
			NetworkMain.GameClient.DeclineClanCreationRequest();
		}
		else
		{
			NetworkMain.GameClient.DeclineClanInvitation();
		}
	}
}
