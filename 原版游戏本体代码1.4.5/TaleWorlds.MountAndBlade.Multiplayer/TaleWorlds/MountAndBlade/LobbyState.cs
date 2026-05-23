using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using TaleWorlds.Core;
using TaleWorlds.Diamond;
using TaleWorlds.Diamond.AccessProvider.Test;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.Library.NewsManager;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade.Diamond;
using TaleWorlds.MountAndBlade.Diamond.Lobby;
using TaleWorlds.MountAndBlade.Diamond.Ranked;
using TaleWorlds.MountAndBlade.Multiplayer;
using TaleWorlds.ObjectSystem;
using TaleWorlds.PlatformService;
using TaleWorlds.PlayerServices;

namespace TaleWorlds.MountAndBlade;

public class LobbyState : GameState
{
	private const string _newsSourceURLBase = "https://cdn.taleworlds.com/upload/bannerlordnews/NewsFeed_";

	private BannerlordFriendListService _bannerlordFriendListService;

	private RecentPlayersFriendListService _recentPlayersFriendListService;

	private ClanFriendListService _clanFriendListService;

	private readonly object _sessionInvitationDataLock = new object();

	private ILobbyStateHandler _handler;

	private LobbyGameClientHandler _lobbyGameClientManager;

	private ConcurrentDictionary<(PlayerId PlayerId, Permission Permission), bool> _registeredPermissionEvents;

	private List<Func<GameServerEntry, List<CustomServerAction>>> _onCustomServerActionRequestedForServerEntry;

	public Action<bool> OnMultiplayerPrivilegeUpdated;

	public Action<bool> OnCrossplayPrivilegeUpdated;

	public Action<bool> OnUserGeneratedContentPrivilegeUpdated;

	private bool AutoConnect
	{
		get
		{
			if (TestCommonBase.BaseInstance != null && TestCommonBase.BaseInstance.IsTestEnabled)
			{
				return false;
			}
			return true;
		}
	}

	public override bool IsMenuState => true;

	public override bool IsMusicMenuState => false;

	public bool IsLoggingIn { get; private set; }

	public ILobbyStateHandler Handler
	{
		get
		{
			return _handler;
		}
		set
		{
			_handler = value;
		}
	}

	public LobbyClient LobbyClient => NetworkMain.GameClient;

	public NewsManager NewsManager { get; private set; }

	public bool? HasMultiplayerPrivilege { get; private set; }

	public bool? HasCrossplayPrivilege { get; private set; }

	public bool? HasUserGeneratedContentPrivilege { get; private set; }

	public event Action<GameServerEntry> ClientRefusedToJoinCustomServer;

	public LobbyState()
	{
		_registeredPermissionEvents = new ConcurrentDictionary<(PlayerId, Permission), bool>();
		_onCustomServerActionRequestedForServerEntry = new List<Func<GameServerEntry, List<CustomServerAction>>>();
	}

	public void InitializeLogic(ILobbyStateHandler lobbyStateHandler)
	{
		Handler = lobbyStateHandler;
	}

	protected override async void OnInitialize()
	{
		base.OnInitialize();
		MultiplayerLocalDataManager.InitializeManager();
		CommunityClient communityClient = NetworkMain.CommunityClient;
		CommunityClientOnlineLobbyGameHandler handler = new CommunityClientOnlineLobbyGameHandler(this);
		communityClient.Handler = handler;
		LobbyClient.SetLoadedModules(Utilities.GetModulesNames());
		PlatformServices.Instance.OnSignInStateUpdated += OnPlatformSignInStateUpdated;
		PlatformServices.Instance.OnNameUpdated += OnPlayerNameUpdated;
		IFriendListService[] friendListServices = PlatformServices.Instance.GetFriendListServices();
		foreach (IFriendListService friendListService in friendListServices)
		{
			Type type = friendListService.GetType();
			if (type == typeof(BannerlordFriendListService))
			{
				_bannerlordFriendListService = (BannerlordFriendListService)friendListService;
			}
			else if (type == typeof(RecentPlayersFriendListService))
			{
				_recentPlayersFriendListService = (RecentPlayersFriendListService)friendListService;
			}
			else if (type == typeof(ClanFriendListService))
			{
				_clanFriendListService = (ClanFriendListService)friendListService;
			}
		}
		NewsManager = new NewsManager();
		NewsManager.SetNewsSourceURL(GetApplicableNewsSourceURL());
		RecentPlayersManager.Initialize();
		_onCustomServerActionRequestedForServerEntry = new List<Func<GameServerEntry, List<CustomServerAction>>>();
		_lobbyGameClientManager = new LobbyGameClientHandler();
		_lobbyGameClientManager.LobbyState = this;
		NewsManager.UpdateNewsItems(forceRefresh: false);
		if (HasMultiplayerPrivilege == true && AutoConnect)
		{
			await TryLogin();
		}
		else
		{
			SetConnectionState(isAuthenticated: false);
			OnResume();
		}
		if (PlatformServices.SessionInvitationType != SessionInvitationType.None)
		{
			OnSessionInvitationAccepted(PlatformServices.SessionInvitationType);
		}
		else if (PlatformServices.IsPlatformRequestedMultiplayer)
		{
			OnPlatformRequestedMultiplayer();
		}
		PlatformServices.OnSessionInvitationAccepted = (Action<SessionInvitationType>)Delegate.Combine(PlatformServices.OnSessionInvitationAccepted, new Action<SessionInvitationType>(OnSessionInvitationAccepted));
		PlatformServices.OnPlatformRequestedMultiplayer = (Action)Delegate.Combine(PlatformServices.OnPlatformRequestedMultiplayer, new Action(OnPlatformRequestedMultiplayer));
	}

	protected override void OnActivate()
	{
		base.OnActivate();
		MultiplayerIntermissionVotingManager.Instance.UsableMaps.Clear();
	}

	private void OnPlayerNameUpdated(string newName)
	{
		LobbyClient.OnPlayerNameUpdated(newName);
		Handler?.OnPlayerNameUpdated(newName);
	}

	protected override void OnFinalize()
	{
		base.OnFinalize();
		MultiplayerLocalDataManager.FinalizeManager();
		PlatformServices.OnPlatformRequestedMultiplayer = (Action)Delegate.Remove(PlatformServices.OnPlatformRequestedMultiplayer, new Action(OnPlatformRequestedMultiplayer));
		PlatformServices.OnSessionInvitationAccepted = (Action<SessionInvitationType>)Delegate.Remove(PlatformServices.OnSessionInvitationAccepted, new Action<SessionInvitationType>(OnSessionInvitationAccepted));
		PlatformServices.Instance.OnSignInStateUpdated -= OnPlatformSignInStateUpdated;
		PlatformServices.Instance.OnNameUpdated -= OnPlayerNameUpdated;
		LobbyClient.RemoveLobbyClientHandler();
		RecentPlayersManager.Serialize();
		NewsManager.OnFinalize();
		NewsManager = null;
		_onCustomServerActionRequestedForServerEntry.Clear();
		_onCustomServerActionRequestedForServerEntry = null;
		foreach (var key in _registeredPermissionEvents.Keys)
		{
			if (PlatformServices.Instance.UnregisterPermissionChangeEvent(key.PlayerId, key.Permission, MultiplayerPermissionWithPlayerChanged))
			{
				_registeredPermissionEvents.TryRemove((key.PlayerId, key.Permission), out var _);
			}
		}
	}

	protected override void OnTick(float dt)
	{
		base.OnTick(dt);
		MultiplayerLocalDataManager.Instance.Tick(dt);
	}

	private string GetApplicableNewsSourceURL()
	{
		bool num = NewsManager.LocalizationID == "zh";
		bool isInPreviewMode = NewsManager.IsInPreviewMode;
		string text = (num ? "zh" : "en");
		if (!isInPreviewMode)
		{
			return "https://cdn.taleworlds.com/upload/bannerlordnews/NewsFeed_" + text + ".json";
		}
		return "https://cdn.taleworlds.com/upload/bannerlordnews/NewsFeed_" + text + "_preview.json";
	}

	private string GetApplicableAnnouncementsURL()
	{
		return "https://cdn.taleworlds.com/bannerlord-ingame/LobbyNewsFeed.json";
	}

	[Conditional("_RGL_KEEP_ASSERTS")]
	private void CheckValidityOfItems()
	{
		foreach (ItemObject objectType in MBObjectManager.Instance.GetObjectTypeList<ItemObject>())
		{
			if (!objectType.IsUsingTeamColor)
			{
				continue;
			}
			MetaMesh copy = MetaMesh.GetCopy(objectType.MultiMeshName, showErrors: false);
			for (int i = 0; i < copy.MeshCount; i++)
			{
				Material material = copy.GetMeshAtIndex(i).GetMaterial();
				if (material.Name != "vertex_color_lighting_skinned" && material.Name != "vertex_color_lighting" && material.GetTexture(Material.MBTextureType.DiffuseMap2) == null)
				{
					MBDebug.ShowWarning(string.Concat("Item object(", objectType.Name, ") has 'Using Team Color' flag but does not have a mask texture in diffuse2 slot. "));
					break;
				}
			}
		}
	}

	public async Task UpdateHasMultiplayerPrivilege()
	{
		TaskCompletionSource<bool> tsc = new TaskCompletionSource<bool>();
		PlatformServices.Instance.CheckPrivilege(Privilege.Multiplayer, displayResolveUI: true, delegate(bool result)
		{
			tsc.SetResult(result);
		});
		HasMultiplayerPrivilege = await tsc.Task;
		OnMultiplayerPrivilegeUpdated?.Invoke(HasMultiplayerPrivilege.Value);
	}

	public async Task UpdateHasCrossplayPrivilege()
	{
		TaskCompletionSource<bool> tsc = new TaskCompletionSource<bool>();
		PlatformServices.Instance.CheckPrivilege(Privilege.Crossplay, displayResolveUI: false, delegate(bool result)
		{
			tsc.SetResult(result);
		});
		HasCrossplayPrivilege = await tsc.Task;
		OnCrossplayPrivilegeUpdated?.Invoke(HasCrossplayPrivilege.Value);
	}

	public void OnClientRefusedToJoinCustomServer(GameServerEntry serverEntry)
	{
		this.ClientRefusedToJoinCustomServer?.Invoke(serverEntry);
	}

	public async Task UpdateHasUserGeneratedContentPrivilege(bool showResolveUI)
	{
		TaskCompletionSource<bool> tsc = new TaskCompletionSource<bool>();
		PlatformServices.Instance.CheckPrivilege(Privilege.UserGeneratedContent, showResolveUI, delegate(bool result)
		{
			tsc.SetResult(result);
		});
		HasUserGeneratedContentPrivilege = await tsc.Task;
		OnUserGeneratedContentPrivilegeUpdated?.Invoke(HasUserGeneratedContentPrivilege.Value);
	}

	public async Task TryLogin()
	{
		IsLoggingIn = true;
		LobbyClient gameClient = LobbyClient;
		if (gameClient.IsIdle)
		{
			await UpdateHasMultiplayerPrivilege();
			if (!HasMultiplayerPrivilege.Value)
			{
				string title = new TextObject("{=lVfmVHbz}Login Failed").ToString();
				string message = new TextObject("{=cS0Hafjl}Player does not have access to multiplayer.").ToString();
				ShowFeedback(title, message);
				IsLoggingIn = false;
				return;
			}
			await UpdateHasCrossplayPrivilege();
			await UpdateHasUserGeneratedContentPrivilege(showResolveUI: false);
			ILoginAccessProvider loginAccessProvider = await PlatformServices.Instance.CreateLobbyClientLoginProvider();
			string userName = loginAccessProvider.GetUserName();
			Func<Task<bool>> preLoginTask = null;
			if (PlatformServices.InvitationServices != null)
			{
				preLoginTask = async () => await PlatformServices.InvitationServices.OnLogin();
			}
			LobbyClientConnectResult lobbyClientConnectResult = await gameClient.Connect(_lobbyGameClientManager, loginAccessProvider, userName, HasUserGeneratedContentPrivilege == true, PlatformServices.Instance.GetInitParams(), preLoginTask);
			if (lobbyClientConnectResult.Connected)
			{
				Game.Current.GetGameHandler<ChatBox>().OnLogin();
				OnResume();
			}
			else
			{
				string title2 = new TextObject("{=lVfmVHbz}Login Failed").ToString();
				ShowFeedback(title2, lobbyClientConnectResult.Error.ToString());
				SetConnectionState(isAuthenticated: false);
				OnResume();
			}
		}
		IsLoggingIn = false;
	}

	public async Task TryLogin(string userName, string password)
	{
		IsLoggingIn = true;
		LobbyClientConnectResult lobbyClientConnectResult = await NetworkMain.GameClient.Connect(_lobbyGameClientManager, new TestLoginAccessProvider(), userName, hasUserGeneratedContentPrivilege: true, PlatformServices.Instance.GetInitParams(), null);
		if (!lobbyClientConnectResult.Connected)
		{
			string title = new TextObject("{=lVfmVHbz}Login Failed").ToString();
			ShowFeedback(title, lobbyClientConnectResult.Error.ToString());
		}
		IsLoggingIn = false;
	}

	public void HostGame()
	{
		if (string.IsNullOrEmpty(MultiplayerOptions.OptionType.ServerName.GetStrValue()))
		{
			MultiplayerOptions.OptionType.ServerName.SetValue(NetworkMain.GameClient.Name);
		}
		string strValue = MultiplayerOptions.OptionType.GamePassword.GetStrValue();
		string strValue2 = MultiplayerOptions.OptionType.AdminPassword.GetStrValue();
		string value = ((!string.IsNullOrEmpty(strValue)) ? Common.CalculateMD5Hash(strValue) : null);
		string value2 = ((!string.IsNullOrEmpty(strValue2)) ? Common.CalculateMD5Hash(strValue2) : null);
		MultiplayerOptions.OptionType.GamePassword.SetValue(value);
		MultiplayerOptions.OptionType.AdminPassword.SetValue(value2);
		string strValue3 = MultiplayerOptions.OptionType.GameType.GetStrValue();
		string gameModule = MultiplayerGameTypes.GetGameTypeInfo(strValue3).GameModule;
		string strValue4 = MultiplayerOptions.OptionType.Map.GetStrValue();
		string uniqueMapId = null;
		if (Utilities.TryGetUniqueIdentifiersForScene(strValue4, out var identifiers))
		{
			uniqueMapId = identifiers.Serialize();
		}
		NetworkMain.GameClient.RegisterCustomGame(gameModule, strValue3, MultiplayerOptions.OptionType.ServerName.GetStrValue(), MultiplayerOptions.OptionType.MaxNumberOfPlayers.GetIntValue(), strValue4, uniqueMapId, MultiplayerOptions.OptionType.GamePassword.GetStrValue(), MultiplayerOptions.OptionType.AdminPassword.GetStrValue(), 9999);
	}

	public void CreatePremadeGame()
	{
		string strValue = MultiplayerOptions.OptionType.ServerName.GetStrValue();
		string strValue2 = MultiplayerOptions.OptionType.PremadeMatchGameMode.GetStrValue();
		string strValue3 = MultiplayerOptions.OptionType.Map.GetStrValue();
		string strValue4 = MultiplayerOptions.OptionType.CultureTeam1.GetStrValue();
		string strValue5 = MultiplayerOptions.OptionType.CultureTeam2.GetStrValue();
		string strValue6 = MultiplayerOptions.OptionType.GamePassword.GetStrValue();
		PremadeGameType premadeGameType = (PremadeGameType)Enum.GetValues(typeof(PremadeGameType)).GetValue(MultiplayerOptions.OptionType.PremadeGameType.GetIntValue());
		if (premadeGameType == PremadeGameType.Clan)
		{
			bool flag = true;
			foreach (PartyPlayerInLobbyClient partyPlayer in NetworkMain.GameClient.PlayersInParty)
			{
				if (NetworkMain.GameClient.PlayersInClan.FirstOrDefault((ClanPlayer clanPlayer) => clanPlayer.PlayerId == partyPlayer.PlayerId) == null)
				{
					flag = false;
				}
			}
			if (!flag)
			{
				ShowFeedback(new TextObject("{=oZrVNUOk}Error").ToString(), new TextObject("{=uNrXwGzr}Only practice matches are allowed with your current party. All members should be in the same clan for a clan match.").ToString());
				return;
			}
		}
		if (strValue != null && !strValue.IsEmpty() && premadeGameType != PremadeGameType.Invalid)
		{
			NetworkMain.GameClient.CreatePremadeGame(strValue, strValue2, strValue3, strValue4, strValue5, strValue6, premadeGameType);
		}
		else if (premadeGameType == PremadeGameType.Invalid)
		{
			ShowFeedback(new TextObject("{=oZrVNUOk}Error").ToString(), new TextObject("{=PfnS8HUd}Premade game type is invalid!").ToString());
		}
		else
		{
			ShowFeedback(new TextObject("{=oZrVNUOk}Error").ToString(), new TextObject("{=EgTUzWUz}Name Can't Be Empty!").ToString());
		}
	}

	public string ShowFeedback(string title, string message)
	{
		if (Handler != null)
		{
			return Handler.ShowFeedback(title, message);
		}
		return null;
	}

	public string ShowFeedback(InquiryData inquiryData)
	{
		if (Handler != null)
		{
			return Handler.ShowFeedback(inquiryData);
		}
		return null;
	}

	public void DismissFeedback(string messageId)
	{
		if (Handler != null)
		{
			Handler.DismissFeedback(messageId);
		}
	}

	public void OnPause()
	{
		if (Handler != null)
		{
			Handler.OnPause();
		}
	}

	public void OnResume()
	{
		if (Handler != null)
		{
			Handler.OnResume();
		}
	}

	public void OnRequestedToSearchBattle()
	{
		if (Handler != null)
		{
			Handler.OnRequestedToSearchBattle();
		}
	}

	public void OnUpdateFindingGame(MatchmakingWaitTimeStats matchmakingWaitTimeStats, string[] gameTypeInfo = null)
	{
		if (Handler != null)
		{
			Handler.OnUpdateFindingGame(matchmakingWaitTimeStats, gameTypeInfo);
		}
	}

	public void OnRequestedToCancelSearchBattle()
	{
		if (Handler != null)
		{
			Handler.OnRequestedToCancelSearchBattle();
		}
	}

	public void OnCancelFindingGame()
	{
		if (Handler != null)
		{
			Handler.OnSearchBattleCanceled();
		}
	}

	public void OnDisconnected(TextObject feedback)
	{
		if (Handler != null)
		{
			Handler.OnDisconnected();
		}
		if (feedback != null)
		{
			string title = new TextObject("{=MbXatV1Q}Disconnected").ToString();
			ShowFeedback(title, feedback.ToString());
		}
	}

	public void OnPlayerDataReceived(PlayerData playerData)
	{
		if (Handler != null)
		{
			Handler.OnPlayerDataReceived(playerData);
		}
	}

	public void OnPendingRejoin()
	{
		Handler?.OnPendingRejoin();
	}

	public void OnEnterBattleWithParty(string[] selectedGameTypes)
	{
		if (Handler != null)
		{
			Handler.OnEnterBattleWithParty(selectedGameTypes);
		}
	}

	public async void OnPartyInvitationReceived(string inviterPlayerName, PlayerId playerId)
	{
		while (IsLoggingIn)
		{
			TaleWorlds.Library.Debug.Print("Waiting for logging in to be done..");
			await Task.Delay(100);
		}
		if (PermaMuteList.IsPlayerMuted(playerId))
		{
			LobbyClient.DeclinePartyInvitation();
		}
		else
		{
			if (Handler == null)
			{
				return;
			}
			PlatformServices.Instance.CheckPrivilege(Privilege.Communication, displayResolveUI: true, delegate(bool privilegeResult)
			{
				if (privilegeResult)
				{
					if (playerId.ProvidedType != NetworkMain.GameClient.PlayerID.ProvidedType)
					{
						Handler?.OnPartyInvitationReceived(playerId);
					}
					else
					{
						PlatformServices.Instance.CheckPermissionWithUser(Permission.CommunicateUsingText, playerId, delegate(bool permissionResult)
						{
							if (permissionResult)
							{
								Handler?.OnPartyInvitationReceived(playerId);
							}
							else
							{
								LobbyClient.DeclinePartyInvitation();
							}
						});
					}
				}
				else
				{
					LobbyClient.DeclinePartyInvitation();
				}
			});
		}
	}

	public async void OnPartyJoinRequestReceived(PlayerId joiningPlayerId, PlayerId viaPlayerId, string viaFriendName)
	{
		while (IsLoggingIn)
		{
			TaleWorlds.Library.Debug.Print("Waiting for logging in to be done..");
			await Task.Delay(100);
		}
		if (PermaMuteList.IsPlayerMuted(joiningPlayerId))
		{
			LobbyClient.DeclinePartyJoinRequest(joiningPlayerId, PartyJoinDeclineReason.NoPlatformPermission);
		}
		else
		{
			if (Handler == null)
			{
				return;
			}
			if (joiningPlayerId.ProvidedType != NetworkMain.GameClient.PlayerID.ProvidedType)
			{
				Handler?.OnPartyJoinRequestReceived(joiningPlayerId, viaPlayerId, viaFriendName, !LobbyClient.IsInParty);
				return;
			}
			PlatformServices.Instance.CheckPermissionWithUser(Permission.CommunicateUsingText, joiningPlayerId, delegate(bool permissionResult)
			{
				if (permissionResult)
				{
					Handler?.OnPartyJoinRequestReceived(joiningPlayerId, viaPlayerId, viaFriendName, !LobbyClient.IsInParty);
				}
				else
				{
					LobbyClient.DeclinePartyJoinRequest(joiningPlayerId, PartyJoinDeclineReason.NoPlatformPermission);
				}
			});
		}
	}

	public void OnAdminMessageReceived(string message)
	{
		if (Handler != null)
		{
			Handler.OnAdminMessageReceived(message);
		}
	}

	public void OnPartyInvitationInvalidated()
	{
		if (Handler != null)
		{
			Handler.OnPartyInvitationInvalidated();
		}
	}

	public void OnPlayerInvitedToParty(PlayerId playerId)
	{
		if (Handler != null)
		{
			Handler.OnPlayerInvitedToParty(playerId);
		}
	}

	public void OnPlayerRemovedFromParty(PlayerId playerId, PartyRemoveReason reason)
	{
		if (playerId.Equals(LobbyClient.PlayerID))
		{
			PlatformServices.InvitationServices?.OnLeftParty();
		}
		if (PlatformServices.Instance.UnregisterPermissionChangeEvent(playerId, Permission.PlayMultiplayer, MultiplayerPermissionWithPlayerChanged))
		{
			_registeredPermissionEvents.TryRemove((playerId, Permission.PlayMultiplayer), out var _);
		}
		if (Handler != null)
		{
			Handler.OnPlayerRemovedFromParty(playerId, reason);
		}
	}

	public void OnPlayersAddedToParty(List<(PlayerId PlayerId, string PlayerName, bool IsPartyLeader)> addedPlayers, List<(PlayerId PlayerId, string PlayerName)> invitedPlayers)
	{
		foreach (var player in addedPlayers)
		{
			PlayerId item = player.PlayerId;
			if (item.ProvidedType != LobbyClient.PlayerID.ProvidedType)
			{
				Handler?.OnPlayerAddedToParty(player.PlayerId, player.PlayerName, player.IsPartyLeader);
				continue;
			}
			PlatformServices.Instance.CheckPermissionWithUser(Permission.PlayMultiplayer, player.PlayerId, delegate(bool hasPermission)
			{
				if (hasPermission)
				{
					if (PlatformServices.Instance.RegisterPermissionChangeEvent(player.PlayerId, Permission.PlayMultiplayer, MultiplayerPermissionWithPlayerChanged))
					{
						_registeredPermissionEvents.TryRemove((player.PlayerId, Permission.PlayMultiplayer), out var _);
					}
					Handler?.OnPlayerAddedToParty(player.PlayerId, player.PlayerName, player.IsPartyLeader);
				}
				else
				{
					NetworkMain.GameClient.KickPlayerFromParty(NetworkMain.GameClient.PlayerID);
				}
			});
		}
		if (Handler == null)
		{
			return;
		}
		foreach (var invitedPlayer in invitedPlayers)
		{
			var (playerId, _) = invitedPlayer;
			if (playerId.ProvidedType != LobbyClient.PlayerID.ProvidedType)
			{
				Handler?.OnPlayerInvitedToParty(playerId);
				continue;
			}
			PlatformServices.Instance.CheckPermissionWithUser(Permission.PlayMultiplayer, playerId, delegate(bool hasPermission)
			{
				if (hasPermission)
				{
					Handler?.OnPlayerInvitedToParty(playerId);
				}
			});
		}
	}

	private void MultiplayerPermissionWithPlayerChanged(PlayerId targetPlayerId, Permission permission, bool hasPermission)
	{
		if (!hasPermission && NetworkMain.GameClient.PlayersInParty.FirstOrDefault((PartyPlayerInLobbyClient p) => p.PlayerId == targetPlayerId) != null)
		{
			NetworkMain.GameClient.KickPlayerFromParty(NetworkMain.GameClient.PlayerID);
		}
	}

	public void OnGameClientStateChange(LobbyClient.State state)
	{
		if (!LobbyClient.IsInGame)
		{
			PlatformServices.MultiplayerGameStateChanged(isPlaying: false);
		}
		Handler?.OnGameClientStateChange(state);
		switch (state)
		{
		case LobbyClient.State.SessionRequested:
			MPPerkSelectionManager.Instance.InitializeForUser(LobbyClient.Name, LobbyClient.PlayerID);
			break;
		case LobbyClient.State.Idle:
			MPPerkSelectionManager.FreeInstance();
			break;
		default:
			if (!LobbyClient.AtLobby)
			{
				MPPerkSelectionManager.Instance.ResetPendingChanges();
			}
			break;
		}
		PlatformServices.LobbyClientStateChanged(state == LobbyClient.State.AtLobby, !LobbyClient.IsInParty || LobbyClient.IsPartyLeader);
	}

	public void SetConnectionState(bool isAuthenticated)
	{
		Handler?.SetConnectionState(isAuthenticated);
		PlatformServices.ConnectionStateChanged(isAuthenticated);
	}

	public void OnActivateHome()
	{
		Handler?.OnActivateHome();
	}

	public void OnActivateCustomServer()
	{
		Handler?.OnActivateCustomServer();
	}

	public void OnActivateMatchmaking()
	{
		Handler?.OnActivateMatchmaking();
	}

	public void OnActivateProfile()
	{
		Handler?.OnActivateProfile();
	}

	public void OnClanInvitationReceived(string clanName, string clanTag, bool isCreation)
	{
		Handler?.OnClanInvitationReceived(clanName, clanTag, isCreation);
	}

	public void OnClanInvitationAnswered(PlayerId playerId, ClanCreationAnswer answer)
	{
		Handler?.OnClanInvitationAnswered(playerId, answer);
	}

	public void OnClanCreationSuccessful()
	{
		Handler?.OnClanCreationSuccessful();
	}

	public void OnClanCreationFailed()
	{
		Handler?.OnClanCreationFailed();
	}

	public void OnClanCreationStarted()
	{
		Handler?.OnClanCreationStarted();
	}

	public void OnClanInfoChanged()
	{
		_clanFriendListService?.OnClanInfoChanged(LobbyClient.PlayerInfosInClan);
		Handler?.OnClanInfoChanged();
	}

	public void OnPremadeGameEligibilityStatusReceived(bool isEligible)
	{
		Handler?.OnPremadeGameEligibilityStatusReceived(isEligible);
	}

	public void OnPremadeGameCreated()
	{
		Handler?.OnPremadeGameCreated();
	}

	public void OnPremadeGameListReceived()
	{
		Handler?.OnPremadeGameListReceived();
	}

	public void OnPremadeGameCreationCancelled()
	{
		Handler?.OnPremadeGameCreationCancelled();
	}

	public void OnJoinPremadeGameRequested(string clanName, string clanSigilCode, Guid partyId, PlayerId[] challengerPlayerIDs, PlayerId challengerPartyLeaderID, PremadeGameType premadeGameType)
	{
		Handler?.OnJoinPremadeGameRequested(clanName, clanSigilCode, partyId, challengerPlayerIDs, challengerPartyLeaderID, premadeGameType);
	}

	public void OnJoinPremadeGameRequestSuccessful()
	{
		Handler?.OnJoinPremadeGameRequestSuccessful();
	}

	public void OnActivateArmory()
	{
		Handler?.OnActivateArmory();
	}

	public void OnActivateOptions()
	{
		Handler?.OnActivateOptions();
	}

	public void OnDeactivateOptions()
	{
		Handler?.OnDeactivateOptions();
	}

	public void OnCustomGameServerListReceived(AvailableCustomGames customGameServerList)
	{
		Handler?.OnCustomGameServerListReceived(customGameServerList);
	}

	public void OnMatchmakerGameOver(int oldExp, int newExp, List<string> badgesEarned, int lootGained, RankBarInfo oldRankBarInfo, RankBarInfo newRankBarInfo, BattleCancelReason battleCancelReason)
	{
		Handler?.OnMatchmakerGameOver(oldExp, newExp, badgesEarned, lootGained, oldRankBarInfo, newRankBarInfo, battleCancelReason);
	}

	public void OnBattleServerLost()
	{
		Handler?.OnBattleServerLost();
	}

	public void OnRemovedFromMatchmakerGame(DisconnectType disconnectType)
	{
		Handler?.OnRemovedFromMatchmakerGame(disconnectType);
	}

	public void OnRemovedFromCustomGame(DisconnectType disconnectType)
	{
		Handler?.OnRemovedFromCustomGame(disconnectType);
	}

	public void OnPlayerAssignedPartyLeader(PlayerId partyLeaderId)
	{
		Handler?.OnPlayerAssignedPartyLeader(partyLeaderId);
	}

	public void OnPlayerSuggestedToParty(PlayerId playerId, string playerName, PlayerId suggestingPlayerId, string suggestingPlayerName)
	{
		Handler?.OnPlayerSuggestedToParty(playerId, playerName, suggestingPlayerId, suggestingPlayerName);
	}

	public void OnJoinCustomGameFailureResponse(CustomGameJoinResponse response)
	{
		Handler?.OnJoinCustomGameFailureResponse(response);
	}

	public void OnServerStatusReceived(ServerStatus serverStatus)
	{
		Handler?.OnServerStatusReceived(serverStatus);
	}

	public void OnFriendListReceived(FriendInfo[] friends)
	{
		Handler?.OnFriendListUpdated();
		_bannerlordFriendListService?.OnFriendListReceived(friends);
	}

	public void OnRecentPlayerStatusesReceived(FriendInfo[] friends)
	{
		_recentPlayersFriendListService?.OnFriendListReceived(friends);
	}

	public void OnBattleServerInformationReceived(BattleServerInformationForClient battleServerInformation)
	{
		Handler?.OnBattleServerInformationReceived(battleServerInformation);
	}

	public void OnRejoinBattleRequestAnswered(bool isSuccessful)
	{
		Handler?.OnRejoinBattleRequestAnswered(isSuccessful);
	}

	internal void OnSigilChanged()
	{
		if (Handler != null)
		{
			Handler.OnSigilChanged();
		}
	}

	public void OnNotificationsReceived(LobbyNotification[] notifications)
	{
		if (Handler != null)
		{
			Handler.OnNotificationsReceived(notifications);
		}
	}

	private void OnPlatformSignInStateUpdated(bool isSignedIn, TextObject message)
	{
		if (!isSignedIn && LobbyClient.Connected)
		{
			LobbyClient.Logout(message ?? new TextObject("{=oPOa77dI}Logged out of platform"));
		}
	}

	[Conditional("DEBUG")]
	private void PrintCompressionInfoKey()
	{
		try
		{
			List<Type> list = new List<Type>();
			Assembly[] array = (from assembly in AppDomain.CurrentDomain.GetAssemblies()
				where assembly.GetName().Name.StartsWith("TaleWorlds.")
				select assembly).ToArray();
			Assembly[] array2 = array;
			for (int num = 0; num < array2.Length; num++)
			{
				Type type = array2[num].GetTypesSafe().FirstOrDefault((Type ty) => ty.Name.Contains("CompressionInfo"));
				if (type != null)
				{
					list.AddRange(type.GetNestedTypes());
					break;
				}
			}
			List<FieldInfo> list2 = new List<FieldInfo>();
			array2 = array;
			for (int num = 0; num < array2.Length; num++)
			{
				foreach (Type item in array2[num].GetTypesSafe())
				{
					FieldInfo[] fields = item.GetFields();
					foreach (FieldInfo fieldInfo in fields)
					{
						if (list.Contains(fieldInfo.FieldType))
						{
							list2.Add(fieldInfo);
						}
					}
				}
			}
			int num3 = 0;
			foreach (FieldInfo item2 in list2)
			{
				object value = item2.GetValue(null);
				MethodInfo method = item2.FieldType.GetMethod("GetHashKey", BindingFlags.Instance | BindingFlags.NonPublic);
				num3 += (int)method.Invoke(value, new object[0]);
			}
			TaleWorlds.Library.Debug.Print("CompressionInfoKey: " + num3, 0, TaleWorlds.Library.Debug.DebugColor.Cyan, 17179869184uL);
		}
		catch
		{
			TaleWorlds.Library.Debug.Print("CompressionInfoKey checking failed.", 0, TaleWorlds.Library.Debug.DebugColor.Cyan, 17179869184uL);
		}
	}

	public async Task<bool> OnInviteToPlatformSession(PlayerId playerId)
	{
		if (!LobbyClient.Connected)
		{
			return false;
		}
		bool flag = false;
		if ((!LobbyClient.IsInParty || LobbyClient.IsPartyLeader) && LobbyClient.PlayersInParty.Count < Parameters.MaxPlayerCountInParty && PlatformServices.Instance.UsePlatformInvitationService(playerId))
		{
			flag = await PlatformServices.InvitationServices.OnInviteToPlatformSession(playerId);
		}
		if (!flag)
		{
			InformationManager.DisplayMessage(new InformationMessage(new TextObject("{=ljHPjjmX}Could not invite player to the game").ToString()));
		}
		return flag;
	}

	public async void OnPlatformRequestedMultiplayer()
	{
		PlatformServices.OnPlatformMultiplayerRequestHandled();
		await UpdateHasMultiplayerPrivilege();
		if (HasMultiplayerPrivilege.HasValue && HasMultiplayerPrivilege.Value && LobbyClient.IsIdle)
		{
			await TryLogin();
			int waitTime = 0;
			while (LobbyClient.CurrentState != LobbyClient.State.Idle && LobbyClient.CurrentState != LobbyClient.State.AtLobby && waitTime < 3000)
			{
				await Task.Delay(100);
				waitTime += 100;
			}
		}
	}

	public async void OnSessionInvitationAccepted(SessionInvitationType targetGameType)
	{
		if (targetGameType != SessionInvitationType.Multiplayer)
		{
			return;
		}
		PlatformServices.OnSessionInvitationHandled();
		await UpdateHasMultiplayerPrivilege();
		if (!HasMultiplayerPrivilege.HasValue || !HasMultiplayerPrivilege.Value)
		{
			return;
		}
		await Task.Delay(2000);
		if (LobbyClient.IsIdle)
		{
			await TryLogin();
			int waitTime = 0;
			while (LobbyClient.CurrentState != LobbyClient.State.Idle && LobbyClient.CurrentState != LobbyClient.State.AtLobby && waitTime < 3000)
			{
				await Task.Delay(100);
				waitTime += 100;
			}
		}
		_ = LobbyClient.CurrentState;
		_ = 4;
	}

	public List<CustomServerAction> GetCustomActionsForServer(GameServerEntry gameServerEntry)
	{
		List<CustomServerAction> list = new List<CustomServerAction>();
		for (int i = 0; i < _onCustomServerActionRequestedForServerEntry.Count; i++)
		{
			List<CustomServerAction> list2 = _onCustomServerActionRequestedForServerEntry[i](gameServerEntry);
			if (list2 != null && list2.Count > 0)
			{
				list.AddRange(list2);
			}
		}
		return list;
	}

	public void RegisterForCustomServerAction(Func<GameServerEntry, List<CustomServerAction>> action)
	{
		if (_onCustomServerActionRequestedForServerEntry != null)
		{
			_onCustomServerActionRequestedForServerEntry.Add(action);
		}
		else
		{
			TaleWorlds.Library.Debug.FailedAssert("Lobby state is finalized", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.Multiplayer\\LobbyState.cs", "RegisterForCustomServerAction", 1179);
		}
	}

	public void UnregisterForCustomServerAction(Func<GameServerEntry, List<CustomServerAction>> action)
	{
		if (_onCustomServerActionRequestedForServerEntry != null)
		{
			_onCustomServerActionRequestedForServerEntry.Remove(action);
		}
		else
		{
			TaleWorlds.Library.Debug.FailedAssert("Lobby state is finalized", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.Multiplayer\\LobbyState.cs", "UnregisterForCustomServerAction", 1191);
		}
	}
}
