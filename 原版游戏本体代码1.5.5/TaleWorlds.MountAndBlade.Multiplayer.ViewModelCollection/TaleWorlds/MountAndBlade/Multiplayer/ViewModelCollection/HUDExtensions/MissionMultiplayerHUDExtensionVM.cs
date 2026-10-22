using System;
using System.Collections.Generic;
using System.ComponentModel;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.HUDExtensions;

public class MissionMultiplayerHUDExtensionVM : ViewModel
{
	private const float RemainingTimeWarningThreshold = 5f;

	private readonly Mission _mission;

	private readonly Dictionary<MissionPeer, MPPlayerVM> _teammateDictionary;

	private readonly Dictionary<MissionPeer, MPPlayerVM> _enemyDictionary;

	private readonly List<MPPlayerVM> _teammatesToRemoveScratch = new List<MPPlayerVM>();

	private readonly List<MPPlayerVM> _enemiesToRemoveScratch = new List<MPPlayerVM>();

	private const int MaxVisibleOverlayRows = 8;

	private readonly Dictionary<MissionPeer, MPOverlayPlayerVM> _overlayPlayerDictionary;

	private readonly List<MPOverlayPlayerVM> _overlayPlayersToRemoveScratch = new List<MPOverlayPlayerVM>();

	private readonly List<MPOverlayPlayerVM> _overlayAttackersScratch = new List<MPOverlayPlayerVM>();

	private readonly List<MPOverlayPlayerVM> _overlayDefendersScratch = new List<MPOverlayPlayerVM>();

	private MissionPeer _followedPeer;

	private MPOverlayPlayerVM _focusedOverlayPlayer;

	private readonly MissionScoreboardComponent _missionScoreboardComponent;

	private readonly MissionNetworkComponent _missionNetworkComponent;

	private readonly MissionMultiplayerGameModeBaseClient _gameMode;

	private readonly bool _isTeamsEnabled;

	private bool _isTeammateAndEnemiesRelevant;

	private bool _isTeamScoresEnabled;

	private bool _isTeamScoresDirty;

	private bool _isMatchEnded;

	private bool _isOrderActive;

	private CommanderInfoVM _commanderInfo;

	private MissionMultiplayerSpectatorHUDVM _spectatorControls;

	private bool _warnRemainingTime;

	private bool _isRoundCountdownAvailable;

	private bool _isRoundCountdownSuspended;

	private bool _showTeamHeader;

	private bool _showTeamScores;

	private string _remainingRoundTime;

	private string _allyTeamColor;

	private string _allyTeamColor2;

	private string _enemyTeamColor;

	private string _enemyTeamColor2;

	private string _warmupInfoText;

	private int _allyTeamScore = -1;

	private int _enemyTeamScore = -1;

	private MBBindingList<MPPlayerVM> _teammatesList;

	private MBBindingList<MPPlayerVM> _enemiesList;

	private MPOverlaySideVM _overlayAttackerSide;

	private MPOverlaySideVM _overlayDefenderSide;

	private bool _showAllPlayersOverlay;

	private bool _showHUD;

	private bool _isRetracted;

	private bool _isSpectator;

	private bool _showFollowHint;

	private string _hudToggleHintText;

	private bool _showCommanderInfo;

	private bool _showPowerLevels;

	private bool _isInWarmup;

	private int _generalWarningCountdown;

	private bool _isGeneralWarningCountdownActive;

	private BannerImageIdentifierVM _defenderBanner;

	private BannerImageIdentifierVM _attackerBanner;

	private bool ShowsStreamerTeamOverview
	{
		get
		{
			if (_isTeamsEnabled)
			{
				return MultiplayerSpectatorHelper.IsLocalPeerStreamer();
			}
			return false;
		}
	}

	private bool IsAttackerTeamAlly
	{
		get
		{
			NetworkCommunicator myPeer = GameNetwork.MyPeer;
			if (myPeer == null)
			{
				return true;
			}
			return myPeer.GetComponent<MissionPeer>()?.Team?.Side != BattleSideEnum.Defender;
		}
	}

	private Team _playerTeam
	{
		get
		{
			if (!GameNetwork.IsMyPeerReady)
			{
				return null;
			}
			MissionPeer component = GameNetwork.MyPeer.GetComponent<MissionPeer>();
			if (component == null)
			{
				return null;
			}
			if (component.Team == null || component.Team.Side == BattleSideEnum.None)
			{
				return null;
			}
			return component.Team;
		}
	}

	[DataSourceProperty]
	public bool IsSpectator
	{
		get
		{
			return _isSpectator;
		}
		set
		{
			if (value != _isSpectator)
			{
				_isSpectator = value;
				OnPropertyChangedWithValue(value, "IsSpectator");
			}
		}
	}

	[DataSourceProperty]
	public string HudToggleHintText
	{
		get
		{
			return _hudToggleHintText;
		}
		set
		{
			if (value != _hudToggleHintText)
			{
				_hudToggleHintText = value;
				OnPropertyChangedWithValue(value, "HudToggleHintText");
			}
		}
	}

	[DataSourceProperty]
	public bool IsOrderActive
	{
		get
		{
			return _isOrderActive;
		}
		set
		{
			if (value != _isOrderActive)
			{
				_isOrderActive = value;
				OnPropertyChangedWithValue(value, "IsOrderActive");
			}
		}
	}

	[DataSourceProperty]
	public CommanderInfoVM CommanderInfo
	{
		get
		{
			return _commanderInfo;
		}
		set
		{
			if (value != _commanderInfo)
			{
				_commanderInfo = value;
				OnPropertyChangedWithValue(value, "CommanderInfo");
			}
		}
	}

	[DataSourceProperty]
	public MissionMultiplayerSpectatorHUDVM SpectatorControls
	{
		get
		{
			return _spectatorControls;
		}
		set
		{
			if (value != _spectatorControls)
			{
				_spectatorControls = value;
				OnPropertyChangedWithValue(value, "SpectatorControls");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<MPPlayerVM> Teammates
	{
		get
		{
			return _teammatesList;
		}
		set
		{
			if (value != _teammatesList)
			{
				_teammatesList = value;
				OnPropertyChangedWithValue(value, "Teammates");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<MPPlayerVM> Enemies
	{
		get
		{
			return _enemiesList;
		}
		set
		{
			if (value != _enemiesList)
			{
				_enemiesList = value;
				OnPropertyChangedWithValue(value, "Enemies");
			}
		}
	}

	[DataSourceProperty]
	public MPOverlaySideVM OverlayAttackerSide
	{
		get
		{
			return _overlayAttackerSide;
		}
		set
		{
			if (value != _overlayAttackerSide)
			{
				_overlayAttackerSide = value;
				OnPropertyChangedWithValue(value, "OverlayAttackerSide");
			}
		}
	}

	[DataSourceProperty]
	public MPOverlaySideVM OverlayDefenderSide
	{
		get
		{
			return _overlayDefenderSide;
		}
		set
		{
			if (value != _overlayDefenderSide)
			{
				_overlayDefenderSide = value;
				OnPropertyChangedWithValue(value, "OverlayDefenderSide");
			}
		}
	}

	[DataSourceProperty]
	public bool ShowAllPlayersOverlay
	{
		get
		{
			return _showAllPlayersOverlay;
		}
		set
		{
			if (value != _showAllPlayersOverlay)
			{
				_showAllPlayersOverlay = value;
				OnPropertyChangedWithValue(value, "ShowAllPlayersOverlay");
			}
		}
	}

	[DataSourceProperty]
	public BannerImageIdentifierVM AllyBanner
	{
		get
		{
			return _defenderBanner;
		}
		set
		{
			if (value != _defenderBanner)
			{
				_defenderBanner = value;
				OnPropertyChangedWithValue(value, "AllyBanner");
			}
		}
	}

	[DataSourceProperty]
	public BannerImageIdentifierVM EnemyBanner
	{
		get
		{
			return _attackerBanner;
		}
		set
		{
			if (value != _attackerBanner)
			{
				_attackerBanner = value;
				OnPropertyChangedWithValue(value, "EnemyBanner");
			}
		}
	}

	[DataSourceProperty]
	public bool IsRoundCountdownAvailable
	{
		get
		{
			return _isRoundCountdownAvailable;
		}
		set
		{
			if (value != _isRoundCountdownAvailable)
			{
				_isRoundCountdownAvailable = value;
				OnPropertyChangedWithValue(value, "IsRoundCountdownAvailable");
			}
		}
	}

	[DataSourceProperty]
	public bool IsRoundCountdownSuspended
	{
		get
		{
			return _isRoundCountdownSuspended;
		}
		set
		{
			if (value != _isRoundCountdownSuspended)
			{
				_isRoundCountdownSuspended = value;
				OnPropertyChangedWithValue(value, "IsRoundCountdownSuspended");
			}
		}
	}

	[DataSourceProperty]
	public bool ShowTeamHeader
	{
		get
		{
			return _showTeamHeader;
		}
		set
		{
			if (value != _showTeamHeader)
			{
				_showTeamHeader = value;
				OnPropertyChangedWithValue(value, "ShowTeamHeader");
			}
		}
	}

	[DataSourceProperty]
	public bool ShowTeamScores
	{
		get
		{
			return _showTeamScores;
		}
		set
		{
			if (value != _showTeamScores)
			{
				_showTeamScores = value;
				OnPropertyChangedWithValue(value, "ShowTeamScores");
			}
		}
	}

	[DataSourceProperty]
	public string RemainingRoundTime
	{
		get
		{
			return _remainingRoundTime;
		}
		set
		{
			if (value != _remainingRoundTime)
			{
				_remainingRoundTime = value;
				OnPropertyChangedWithValue(value, "RemainingRoundTime");
			}
		}
	}

	[DataSourceProperty]
	public bool WarnRemainingTime
	{
		get
		{
			return _warnRemainingTime;
		}
		set
		{
			if (value != _warnRemainingTime)
			{
				_warnRemainingTime = value;
				OnPropertyChangedWithValue(value, "WarnRemainingTime");
			}
		}
	}

	[DataSourceProperty]
	public int AllyTeamScore
	{
		get
		{
			return _allyTeamScore;
		}
		set
		{
			if (value != _allyTeamScore)
			{
				_allyTeamScore = value;
				OnPropertyChangedWithValue(value, "AllyTeamScore");
			}
		}
	}

	[DataSourceProperty]
	public int EnemyTeamScore
	{
		get
		{
			return _enemyTeamScore;
		}
		set
		{
			if (value != _enemyTeamScore)
			{
				_enemyTeamScore = value;
				OnPropertyChangedWithValue(value, "EnemyTeamScore");
			}
		}
	}

	[DataSourceProperty]
	public string AllyTeamColor
	{
		get
		{
			return _allyTeamColor;
		}
		set
		{
			if (value != _allyTeamColor)
			{
				_allyTeamColor = value;
				OnPropertyChangedWithValue(value, "AllyTeamColor");
			}
		}
	}

	[DataSourceProperty]
	public string AllyTeamColor2
	{
		get
		{
			return _allyTeamColor2;
		}
		set
		{
			if (value != _allyTeamColor2)
			{
				_allyTeamColor2 = value;
				OnPropertyChangedWithValue(value, "AllyTeamColor2");
			}
		}
	}

	[DataSourceProperty]
	public string EnemyTeamColor
	{
		get
		{
			return _enemyTeamColor;
		}
		set
		{
			if (value != _enemyTeamColor)
			{
				_enemyTeamColor = value;
				OnPropertyChangedWithValue(value, "EnemyTeamColor");
			}
		}
	}

	[DataSourceProperty]
	public string EnemyTeamColor2
	{
		get
		{
			return _enemyTeamColor2;
		}
		set
		{
			if (value != _enemyTeamColor2)
			{
				_enemyTeamColor2 = value;
				OnPropertyChangedWithValue(value, "EnemyTeamColor2");
			}
		}
	}

	[DataSourceProperty]
	public bool ShowHud
	{
		get
		{
			return _showHUD;
		}
		set
		{
			if (value != _showHUD)
			{
				_showHUD = value;
				OnPropertyChangedWithValue(value, "ShowHud");
			}
		}
	}

	[DataSourceProperty]
	public bool IsRetracted
	{
		get
		{
			return _isRetracted;
		}
		set
		{
			if (value != _isRetracted)
			{
				_isRetracted = value;
				OnPropertyChangedWithValue(value, "IsRetracted");
				ApplyRetractedToChildren();
				RefreshHudToggleHint();
			}
		}
	}

	[DataSourceProperty]
	public bool ShowCommanderInfo
	{
		get
		{
			return _showCommanderInfo;
		}
		set
		{
			if (value != _showCommanderInfo)
			{
				_showCommanderInfo = value;
				OnPropertyChangedWithValue(value, "ShowCommanderInfo");
				UpdateShowTeamScores();
			}
		}
	}

	[DataSourceProperty]
	public bool ShowPowerLevels
	{
		get
		{
			return _showPowerLevels;
		}
		set
		{
			if (value != _showPowerLevels)
			{
				_showPowerLevels = value;
				OnPropertyChangedWithValue(value, "ShowPowerLevels");
			}
		}
	}

	[DataSourceProperty]
	public bool IsInWarmup
	{
		get
		{
			return _isInWarmup;
		}
		set
		{
			if (value != _isInWarmup)
			{
				_isInWarmup = value;
				OnPropertyChangedWithValue(value, "IsInWarmup");
				UpdateShowTeamScores();
				CommanderInfo?.UpdateWarmupDependentFlags(_isInWarmup);
			}
		}
	}

	[DataSourceProperty]
	public string WarmupInfoText
	{
		get
		{
			return _warmupInfoText;
		}
		set
		{
			if (value != _warmupInfoText)
			{
				_warmupInfoText = value;
				OnPropertyChangedWithValue(value, "WarmupInfoText");
			}
		}
	}

	[DataSourceProperty]
	public int GeneralWarningCountdown
	{
		get
		{
			return _generalWarningCountdown;
		}
		set
		{
			if (value != _generalWarningCountdown)
			{
				_generalWarningCountdown = value;
				OnPropertyChangedWithValue(value, "GeneralWarningCountdown");
			}
		}
	}

	[DataSourceProperty]
	public bool IsGeneralWarningCountdownActive
	{
		get
		{
			return _isGeneralWarningCountdownActive;
		}
		set
		{
			if (value != _isGeneralWarningCountdownActive)
			{
				_isGeneralWarningCountdownActive = value;
				OnPropertyChangedWithValue(value, "IsGeneralWarningCountdownActive");
			}
		}
	}

	public event Action<Agent> OnPlayerFollowRequested;

	public MissionMultiplayerHUDExtensionVM(Mission mission)
	{
		_mission = mission;
		_missionScoreboardComponent = mission.GetMissionBehavior<MissionScoreboardComponent>();
		_gameMode = _mission.GetMissionBehavior<MissionMultiplayerGameModeBaseClient>();
		SpectatorControls = new MissionMultiplayerSpectatorHUDVM(_mission);
		if (_gameMode.RoundComponent != null)
		{
			_gameMode.RoundComponent.OnCurrentRoundStateChanged += OnCurrentGameModeStateChanged;
		}
		if (_gameMode.WarmupComponent != null)
		{
			_gameMode.WarmupComponent.OnWarmupEnded += OnCurrentGameModeStateChanged;
		}
		_missionScoreboardComponent.OnRoundPropertiesChanged += SetTeamScoresDirty;
		_missionNetworkComponent = mission.GetMissionBehavior<MissionNetworkComponent>();
		_missionNetworkComponent.OnMyClientSynchronized += OnMyClientSynchronized;
		MissionPeer.OnTeamChanged += OnTeamChanged;
		NetworkCommunicator.OnPeerComponentAdded += OnPeerComponentAdded;
		Mission.Current.OnMissionReset += OnMissionReset;
		ManagedOptions.OnManagedOptionChanged = (ManagedOptions.OnManagedOptionChangedDelegate)Delegate.Combine(ManagedOptions.OnManagedOptionChanged, new ManagedOptions.OnManagedOptionChangedDelegate(OnManagedOptionChanged));
		MissionLobbyComponent missionBehavior = mission.GetMissionBehavior<MissionLobbyComponent>();
		_isTeamsEnabled = missionBehavior.MissionType != MultiplayerGameType.Duel;
		IsRoundCountdownAvailable = _gameMode.IsGameModeUsingRoundCountdown;
		IsRoundCountdownSuspended = false;
		_isTeamScoresEnabled = _isTeamsEnabled;
		UpdateShowTeamScores();
		ShowTeamHeader = _isTeamsEnabled;
		Teammates = new MBBindingList<MPPlayerVM>();
		Enemies = new MBBindingList<MPPlayerVM>();
		_teammateDictionary = new Dictionary<MissionPeer, MPPlayerVM>();
		_enemyDictionary = new Dictionary<MissionPeer, MPPlayerVM>();
		OverlayAttackerSide = new MPOverlaySideVM();
		OverlayDefenderSide = new MPOverlaySideVM();
		_overlayPlayerDictionary = new Dictionary<MissionPeer, MPOverlayPlayerVM>();
		ShowHud = _isTeamsEnabled;
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		string strValue = MultiplayerOptions.OptionType.GameType.GetStrValue();
		TextObject textObject = new TextObject("{=XJTX8w8M}Warmup Phase - {GAME_MODE}{newline}Waiting for players to join");
		textObject.SetTextVariable("GAME_MODE", GameTexts.FindText("str_multiplayer_official_game_type_name", strValue));
		WarmupInfoText = textObject.ToString();
		RefreshHudToggleHint();
		SpectatorControls.RefreshValues();
	}

	private void RefreshHudToggleHint()
	{
		if (_showFollowHint)
		{
			TextObject textObject = new TextObject("{=*}Press {PREVIOUS_KEY} or {NEXT_KEY} to follow a player");
			textObject.SetTextVariable("PREVIOUS_KEY", HyperlinkTexts.GetKeyHyperlinkText(HotKeyManager.GetHotKeyId("MultiplayerHotkeyCategory", "CycleSpectatorTargetPrevious")));
			textObject.SetTextVariable("NEXT_KEY", HyperlinkTexts.GetKeyHyperlinkText(HotKeyManager.GetHotKeyId("MultiplayerHotkeyCategory", "CycleSpectatorTargetNext")));
			HudToggleHintText = textObject.ToString();
		}
		else
		{
			string keyHyperlinkText = HyperlinkTexts.GetKeyHyperlinkText(HotKeyManager.GetHotKeyId("ScoreboardHotKeyCategory", "ToggleHud"));
			TextObject textObject2 = (_isRetracted ? new TextObject("{=*}Press {KEY} for full HUD") : new TextObject("{=*}Press {KEY} for compact HUD"));
			textObject2.SetTextVariable("KEY", keyHyperlinkText);
			HudToggleHintText = textObject2.ToString();
		}
	}

	private void OnMissionReset(object sender, PropertyChangedEventArgs e)
	{
		IsGeneralWarningCountdownActive = false;
	}

	private void OnMyClientSynchronized()
	{
		RefreshTeamVisuals();
	}

	private void OnManagedOptionChanged(ManagedOptions.ManagedOptionsType changedManagedOptionsType)
	{
		bool flag = changedManagedOptionsType == ManagedOptions.ManagedOptionsType.EnableGenericNames;
		bool flag2 = changedManagedOptionsType == ManagedOptions.ManagedOptionsType.EnableGenericAvatars;
		if (flag || flag2)
		{
			RefreshPlayerIdentity(Teammates, flag);
			RefreshPlayerIdentity(Enemies, flag);
			RefreshPlayerIdentity(OverlayAttackerSide.Players, flag);
			RefreshPlayerIdentity(OverlayDefenderSide.Players, flag);
			if (flag)
			{
				SpectatorControls?.RefreshSpectatedPlayerName();
			}
		}
	}

	private static void RefreshPlayerIdentity<T>(MBBindingList<T> players, bool isNameOption) where T : MPPlayerVM
	{
		for (int i = 0; i < players.Count; i++)
		{
			if (isNameOption)
			{
				players[i].RefreshName();
			}
			else
			{
				players[i].RefreshAvatar();
			}
		}
	}

	private void OnPeerComponentAdded(PeerComponent component)
	{
		if (component.IsMine && component is MissionRepresentativeBase)
		{
			MissionRepresentativeBase missionRepresentative = GameNetwork.MyPeer?.VirtualPlayer.GetComponent<MissionRepresentativeBase>();
			AllyTeamScore = _missionScoreboardComponent.GetRoundScore(BattleSideEnum.Attacker);
			EnemyTeamScore = _missionScoreboardComponent.GetRoundScore(BattleSideEnum.Defender);
			_isTeammateAndEnemiesRelevant = Mission.Current.GetMissionBehavior<MissionMultiplayerGameModeBaseClient>().IsGameModeTactical && !Mission.Current.HasMissionBehavior<MissionMultiplayerSiegeClient>() && _gameMode.GameType != MultiplayerGameType.Battle;
			CommanderInfo = new CommanderInfoVM(missionRepresentative);
			CommanderInfo.OnTeamChanged();
			ShowCommanderInfo = true;
			if (_isTeammateAndEnemiesRelevant)
			{
				OnRefreshTeamMembers();
				OnRefreshEnemyMembers();
			}
			ShowPowerLevels = _gameMode.GameType == MultiplayerGameType.Battle;
		}
	}

	public override void OnFinalize()
	{
		MissionPeer.OnTeamChanged -= OnTeamChanged;
		_missionNetworkComponent.OnMyClientSynchronized -= OnMyClientSynchronized;
		if (_gameMode.RoundComponent != null)
		{
			_gameMode.RoundComponent.OnCurrentRoundStateChanged -= OnCurrentGameModeStateChanged;
		}
		if (_gameMode.WarmupComponent != null)
		{
			_gameMode.WarmupComponent.OnWarmupEnded -= OnCurrentGameModeStateChanged;
		}
		_missionScoreboardComponent.OnRoundPropertiesChanged -= SetTeamScoresDirty;
		NetworkCommunicator.OnPeerComponentAdded -= OnPeerComponentAdded;
		ManagedOptions.OnManagedOptionChanged = (ManagedOptions.OnManagedOptionChangedDelegate)Delegate.Remove(ManagedOptions.OnManagedOptionChanged, new ManagedOptions.OnManagedOptionChangedDelegate(OnManagedOptionChanged));
		CommanderInfo?.OnFinalize();
		CommanderInfo = null;
		SpectatorControls?.OnFinalize();
		SpectatorControls = null;
		base.OnFinalize();
	}

	public void Tick(float dt)
	{
		IsInWarmup = _gameMode.IsInWarmup;
		CheckTimers();
		if (_isTeammateAndEnemiesRelevant)
		{
			OnRefreshTeamMembers();
			OnRefreshEnemyMembers();
		}
		IsSpectator = MultiplayerSpectatorHelper.IsLocalPeerSpectator();
		ShowHud = !_isMatchEnded && (_isTeamsEnabled || IsSpectator);
		ShowAllPlayersOverlay = ShowsStreamerTeamOverview && _missionScoreboardComponent.IsInitialized;
		if (ShowAllPlayersOverlay)
		{
			OnRefreshOverlayMembers();
		}
		if (_isTeamScoresDirty)
		{
			UpdateTeamScores();
			_isTeamScoresDirty = false;
		}
		_commanderInfo?.Tick(dt);
		_spectatorControls?.Tick(dt);
		bool flag = (_spectatorControls == null || !_spectatorControls.IsSpectatingAgent) && !MultiplayerSpectatorHelper.IsLocalPeerStreamer();
		if (flag != _showFollowHint)
		{
			_showFollowHint = flag;
			RefreshHudToggleHint();
		}
	}

	private void CheckTimers(bool forceUpdate = false)
	{
		if (_gameMode.CheckTimer(out var remainingTime, out var remainingWarningTime, forceUpdate))
		{
			RemainingRoundTime = TimeSpan.FromSeconds(remainingTime).ToString("mm':'ss");
			WarnRemainingTime = (float)remainingTime <= 5f;
			if (GeneralWarningCountdown != remainingWarningTime)
			{
				IsGeneralWarningCountdownActive = remainingWarningTime > 0;
				GeneralWarningCountdown = remainingWarningTime;
			}
		}
	}

	public void OnSpectatedAgentFocusIn(Agent followedAgent)
	{
		_spectatorControls?.OnSpectatedAgentFocusIn(followedAgent);
		_followedPeer = followedAgent?.MissionPeer ?? followedAgent?.Formation?.PlayerOwner?.MissionPeer;
		RefreshFollowedOverlayRow();
	}

	public void OnSpectatedAgentFocusOut(Agent followedPeer)
	{
		_spectatorControls?.OnSpectatedAgentFocusOut(followedPeer);
		_followedPeer = null;
		RefreshFollowedOverlayRow();
	}

	private void OnOverlayPlayerSelected(MPOverlayPlayerVM overlayPlayer)
	{
		OnAvatarPlayerSelected(overlayPlayer);
	}

	private void OnAvatarPlayerSelected(MPPlayerVM player)
	{
		Agent agent = player?.Peer?.ControlledAgent;
		if (agent != null)
		{
			OnPlayerFollowRequested?.Invoke(agent);
		}
	}

	private void RefreshFollowedOverlayRow()
	{
		if (_focusedOverlayPlayer != null)
		{
			_focusedOverlayPlayer.IsFocused = false;
			_focusedOverlayPlayer = null;
		}
		if (_followedPeer != null && _overlayPlayerDictionary.TryGetValue(_followedPeer, out var value))
		{
			value.IsFocused = true;
			_focusedOverlayPlayer = value;
		}
		OverlayAttackerSide.SetFollowedPeer(_followedPeer);
		OverlayDefenderSide.SetFollowedPeer(_followedPeer);
	}

	private void OnCurrentGameModeStateChanged()
	{
		CheckTimers(forceUpdate: true);
	}

	private void SetTeamScoresDirty()
	{
		_isTeamScoresDirty = true;
	}

	private void UpdateTeamScores()
	{
		if (_isTeamScoresEnabled)
		{
			int roundScore = _missionScoreboardComponent.GetRoundScore(BattleSideEnum.Attacker);
			int roundScore2 = _missionScoreboardComponent.GetRoundScore(BattleSideEnum.Defender);
			AllyTeamScore = (IsAttackerTeamAlly ? roundScore : roundScore2);
			EnemyTeamScore = (IsAttackerTeamAlly ? roundScore2 : roundScore);
		}
	}

	private void UpdateTeamBanners()
	{
		BannerImageIdentifierVM bannerImageIdentifierVM = new BannerImageIdentifierVM(Mission.Current.AttackerTeam?.Banner, nineGrid: true);
		BannerImageIdentifierVM bannerImageIdentifierVM2 = new BannerImageIdentifierVM(Mission.Current.DefenderTeam?.Banner, nineGrid: true);
		AllyBanner = (IsAttackerTeamAlly ? bannerImageIdentifierVM : bannerImageIdentifierVM2);
		EnemyBanner = (IsAttackerTeamAlly ? bannerImageIdentifierVM2 : bannerImageIdentifierVM);
	}

	private void OnTeamChanged(NetworkCommunicator peer, Team previousTeam, Team newTeam)
	{
		if (peer.IsMine)
		{
			if (_isTeamScoresEnabled || _gameMode.GameType == MultiplayerGameType.Battle)
			{
				SetTeamScoresDirty();
			}
			CommanderInfo?.OnTeamChanged();
		}
		if (CommanderInfo != null)
		{
			MissionPeer missionPeer = peer?.GetComponent<MissionPeer>();
			if (missionPeer != null && _teammateDictionary.TryGetValue(missionPeer, out var value))
			{
				value.RefreshTeam();
			}
			RefreshTeamVisuals();
		}
	}

	private void RefreshTeamVisuals()
	{
		GetTeamColors(Mission.Current.AttackerTeam, out var color, out var color2);
		if (_isTeamScoresEnabled || _gameMode.GameType == MultiplayerGameType.Battle)
		{
			GetTeamColors(Mission.Current.DefenderTeam, out var color3, out var color4);
			if (IsAttackerTeamAlly)
			{
				AllyTeamColor = color;
				AllyTeamColor2 = color2;
				EnemyTeamColor = color3;
				EnemyTeamColor2 = color4;
			}
			else
			{
				AllyTeamColor = color3;
				AllyTeamColor2 = color4;
				EnemyTeamColor = color;
				EnemyTeamColor2 = color2;
			}
			CommanderInfo.RefreshColors(AllyTeamColor, AllyTeamColor2, EnemyTeamColor, EnemyTeamColor2);
		}
		else
		{
			AllyTeamColor = color;
			AllyTeamColor2 = color2;
			CommanderInfo.RefreshColors(AllyTeamColor, AllyTeamColor2, EnemyTeamColor, EnemyTeamColor2);
		}
		UpdateTeamBanners();
	}

	private void GetTeamColors(Team team, out string color, out string color2)
	{
		if (team == null)
		{
			Debug.FailedAssert("Team colours were requested before the client received its teams.", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection\\HUDExtensions\\MissionMultiplayerHUDExtensionVM.cs", "GetTeamColors", 443);
			color = "#FFFFFFFF";
			color2 = "#FFFFFFFF";
			return;
		}
		color = team.Color.ToString("X");
		color = color.Remove(0, 2);
		color = "#" + color + "FF";
		color2 = team.Color2.ToString("X");
		color2 = color2.Remove(0, 2);
		color2 = "#" + color2 + "FF";
	}

	private bool IsPeerOnAllySide(MissionPeer lobbyPeer)
	{
		Team team = lobbyPeer?.Team;
		if (team == null || team == Mission.Current.SpectatorTeam)
		{
			return false;
		}
		if (MultiplayerSpectatorHelper.IsLocalPeerSpectator())
		{
			return team.Side == BattleSideEnum.Attacker;
		}
		if (_playerTeam != null)
		{
			return team == _playerTeam;
		}
		return false;
	}

	private bool IsPeerOnEnemySide(MissionPeer lobbyPeer)
	{
		Team team = lobbyPeer?.Team;
		if (team == null || team == Mission.Current.SpectatorTeam)
		{
			return false;
		}
		if (MultiplayerSpectatorHelper.IsLocalPeerSpectator())
		{
			return team.Side == BattleSideEnum.Defender;
		}
		if (_playerTeam != null)
		{
			return team != _playerTeam;
		}
		return false;
	}

	private void OnRefreshTeamMembers()
	{
		_teammatesToRemoveScratch.Clear();
		for (int i = 0; i < Teammates.Count; i++)
		{
			_teammatesToRemoveScratch.Add(Teammates[i]);
		}
		List<MPPlayerVM> teammatesToRemoveScratch = _teammatesToRemoveScratch;
		for (int j = 0; j < GameNetwork.NetworkPeers.Count; j++)
		{
			MissionPeer component = GameNetwork.NetworkPeers[j].GetComponent<MissionPeer>();
			if (component != null && IsPeerOnAllySide(component))
			{
				if (_teammateDictionary.TryGetValue(component, out var value))
				{
					teammatesToRemoveScratch.Remove(value);
					continue;
				}
				MPPlayerVM mPPlayerVM = new MPPlayerVM(component);
				mPPlayerVM.SetSelectionHandler(OnAvatarPlayerSelected);
				Teammates.Add(mPPlayerVM);
				_teammateDictionary.Add(component, mPPlayerVM);
			}
		}
		foreach (MPPlayerVM item in teammatesToRemoveScratch)
		{
			Teammates.Remove(item);
			_teammateDictionary.Remove(item.Peer);
		}
		bool isSelectable = MultiplayerSpectatorHelper.IsLocalPeerSpectator();
		foreach (MPPlayerVM teammate in Teammates)
		{
			teammate.RefreshDivision();
			teammate.RefreshGold();
			teammate.RefreshProperties();
			teammate.UpdateDisabled();
			teammate.IsSelectable = isSelectable;
		}
	}

	private void OnRefreshEnemyMembers()
	{
		_enemiesToRemoveScratch.Clear();
		for (int i = 0; i < Enemies.Count; i++)
		{
			_enemiesToRemoveScratch.Add(Enemies[i]);
		}
		List<MPPlayerVM> enemiesToRemoveScratch = _enemiesToRemoveScratch;
		for (int j = 0; j < GameNetwork.NetworkPeers.Count; j++)
		{
			MissionPeer component = GameNetwork.NetworkPeers[j].GetComponent<MissionPeer>();
			if (component != null && IsPeerOnEnemySide(component))
			{
				if (_enemyDictionary.TryGetValue(component, out var value))
				{
					enemiesToRemoveScratch.Remove(value);
					continue;
				}
				MPPlayerVM mPPlayerVM = new MPPlayerVM(component);
				mPPlayerVM.SetSelectionHandler(OnAvatarPlayerSelected);
				Enemies.Add(mPPlayerVM);
				_enemyDictionary.Add(component, mPPlayerVM);
			}
		}
		foreach (MPPlayerVM item in enemiesToRemoveScratch)
		{
			Enemies.Remove(item);
			_enemyDictionary.Remove(item.Peer);
		}
		bool isSelectable = MultiplayerSpectatorHelper.IsLocalPeerSpectator();
		foreach (MPPlayerVM enemy in Enemies)
		{
			enemy.RefreshDivision();
			enemy.UpdateDisabled();
			enemy.IsSelectable = isSelectable;
		}
	}

	private void OnRefreshOverlayMembers()
	{
		MissionScoreboardComponent.ScoreboardHeader[] headers = _missionScoreboardComponent.Headers;
		OverlayAttackerSide.RefreshStatHeaders(headers);
		OverlayDefenderSide.RefreshStatHeaders(headers);
		_overlayPlayersToRemoveScratch.Clear();
		foreach (MPOverlayPlayerVM value2 in _overlayPlayerDictionary.Values)
		{
			_overlayPlayersToRemoveScratch.Add(value2);
		}
		_overlayAttackersScratch.Clear();
		_overlayDefendersScratch.Clear();
		for (int i = 0; i < GameNetwork.NetworkPeers.Count; i++)
		{
			MissionPeer component = GameNetwork.NetworkPeers[i].GetComponent<MissionPeer>();
			Team team = component?.Team;
			if (team != null && team != Mission.Current.SpectatorTeam && (team.Side == BattleSideEnum.Attacker || team.Side == BattleSideEnum.Defender))
			{
				if (!_overlayPlayerDictionary.TryGetValue(component, out var value))
				{
					value = new MPOverlayPlayerVM(component, OnOverlayPlayerSelected);
					value.RebuildStats(headers);
					_overlayPlayerDictionary.Add(component, value);
				}
				else
				{
					_overlayPlayersToRemoveScratch.Remove(value);
					value.RefreshStats(headers);
				}
				value.RefreshDivision();
				value.RefreshProperties();
				value.UpdateDisabled();
				if (team.Side == BattleSideEnum.Attacker)
				{
					_overlayAttackersScratch.Add(value);
				}
				else
				{
					_overlayDefendersScratch.Add(value);
				}
			}
		}
		foreach (MPOverlayPlayerVM item in _overlayPlayersToRemoveScratch)
		{
			_overlayPlayerDictionary.Remove(item.Peer);
			if (item == _focusedOverlayPlayer)
			{
				_focusedOverlayPlayer = null;
			}
		}
		OverlayAttackerSide.ApplyPlayers(_overlayAttackersScratch);
		OverlayDefenderSide.ApplyPlayers(_overlayDefendersScratch);
		RefreshFollowedOverlayRow();
		UpdateOverlayOverflow();
	}

	private void UpdateOverlayOverflow()
	{
		UpdateOverlaySideOverflow(OverlayAttackerSide);
		UpdateOverlaySideOverflow(OverlayDefenderSide);
	}

	private static void UpdateOverlaySideOverflow(MPOverlaySideVM side)
	{
		int num = MBMath.ClampInt(side.Players.Count - 8, 0, int.MaxValue);
		side.ShowOverflow = num > 0;
		if (side.ShowOverflow)
		{
			TextObject textObject = new TextObject("{=n8jxXmgP}+{COUNT} more");
			textObject.SetTextVariable("COUNT", num);
			side.OverflowText = textObject.ToString();
		}
	}

	private void ApplyRetractedToChildren()
	{
		OverlayAttackerSide.IsRetracted = _isRetracted;
		OverlayDefenderSide.IsRetracted = _isRetracted;
		SpectatorControls.IsRetracted = _isRetracted;
	}

	private void UpdateShowTeamScores()
	{
		ShowTeamScores = _isTeamScoresEnabled && !_gameMode.IsInWarmup && ShowCommanderInfo && _gameMode.GameType != MultiplayerGameType.Siege;
	}

	public void OnMatchEnded()
	{
		_isMatchEnded = true;
		ShowHud = false;
	}
}
