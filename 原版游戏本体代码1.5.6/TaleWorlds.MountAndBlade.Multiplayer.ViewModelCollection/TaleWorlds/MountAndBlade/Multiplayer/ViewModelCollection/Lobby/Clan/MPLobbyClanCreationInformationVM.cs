using System;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade.Diamond;

namespace TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.Lobby.Clan;

public class MPLobbyClanCreationInformationVM : ViewModel
{
	private Action _openClanCreationPopup;

	private bool _isEnabled;

	private bool _canCreateClan;

	private bool _doesHaveEnoughPlayersToCreateClan;

	private int _currentPlayerCount;

	private int _requiredPlayerCount;

	private string _createClanText;

	private string _createClanDescriptionText;

	private string _dontHaveEnoughPlayersInPartyText;

	private string _partyMemberCountText;

	private string _playerText;

	private string _createYourClanText;

	private string _closeText;

	private MBBindingList<MPLobbyClanMemberItemVM> _partyMembers;

	private HintViewModel _cantCreateHint;

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
	public bool CanCreateClan
	{
		get
		{
			return _canCreateClan;
		}
		set
		{
			if (value != _canCreateClan)
			{
				_canCreateClan = value;
				OnPropertyChanged("CanCreateClan");
			}
		}
	}

	[DataSourceProperty]
	public bool DoesHaveEnoughPlayersToCreateClan
	{
		get
		{
			return _doesHaveEnoughPlayersToCreateClan;
		}
		set
		{
			if (value != _doesHaveEnoughPlayersToCreateClan)
			{
				_doesHaveEnoughPlayersToCreateClan = value;
				OnPropertyChanged("DoesHaveEnoughPlayersToCreateClan");
			}
		}
	}

	[DataSourceProperty]
	public int CurrentPlayerCount
	{
		get
		{
			return _currentPlayerCount;
		}
		set
		{
			if (value != _currentPlayerCount)
			{
				_currentPlayerCount = value;
				OnPropertyChanged("CurrentPlayerCount");
			}
		}
	}

	[DataSourceProperty]
	public int RequiredPlayerCount
	{
		get
		{
			return _requiredPlayerCount;
		}
		set
		{
			if (value != _requiredPlayerCount)
			{
				_requiredPlayerCount = value;
				OnPropertyChanged("RequiredPlayerCount");
			}
		}
	}

	[DataSourceProperty]
	public string CreateClanText
	{
		get
		{
			return _createClanText;
		}
		set
		{
			if (value != _createClanText)
			{
				_createClanText = value;
				OnPropertyChanged("CreateClanText");
			}
		}
	}

	[DataSourceProperty]
	public string CreateClanDescriptionText
	{
		get
		{
			return _createClanDescriptionText;
		}
		set
		{
			if (value != _createClanDescriptionText)
			{
				_createClanDescriptionText = value;
				OnPropertyChanged("CreateClanDescriptionText");
			}
		}
	}

	[DataSourceProperty]
	public string DontHaveEnoughPlayersInPartyText
	{
		get
		{
			return _dontHaveEnoughPlayersInPartyText;
		}
		set
		{
			if (value != _dontHaveEnoughPlayersInPartyText)
			{
				_dontHaveEnoughPlayersInPartyText = value;
				OnPropertyChanged("DontHaveEnoughPlayersInPartyText");
			}
		}
	}

	[DataSourceProperty]
	public string PartyMemberCountText
	{
		get
		{
			return _partyMemberCountText;
		}
		set
		{
			if (value != _partyMemberCountText)
			{
				_partyMemberCountText = value;
				OnPropertyChanged("PartyMemberCountText");
			}
		}
	}

	[DataSourceProperty]
	public string PlayerText
	{
		get
		{
			return _playerText;
		}
		set
		{
			if (value != _playerText)
			{
				_playerText = value;
				OnPropertyChanged("PlayerText");
			}
		}
	}

	[DataSourceProperty]
	public string CreateYourClanText
	{
		get
		{
			return _createYourClanText;
		}
		set
		{
			if (value != _createYourClanText)
			{
				_createYourClanText = value;
				OnPropertyChanged("CreateYourClanText");
			}
		}
	}

	[DataSourceProperty]
	public string CloseText
	{
		get
		{
			return _closeText;
		}
		set
		{
			if (value != _closeText)
			{
				_closeText = value;
				OnPropertyChangedWithValue(value, "CloseText");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<MPLobbyClanMemberItemVM> PartyMembers
	{
		get
		{
			return _partyMembers;
		}
		set
		{
			if (value != _partyMembers)
			{
				_partyMembers = value;
				OnPropertyChanged("PartyMembers");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel CantCreateHint
	{
		get
		{
			return _cantCreateHint;
		}
		set
		{
			if (value != _cantCreateHint)
			{
				_cantCreateHint = value;
				OnPropertyChanged("CantCreateHint");
			}
		}
	}

	public MPLobbyClanCreationInformationVM(Action openClanCreationPopup)
	{
		_openClanCreationPopup = openClanCreationPopup;
		PartyMembers = new MBBindingList<MPLobbyClanMemberItemVM>();
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		CloseText = GameTexts.FindText("str_close").ToString();
		CreateClanText = new TextObject("{=ECb8IPbA}Create Clan").ToString();
		CreateClanDescriptionText = new TextObject("{=aWzdkfvn}Currently you are not a member of a clan or you don't own a clan. You need to create a party from non-clan member players to form your own clan.").ToString();
		CreateYourClanText = new TextObject("{=kF3b8cH1}Create Your Clan").ToString();
		DontHaveEnoughPlayersInPartyText = new TextObject("{=bynNUfSr}Your party does not have enough members to create a clan.").ToString();
		PlayerText = new TextObject("{=RN6zHak0}Player").ToString();
	}

	public void RefreshWith(ClanHomeInfo info)
	{
		if (info == null)
		{
			CanCreateClan = false;
			CantCreateHint = new HintViewModel(new TextObject("{=EQAjujjO}Clan creation information can't be retrieved"));
			DoesHaveEnoughPlayersToCreateClan = false;
			PartyMemberCountText = new TextObject("{=y1AGNqyV}Clan creation is not available").ToString();
			PartyMembers.Clear();
			PartyMembers.Add(new MPLobbyClanMemberItemVM(NetworkMain.GameClient.PlayerID));
			return;
		}
		CanCreateClan = info.CanCreateClan && NetworkMain.GameClient.IsPartyLeader;
		CantCreateHint = new HintViewModel();
		if (!NetworkMain.GameClient.IsPartyLeader)
		{
			CantCreateHint = new HintViewModel(new TextObject("{=OiWquyWY}You have to be the leader of the party to create a clan"));
		}
		if (info.NotEnoughPlayersInfo == null)
		{
			DoesHaveEnoughPlayersToCreateClan = true;
		}
		else
		{
			CurrentPlayerCount = info.NotEnoughPlayersInfo.CurrentPlayerCount;
			RequiredPlayerCount = info.NotEnoughPlayersInfo.RequiredPlayerCount;
			DoesHaveEnoughPlayersToCreateClan = CurrentPlayerCount == RequiredPlayerCount;
			GameTexts.SetVariable("LEFT", CurrentPlayerCount);
			GameTexts.SetVariable("RIGHT", RequiredPlayerCount);
			GameTexts.SetVariable("STR1", GameTexts.FindText("str_LEFT_over_RIGHT").ToString());
			GameTexts.SetVariable("STR2", PlayerText);
			PartyMemberCountText = GameTexts.FindText("str_STR1_space_STR2").ToString();
		}
		PartyMembers.Clear();
		if (NetworkMain.GameClient.IsInParty)
		{
			foreach (PartyPlayerInLobbyClient item in NetworkMain.GameClient.PlayersInParty)
			{
				MPLobbyClanMemberItemVM mPLobbyClanMemberItemVM = new MPLobbyClanMemberItemVM(item.PlayerId);
				if (info.PlayerNotEligibleInfos != null)
				{
					PlayerNotEligibleInfo[] playerNotEligibleInfos = info.PlayerNotEligibleInfos;
					foreach (PlayerNotEligibleInfo playerNotEligibleInfo in playerNotEligibleInfos)
					{
						if (playerNotEligibleInfo.PlayerId == item.PlayerId)
						{
							PlayerNotEligibleError[] errors = playerNotEligibleInfo.Errors;
							foreach (PlayerNotEligibleError notEligibleInfo in errors)
							{
								mPLobbyClanMemberItemVM.SetNotEligibleInfo(notEligibleInfo);
							}
						}
					}
				}
				PartyMembers.Add(mPLobbyClanMemberItemVM);
			}
			return;
		}
		PartyMembers.Add(new MPLobbyClanMemberItemVM(NetworkMain.GameClient.PlayerID));
	}

	public void OnFriendListUpdated(bool forceUpdate = false)
	{
		foreach (MPLobbyClanMemberItemVM partyMember in PartyMembers)
		{
			partyMember.UpdateNameAndAvatar(forceUpdate);
		}
	}

	public void OnPlayerNameUpdated()
	{
		for (int i = 0; i < PartyMembers.Count; i++)
		{
			MPLobbyClanMemberItemVM mPLobbyClanMemberItemVM = PartyMembers[i];
			if (mPLobbyClanMemberItemVM.Id == NetworkMain.GameClient.PlayerID)
			{
				mPLobbyClanMemberItemVM.UpdateNameAndAvatar(forceUpdate: true);
			}
		}
	}

	public void ExecuteOpenPopup()
	{
		IsEnabled = true;
	}

	public void ExecuteClosePopup()
	{
		IsEnabled = false;
	}

	private void ExecuteOpenClanCreationPopup()
	{
		ExecuteClosePopup();
		_openClanCreationPopup();
	}
}
