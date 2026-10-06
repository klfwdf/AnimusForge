using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Generic;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade.Diamond;
using TaleWorlds.MountAndBlade.Missions.Multiplayer;
using TaleWorlds.MountAndBlade.ViewModelCollection.Input;
using TaleWorlds.PlatformService;
using TaleWorlds.PlayerServices;

namespace TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.Scoreboard;

public class MissionScoreboardVM : ViewModel
{
	private const float AttributeRefreshDuration = 1f;

	private ChatBox _chatBox;

	private const float PermissionCheckDuration = 45f;

	private readonly Dictionary<BattleSideEnum, MissionScoreboardSideVM> _missionSides;

	private readonly MissionScoreboardComponent _missionScoreboardComponent;

	private readonly MultiplayerPollComponent _missionPollComponent;

	private VoiceChatHandler _voiceChatHandler;

	private MultiplayerPermissionHandler _permissionHandler;

	private readonly Mission _mission;

	private float _attributeRefreshTimeElapsed;

	private bool _hasMutedAll;

	private bool _canStartKickPolls;

	private TextObject _muteAllText = new TextObject("{=AZSbwcG5}Mute All");

	private TextObject _unmuteAllText = new TextObject("{=SzRVIPeZ}Unmute All");

	private bool _isActive;

	private InputKeyItemVM _showMouseKey;

	private MPEndOfBattleVM _endOfBattle;

	private MBBindingList<MissionScoreboardSideVM> _sides;

	private MBBindingList<StringPairItemWithActionVM> _playerActionList;

	private string _spectators;

	private bool _showSpectatorCount;

	private string _missionName;

	private string _gameModeText;

	private string _mapName;

	private string _serverName;

	private bool _isBotsEnabled;

	private bool _isSingleSide;

	private bool _isInitalizationOver;

	private bool _isUpdateOver;

	private bool _isMouseEnabled;

	private bool _isPlayerActionsActive;

	private string _toggleMuteText;

	private BattleSideEnum AllySide
	{
		get
		{
			BattleSideEnum result = BattleSideEnum.None;
			if (GameNetwork.IsMyPeerReady)
			{
				MissionPeer component = GameNetwork.MyPeer.GetComponent<MissionPeer>();
				if (component != null && component.Team != null)
				{
					result = component.Team.Side;
				}
			}
			return result;
		}
	}

	private BattleSideEnum EnemySide
	{
		get
		{
			switch (AllySide)
			{
			case BattleSideEnum.Attacker:
				return BattleSideEnum.Defender;
			case BattleSideEnum.Defender:
				return BattleSideEnum.Attacker;
			default:
				Debug.FailedAssert("Ally side must be either Attacker or Defender", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection\\Scoreboard\\MissionScoreboardVM.cs", "EnemySide", 556);
				return BattleSideEnum.None;
			}
		}
	}

	[DataSourceProperty]
	public MPEndOfBattleVM EndOfBattle
	{
		get
		{
			return _endOfBattle;
		}
		set
		{
			if (value != _endOfBattle)
			{
				_endOfBattle = value;
				OnPropertyChangedWithValue(value, "EndOfBattle");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<StringPairItemWithActionVM> PlayerActionList
	{
		get
		{
			return _playerActionList;
		}
		set
		{
			if (value != _playerActionList)
			{
				_playerActionList = value;
				OnPropertyChangedWithValue(value, "PlayerActionList");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<MissionScoreboardSideVM> Sides
	{
		get
		{
			return _sides;
		}
		set
		{
			if (value != _sides)
			{
				_sides = value;
				OnPropertyChangedWithValue(value, "Sides");
			}
		}
	}

	[DataSourceProperty]
	public bool IsUpdateOver
	{
		get
		{
			return _isUpdateOver;
		}
		set
		{
			_isUpdateOver = value;
			OnPropertyChangedWithValue(value, "IsUpdateOver");
		}
	}

	[DataSourceProperty]
	public bool IsInitalizationOver
	{
		get
		{
			return _isInitalizationOver;
		}
		set
		{
			if (value != _isInitalizationOver)
			{
				_isInitalizationOver = value;
				OnPropertyChangedWithValue(value, "IsInitalizationOver");
			}
		}
	}

	[DataSourceProperty]
	public bool IsMouseEnabled
	{
		get
		{
			return _isMouseEnabled;
		}
		set
		{
			if (value != _isMouseEnabled)
			{
				_isMouseEnabled = value;
				OnPropertyChangedWithValue(value, "IsMouseEnabled");
			}
		}
	}

	[DataSourceProperty]
	public bool IsActive
	{
		get
		{
			return _isActive;
		}
		set
		{
			if (value != _isActive)
			{
				_isActive = value;
				OnPropertyChangedWithValue(value, "IsActive");
			}
		}
	}

	[DataSourceProperty]
	public bool IsPlayerActionsActive
	{
		get
		{
			return _isPlayerActionsActive;
		}
		set
		{
			if (value != _isPlayerActionsActive)
			{
				_isPlayerActionsActive = value;
				OnPropertyChangedWithValue(value, "IsPlayerActionsActive");
			}
		}
	}

	[DataSourceProperty]
	public string Spectators
	{
		get
		{
			return _spectators;
		}
		set
		{
			if (value != _spectators)
			{
				_spectators = value;
				OnPropertyChangedWithValue(value, "Spectators");
			}
		}
	}

	[DataSourceProperty]
	public bool ShowSpectatorCount
	{
		get
		{
			return _showSpectatorCount;
		}
		set
		{
			if (value != _showSpectatorCount)
			{
				_showSpectatorCount = value;
				OnPropertyChangedWithValue(value, "ShowSpectatorCount");
			}
		}
	}

	[DataSourceProperty]
	public InputKeyItemVM ShowMouseKey
	{
		get
		{
			return _showMouseKey;
		}
		set
		{
			if (value != _showMouseKey)
			{
				_showMouseKey = value;
				OnPropertyChangedWithValue(value, "ShowMouseKey");
			}
		}
	}

	[DataSourceProperty]
	public string MissionName
	{
		get
		{
			return _missionName;
		}
		set
		{
			if (value != _missionName)
			{
				_missionName = value;
				OnPropertyChangedWithValue(value, "MissionName");
			}
		}
	}

	[DataSourceProperty]
	public string GameModeText
	{
		get
		{
			return _gameModeText;
		}
		set
		{
			if (value != _gameModeText)
			{
				_gameModeText = value;
				OnPropertyChangedWithValue(value, "GameModeText");
			}
		}
	}

	[DataSourceProperty]
	public string MapName
	{
		get
		{
			return _mapName;
		}
		set
		{
			if (value != _mapName)
			{
				_mapName = value;
				OnPropertyChangedWithValue(value, "MapName");
			}
		}
	}

	[DataSourceProperty]
	public string ServerName
	{
		get
		{
			return _serverName;
		}
		set
		{
			if (value != _serverName)
			{
				_serverName = value;
				OnPropertyChangedWithValue(value, "ServerName");
			}
		}
	}

	[DataSourceProperty]
	public bool IsBotsEnabled
	{
		get
		{
			return _isBotsEnabled;
		}
		set
		{
			if (value != _isBotsEnabled)
			{
				_isBotsEnabled = value;
				OnPropertyChangedWithValue(value, "IsBotsEnabled");
			}
		}
	}

	[DataSourceProperty]
	public bool IsSingleSide
	{
		get
		{
			return _isSingleSide;
		}
		set
		{
			if (value != _isSingleSide)
			{
				_isSingleSide = value;
				OnPropertyChangedWithValue(value, "IsSingleSide");
			}
		}
	}

	[DataSourceProperty]
	public string ToggleMuteText
	{
		get
		{
			return _toggleMuteText;
		}
		set
		{
			if (value != _toggleMuteText)
			{
				_toggleMuteText = value;
				OnPropertyChangedWithValue(value, "ToggleMuteText");
			}
		}
	}

	public MissionScoreboardVM(bool isSingleTeam, Mission mission)
	{
		_chatBox = Game.Current.GetGameHandler<ChatBox>();
		_chatBox.OnPlayerMuteChanged += OnPlayerMuteChanged;
		_mission = mission;
		MissionLobbyComponent missionBehavior = mission.GetMissionBehavior<MissionLobbyComponent>();
		_missionScoreboardComponent = mission.GetMissionBehavior<MissionScoreboardComponent>();
		_voiceChatHandler = _mission.GetMissionBehavior<VoiceChatHandler>();
		_permissionHandler = GameNetwork.GetNetworkComponent<MultiplayerPermissionHandler>();
		_canStartKickPolls = MultiplayerOptions.OptionType.AllowPollsToKickPlayers.GetBoolValue();
		if (_canStartKickPolls)
		{
			_missionPollComponent = mission.GetMissionBehavior<MultiplayerPollComponent>();
		}
		EndOfBattle = new MPEndOfBattleVM(mission, _missionScoreboardComponent, isSingleTeam);
		PlayerActionList = new MBBindingList<StringPairItemWithActionVM>();
		Sides = new MBBindingList<MissionScoreboardSideVM>();
		_missionSides = new Dictionary<BattleSideEnum, MissionScoreboardSideVM>();
		IsSingleSide = isSingleTeam;
		InitSides();
		GameKey gameKey = HotKeyManager.GetCategory("ScoreboardHotKeyCategory").GetGameKey(35);
		ShowMouseKey = InputKeyItemVM.CreateFromGameKey(gameKey, isConsoleOnly: false);
		MissionName = "";
		IsBotsEnabled = missionBehavior.MissionType == MultiplayerGameType.Captain || missionBehavior.MissionType == MultiplayerGameType.Battle;
		RegisterEvents();
		RefreshValues();
	}

	public override void OnFinalize()
	{
		base.OnFinalize();
		UnregisterEvents();
		foreach (MissionScoreboardSideVM side in Sides)
		{
			side.OnFinalize();
		}
	}

	private void RegisterEvents()
	{
		if (_voiceChatHandler != null)
		{
			_voiceChatHandler.OnPeerMuteStatusUpdated += OnPeerMuteStatusUpdated;
		}
		if (_permissionHandler != null)
		{
			_permissionHandler.OnPlayerPlatformMuteChanged += OnPlayerPlatformMuteChanged;
		}
		_missionScoreboardComponent.OnPlayerSideChanged += OnPlayerSideChanged;
		_missionScoreboardComponent.OnPlayerPropertiesChanged += OnPlayerPropertiesChanged;
		_missionScoreboardComponent.OnBotPropertiesChanged += OnBotPropertiesChanged;
		_missionScoreboardComponent.OnRoundPropertiesChanged += OnRoundPropertiesChanged;
		_missionScoreboardComponent.OnScoreboardInitialized += OnScoreboardInitialized;
		_missionScoreboardComponent.OnMVPSelected += OnMVPSelected;
		MissionPeer.OnTeamChanged += OnPeerTeamChanged;
	}

	private void UnregisterEvents()
	{
		_missionScoreboardComponent.OnPlayerSideChanged -= OnPlayerSideChanged;
		_missionScoreboardComponent.OnPlayerPropertiesChanged -= OnPlayerPropertiesChanged;
		_missionScoreboardComponent.OnBotPropertiesChanged -= OnBotPropertiesChanged;
		_missionScoreboardComponent.OnRoundPropertiesChanged -= OnRoundPropertiesChanged;
		_missionScoreboardComponent.OnScoreboardInitialized -= OnScoreboardInitialized;
		_missionScoreboardComponent.OnMVPSelected -= OnMVPSelected;
		_chatBox.OnPlayerMuteChanged -= OnPlayerMuteChanged;
		MissionPeer.OnTeamChanged -= OnPeerTeamChanged;
		if (_voiceChatHandler != null)
		{
			_voiceChatHandler.OnPeerMuteStatusUpdated -= OnPeerMuteStatusUpdated;
		}
		if (_permissionHandler != null)
		{
			_permissionHandler.OnPlayerPlatformMuteChanged -= OnPlayerPlatformMuteChanged;
		}
	}

	private void OnPlayerPlatformMuteChanged(PlayerId playerId, bool isPlayerMuted)
	{
		foreach (MissionScoreboardSideVM side in Sides)
		{
			foreach (MissionScoreboardPlayerVM player in side.Players)
			{
				if (!player.IsBot && player.Peer.Peer.Id.Equals(playerId))
				{
					player.UpdateIsMuted();
					return;
				}
			}
		}
	}

	private void OnPlayerMuteChanged(PlayerId playerId, bool isMuted)
	{
		foreach (MissionScoreboardSideVM side in Sides)
		{
			foreach (MissionScoreboardPlayerVM player in side.Players)
			{
				if (!player.IsBot && player.Peer.Peer.Id.Equals(playerId))
				{
					player.UpdateIsMuted();
					return;
				}
			}
		}
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		MissionLobbyComponent missionBehavior = _mission.GetMissionBehavior<MissionLobbyComponent>();
		UpdateToggleMuteText();
		GameModeText = GameTexts.FindText("str_multiplayer_game_type", missionBehavior.MissionType.ToString()).ToString().ToLower();
		EndOfBattle.RefreshValues();
		Sides.ApplyActionOnAllItems(delegate(MissionScoreboardSideVM x)
		{
			x.RefreshValues();
		});
		if (GameTexts.TryGetText("str_multiplayer_scene_name", out var textObject, missionBehavior.Mission.SceneName))
		{
			MapName = textObject.ToString();
		}
		else
		{
			MapName = missionBehavior.Mission.SceneName;
		}
		ServerName = MultiplayerOptions.OptionType.ServerName.GetStrValue();
		RefreshSpectatorCount();
		ShowMouseKey?.RefreshValues();
	}

	private void ExecutePopulateActionList(MissionScoreboardPlayerVM player)
	{
		PlayerActionList.Clear();
		if (player.Peer != null && !player.IsMine && !player.IsBot)
		{
			PlayerId id = player.Peer.Peer.Id;
			bool flag = _chatBox.IsPlayerMutedFromGame(id);
			bool flag2 = PermaMuteList.IsPlayerMuted(id);
			bool num = _chatBox.IsPlayerMutedFromPlatform(id);
			bool isMutedFromPlatform = player.Peer.IsMutedFromPlatform;
			if (!num)
			{
				if (!flag2)
				{
					if (PlatformServices.Instance.IsPermanentMuteAvailable)
					{
						PlayerActionList.Add(new StringPairItemWithActionVM(ExecutePermanentlyMute, new TextObject("{=77jmd4QF}Mute Permanently").ToString(), "PermanentMute", player));
					}
					string definition = (flag ? GameTexts.FindText("str_mp_scoreboard_context_unmute_text").ToString() : GameTexts.FindText("str_mp_scoreboard_context_mute_text").ToString());
					PlayerActionList.Add(new StringPairItemWithActionVM(ExecuteMute, definition, flag ? "UnmuteText" : "MuteText", player));
				}
				else
				{
					PlayerActionList.Add(new StringPairItemWithActionVM(ExecuteLiftPermanentMute, new TextObject("{=CIVPNf2d}Remove Permanent Mute").ToString(), "UnmuteText", player));
				}
			}
			if (player.IsTeammate)
			{
				if (!isMutedFromPlatform && _voiceChatHandler != null && !flag2)
				{
					string definition2 = (player.Peer.IsMuted ? GameTexts.FindText("str_mp_scoreboard_context_unmute_voice").ToString() : GameTexts.FindText("str_mp_scoreboard_context_mute_voice").ToString());
					PlayerActionList.Add(new StringPairItemWithActionVM(ExecuteMuteVoice, definition2, player.Peer.IsMuted ? "UnmuteVoice" : "MuteVoice", player));
				}
				if (_canStartKickPolls)
				{
					PlayerActionList.Add(new StringPairItemWithActionVM(ExecuteKick, GameTexts.FindText("str_mp_scoreboard_context_kick").ToString(), "StartKickPoll", player));
				}
			}
			StringPairItemWithActionVM stringPairItemWithActionVM = new StringPairItemWithActionVM(ExecuteReport, GameTexts.FindText("str_mp_scoreboard_context_report").ToString(), "Report", player);
			if (MultiplayerReportPlayerManager.IsPlayerReportedOverLimit(id))
			{
				stringPairItemWithActionVM.IsEnabled = false;
				stringPairItemWithActionVM.Hint.HintText = new TextObject("{=klkYFik9}You've already reported this player.");
			}
			PlayerActionList.Add(stringPairItemWithActionVM);
			MultiplayerPlayerContextMenuHelper.AddMissionViewProfileOptions(player, PlayerActionList);
		}
		if (PlayerActionList.Count > 0)
		{
			IsPlayerActionsActive = false;
			IsPlayerActionsActive = true;
		}
	}

	public void SetMouseState(bool isMouseVisible)
	{
		IsMouseEnabled = isMouseVisible;
	}

	private void ExecuteReport(object playerObj)
	{
		MissionScoreboardPlayerVM missionScoreboardPlayerVM = playerObj as MissionScoreboardPlayerVM;
		MultiplayerReportPlayerManager.RequestReportPlayer(NetworkMain.GameClient.CurrentMatchId, missionScoreboardPlayerVM.Peer.Peer.Id, missionScoreboardPlayerVM.Peer.DisplayedName, isRequestedFromMission: true);
	}

	private void ExecuteMute(object playerObj)
	{
		MissionScoreboardPlayerVM missionScoreboardPlayerVM = playerObj as MissionScoreboardPlayerVM;
		bool flag = _chatBox.IsPlayerMutedFromGame(missionScoreboardPlayerVM.Peer.Peer.Id);
		_chatBox.SetPlayerMuted(missionScoreboardPlayerVM.Peer.Peer.Id, !flag);
		GameTexts.SetVariable("PLAYER_NAME", missionScoreboardPlayerVM.Peer.DisplayedName);
		InformationManager.DisplayMessage(new InformationMessage((!flag) ? GameTexts.FindText("str_mute_notification").ToString() : GameTexts.FindText("str_unmute_notification").ToString()));
	}

	private void ExecuteMuteVoice(object playerObj)
	{
		MissionScoreboardPlayerVM missionScoreboardPlayerVM = playerObj as MissionScoreboardPlayerVM;
		missionScoreboardPlayerVM.Peer.SetMuted(!missionScoreboardPlayerVM.Peer.IsMuted);
		missionScoreboardPlayerVM.UpdateIsMuted();
	}

	private void ExecutePermanentlyMute(object playerObj)
	{
		MissionScoreboardPlayerVM missionScoreboardPlayerVM = playerObj as MissionScoreboardPlayerVM;
		PermaMuteList.MutePlayer(missionScoreboardPlayerVM.Peer.Peer.Id, missionScoreboardPlayerVM.Peer.Name);
		missionScoreboardPlayerVM.Peer.SetMuted(isMuted: true);
		missionScoreboardPlayerVM.UpdateIsMuted();
		GameTexts.SetVariable("PLAYER_NAME", missionScoreboardPlayerVM.Peer.DisplayedName);
		InformationManager.DisplayMessage(new InformationMessage(GameTexts.FindText("str_permanent_mute_notification").ToString()));
	}

	private void ExecuteLiftPermanentMute(object playerObj)
	{
		MissionScoreboardPlayerVM missionScoreboardPlayerVM = playerObj as MissionScoreboardPlayerVM;
		PermaMuteList.RemoveMutedPlayer(missionScoreboardPlayerVM.Peer.Peer.Id);
		missionScoreboardPlayerVM.Peer.SetMuted(isMuted: false);
		missionScoreboardPlayerVM.UpdateIsMuted();
		GameTexts.SetVariable("PLAYER_NAME", missionScoreboardPlayerVM.Peer.DisplayedName);
		InformationManager.DisplayMessage(new InformationMessage(GameTexts.FindText("str_unmute_notification").ToString()));
	}

	private void ExecuteKick(object playerObj)
	{
		MissionScoreboardPlayerVM missionScoreboardPlayerVM = playerObj as MissionScoreboardPlayerVM;
		_missionPollComponent.RequestKickPlayerPoll(missionScoreboardPlayerVM.Peer.GetNetworkPeer(), banPlayer: false);
	}

	public void Tick(float dt)
	{
		if (!IsActive)
		{
			return;
		}
		EndOfBattle?.Tick(dt);
		CheckAttributeRefresh(dt);
		foreach (MissionScoreboardSideVM side in Sides)
		{
			side.Tick(dt);
		}
		foreach (MissionScoreboardSideVM side2 in Sides)
		{
			foreach (MissionScoreboardPlayerVM player in side2.Players)
			{
				player.RefreshDivision(IsSingleSide);
			}
		}
	}

	private void CheckAttributeRefresh(float dt)
	{
		_attributeRefreshTimeElapsed += dt;
		if (_attributeRefreshTimeElapsed >= 1f)
		{
			UpdateSideAllPlayersAttributes(BattleSideEnum.Attacker);
			UpdateSideAllPlayersAttributes(BattleSideEnum.Defender);
			RefreshSpectatorCount();
			_attributeRefreshTimeElapsed = 0f;
		}
	}

	private void UpdateSideAllPlayersAttributes(BattleSideEnum battleSide)
	{
		MissionScoreboardComponent.MissionScoreboardSide missionScoreboardSide = _missionScoreboardComponent.Sides.FirstOrDefault((MissionScoreboardComponent.MissionScoreboardSide s) => s != null && s.Side == battleSide);
		if (missionScoreboardSide == null)
		{
			return;
		}
		foreach (MissionPeer player in missionScoreboardSide.Players)
		{
			OnPlayerPropertiesChanged(battleSide, player);
		}
	}

	public void OnPlayerSideChanged(Team curTeam, Team nextTeam, MissionPeer client)
	{
		if (client.IsMine && nextTeam != null && IsSideValid(nextTeam.Side))
		{
			InitSides();
			return;
		}
		if (curTeam != null && IsSideValid(curTeam.Side))
		{
			_missionSides[_missionScoreboardComponent.GetSideSafe(curTeam.Side).Side].RemovePlayer(client);
		}
		if (nextTeam != null && IsSideValid(nextTeam.Side))
		{
			_missionSides[_missionScoreboardComponent.GetSideSafe(nextTeam.Side).Side].AddPlayer(client);
		}
	}

	private void OnRoundPropertiesChanged()
	{
		foreach (MissionScoreboardSideVM value in _missionSides.Values)
		{
			value.UpdateRoundAttributes();
		}
	}

	private void OnPlayerPropertiesChanged(BattleSideEnum side, MissionPeer client)
	{
		if (IsSideValid(side))
		{
			_missionSides[_missionScoreboardComponent.GetSideSafe(side).Side].UpdatePlayerAttributes(client);
		}
	}

	private void OnBotPropertiesChanged(BattleSideEnum side)
	{
		BattleSideEnum side2 = _missionScoreboardComponent.GetSideSafe(side).Side;
		if (IsSideValid(side2))
		{
			_missionSides[side2].UpdateBotAttributes();
		}
	}

	private void OnScoreboardInitialized()
	{
		InitSides();
	}

	private void OnMVPSelected(MissionPeer mvpPeer, int mvpCount)
	{
		foreach (MissionScoreboardSideVM side in Sides)
		{
			foreach (MissionScoreboardPlayerVM player in side.Players)
			{
				if (player.Peer == mvpPeer)
				{
					player.SetMVPBadgeCount(mvpCount);
					break;
				}
			}
		}
	}

	private bool IsSideValid(BattleSideEnum side)
	{
		if (IsSingleSide)
		{
			if (_missionScoreboardComponent != null && side != BattleSideEnum.None)
			{
				return side != BattleSideEnum.NumSides;
			}
			return false;
		}
		if (_missionScoreboardComponent != null && side != BattleSideEnum.None && side != BattleSideEnum.NumSides)
		{
			return _missionScoreboardComponent.Sides.Any((MissionScoreboardComponent.MissionScoreboardSide s) => s != null && s.Side == side);
		}
		return false;
	}

	private void InitSides()
	{
		Sides.Clear();
		_missionSides.Clear();
		if (IsSingleSide)
		{
			MissionScoreboardComponent.MissionScoreboardSide sideSafe = _missionScoreboardComponent.GetSideSafe(BattleSideEnum.Defender);
			MultiplayerBattleColors multiplayerBattleColors = MultiplayerBattleColors.CreateWith(sideSafe.GetCulture(), null);
			MissionScoreboardSideVM missionScoreboardSideVM = new MissionScoreboardSideVM(sideSafe, ExecutePopulateActionList, IsSingleSide, isSecondSide: false, multiplayerBattleColors.AttackerColors);
			Sides.Add(missionScoreboardSideVM);
			_missionSides.Add(sideSafe.Side, missionScoreboardSideVM);
			return;
		}
		MissionPeer missionPeer = GameNetwork.MyPeer?.GetComponent<MissionPeer>();
		MissionScoreboardComponent.MissionScoreboardSide missionScoreboardSide = _missionScoreboardComponent.Sides.FirstOrDefault((MissionScoreboardComponent.MissionScoreboardSide s) => s != null && s.Side == BattleSideEnum.Attacker);
		MissionScoreboardComponent.MissionScoreboardSide missionScoreboardSide2 = _missionScoreboardComponent.Sides.FirstOrDefault((MissionScoreboardComponent.MissionScoreboardSide s) => s != null && s.Side == BattleSideEnum.Defender);
		MissionScoreboardComponent.MissionScoreboardSide missionScoreboardSide3 = ((missionPeer != null && missionPeer.Team?.Side == BattleSideEnum.Attacker) ? missionScoreboardSide : missionScoreboardSide2);
		MissionScoreboardComponent.MissionScoreboardSide missionScoreboardSide4 = ((missionPeer != null && missionPeer.Team?.Side == BattleSideEnum.Attacker) ? missionScoreboardSide2 : missionScoreboardSide);
		BasicCultureObject basicCultureObject = missionScoreboardSide3?.GetCulture();
		BasicCultureObject basicCultureObject2 = missionScoreboardSide4?.GetCulture();
		MultiplayerBattleColors multiplayerBattleColors2 = MultiplayerBattleColors.CreateWith(basicCultureObject, basicCultureObject2);
		if (missionScoreboardSide3 != null)
		{
			MissionScoreboardSideVM missionScoreboardSideVM2 = new MissionScoreboardSideVM(cultureColorInfo: (!(basicCultureObject.StringId == basicCultureObject2.StringId)) ? multiplayerBattleColors2.AttackerColors : ((missionPeer != null && missionPeer.Team?.Side == BattleSideEnum.Attacker) ? multiplayerBattleColors2.AttackerColors : multiplayerBattleColors2.DefenderColors), missionScoreboardSide: missionScoreboardSide3, executeActivate: ExecutePopulateActionList, isSingleSide: IsSingleSide, isSecondSide: false);
			Sides.Add(missionScoreboardSideVM2);
			_missionSides.Add(missionScoreboardSide3.Side, missionScoreboardSideVM2);
		}
		if (missionScoreboardSide4 != null)
		{
			MissionScoreboardSideVM missionScoreboardSideVM3 = new MissionScoreboardSideVM(cultureColorInfo: (!(basicCultureObject.StringId == basicCultureObject2.StringId)) ? multiplayerBattleColors2.DefenderColors : ((missionPeer != null && missionPeer.Team?.Side == BattleSideEnum.Attacker) ? multiplayerBattleColors2.DefenderColors : multiplayerBattleColors2.AttackerColors), missionScoreboardSide: missionScoreboardSide4, executeActivate: ExecutePopulateActionList, isSingleSide: IsSingleSide, isSecondSide: true);
			Sides.Add(missionScoreboardSideVM3);
			_missionSides.Add(missionScoreboardSide4.Side, missionScoreboardSideVM3);
		}
	}

	private void OnPeerTeamChanged(NetworkCommunicator peer, Team previousTeam, Team newTeam)
	{
		Team spectatorTeam = _mission.SpectatorTeam;
		if (previousTeam == spectatorTeam || newTeam == spectatorTeam)
		{
			RefreshSpectatorCount();
		}
	}

	public void RefreshSpectatorCount()
	{
		Team spectatorTeam = _mission.SpectatorTeam;
		if (spectatorTeam == null)
		{
			ShowSpectatorCount = false;
			return;
		}
		int num = 0;
		foreach (NetworkCommunicator networkPeer in GameNetwork.NetworkPeers)
		{
			if (networkPeer != null && !networkPeer.IsServerPeer && networkPeer.GetComponent<MissionPeer>()?.Team == spectatorTeam)
			{
				num++;
			}
		}
		ShowSpectatorCount = num > 0;
		if (ShowSpectatorCount)
		{
			TextObject textObject = new TextObject("{=GLkNGEPu}{?COUNT_PLURAL}{COUNT} spectators are{?}{COUNT} spectator is{\\?} watching this game.");
			textObject.SetTextVariable("COUNT", num);
			textObject.SetTextVariable("COUNT_PLURAL", (num != 1) ? 1 : 0);
			Spectators = textObject.ToString();
		}
	}

	public void ExecuteToggleMute()
	{
		foreach (MissionScoreboardSideVM side in Sides)
		{
			foreach (MissionScoreboardPlayerVM player in side.Players)
			{
				if (!player.IsMine && player.Peer != null)
				{
					_chatBox.SetPlayerMuted(player.Peer.Peer.Id, !_hasMutedAll);
					player.Peer.SetMuted(!_hasMutedAll);
					player.UpdateIsMuted();
				}
			}
		}
		_hasMutedAll = !_hasMutedAll;
		UpdateToggleMuteText();
	}

	private void UpdateToggleMuteText()
	{
		if (_hasMutedAll)
		{
			ToggleMuteText = _unmuteAllText.ToString();
		}
		else
		{
			ToggleMuteText = _muteAllText.ToString();
		}
	}

	private void OnPeerMuteStatusUpdated(MissionPeer peer)
	{
		foreach (MissionScoreboardSideVM side in Sides)
		{
			foreach (MissionScoreboardPlayerVM player in side.Players)
			{
				if (player.Peer == peer)
				{
					player.UpdateIsMuted();
					break;
				}
			}
		}
	}
}
