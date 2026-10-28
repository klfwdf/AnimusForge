using System;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade.Diamond;
using TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.Lobby.Friends;
using TaleWorlds.PlayerServices;

namespace TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.Lobby.Clan;

public class MPLobbyClanMatchmakingRequestPopupVM : ViewModel
{
	private Guid _partyId;

	private bool _isEnabled;

	private bool _isClanMatch;

	private bool _isPracticeMatch;

	private string _titleText;

	private string _clanName;

	private string _wantsToJoinText;

	private string _doYouAcceptText;

	private BannerImageIdentifierVM _clanSigil;

	private MPLobbyPlayerBaseVM _challengerPartyLeader;

	private MBBindingList<MPLobbyPlayerBaseVM> _challengerPartyPlayers;

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
	public bool IsClanMatch
	{
		get
		{
			return _isClanMatch;
		}
		set
		{
			if (value != _isClanMatch)
			{
				_isClanMatch = value;
				OnPropertyChanged("IsClanMatch");
			}
		}
	}

	[DataSourceProperty]
	public bool IsPracticeMatch
	{
		get
		{
			return _isPracticeMatch;
		}
		set
		{
			if (value != _isPracticeMatch)
			{
				_isPracticeMatch = value;
				OnPropertyChanged("IsPracticeMatch");
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
	public string ClanName
	{
		get
		{
			return _clanName;
		}
		set
		{
			if (value != _clanName)
			{
				_clanName = value;
				OnPropertyChanged("ClanName");
			}
		}
	}

	[DataSourceProperty]
	public string WantsToJoinText
	{
		get
		{
			return _wantsToJoinText;
		}
		set
		{
			if (value != _wantsToJoinText)
			{
				_wantsToJoinText = value;
				OnPropertyChanged("WantsToJoinText");
			}
		}
	}

	[DataSourceProperty]
	public string DoYouAcceptText
	{
		get
		{
			return _doYouAcceptText;
		}
		set
		{
			if (value != _doYouAcceptText)
			{
				_doYouAcceptText = value;
				OnPropertyChanged("DoYouAcceptText");
			}
		}
	}

	[DataSourceProperty]
	public BannerImageIdentifierVM ClanSigil
	{
		get
		{
			return _clanSigil;
		}
		set
		{
			if (value != _clanSigil)
			{
				_clanSigil = value;
				OnPropertyChanged("ClanSigil");
			}
		}
	}

	[DataSourceProperty]
	public MPLobbyPlayerBaseVM ChallengerPartyLeader
	{
		get
		{
			return _challengerPartyLeader;
		}
		set
		{
			if (value != _challengerPartyLeader)
			{
				_challengerPartyLeader = value;
				OnPropertyChangedWithValue(value, "ChallengerPartyLeader");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<MPLobbyPlayerBaseVM> ChallengerPartyPlayers
	{
		get
		{
			return _challengerPartyPlayers;
		}
		set
		{
			if (value != _challengerPartyPlayers)
			{
				_challengerPartyPlayers = value;
				OnPropertyChangedWithValue(value, "ChallengerPartyPlayers");
			}
		}
	}

	public MPLobbyClanMatchmakingRequestPopupVM()
	{
		ChallengerPartyPlayers = new MBBindingList<MPLobbyPlayerBaseVM>();
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		TitleText = new TextObject("{=1pwQgr04}Matchmaking Request").ToString();
		WantsToJoinText = new TextObject("{=WHKG5Rbq}This team wants to join the match you created.").ToString();
		DoYouAcceptText = new TextObject("{=xkV9g4le}Do you accept them as your opponent?").ToString();
	}

	public void OpenWith(string clanName, string clanSigilCode, Guid partyId, PlayerId[] challengerPlayerIDs, PlayerId challengerPartyLeaderID, PremadeGameType premadeGameType)
	{
		ChallengerPartyPlayers.Clear();
		IsClanMatch = false;
		IsPracticeMatch = false;
		_partyId = partyId;
		switch (premadeGameType)
		{
		case PremadeGameType.Clan:
			IsClanMatch = true;
			ClanName = clanName;
			ClanSigil = new BannerImageIdentifierVM(new Banner(clanSigilCode), nineGrid: true);
			break;
		case PremadeGameType.Practice:
			IsPracticeMatch = true;
			ChallengerPartyLeader = new MPLobbyPlayerBaseVM(challengerPartyLeaderID);
			foreach (PlayerId id in challengerPlayerIDs)
			{
				ChallengerPartyPlayers.Add(new MPLobbyPlayerBaseVM(id));
			}
			break;
		}
		IsEnabled = true;
	}

	public void Close()
	{
		IsEnabled = false;
	}

	public void ExecuteAcceptMatchmaking()
	{
		NetworkMain.GameClient.AcceptJoinPremadeGameRequest(_partyId);
		Close();
	}

	public void ExecuteDeclineMatchmaking()
	{
		NetworkMain.GameClient.DeclineJoinPremadeGameRequest(_partyId);
		Close();
	}
}
