using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Avatar.PlayerServices;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade.Diamond;
using TaleWorlds.MountAndBlade.Diamond.MultiplayerBadges;
using TaleWorlds.MountAndBlade.Diamond.Ranked;
using TaleWorlds.ObjectSystem;
using TaleWorlds.PlatformService;
using TaleWorlds.PlayerServices;

namespace TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.Lobby.Friends;

public class MPLobbyPlayerBaseVM : ViewModel
{
	public enum OnlineStatus
	{
		None,
		InGame,
		Online,
		Offline
	}

	public static Action<PlayerId> OnPlayerProfileRequested;

	public static Action<PlayerId> OnBannerlordIDChangeRequested;

	public static Action<PlayerId> OnAddFriendWithBannerlordIDRequested;

	public static Action<PlayerId> OnSigilChangeRequested;

	public static Action<PlayerId> OnBadgeChangeRequested;

	public static Action<MPLobbyPlayerBaseVM> OnRankProgressionRequested;

	public static Action<string> OnRankLeaderboardRequested;

	public static Action OnClanPageRequested;

	public static Action OnClanLeaderboardRequested;

	private const int DefaultBannerBackgroundColorId = 99;

	private CustomBattleId? _spectatableCustomBattleId;

	private PlayerId _providedID;

	private readonly string _forcedName = string.Empty;

	private int _forcedAvatarIndex = -1;

	private Action<PlayerId> _onInviteToParty;

	private readonly Action<PlayerId> _onInviteToClan;

	private readonly Action<PlayerId> _onFriendRequestAnswered;

	private bool _isKnownPlayer;

	private readonly TextObject _genericPlayerName = new TextObject("{=RN6zHak0}Player");

	public Action OnPlayerStatsReceived;

	protected bool _hasReceivedPlayerStats;

	protected bool _isReceivingPlayerStats;

	private const string _skirmishGameTypeID = "Skirmish";

	private const string _captainGameTypeID = "Captain";

	private const string _duelGameTypeID = "Duel";

	private const string _teamDeathmatchGameTypeID = "TeamDeathmatch";

	private const string _siegeGameTypeID = "Siege";

	public Action<string> OnRankInfoChanged;

	private bool _canCopyID;

	private bool _showLevel;

	private bool _isSelected;

	private bool _hasNotification;

	private bool _isFriendRequest;

	private bool _isPendingRequest;

	private bool _canRemove;

	private bool _canBeInvited;

	private bool _canInviteToParty;

	private bool _canInviteToClan;

	private bool _isSigilChangeInformationEnabled;

	private bool _isRankInfoLoading;

	private bool _isRankInfoCasual;

	private bool _isClanInfoSupported;

	private bool _isBannerlordIDSupported;

	private int _level;

	private int _rating;

	private int _loot;

	private int _experienceRatio;

	private int _ratingRatio;

	private string _name = "";

	private string _stateText;

	private string _levelText;

	private string _levelTitleText;

	private string _ratingText;

	private string _gameTypeText;

	private string _ratingID;

	private string _clanName;

	private string _clanTag;

	private string _changeText;

	private string _clanInfoTitleText;

	private string _badgeInfoTitleText;

	private string _avatarInfoTitleText;

	private string _experienceText;

	private string _rankText;

	private string _bannerlordID;

	private string _selectedBadgeID;

	private HintViewModel _nameHint;

	private HintViewModel _inviteToPartyHint;

	private HintViewModel _removeFriendHint;

	private HintViewModel _acceptFriendRequestHint;

	private HintViewModel _declineFriendRequestHint;

	private HintViewModel _cancelFriendRequestHint;

	private HintViewModel _inviteToClanHint;

	private HintViewModel _changeBannerlordIDHint;

	private HintViewModel _copyBannerlordIDHint;

	private HintViewModel _addFriendWithBannerlordIDHint;

	private HintViewModel _experienceHint;

	private HintViewModel _ratingHint;

	private HintViewModel _lootHint;

	private HintViewModel _skirmishRatingHint;

	private HintViewModel _captainRatingHint;

	private HintViewModel _clanLeaderboardHint;

	private PlayerAvatarImageIdentifierVM _avatar;

	private BannerImageIdentifierVM _clanBanner;

	private MPLobbySigilItemVM _sigil;

	private MPLobbyBadgeItemVM _shownBadge;

	private CharacterViewModel _characterVisual;

	private MBBindingList<MPLobbyPlayerStatItemVM> _displayedStats;

	private MBBindingList<MPLobbyGameTypeVM> _gameTypes;

	private bool _canSpectate;

	public OnlineStatus CurrentOnlineStatus { get; private set; }

	public PlayerId ProvidedID
	{
		get
		{
			return _providedID;
		}
		protected set
		{
			if (_providedID != value)
			{
				_providedID = value;
				UpdateAvatar(NetworkMain.GameClient?.IsKnownPlayer(ProvidedID) ?? false);
			}
		}
	}

	public PlayerData PlayerData { get; private set; }

	public AnotherPlayerState State { get; protected set; }

	public float TimeSinceLastStateUpdate { get; protected set; }

	public PlayerStatsBase[] PlayerStats { get; private set; }

	public GameTypeRankInfo[] RankInfo { get; private set; }

	public string RankInfoGameTypeID { get; private set; }

	[DataSourceProperty]
	public bool CanCopyID
	{
		get
		{
			return _canCopyID;
		}
		set
		{
			if (value != _canCopyID)
			{
				_canCopyID = value;
				OnPropertyChangedWithValue(value, "CanCopyID");
			}
		}
	}

	[DataSourceProperty]
	public bool ShowLevel
	{
		get
		{
			return _showLevel;
		}
		set
		{
			if (value != _showLevel)
			{
				_showLevel = value;
				OnPropertyChangedWithValue(value, "ShowLevel");
			}
		}
	}

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
	public bool HasNotification
	{
		get
		{
			return _hasNotification;
		}
		set
		{
			if (value != _hasNotification)
			{
				_hasNotification = value;
				OnPropertyChangedWithValue(value, "HasNotification");
			}
		}
	}

	[DataSourceProperty]
	public bool IsFriendRequest
	{
		get
		{
			return _isFriendRequest;
		}
		set
		{
			if (value != _isFriendRequest)
			{
				_isFriendRequest = value;
				OnPropertyChangedWithValue(value, "IsFriendRequest");
			}
		}
	}

	[DataSourceProperty]
	public bool IsPendingRequest
	{
		get
		{
			return _isPendingRequest;
		}
		set
		{
			if (value != _isPendingRequest)
			{
				_isPendingRequest = value;
				OnPropertyChangedWithValue(value, "IsPendingRequest");
			}
		}
	}

	[DataSourceProperty]
	public bool CanRemove
	{
		get
		{
			return _canRemove;
		}
		set
		{
			if (value != _canRemove)
			{
				_canRemove = value;
				OnPropertyChangedWithValue(value, "CanRemove");
			}
		}
	}

	[DataSourceProperty]
	public bool CanBeInvited
	{
		get
		{
			return _canBeInvited;
		}
		set
		{
			if (value != _canBeInvited)
			{
				_canBeInvited = value;
				OnPropertyChangedWithValue(value, "CanBeInvited");
			}
		}
	}

	[DataSourceProperty]
	public bool CanInviteToParty
	{
		get
		{
			return _canInviteToParty;
		}
		set
		{
			if (value != _canInviteToParty)
			{
				_canInviteToParty = value;
				OnPropertyChangedWithValue(value, "CanInviteToParty");
			}
		}
	}

	[DataSourceProperty]
	public bool CanSpectate
	{
		get
		{
			return _canSpectate;
		}
		set
		{
			if (value != _canSpectate)
			{
				_canSpectate = value;
				OnPropertyChangedWithValue(value, "CanSpectate");
			}
		}
	}

	[DataSourceProperty]
	public bool CanInviteToClan
	{
		get
		{
			return _canInviteToClan;
		}
		set
		{
			if (value != _canInviteToClan)
			{
				_canInviteToClan = value;
				OnPropertyChangedWithValue(value, "CanInviteToClan");
			}
		}
	}

	[DataSourceProperty]
	public bool IsSigilChangeInformationEnabled
	{
		get
		{
			return _isSigilChangeInformationEnabled;
		}
		set
		{
			if (value != _isSigilChangeInformationEnabled)
			{
				_isSigilChangeInformationEnabled = value;
				OnPropertyChangedWithValue(value, "IsSigilChangeInformationEnabled");
			}
		}
	}

	[DataSourceProperty]
	public bool IsRankInfoLoading
	{
		get
		{
			return _isRankInfoLoading;
		}
		set
		{
			if (value != _isRankInfoLoading)
			{
				_isRankInfoLoading = value;
				OnPropertyChangedWithValue(value, "IsRankInfoLoading");
			}
		}
	}

	[DataSourceProperty]
	public bool IsRankInfoCasual
	{
		get
		{
			return _isRankInfoCasual;
		}
		set
		{
			if (value != _isRankInfoCasual)
			{
				_isRankInfoCasual = value;
				OnPropertyChangedWithValue(value, "IsRankInfoCasual");
			}
		}
	}

	[DataSourceProperty]
	public bool IsClanInfoSupported
	{
		get
		{
			return _isClanInfoSupported;
		}
		set
		{
			if (value != _isClanInfoSupported)
			{
				_isClanInfoSupported = value;
				OnPropertyChangedWithValue(value, "IsClanInfoSupported");
			}
		}
	}

	[DataSourceProperty]
	public bool IsBannerlordIDSupported
	{
		get
		{
			return _isBannerlordIDSupported;
		}
		set
		{
			if (value != _isBannerlordIDSupported)
			{
				_isBannerlordIDSupported = value;
				OnPropertyChangedWithValue(value, "IsBannerlordIDSupported");
			}
		}
	}

	[DataSourceProperty]
	public int Level
	{
		get
		{
			return _level;
		}
		set
		{
			if (value != _level)
			{
				_level = value;
				OnPropertyChangedWithValue(value, "Level");
			}
		}
	}

	[DataSourceProperty]
	public int Rating
	{
		get
		{
			return _rating;
		}
		set
		{
			if (value != _rating)
			{
				_rating = value;
				OnPropertyChangedWithValue(value, "Rating");
			}
		}
	}

	[DataSourceProperty]
	public int Loot
	{
		get
		{
			return _loot;
		}
		set
		{
			if (value != _loot)
			{
				_loot = value;
				OnPropertyChangedWithValue(value, "Loot");
			}
		}
	}

	[DataSourceProperty]
	public int ExperienceRatio
	{
		get
		{
			return _experienceRatio;
		}
		set
		{
			if (value != _experienceRatio)
			{
				_experienceRatio = value;
				OnPropertyChangedWithValue(value, "ExperienceRatio");
			}
		}
	}

	[DataSourceProperty]
	public int RatingRatio
	{
		get
		{
			return _ratingRatio;
		}
		set
		{
			if (value != _ratingRatio)
			{
				_ratingRatio = value;
				OnPropertyChangedWithValue(value, "RatingRatio");
			}
		}
	}

	[DataSourceProperty]
	public string Name
	{
		get
		{
			return _name;
		}
		set
		{
			if (value != _name)
			{
				_name = value;
				OnPropertyChangedWithValue(value, "Name");
			}
		}
	}

	[DataSourceProperty]
	public string StateText
	{
		get
		{
			return _stateText;
		}
		set
		{
			if (value != _stateText)
			{
				_stateText = value;
				OnPropertyChangedWithValue(value, "StateText");
			}
		}
	}

	[DataSourceProperty]
	public string LevelText
	{
		get
		{
			return _levelText;
		}
		set
		{
			if (value != _levelText)
			{
				_levelText = value;
				OnPropertyChangedWithValue(value, "LevelText");
			}
		}
	}

	[DataSourceProperty]
	public string LevelTitleText
	{
		get
		{
			return _levelTitleText;
		}
		set
		{
			if (value != _levelTitleText)
			{
				_levelTitleText = value;
				OnPropertyChangedWithValue(value, "LevelTitleText");
			}
		}
	}

	[DataSourceProperty]
	public string RatingText
	{
		get
		{
			return _ratingText;
		}
		set
		{
			if (value != _ratingText)
			{
				_ratingText = value;
				OnPropertyChangedWithValue(value, "RatingText");
			}
		}
	}

	[DataSourceProperty]
	public string GameTypeText
	{
		get
		{
			return _gameTypeText;
		}
		set
		{
			if (value != _gameTypeText)
			{
				_gameTypeText = value;
				OnPropertyChangedWithValue(value, "GameTypeText");
			}
		}
	}

	[DataSourceProperty]
	public string RatingID
	{
		get
		{
			return _ratingID;
		}
		set
		{
			if (value != _ratingID)
			{
				_ratingID = value;
				OnPropertyChangedWithValue(value, "RatingID");
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
				OnPropertyChangedWithValue(value, "ClanName");
			}
		}
	}

	[DataSourceProperty]
	public string ClanTag
	{
		get
		{
			return _clanTag;
		}
		set
		{
			if (value != _clanTag)
			{
				_clanTag = value;
				OnPropertyChangedWithValue(value, "ClanTag");
			}
		}
	}

	[DataSourceProperty]
	public string ChangeText
	{
		get
		{
			return _changeText;
		}
		set
		{
			if (value != _changeText)
			{
				_changeText = value;
				OnPropertyChangedWithValue(value, "ChangeText");
			}
		}
	}

	[DataSourceProperty]
	public string ClanInfoTitleText
	{
		get
		{
			return _clanInfoTitleText;
		}
		set
		{
			if (value != _clanInfoTitleText)
			{
				_clanInfoTitleText = value;
				OnPropertyChangedWithValue(value, "ClanInfoTitleText");
			}
		}
	}

	[DataSourceProperty]
	public string BadgeInfoTitleText
	{
		get
		{
			return _badgeInfoTitleText;
		}
		set
		{
			if (value != _badgeInfoTitleText)
			{
				_badgeInfoTitleText = value;
				OnPropertyChangedWithValue(value, "BadgeInfoTitleText");
			}
		}
	}

	[DataSourceProperty]
	public string AvatarInfoTitleText
	{
		get
		{
			return _avatarInfoTitleText;
		}
		set
		{
			if (value != _avatarInfoTitleText)
			{
				_avatarInfoTitleText = value;
				OnPropertyChangedWithValue(value, "AvatarInfoTitleText");
			}
		}
	}

	[DataSourceProperty]
	public string ExperienceText
	{
		get
		{
			return _experienceText;
		}
		set
		{
			if (value != _experienceText)
			{
				_experienceText = value;
				OnPropertyChangedWithValue(value, "ExperienceText");
			}
		}
	}

	[DataSourceProperty]
	public string RankText
	{
		get
		{
			return _rankText;
		}
		set
		{
			if (value != _rankText)
			{
				_rankText = value;
				OnPropertyChangedWithValue(value, "RankText");
			}
		}
	}

	[DataSourceProperty]
	public string BannerlordID
	{
		get
		{
			return _bannerlordID;
		}
		set
		{
			if (value != _bannerlordID)
			{
				_bannerlordID = value;
				OnPropertyChangedWithValue(value, "BannerlordID");
			}
		}
	}

	[DataSourceProperty]
	public string SelectedBadgeID
	{
		get
		{
			return _selectedBadgeID;
		}
		set
		{
			if (value != _selectedBadgeID)
			{
				_selectedBadgeID = value;
				OnPropertyChangedWithValue(value, "SelectedBadgeID");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel NameHint
	{
		get
		{
			return _nameHint;
		}
		set
		{
			if (value != _nameHint)
			{
				_nameHint = value;
				OnPropertyChangedWithValue(value, "NameHint");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel InviteToPartyHint
	{
		get
		{
			return _inviteToPartyHint;
		}
		set
		{
			if (value != _inviteToPartyHint)
			{
				_inviteToPartyHint = value;
				OnPropertyChangedWithValue(value, "InviteToPartyHint");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel RemoveFriendHint
	{
		get
		{
			return _removeFriendHint;
		}
		set
		{
			if (value != _removeFriendHint)
			{
				_removeFriendHint = value;
				OnPropertyChangedWithValue(value, "RemoveFriendHint");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel AcceptFriendRequestHint
	{
		get
		{
			return _acceptFriendRequestHint;
		}
		set
		{
			if (value != _acceptFriendRequestHint)
			{
				_acceptFriendRequestHint = value;
				OnPropertyChangedWithValue(value, "AcceptFriendRequestHint");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel DeclineFriendRequestHint
	{
		get
		{
			return _declineFriendRequestHint;
		}
		set
		{
			if (value != _declineFriendRequestHint)
			{
				_declineFriendRequestHint = value;
				OnPropertyChangedWithValue(value, "DeclineFriendRequestHint");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel CancelFriendRequestHint
	{
		get
		{
			return _cancelFriendRequestHint;
		}
		set
		{
			if (value != _cancelFriendRequestHint)
			{
				_cancelFriendRequestHint = value;
				OnPropertyChangedWithValue(value, "CancelFriendRequestHint");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel InviteToClanHint
	{
		get
		{
			return _inviteToClanHint;
		}
		set
		{
			if (value != _inviteToClanHint)
			{
				_inviteToClanHint = value;
				OnPropertyChangedWithValue(value, "InviteToClanHint");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel ChangeBannerlordIDHint
	{
		get
		{
			return _changeBannerlordIDHint;
		}
		set
		{
			if (value != _changeBannerlordIDHint)
			{
				_changeBannerlordIDHint = value;
				OnPropertyChangedWithValue(value, "ChangeBannerlordIDHint");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel CopyBannerlordIDHint
	{
		get
		{
			return _copyBannerlordIDHint;
		}
		set
		{
			if (value != _copyBannerlordIDHint)
			{
				_copyBannerlordIDHint = value;
				OnPropertyChangedWithValue(value, "CopyBannerlordIDHint");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel AddFriendWithBannerlordIDHint
	{
		get
		{
			return _addFriendWithBannerlordIDHint;
		}
		set
		{
			if (value != _addFriendWithBannerlordIDHint)
			{
				_addFriendWithBannerlordIDHint = value;
				OnPropertyChangedWithValue(value, "AddFriendWithBannerlordIDHint");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel ExperienceHint
	{
		get
		{
			return _experienceHint;
		}
		set
		{
			if (value != _experienceHint)
			{
				_experienceHint = value;
				OnPropertyChangedWithValue(value, "ExperienceHint");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel RatingHint
	{
		get
		{
			return _ratingHint;
		}
		set
		{
			if (value != _ratingHint)
			{
				_ratingHint = value;
				OnPropertyChangedWithValue(value, "RatingHint");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel LootHint
	{
		get
		{
			return _lootHint;
		}
		set
		{
			if (value != _lootHint)
			{
				_lootHint = value;
				OnPropertyChangedWithValue(value, "LootHint");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel SkirmishRatingHint
	{
		get
		{
			return _skirmishRatingHint;
		}
		set
		{
			if (value != _skirmishRatingHint)
			{
				_skirmishRatingHint = value;
				OnPropertyChangedWithValue(value, "SkirmishRatingHint");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel CaptainRatingHint
	{
		get
		{
			return _captainRatingHint;
		}
		set
		{
			if (value != _captainRatingHint)
			{
				_captainRatingHint = value;
				OnPropertyChangedWithValue(value, "CaptainRatingHint");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel ClanLeaderboardHint
	{
		get
		{
			return _clanLeaderboardHint;
		}
		set
		{
			if (value != _clanLeaderboardHint)
			{
				_clanLeaderboardHint = value;
				OnPropertyChanged("ClanLeaderboardHint");
			}
		}
	}

	[DataSourceProperty]
	public PlayerAvatarImageIdentifierVM Avatar
	{
		get
		{
			return _avatar;
		}
		set
		{
			if (value != _avatar)
			{
				_avatar = value;
				OnPropertyChangedWithValue(value, "Avatar");
			}
		}
	}

	[DataSourceProperty]
	public BannerImageIdentifierVM ClanBanner
	{
		get
		{
			return _clanBanner;
		}
		set
		{
			if (value != _clanBanner)
			{
				_clanBanner = value;
				OnPropertyChangedWithValue(value, "ClanBanner");
			}
		}
	}

	[DataSourceProperty]
	public MPLobbySigilItemVM Sigil
	{
		get
		{
			return _sigil;
		}
		set
		{
			if (value != _sigil)
			{
				_sigil = value;
				OnPropertyChangedWithValue(value, "Sigil");
			}
		}
	}

	[DataSourceProperty]
	public MPLobbyBadgeItemVM ShownBadge
	{
		get
		{
			return _shownBadge;
		}
		set
		{
			if (value != _shownBadge)
			{
				_shownBadge = value;
				OnPropertyChangedWithValue(value, "ShownBadge");
			}
		}
	}

	[DataSourceProperty]
	public CharacterViewModel CharacterVisual
	{
		get
		{
			return _characterVisual;
		}
		set
		{
			if (value != _characterVisual)
			{
				_characterVisual = value;
				OnPropertyChangedWithValue(value, "CharacterVisual");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<MPLobbyPlayerStatItemVM> DisplayedStats
	{
		get
		{
			return _displayedStats;
		}
		set
		{
			if (value != _displayedStats)
			{
				_displayedStats = value;
				OnPropertyChangedWithValue(value, "DisplayedStats");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<MPLobbyGameTypeVM> GameTypes
	{
		get
		{
			return _gameTypes;
		}
		set
		{
			if (value != _gameTypes)
			{
				_gameTypes = value;
				OnPropertyChangedWithValue(value, "GameTypes");
			}
		}
	}

	public MPLobbyPlayerBaseVM(PlayerId id, string forcedName = "", Action<PlayerId> onInviteToClan = null, Action<PlayerId> onFriendRequestAnswered = null)
	{
		ProvidedID = id;
		_forcedName = forcedName;
		SetOnInvite(null);
		_onInviteToClan = onInviteToClan;
		_onFriendRequestAnswered = onFriendRequestAnswered;
		NameHint = new HintViewModel();
		ExperienceHint = new HintViewModel();
		RatingHint = new HintViewModel();
		UpdateName(NetworkMain.GameClient?.IsKnownPlayer(ProvidedID) ?? false);
		CanBeInvited = true;
		CanInviteToParty = _onInviteToParty != null;
		CanInviteToClan = _onInviteToClan != null;
		PlatformServices.Instance.CheckPermissionWithUser(Permission.ViewUserGeneratedContent, id, delegate(bool hasBannerlordIDPrivilege)
		{
			CanCopyID = hasBannerlordIDPrivilege;
		});
		IsRankInfoLoading = true;
		GameTypes = new MBBindingList<MPLobbyGameTypeVM>();
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		ClanInfoTitleText = new TextObject("{=j4F7tTzy}Clan").ToString();
		BadgeInfoTitleText = new TextObject("{=4PrfimcK}Badge").ToString();
		AvatarInfoTitleText = new TextObject("{=5tbWdY1j}Avatar").ToString();
		ChangeText = new TextObject("{=Ba50zU7Z}Change").ToString();
		LevelTitleText = new TextObject("{=OKUTPdaa}Level").ToString();
		GameTypeText = new TextObject("{=JPimShCw}Game Type").ToString();
		InviteToPartyHint = new HintViewModel(new TextObject("{=aZnS9ECC}Invite"));
		InviteToClanHint = new HintViewModel(new TextObject("{=fLddxLjh}Invite to Clan"));
		RemoveFriendHint = new HintViewModel(new TextObject("{=d7ysGcsN}Remove Friend"));
		AcceptFriendRequestHint = new HintViewModel(new TextObject("{=BSUteZmt}Accept Friend Request"));
		DeclineFriendRequestHint = new HintViewModel(new TextObject("{=942B3LfA}Decline Friend Request"));
		CancelFriendRequestHint = new HintViewModel(new TextObject("{=lGbrWyEe}Cancel Friend Request"));
		LootHint = new HintViewModel(new TextObject("{=Th8q8wC2}Loot"));
		ClanLeaderboardHint = new HintViewModel(new TextObject("{=JdEiK70R}Clan Leaderboard"));
		ChangeBannerlordIDHint = new HintViewModel(new TextObject("{=ozREO8ev}Change Bannerlord ID"));
		AddFriendWithBannerlordIDHint = new HintViewModel(new TextObject("{=tC9C8TLi}Add Friend"));
		CopyBannerlordIDHint = new HintViewModel(new TextObject("{=Pwi1YCjH}Copy Bannerlord ID"));
		DisplayedStats?.ApplyActionOnAllItems(delegate(MPLobbyPlayerStatItemVM s)
		{
			s.RefreshValues();
		});
		ShownBadge?.RefreshValues();
	}

	public void RefreshSelectableGameTypes(bool isRankedOnly, Action<string> onRefreshed, string initialGameTypeID = "")
	{
		GameTypes.Clear();
		GameTypes.Add(new MPLobbyGameTypeVM("Skirmish", isCasual: false, onRefreshed));
		GameTypes.Add(new MPLobbyGameTypeVM("Captain", isCasual: false, onRefreshed));
		if (!isRankedOnly)
		{
			GameTypes.Add(new MPLobbyGameTypeVM("Duel", isCasual: true, onRefreshed));
			GameTypes.Add(new MPLobbyGameTypeVM("TeamDeathmatch", isCasual: true, onRefreshed));
			GameTypes.Add(new MPLobbyGameTypeVM("Siege", isCasual: true, UpdateDisplayedRankInfo));
		}
		MPLobbyGameTypeVM mPLobbyGameTypeVM = GameTypes.FirstOrDefault((MPLobbyGameTypeVM gt) => gt.GameTypeID == initialGameTypeID);
		if (mPLobbyGameTypeVM != null)
		{
			mPLobbyGameTypeVM.IsSelected = true;
		}
		else
		{
			GameTypes[0].IsSelected = true;
		}
	}

	private void UpdateForcedAvatarIndex(bool isKnownPlayer)
	{
		if (ProvidedID != NetworkMain.GameClient.PlayerID)
		{
			LobbyState obj = Game.Current?.GameStateManager?.ActiveState as LobbyState;
			if (obj != null && obj.HasUserGeneratedContentPrivilege == false)
			{
				_forcedAvatarIndex = AvatarServices.GetForcedAvatarIndexOfPlayer(ProvidedID);
				return;
			}
		}
		if (!BannerlordConfig.EnableGenericAvatars || ProvidedID == NetworkMain.GameClient.PlayerID || isKnownPlayer)
		{
			_forcedAvatarIndex = -1;
		}
		else
		{
			_forcedAvatarIndex = AvatarServices.GetForcedAvatarIndexOfPlayer(ProvidedID);
		}
	}

	protected async void UpdateName(bool isKnownPlayer)
	{
		string genericName = (Name = _genericPlayerName.ToString());
		if (ProvidedID != NetworkMain.GameClient.PlayerID)
		{
			LobbyState obj = Game.Current?.GameStateManager?.ActiveState as LobbyState;
			if (obj != null && obj.HasUserGeneratedContentPrivilege == false && ProvidedID.ProvidedType != PlayerIdProvidedTypes.PS && ProvidedID.ProvidedType != PlayerIdProvidedTypes.GDK)
			{
				Name = genericName;
				goto IL_0279;
			}
		}
		if (_forcedName != string.Empty && !BannerlordConfig.EnableGenericNames)
		{
			Name = _forcedName;
		}
		else if (ProvidedID == NetworkMain.GameClient.PlayerID)
		{
			Name = NetworkMain.GameClient.Name;
		}
		else if (!isKnownPlayer && BannerlordConfig.EnableGenericNames)
		{
			Name = genericName;
		}
		else if (PlayerData != null)
		{
			string lastPlayerName = PlayerData.LastPlayerName;
			Name = lastPlayerName;
		}
		else if (ProvidedID.IsValid)
		{
			IFriendListService[] friendListServices = PlatformServices.Instance.GetFriendListServices();
			string foundName = genericName;
			for (int i = friendListServices.Length - 1; i >= 0; i--)
			{
				string text2 = await friendListServices[i].GetUserName(ProvidedID);
				if (!string.IsNullOrEmpty(text2) && text2 != "-" && text2 != genericName)
				{
					foundName = text2;
					break;
				}
			}
			Name = foundName;
		}
		goto IL_0279;
		IL_0279:
		NameHint.HintText = new TextObject("{=!}" + Name);
	}

	protected void UpdateAvatar(bool isKnownPlayer)
	{
		UpdateForcedAvatarIndex(isKnownPlayer);
		Avatar = new PlayerAvatarImageIdentifierVM(ProvidedID, _forcedAvatarIndex);
	}

	public void UpdatePlayerState(AnotherPlayerData playerData)
	{
		if (playerData != null)
		{
			if (playerData.PlayerState != AnotherPlayerState.NoAnswer)
			{
				State = playerData.PlayerState;
				StateText = GameTexts.FindText("str_multiplayer_lobby_state", State.ToString()).ToString();
			}
			_spectatableCustomBattleId = playerData.SpectatableCustomBattleId;
			CanSpectate = _spectatableCustomBattleId.HasValue;
			TimeSinceLastStateUpdate = Game.Current.ApplicationTime;
		}
	}

	public virtual void UpdateWith(PlayerData playerData)
	{
		if (playerData == null)
		{
			Debug.FailedAssert("PlayerData shouldn't be null at this stage!", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection\\Lobby\\Friends\\MPLobbyPlayerBaseVM.cs", "UpdateWith", 283);
			return;
		}
		PlayerData = playerData;
		ProvidedID = PlayerData.PlayerId;
		UpdateNameAndAvatar(forceUpdate: true);
		UpdateExperienceData();
		if (NetworkMain.GameClient != null && NetworkMain.GameClient.SupportedFeatures.SupportsFeatures(Features.Clan))
		{
			IsClanInfoSupported = true;
		}
		else
		{
			IsClanInfoSupported = false;
		}
		Loot = playerData.Gold;
		Sigil = new MPLobbySigilItemVM();
		Sigil.RefreshWith(playerData.Sigil);
		ShownBadge = new MPLobbyBadgeItemVM(BadgeManager.GetById(playerData.ShownBadgeId), null, (Badge badge) => true, null);
		BannerlordID = $"{playerData.Username}#{playerData.UserId}";
		SelectedBadgeID = playerData.ShownBadgeId;
		StateText = "";
		_spectatableCustomBattleId = null;
		CanSpectate = false;
		_hasReceivedPlayerStats = false;
		_isReceivingPlayerStats = false;
	}

	public void UpdateNameAndAvatar(bool forceUpdate = false)
	{
		bool flag = NetworkMain.GameClient.IsKnownPlayer(ProvidedID);
		if (_isKnownPlayer != flag || forceUpdate)
		{
			_isKnownPlayer = flag;
			UpdateAvatar(flag);
			UpdateName(flag);
		}
	}

	public void OnStatusChanged(OnlineStatus status, bool isInGameStatusActive)
	{
		CurrentOnlineStatus = status;
		StateText = "";
		TimeSinceLastStateUpdate = 0f;
		CanInviteToParty = _onInviteToParty != null && status switch
		{
			OnlineStatus.Online => !isInGameStatusActive, 
			OnlineStatus.InGame => true, 
			_ => false, 
		};
		ShowLevel = status switch
		{
			OnlineStatus.Online => !isInGameStatusActive, 
			OnlineStatus.InGame => true, 
			_ => false, 
		};
		CanInviteToClan = _onInviteToClan != null && status == OnlineStatus.Online;
	}

	public void SetOnInvite(Action<PlayerId> onInvite)
	{
		_onInviteToParty = onInvite;
		CanInviteToParty = onInvite != null;
		RefreshValues();
	}

	public async void UpdateStats(Action onDone)
	{
		if (!_hasReceivedPlayerStats && !_isReceivingPlayerStats)
		{
			_isReceivingPlayerStats = true;
			PlayerStats = await NetworkMain.GameClient.GetPlayerStats(ProvidedID);
			_isReceivingPlayerStats = false;
			_hasReceivedPlayerStats = PlayerStats != null;
			if (_hasReceivedPlayerStats)
			{
				OnPlayerStatsReceived?.Invoke();
				onDone?.Invoke();
			}
		}
	}

	public void UpdateExperienceData()
	{
		Level = PlayerData.Level;
		int num = PlayerDataExperience.ExperienceRequiredForLevel(PlayerData.Level + 1);
		float num2 = (float)PlayerData.ExperienceInCurrentLevel / (float)num;
		ExperienceRatio = (int)(num2 * 100f);
		string text = PlayerData.ExperienceInCurrentLevel + " / " + num;
		ExperienceHint.HintText = new TextObject("{=!}" + text);
		TextObject textObject = new TextObject("{=5Z0pvuNL}Level {LEVEL}");
		textObject.SetTextVariable("LEVEL", Level);
		LevelText = textObject.ToString();
		int experienceToNextLevel = PlayerData.ExperienceToNextLevel;
		TextObject textObject2 = new TextObject("{=NUSH5bJu}{EXPERIENCE} exp to next level");
		textObject2.SetTextVariable("EXPERIENCE", experienceToNextLevel);
		ExperienceText = textObject2.ToString();
	}

	public async void UpdateRating(Action onDone)
	{
		IsRankInfoLoading = true;
		RankInfo = await NetworkMain.GameClient.GetGameTypeRankInfo(ProvidedID);
		IsRankInfoLoading = false;
		onDone?.Invoke();
	}

	public void UpdateDisplayedRankInfo(string gameType)
	{
		GameTypeRankInfo gameTypeRankInfo = null;
		if (gameType == "Skirmish")
		{
			gameTypeRankInfo = RankInfo?.FirstOrDefault((GameTypeRankInfo r) => r.GameType == "Skirmish");
			RankInfoGameTypeID = "Skirmish";
		}
		else if (gameType == "Captain")
		{
			gameTypeRankInfo = RankInfo?.FirstOrDefault((GameTypeRankInfo r) => r.GameType == "Captain");
			RankInfoGameTypeID = "Captain";
		}
		if (gameTypeRankInfo != null)
		{
			RankBarInfo rankBarInfo = gameTypeRankInfo.RankBarInfo;
			Rating = rankBarInfo.Rating;
			RatingID = rankBarInfo.RankId;
			RatingText = MPLobbyVM.GetLocalizedRankName(RatingID);
			if (rankBarInfo.IsEvaluating)
			{
				TextObject textObject = new TextObject("{=Ise5gWw3}{PLAYED_GAMES} / {TOTAL_GAMES} Evaluation matches played");
				textObject.SetTextVariable("PLAYED_GAMES", rankBarInfo.EvaluationMatchesPlayed);
				textObject.SetTextVariable("TOTAL_GAMES", rankBarInfo.TotalEvaluationMatchesRequired);
				RankText = textObject.ToString();
				RatingRatio = TaleWorlds.Library.MathF.Floor((float)rankBarInfo.EvaluationMatchesPlayed / (float)rankBarInfo.TotalEvaluationMatchesRequired * 100f);
			}
			else
			{
				TextObject textObject2 = new TextObject("{=BUOtUW1u}{RATING} Points");
				textObject2.SetTextVariable("RATING", rankBarInfo.Rating);
				RankText = textObject2.ToString();
				RatingRatio = (string.IsNullOrEmpty(rankBarInfo.NextRankId) ? 100 : TaleWorlds.Library.MathF.Floor(rankBarInfo.ProgressPercentage));
			}
			GameTexts.SetVariable("NUMBER", RatingRatio.ToString("0.00"));
			RatingHint.HintText = GameTexts.FindText("str_NUMBER_percent");
		}
		else
		{
			Rating = 0;
			RatingRatio = 0;
			RatingID = "norank";
			RatingText = new TextObject("{=GXosklej}Casual").ToString();
			RankText = new TextObject("{=56FyokuX}Game mode is casual").ToString();
			RatingHint.HintText = TextObject.GetEmpty();
		}
		OnRankInfoChanged?.Invoke(gameType);
		IsRankInfoCasual = gameType != "Skirmish" && gameType != "Captain";
	}

	public async void UpdateClanInfo()
	{
		if (!(ProvidedID == PlayerId.Empty))
		{
			bool isSelfPlayer = ProvidedID == NetworkMain.GameClient.PlayerID;
			ClanInfo clanInfo = ((!isSelfPlayer) ? (await NetworkMain.GameClient.GetPlayerClanInfo(ProvidedID)) : NetworkMain.GameClient.ClanInfo);
			if (clanInfo != null && (isSelfPlayer || (!isSelfPlayer && clanInfo.Players.Length != 0)))
			{
				ClanBanner = new BannerImageIdentifierVM(new Banner(clanInfo.Sigil), nineGrid: true);
				ClanName = clanInfo.Name;
				GameTexts.SetVariable("STR", clanInfo.Tag);
				ClanTag = new TextObject("{=uTXYEAOg}[{STR}]").ToString();
			}
			else
			{
				ClanBanner = new BannerImageIdentifierVM(Banner.CreateOneColoredEmptyBanner(99));
				ClanName = new TextObject("{=0DnHFlia}Not In a Clan").ToString();
				ClanTag = string.Empty;
			}
		}
	}

	public void FilterStatsForGameMode(string gameModeCode)
	{
		if (PlayerStats == null)
		{
			return;
		}
		if (DisplayedStats == null)
		{
			DisplayedStats = new MBBindingList<MPLobbyPlayerStatItemVM>();
		}
		DisplayedStats.Clear();
		IEnumerable<PlayerStatsBase> enumerable = PlayerStats.Where((PlayerStatsBase s) => s.GameType == gameModeCode);
		foreach (PlayerStatsBase item in enumerable)
		{
			if (gameModeCode == "Skirmish" || gameModeCode == "Captain")
			{
				DisplayedStats.Add(new MPLobbyPlayerStatItemVM(item.GameType, new TextObject("{=WW2N3zJf}Wins"), item.WinCount));
				DisplayedStats.Add(new MPLobbyPlayerStatItemVM(item.GameType, new TextObject("{=4nr9Km6t}Losses"), item.LoseCount));
			}
			if (gameModeCode == "Skirmish")
			{
				DisplayedStats.Add(new MPLobbyPlayerStatItemVM(item.GameType, new TextObject("{=ab2cbidI}Total Score"), (item as PlayerStatsSkirmish).Score));
				DisplayedStats.Add(new MPLobbyPlayerStatItemVM(item.GameType, new TextObject("{=fdR3xpBS}MVP Badges"), (item as PlayerStatsSkirmish).MVPs));
				DisplayedStats.Add(new MPLobbyPlayerStatItemVM(item.GameType, new TextObject("{=2FaZ6E1k}Kill Death Ratio"), item.AverageKillPerDeath));
			}
			else if (gameModeCode == "Captain")
			{
				DisplayedStats.Add(new MPLobbyPlayerStatItemVM(item.GameType, new TextObject("{=ab2cbidI}Total Score"), (item as PlayerStatsCaptain).Score));
				DisplayedStats.Add(new MPLobbyPlayerStatItemVM(item.GameType, new TextObject("{=fdR3xpBS}MVP Badges"), (item as PlayerStatsCaptain).MVPs));
				DisplayedStats.Add(new MPLobbyPlayerStatItemVM(item.GameType, new TextObject("{=9FSk2daF}Captains Killed"), (item as PlayerStatsCaptain).CaptainsKilled));
			}
			else if (gameModeCode == "Siege")
			{
				DisplayedStats.Add(new MPLobbyPlayerStatItemVM(item.GameType, new TextObject("{=ab2cbidI}Total Score"), (item as PlayerStatsSiege).Score));
				DisplayedStats.Add(new MPLobbyPlayerStatItemVM(item.GameType, new TextObject("{=XKWGPrYt}Siege Engines Destroyed"), (item as PlayerStatsSiege).SiegeEnginesDestroyed));
				DisplayedStats.Add(new MPLobbyPlayerStatItemVM(item.GameType, new TextObject("{=7APa598U}Kills With a Siege Engine"), (item as PlayerStatsSiege).SiegeEngineKills));
				DisplayedStats.Add(new MPLobbyPlayerStatItemVM(item.GameType, new TextObject("{=FaKWQccs}Gold Gained From Objectives"), (item as PlayerStatsSiege).ObjectiveGoldGained));
				DisplayedStats.Add(new MPLobbyPlayerStatItemVM(item.GameType, new TextObject("{=2FaZ6E1k}Kill Death Ratio"), item.AverageKillPerDeath));
			}
			else if (gameModeCode == "Duel")
			{
				DisplayedStats.Add(new MPLobbyPlayerStatItemVM(item.GameType, new TextObject("{=SS5WyUWR}Duels Won"), (item as PlayerStatsDuel).DuelsWon));
				DisplayedStats.Add(new MPLobbyPlayerStatItemVM(item.GameType, new TextObject("{=Iu2eFSsh}Infantry Wins"), (item as PlayerStatsDuel).InfantryWins));
				DisplayedStats.Add(new MPLobbyPlayerStatItemVM(item.GameType, new TextObject("{=wyKhcvbd}Ranged Wins"), (item as PlayerStatsDuel).ArcherWins));
				DisplayedStats.Add(new MPLobbyPlayerStatItemVM(item.GameType, new TextObject("{=qipBkhys}Cavalry Wins"), (item as PlayerStatsDuel).CavalryWins));
			}
			else if (gameModeCode == "TeamDeathmatch")
			{
				DisplayedStats.Add(new MPLobbyPlayerStatItemVM(item.GameType, new TextObject("{=ab2cbidI}Total Score"), (item as PlayerStatsTeamDeathmatch).Score));
				DisplayedStats.Add(new MPLobbyPlayerStatItemVM(item.GameType, new TextObject("{=9ET13VOe}Average Score"), (item as PlayerStatsTeamDeathmatch).AverageScore));
				DisplayedStats.Add(new MPLobbyPlayerStatItemVM(item.GameType, new TextObject("{=2FaZ6E1k}Kill Death Ratio"), item.AverageKillPerDeath));
			}
			if (gameModeCode != "Duel")
			{
				DisplayedStats.Add(new MPLobbyPlayerStatItemVM(item.GameType, new TextObject("{=FKe05WtJ}Kills"), item.KillCount));
				DisplayedStats.Add(new MPLobbyPlayerStatItemVM(item.GameType, new TextObject("{=8eZFlPVu}Deaths"), item.DeathCount));
				DisplayedStats.Add(new MPLobbyPlayerStatItemVM(item.GameType, new TextObject("{=1imGhhZl}Assists"), item.AssistCount));
			}
		}
		if (enumerable.IsEmpty())
		{
			if (gameModeCode == "Skirmish" || gameModeCode == "Captain")
			{
				DisplayedStats.Add(new MPLobbyPlayerStatItemVM(gameModeCode, new TextObject("{=WW2N3zJf}Wins"), "-"));
				DisplayedStats.Add(new MPLobbyPlayerStatItemVM(gameModeCode, new TextObject("{=4nr9Km6t}Losses"), "-"));
			}
			if (gameModeCode == "Skirmish")
			{
				DisplayedStats.Add(new MPLobbyPlayerStatItemVM(gameModeCode, new TextObject("{=ab2cbidI}Total Score"), "-"));
				DisplayedStats.Add(new MPLobbyPlayerStatItemVM(gameModeCode, new TextObject("{=fdR3xpBS}MVP Badges"), "-"));
				DisplayedStats.Add(new MPLobbyPlayerStatItemVM(gameModeCode, new TextObject("{=2FaZ6E1k}Kill Death Ratio"), "-"));
			}
			else if (gameModeCode == "Captain")
			{
				DisplayedStats.Add(new MPLobbyPlayerStatItemVM(gameModeCode, new TextObject("{=ab2cbidI}Total Score"), "-"));
				DisplayedStats.Add(new MPLobbyPlayerStatItemVM(gameModeCode, new TextObject("{=fdR3xpBS}MVP Badges"), "-"));
				DisplayedStats.Add(new MPLobbyPlayerStatItemVM(gameModeCode, new TextObject("{=9FSk2daF}Captains Killed"), "-"));
			}
			else if (gameModeCode == "Siege")
			{
				DisplayedStats.Add(new MPLobbyPlayerStatItemVM(gameModeCode, new TextObject("{=ab2cbidI}Total Score"), "-"));
				DisplayedStats.Add(new MPLobbyPlayerStatItemVM(gameModeCode, new TextObject("{=XKWGPrYt}Siege Engines Destroyed"), "-"));
				DisplayedStats.Add(new MPLobbyPlayerStatItemVM(gameModeCode, new TextObject("{=7APa598U}Kills With a Siege Engine"), "-"));
				DisplayedStats.Add(new MPLobbyPlayerStatItemVM(gameModeCode, new TextObject("{=FaKWQccs}Gold Gained From Objectives"), "-"));
				DisplayedStats.Add(new MPLobbyPlayerStatItemVM(gameModeCode, new TextObject("{=2FaZ6E1k}Kill Death Ratio"), "-"));
			}
			else if (gameModeCode == "Duel")
			{
				DisplayedStats.Add(new MPLobbyPlayerStatItemVM(gameModeCode, new TextObject("{=SS5WyUWR}Duels Won"), "-"));
				DisplayedStats.Add(new MPLobbyPlayerStatItemVM(gameModeCode, new TextObject("{=Iu2eFSsh}Infantry Wins"), "-"));
				DisplayedStats.Add(new MPLobbyPlayerStatItemVM(gameModeCode, new TextObject("{=wyKhcvbd}Ranged Wins"), "-"));
				DisplayedStats.Add(new MPLobbyPlayerStatItemVM(gameModeCode, new TextObject("{=qipBkhys}Cavalry Wins"), "-"));
			}
			else if (gameModeCode == "TeamDeathmatch")
			{
				DisplayedStats.Add(new MPLobbyPlayerStatItemVM(gameModeCode, new TextObject("{=ab2cbidI}Total Score"), "-"));
				DisplayedStats.Add(new MPLobbyPlayerStatItemVM(gameModeCode, new TextObject("{=9ET13VOe}Average Score"), "-"));
				DisplayedStats.Add(new MPLobbyPlayerStatItemVM(gameModeCode, new TextObject("{=2FaZ6E1k}Kill Death Ratio"), "-"));
			}
			if (gameModeCode != "Duel")
			{
				DisplayedStats.Add(new MPLobbyPlayerStatItemVM(gameModeCode, new TextObject("{=FKe05WtJ}Kills"), "-"));
				DisplayedStats.Add(new MPLobbyPlayerStatItemVM(gameModeCode, new TextObject("{=8eZFlPVu}Deaths"), "-"));
				DisplayedStats.Add(new MPLobbyPlayerStatItemVM(gameModeCode, new TextObject("{=1imGhhZl}Assists"), "-"));
			}
		}
	}

	public void RefreshCharacterVisual()
	{
		CharacterVisual = new CharacterViewModel();
		BasicCharacterObject basicCharacterObject = MBObjectManager.Instance.GetObject<BasicCharacterObject>("mp_character");
		basicCharacterObject.UpdatePlayerCharacterBodyProperties(PlayerData.BodyProperties, PlayerData.Race, PlayerData.IsFemale);
		CharacterVisual.FillFrom(basicCharacterObject);
		CharacterVisual.BodyProperties = new BodyProperties(PlayerData.BodyProperties.DynamicProperties, basicCharacterObject.BodyPropertyRange.BodyPropertyMin.StaticProperties).ToString();
		CharacterVisual.IsFemale = PlayerData.IsFemale;
		CharacterVisual.Race = PlayerData.Race;
	}

	public void ExecuteSelectPlayer()
	{
		IsSelected = !IsSelected;
	}

	public void ExecuteInviteToParty()
	{
		_onInviteToParty?.Invoke(ProvidedID);
	}

	public void ExecuteSpectateMatch()
	{
		if (_spectatableCustomBattleId.HasValue)
		{
			SpectateMatchAsync(_spectatableCustomBattleId.Value);
		}
	}

	private async void SpectateMatchAsync(CustomBattleId battleId)
	{
		if (!(await NetworkMain.GameClient.RequestJoinCustomGame(battleId, CustomGameJoinType.Spectator, null)))
		{
			InformationManager.ShowInquiry(new InquiryData("", GameTexts.FindText("str_couldnt_join_server").ToString(), isAffirmativeOptionShown: true, isNegativeOptionShown: false, GameTexts.FindText("str_ok").ToString(), "", null, null));
		}
	}

	public void ExecuteInviteToClan()
	{
		_onInviteToClan?.Invoke(ProvidedID);
	}

	public void ExecuteKickFromParty()
	{
		if (NetworkMain.GameClient.IsInParty && NetworkMain.GameClient.IsPartyLeader)
		{
			NetworkMain.GameClient.KickPlayerFromParty(ProvidedID);
		}
	}

	public void ExecuteAcceptFriendRequest()
	{
		bool dontUseNameForUnknownPlayer = BannerlordConfig.EnableGenericNames && !NetworkMain.GameClient.IsKnownPlayer(ProvidedID);
		NetworkMain.GameClient.RespondToFriendRequest(ProvidedID, dontUseNameForUnknownPlayer, isAccepted: true);
		_onFriendRequestAnswered?.Invoke(ProvidedID);
		if (HasNotification)
		{
			HasNotification = false;
		}
	}

	public void ExecuteDeclineFriendRequest()
	{
		bool dontUseNameForUnknownPlayer = BannerlordConfig.EnableGenericNames && !NetworkMain.GameClient.IsKnownPlayer(ProvidedID);
		NetworkMain.GameClient.RespondToFriendRequest(ProvidedID, dontUseNameForUnknownPlayer, isAccepted: false);
		_onFriendRequestAnswered?.Invoke(ProvidedID);
		if (HasNotification)
		{
			HasNotification = false;
		}
	}

	public void ExecuteCancelPendingFriendRequest()
	{
		NetworkMain.GameClient.RemoveFriend(ProvidedID);
	}

	public void ExecuteRemoveFriend()
	{
		NetworkMain.GameClient.RemoveFriend(ProvidedID);
	}

	public void ExecuteCopyBannerlordID()
	{
		Input.SetClipboardText(BannerlordID);
	}

	private void ExecuteAddFriend()
	{
		string[] array = BannerlordID.Split(new char[1] { '#' });
		string username = array[0];
		if (int.TryParse(array[1], out var result))
		{
			bool dontUseNameForUnknownPlayer = BannerlordConfig.EnableGenericNames && !NetworkMain.GameClient.IsKnownPlayer(ProvidedID);
			NetworkMain.GameClient.AddFriendByUsernameAndId(username, result, dontUseNameForUnknownPlayer);
		}
	}

	public void ExecuteShowProfile()
	{
		OnPlayerProfileRequested?.Invoke(ProvidedID);
	}

	private void ExecuteActivateSigilChangeInformation()
	{
		IsSigilChangeInformationEnabled = true;
	}

	private void ExecuteDeactivateSigilChangeInformation()
	{
		IsSigilChangeInformationEnabled = false;
	}

	private void ExecuteChangeSigil()
	{
		OnSigilChangeRequested?.Invoke(ProvidedID);
	}

	private void ExecuteChangeBannerlordID()
	{
		OnBannerlordIDChangeRequested?.Invoke(ProvidedID);
	}

	private void ExecuteAddFriendWithBannerlordID()
	{
		OnAddFriendWithBannerlordIDRequested?.Invoke(ProvidedID);
	}

	private void ExecuteChangeBadge()
	{
		OnBadgeChangeRequested?.Invoke(ProvidedID);
	}

	private void ExecuteShowRankProgression()
	{
		MPLobbyGameTypeVM mPLobbyGameTypeVM = GameTypes.FirstOrDefault((MPLobbyGameTypeVM gt) => gt.IsSelected);
		if (mPLobbyGameTypeVM != null && !mPLobbyGameTypeVM.IsCasual)
		{
			OnRankProgressionRequested?.Invoke(this);
		}
	}

	private void ExecuteShowRankLeaderboard()
	{
		MPLobbyGameTypeVM mPLobbyGameTypeVM = GameTypes.FirstOrDefault((MPLobbyGameTypeVM gt) => gt.IsSelected);
		if (mPLobbyGameTypeVM != null && !mPLobbyGameTypeVM.IsCasual)
		{
			OnRankLeaderboardRequested?.Invoke(mPLobbyGameTypeVM.GameTypeID);
		}
	}

	private void ExecuteShowClanPage()
	{
		OnClanPageRequested?.Invoke();
	}

	private void ExecuteShowClanLeaderboard()
	{
		OnClanLeaderboardRequested?.Invoke();
	}
}
