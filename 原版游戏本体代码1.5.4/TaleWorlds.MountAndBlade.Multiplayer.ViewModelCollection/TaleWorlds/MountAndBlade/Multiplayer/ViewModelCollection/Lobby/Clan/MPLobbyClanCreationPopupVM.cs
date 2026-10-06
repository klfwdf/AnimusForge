using System.Collections.Generic;
using Messages.FromLobbyServer.ToClient;
using TaleWorlds.Core;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade.Diamond;
using TaleWorlds.MountAndBlade.ViewModelCollection.Input;
using TaleWorlds.PlatformService;
using TaleWorlds.PlayerServices;

namespace TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.Lobby.Clan;

public class MPLobbyClanCreationPopupVM : ViewModel
{
	private MPCultureItemVM _selectedFaction;

	private MPLobbySigilItemVM _selectedSigilIcon;

	private InputKeyItemVM _cancelInputKey;

	private bool _isEnabled;

	private bool _hasCreationStarted;

	private bool _isWaiting;

	private string _createClanText;

	private string _nameText;

	private string _nameErrorText;

	private string _tagText;

	private string _tagErrorText;

	private string _factionText;

	private string _factionErrorText;

	private string _sigilText;

	private string _sigilIconErrorText;

	private string _createText;

	private string _cancelText;

	private string _nameInputText;

	private string _tagInputText;

	private string _waitingForConfirmationText;

	private MBBindingList<MPCultureItemVM> _factionsList;

	private MBBindingList<MPLobbySigilItemVM> _iconsList;

	private MBBindingList<MPLobbyClanMemberItemVM> _partyMembersList;

	[DataSourceProperty]
	public InputKeyItemVM CancelInputKey
	{
		get
		{
			return _cancelInputKey;
		}
		set
		{
			if (value != _cancelInputKey)
			{
				_cancelInputKey = value;
				OnPropertyChanged("CancelInputKey");
			}
		}
	}

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
	public bool HasCreationStarted
	{
		get
		{
			return _hasCreationStarted;
		}
		set
		{
			if (value != _hasCreationStarted)
			{
				_hasCreationStarted = value;
				OnPropertyChanged("HasCreationStarted");
			}
		}
	}

	[DataSourceProperty]
	public bool IsWaiting
	{
		get
		{
			return _isWaiting;
		}
		set
		{
			if (value != _isWaiting)
			{
				_isWaiting = value;
				OnPropertyChanged("IsWaiting");
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
	public string NameErrorText
	{
		get
		{
			return _nameErrorText;
		}
		set
		{
			if (value != _nameErrorText)
			{
				_nameErrorText = value;
				OnPropertyChanged("NameErrorText");
			}
		}
	}

	[DataSourceProperty]
	public string TagText
	{
		get
		{
			return _tagText;
		}
		set
		{
			if (value != _tagText)
			{
				_tagText = value;
				OnPropertyChanged("TagText");
			}
		}
	}

	[DataSourceProperty]
	public string TagErrorText
	{
		get
		{
			return _tagErrorText;
		}
		set
		{
			if (value != _tagErrorText)
			{
				_tagErrorText = value;
				OnPropertyChanged("TagErrorText");
			}
		}
	}

	[DataSourceProperty]
	public string FactionText
	{
		get
		{
			return _factionText;
		}
		set
		{
			if (value != _factionText)
			{
				_factionText = value;
				OnPropertyChanged("FactionText");
			}
		}
	}

	[DataSourceProperty]
	public string FactionErrorText
	{
		get
		{
			return _factionErrorText;
		}
		set
		{
			if (value != _factionErrorText)
			{
				_factionErrorText = value;
				OnPropertyChanged("FactionErrorText");
			}
		}
	}

	[DataSourceProperty]
	public string SigilText
	{
		get
		{
			return _sigilText;
		}
		set
		{
			if (value != _sigilText)
			{
				_sigilText = value;
				OnPropertyChanged("SigilText");
			}
		}
	}

	[DataSourceProperty]
	public string SigilIconErrorText
	{
		get
		{
			return _sigilIconErrorText;
		}
		set
		{
			if (value != _sigilIconErrorText)
			{
				_sigilIconErrorText = value;
				OnPropertyChanged("SigilIconErrorText");
			}
		}
	}

	[DataSourceProperty]
	public string CreateText
	{
		get
		{
			return _createText;
		}
		set
		{
			if (value != _createText)
			{
				_createText = value;
				OnPropertyChanged("CreateText");
			}
		}
	}

	[DataSourceProperty]
	public string CancelText
	{
		get
		{
			return _cancelText;
		}
		set
		{
			if (value != _cancelText)
			{
				_cancelText = value;
				OnPropertyChanged("CancelText");
			}
		}
	}

	[DataSourceProperty]
	public string NameInputText
	{
		get
		{
			return _nameInputText;
		}
		set
		{
			if (value != _nameInputText)
			{
				_nameInputText = value;
				OnPropertyChanged("NameInputText");
				NameErrorText = "";
			}
		}
	}

	[DataSourceProperty]
	public string TagInputText
	{
		get
		{
			return _tagInputText;
		}
		set
		{
			if (value != _tagInputText)
			{
				_tagInputText = value;
				OnPropertyChanged("TagInputText");
				TagErrorText = "";
			}
		}
	}

	[DataSourceProperty]
	public string WaitingForConfirmationText
	{
		get
		{
			return _waitingForConfirmationText;
		}
		set
		{
			if (value != _waitingForConfirmationText)
			{
				_waitingForConfirmationText = value;
				OnPropertyChanged("WaitingForConfirmationText");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<MPCultureItemVM> FactionsList
	{
		get
		{
			return _factionsList;
		}
		set
		{
			if (value != _factionsList)
			{
				_factionsList = value;
				OnPropertyChanged("FactionsList");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<MPLobbySigilItemVM> IconsList
	{
		get
		{
			return _iconsList;
		}
		set
		{
			if (value != _iconsList)
			{
				_iconsList = value;
				OnPropertyChanged("IconsList");
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

	public MPLobbyClanCreationPopupVM()
	{
		PartyMembersList = new MBBindingList<MPLobbyClanMemberItemVM>();
		PrepareFactionsList();
		PrepareSigilIconsList();
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		CreateClanText = new TextObject("{=ECb8IPbA}Create Clan").ToString();
		NameText = new TextObject("{=PDdh1sBj}Name").ToString();
		TagText = new TextObject("{=OUvFT99g}Tag").ToString();
		FactionText = new TextObject("{=PUjDWe5j}Culture").ToString();
		SigilText = new TextObject("{=P5Z9owOy}Sigil").ToString();
		CreateText = new TextObject("{=65oGXBYQ}Create").ToString();
		CancelText = new TextObject("{=3CpNUnVl}Cancel").ToString();
		WaitingForConfirmationText = new TextObject("{=08KLQa3P}Waiting For Party Members").ToString();
		ResetAll();
	}

	private void ResetAll()
	{
		ResetErrorTexts();
		ResetUserInputs();
	}

	private void ResetErrorTexts()
	{
		NameErrorText = "";
		TagErrorText = "";
		FactionErrorText = "";
		SigilIconErrorText = "";
	}

	private void ResetUserInputs()
	{
		NameInputText = "";
		TagInputText = "";
		OnFactionSelection(null);
		OnSigilIconSelection(null);
	}

	public void ExecuteOpenPopup()
	{
		RefreshValues();
		IsEnabled = true;
	}

	public void ExecuteClosePopup()
	{
		IsEnabled = false;
	}

	private void PrepareFactionsList()
	{
		_selectedFaction = null;
		FactionsList = new MBBindingList<MPCultureItemVM>
		{
			new MPCultureItemVM(Game.Current.ObjectManager.GetObject<BasicCultureObject>("vlandia").StringId, OnFactionSelection),
			new MPCultureItemVM(Game.Current.ObjectManager.GetObject<BasicCultureObject>("sturgia").StringId, OnFactionSelection),
			new MPCultureItemVM(Game.Current.ObjectManager.GetObject<BasicCultureObject>("empire").StringId, OnFactionSelection),
			new MPCultureItemVM(Game.Current.ObjectManager.GetObject<BasicCultureObject>("battania").StringId, OnFactionSelection),
			new MPCultureItemVM(Game.Current.ObjectManager.GetObject<BasicCultureObject>("khuzait").StringId, OnFactionSelection),
			new MPCultureItemVM(Game.Current.ObjectManager.GetObject<BasicCultureObject>("aserai").StringId, OnFactionSelection)
		};
	}

	private void PrepareSigilIconsList()
	{
		IconsList = new MBBindingList<MPLobbySigilItemVM>();
		_selectedSigilIcon = null;
		foreach (BannerIconGroup bannerIconGroup in BannerManager.Instance.BannerIconGroups)
		{
			if (bannerIconGroup.IsPattern)
			{
				continue;
			}
			foreach (KeyValuePair<int, BannerIconData> availableIcon in bannerIconGroup.AvailableIcons)
			{
				MPLobbySigilItemVM item = new MPLobbySigilItemVM(availableIcon.Key, OnSigilIconSelection);
				IconsList.Add(item);
			}
		}
	}

	private void PreparePartyMembersList()
	{
		PartyMembersList.Clear();
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

	private void OnFactionSelection(MPCultureItemVM faction)
	{
		if (faction != _selectedFaction)
		{
			if (_selectedFaction != null)
			{
				_selectedFaction.IsSelected = false;
			}
			_selectedFaction = faction;
			if (_selectedFaction != null)
			{
				_selectedFaction.IsSelected = true;
				FactionErrorText = "";
			}
		}
	}

	private void OnSigilIconSelection(MPLobbySigilItemVM sigilIcon)
	{
		if (sigilIcon != _selectedSigilIcon)
		{
			if (_selectedSigilIcon != null)
			{
				_selectedSigilIcon.IsSelected = false;
			}
			_selectedSigilIcon = sigilIcon;
			if (_selectedSigilIcon != null)
			{
				_selectedSigilIcon.IsSelected = true;
				SigilIconErrorText = "";
			}
		}
	}

	private void UpdateNameErrorText(StringValidationError error)
	{
		NameErrorText = "";
		switch (error)
		{
		case StringValidationError.InvalidLength:
			NameErrorText = new TextObject("{=bExIl1A2}Name Length Is Invalid").ToString();
			break;
		case StringValidationError.AlreadyExists:
			NameErrorText = new TextObject("{=Agtv9l7S}This Name Already Exists").ToString();
			break;
		case StringValidationError.HasNonLettersCharacters:
			NameErrorText = new TextObject("{=lO1hok44}Name Has Invalid Characters In It").ToString();
			break;
		case StringValidationError.ContainsProfanity:
			NameErrorText = new TextObject("{=cl2DnRYR}Name Should Not Contain Offensive Words").ToString();
			break;
		case StringValidationError.Unspecified:
			NameErrorText = new TextObject("{=UEgS8RcB}Name Has Invalid Content").ToString();
			break;
		}
	}

	private void UpdateTagErrorText(StringValidationError error)
	{
		TagErrorText = "";
		switch (error)
		{
		case StringValidationError.InvalidLength:
			TagErrorText = new TextObject("{=MjnlWhih}Tag Length Is Invalid").ToString();
			break;
		case StringValidationError.AlreadyExists:
			TagErrorText = new TextObject("{=ulzyykHO}This Tag Already Exists").ToString();
			break;
		case StringValidationError.HasNonLettersCharacters:
			TagErrorText = new TextObject("{=FjmxNxZJ}Tag Has Invalid Characters In It").ToString();
			break;
		case StringValidationError.ContainsProfanity:
			TagErrorText = new TextObject("{=jyJXcOLe}Tag Should Not Contain Offensive Words").ToString();
			break;
		case StringValidationError.Unspecified:
			TagErrorText = new TextObject("{=hCNnqVgK}Tag Has Invalid Content").ToString();
			break;
		}
	}

	public void UpdateFactionErrorText()
	{
		FactionErrorText = "";
		FactionErrorText = new TextObject("{=p83IO9ls}You must select a culture").ToString();
	}

	public void UpdateSigilIconErrorText()
	{
		SigilIconErrorText = "";
		SigilIconErrorText = new TextObject("{=uOrwqeQl}You must select a sigil icon").ToString();
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

	private BasicCultureObject GetSelectedCulture()
	{
		return Game.Current.ObjectManager.GetObject<BasicCultureObject>(_selectedFaction.CultureCode);
	}

	private Banner GetCreatedClanSigil()
	{
		BasicCultureObject selectedCulture = GetSelectedCulture();
		Banner banner = new Banner(selectedCulture.Banner, selectedCulture.BackgroundColor1, selectedCulture.ForegroundColor1);
		banner.SetIconMeshId(_selectedSigilIcon.IconID);
		return banner;
	}

	private async void ExecuteTryCreateClan()
	{
		bool areAllInputsValid = true;
		ResetErrorTexts();
		CheckClanParameterValidResult checkClanParameterValidResult = await NetworkMain.GameClient.ClanNameExists(NameInputText);
		if (!checkClanParameterValidResult.IsValid)
		{
			areAllInputsValid = false;
			UpdateNameErrorText(checkClanParameterValidResult.Error);
		}
		if (!(await PlatformServices.Instance.VerifyString(NameInputText)))
		{
			areAllInputsValid = false;
			UpdateNameErrorText(StringValidationError.Unspecified);
		}
		CheckClanParameterValidResult checkClanParameterValidResult2 = await NetworkMain.GameClient.ClanTagExists(TagInputText);
		if (!checkClanParameterValidResult2.IsValid)
		{
			areAllInputsValid = false;
			UpdateTagErrorText(checkClanParameterValidResult2.Error);
		}
		if (!(await PlatformServices.Instance.VerifyString(TagInputText)))
		{
			areAllInputsValid = false;
			UpdateTagErrorText(StringValidationError.Unspecified);
		}
		if (_selectedFaction == null)
		{
			areAllInputsValid = false;
			UpdateFactionErrorText();
		}
		if (_selectedSigilIcon == null)
		{
			areAllInputsValid = false;
			UpdateSigilIconErrorText();
		}
		if (areAllInputsValid)
		{
			HasCreationStarted = true;
			NetworkMain.GameClient.SendCreateClanMessage(NameInputText, TagInputText, GetSelectedCulture().StringId, GetCreatedClanSigil().Serialize());
		}
	}

	public void ExecuteSwitchToWaiting()
	{
		PreparePartyMembersList();
		IsWaiting = true;
	}

	public override void OnFinalize()
	{
		base.OnFinalize();
		CancelInputKey?.OnFinalize();
	}

	public void SetCancelInputKey(HotKey hotKey)
	{
		CancelInputKey = InputKeyItemVM.CreateFromHotKey(hotKey, isConsoleOnly: true);
	}
}
