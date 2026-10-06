using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Steamworks;
using TaleWorlds.AchievementSystem;
using TaleWorlds.ActivitySystem;
using TaleWorlds.Avatar.PlayerServices;
using TaleWorlds.Diamond;
using TaleWorlds.Diamond.AccessProvider.Steam;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ModuleManager;
using TaleWorlds.PlayerServices;
using TaleWorlds.PlayerServices.Avatar;

namespace TaleWorlds.PlatformService.Steam;

public class SteamPlatformServices : IPlatformServices
{
	private PlatformInitParams _initParams;

	private SteamFriendListService _steamFriendListService;

	private IFriendListService[] _friendListServices;

	public SteamAchievementService _achievementService;

	private Dictionary<PlayerId, AvatarData> _avatarCache = new Dictionary<PlayerId, AvatarData>();

	private const int CommandRequestTimeOut = 5000;

	private Callback<PersonaStateChange_t> _personaStateChangeT;

	private Callback<AvatarImageLoaded_t> _avatarImageLoadedT;

	private Callback<GamepadTextInputDismissed_t> _gamepadTextInputDismissedT;

	private static List<CSteamID> _avatarUpdates = new List<CSteamID>();

	private static List<CSteamID> _avatarLoadedUpdates = new List<CSteamID>();

	private static List<CSteamID> _nameUpdates = new List<CSteamID>();

	private static SteamPlatformServices Instance => PlatformServices.Instance as SteamPlatformServices;

	internal bool Initialized { get; private set; }

	string IPlatformServices.ProviderName => "Steam";

	string IPlatformServices.UserId => ((ulong)SteamUser.GetSteamID()).ToString();

	PlayerId IPlatformServices.PlayerId => SteamUser.GetSteamID().ToPlayerId();

	bool IPlatformServices.UserLoggedIn
	{
		get
		{
			throw new NotImplementedException();
		}
	}

	string IPlatformServices.UserDisplayName
	{
		get
		{
			if (!Initialized)
			{
				return string.Empty;
			}
			return SteamFriends.GetPersonaName();
		}
	}

	IReadOnlyCollection<PlayerId> IPlatformServices.BlockedUsers => new List<PlayerId>();

	bool IPlatformServices.IsPermanentMuteAvailable => true;

	public event Action<AvatarData> OnAvatarUpdated;

	public event Action<string> OnNameUpdated;

	public event Action<bool, TextObject> OnSignInStateUpdated;

	public event Action OnBlockedUserListUpdated;

	public event Action<string> OnTextEnteredFromPlatform;

	public event Action OnTextCanceledFromPlatform;

	public SteamPlatformServices(PlatformInitParams initParams)
	{
		_initParams = initParams;
		AvatarServices.AddAvatarService(PlayerIdProvidedTypes.Steam, new SteamPlatformAvatarService(this));
		_achievementService = new SteamAchievementService(this);
		_steamFriendListService = new SteamFriendListService(this);
	}

	void IPlatformServices.LoginUser()
	{
		throw new NotImplementedException();
	}

	bool IPlatformServices.Initialize(IFriendListService[] additionalFriendListServices)
	{
		_friendListServices = new IFriendListService[additionalFriendListServices.Length + 1];
		_friendListServices[0] = _steamFriendListService;
		for (int i = 0; i < additionalFriendListServices.Length; i++)
		{
			_friendListServices[i + 1] = additionalFriendListServices[i];
		}
		if (!SteamAPI.Init())
		{
			return false;
		}
		ModuleHelper.InitializePlatformModuleExtension(new SteamModuleExtension(), null);
		InitCallbacks();
		_achievementService.Initialize();
		SteamUserStats.RequestCurrentStats();
		Initialized = true;
		return true;
	}

	void IPlatformServices.Tick(float dt)
	{
		if (Initialized)
		{
			SteamAPI.RunCallbacks();
			_achievementService.Tick(dt);
		}
	}

	void IPlatformServices.Terminate()
	{
		SteamAPI.Shutdown();
	}

	bool IPlatformServices.ShowGamepadTextInput(string descriptionText, string existingText, uint maxChars, bool isObfuscated)
	{
		if (Initialized)
		{
			return SteamUtils.ShowGamepadTextInput(isObfuscated ? EGamepadTextInputMode.k_EGamepadTextInputModePassword : EGamepadTextInputMode.k_EGamepadTextInputModeNormal, EGamepadTextInputLineMode.k_EGamepadTextInputLineModeSingleLine, descriptionText, maxChars, existingText);
		}
		return false;
	}

	bool IPlatformServices.IsPlayerProfileCardAvailable(PlayerId providedId)
	{
		return false;
	}

	void IPlatformServices.ShowPlayerProfileCard(PlayerId providedId)
	{
		SteamFriends.ActivateGameOverlayToUser("steamid", providedId.ToSteamId());
	}

	async Task<AvatarData> IPlatformServices.GetUserAvatar(PlayerId providedId)
	{
		if (!providedId.IsValid)
		{
			return null;
		}
		if (_avatarCache.ContainsKey(providedId))
		{
			return _avatarCache[providedId];
		}
		if (_avatarCache.Count > 300)
		{
			_avatarCache.Clear();
		}
		long startTime = DateTime.UtcNow.Ticks;
		CSteamID steamId = providedId.ToSteamId();
		if (SteamFriends.RequestUserInformation(steamId, bRequireNameOnly: false))
		{
			while (!_avatarUpdates.Contains(steamId) && !TimedOut(startTime, 5000L))
			{
				await Task.Delay(5);
			}
			_avatarUpdates.Remove(steamId);
		}
		int userAvatar = SteamFriends.GetLargeFriendAvatar(steamId);
		if (userAvatar == -1)
		{
			while (!_avatarLoadedUpdates.Contains(steamId) && !TimedOut(startTime, 5000L))
			{
				await Task.Delay(5);
			}
			_avatarLoadedUpdates.Remove(steamId);
			while (userAvatar == -1 && !TimedOut(startTime, 5000L))
			{
				userAvatar = SteamFriends.GetLargeFriendAvatar(steamId);
			}
		}
		if (userAvatar != -1)
		{
			SteamUtils.GetImageSize(userAvatar, out var pnWidth, out var pnHeight);
			if (pnWidth != 0)
			{
				uint num = pnWidth * pnHeight * 4;
				byte[] array = new byte[num];
				if (SteamUtils.GetImageRGBA(userAvatar, array, (int)num))
				{
					AvatarData avatarData = new AvatarData(array, pnWidth, pnHeight);
					lock (_avatarCache)
					{
						if (!_avatarCache.ContainsKey(providedId))
						{
							_avatarCache.Add(providedId, avatarData);
						}
					}
					return avatarData;
				}
			}
		}
		return null;
	}

	public void ClearAvatarCache()
	{
		_avatarCache.Clear();
	}

	private bool TimedOut(long startUTCTicks, long timeOut)
	{
		return (DateTime.Now - new DateTime(startUTCTicks)).Milliseconds > timeOut;
	}

	internal async Task<string> GetUserName(PlayerId providedId)
	{
		if (!providedId.IsValid || providedId.ProvidedType != PlayerIdProvidedTypes.Steam)
		{
			return null;
		}
		long startTime = DateTime.UtcNow.Ticks;
		CSteamID steamId = providedId.ToSteamId();
		if (SteamFriends.RequestUserInformation(steamId, bRequireNameOnly: false))
		{
			while (!_nameUpdates.Contains(steamId) && !TimedOut(startTime, 5000L))
			{
				await Task.Delay(5);
			}
			_nameUpdates.Remove(steamId);
		}
		string friendPersonaName = SteamFriends.GetFriendPersonaName(steamId);
		if (!string.IsNullOrEmpty(friendPersonaName))
		{
			return friendPersonaName;
		}
		return null;
	}

	PlatformInitParams IPlatformServices.GetInitParams()
	{
		return _initParams;
	}

	IAchievementService IPlatformServices.GetAchievementService()
	{
		return _achievementService;
	}

	IActivityService IPlatformServices.GetActivityService()
	{
		return new TestActivityService();
	}

	async Task<bool> IPlatformServices.ShowOverlayForWebPage(string url)
	{
		await Task.Delay(0);
		SteamFriends.ActivateGameOverlayToWebPage(url);
		return true;
	}

	void IPlatformServices.CheckPrivilege(Privilege privilege, bool displayResolveUI, PrivilegeResult callback)
	{
		callback(result: true);
	}

	void IPlatformServices.CheckPermissionWithUser(Permission privilege, PlayerId targetPlayerId, PermissionResult callback)
	{
		callback(result: true);
	}

	bool IPlatformServices.RegisterPermissionChangeEvent(PlayerId targetPlayerId, Permission permission, PermissionChanged callback)
	{
		return false;
	}

	bool IPlatformServices.UnregisterPermissionChangeEvent(PlayerId targetPlayerId, Permission permission, PermissionChanged callback)
	{
		return false;
	}

	void IPlatformServices.ShowRestrictedInformation()
	{
	}

	Task<bool> IPlatformServices.VerifyString(string content)
	{
		return Task.FromResult(result: true);
	}

	void IPlatformServices.GetPlatformId(PlayerId playerId, Action<object> callback)
	{
		callback(playerId.ToSteamId());
	}

	void IPlatformServices.OnFocusGained()
	{
	}

	internal Task<bool> GetUserOnlineStatus(PlayerId providedId)
	{
		SteamUtils.GetAppID();
		if (SteamFriends.GetFriendPersonaState(new CSteamID(providedId.Part4)) != EPersonaState.k_EPersonaStateOffline)
		{
			return Task.FromResult(result: true);
		}
		return Task.FromResult(result: false);
	}

	internal Task<bool> IsPlayingThisGame(PlayerId providedId)
	{
		AppId_t appID = SteamUtils.GetAppID();
		if (SteamFriends.GetFriendGamePlayed(new CSteamID(providedId.Part4), out var pFriendGameInfo) && pFriendGameInfo.m_gameID.AppID() == appID)
		{
			return Task.FromResult(result: true);
		}
		return Task.FromResult(result: false);
	}

	internal async Task<PlayerId> GetUserWithName(string name)
	{
		await Task.Delay(0);
		int friendCount = SteamFriends.GetFriendCount(EFriendFlags.k_EFriendFlagImmediate);
		CSteamID steamId = default(CSteamID);
		int num = 0;
		for (int i = 0; i < friendCount; i++)
		{
			CSteamID friendByIndex = SteamFriends.GetFriendByIndex(i, EFriendFlags.k_EFriendFlagImmediate);
			if (SteamFriends.GetFriendPersonaName(friendByIndex).Equals(name))
			{
				steamId = friendByIndex;
				num++;
			}
		}
		friendCount = SteamFriends.GetCoplayFriendCount();
		for (int j = 0; j < friendCount; j++)
		{
			CSteamID coplayFriend = SteamFriends.GetCoplayFriend(j);
			if (SteamFriends.GetFriendPersonaName(coplayFriend).Equals(name))
			{
				steamId = coplayFriend;
				num++;
			}
		}
		if (num != 1)
		{
			return default(PlayerId);
		}
		return steamId.ToPlayerId();
	}

	private async void OnAvatarUpdateReceived(ulong userId)
	{
		int userAvatar = -1;
		while (userAvatar == -1)
		{
			userAvatar = SteamFriends.GetLargeFriendAvatar(new CSteamID(userId));
			await Task.Delay(5);
		}
		if (userAvatar == -1)
		{
			return;
		}
		SteamUtils.GetImageSize(userAvatar, out var pnWidth, out var pnHeight);
		if (pnWidth != 0)
		{
			uint num = pnWidth * pnHeight * 4;
			byte[] array = new byte[num];
			if (SteamUtils.GetImageRGBA(userAvatar, array, (int)num))
			{
				this.OnAvatarUpdated?.Invoke(new AvatarData(array, pnWidth, pnHeight));
			}
		}
	}

	private void OnNameUpdateReceived(PlayerId userId)
	{
		string friendPersonaName = SteamFriends.GetFriendPersonaName(userId.ToSteamId());
		if (!string.IsNullOrEmpty(friendPersonaName))
		{
			this.OnNameUpdated?.Invoke(friendPersonaName);
		}
	}

	private void Dummy()
	{
		if (this.OnSignInStateUpdated != null)
		{
			this.OnSignInStateUpdated(arg1: false, null);
		}
		if (this.OnBlockedUserListUpdated != null)
		{
			this.OnBlockedUserListUpdated();
		}
	}

	private void InitCallbacks()
	{
		_personaStateChangeT = Callback<PersonaStateChange_t>.Create(UserInformationUpdated);
		_avatarImageLoadedT = Callback<AvatarImageLoaded_t>.Create(AvatarLoaded);
		_gamepadTextInputDismissedT = Callback<GamepadTextInputDismissed_t>.Create(GamepadTextInputDismissed);
	}

	private static void AvatarLoaded(AvatarImageLoaded_t avatarImageLoadedT)
	{
		_avatarLoadedUpdates.Add(avatarImageLoadedT.m_steamID);
	}

	private static void UserInformationUpdated(PersonaStateChange_t pCallback)
	{
		if ((pCallback.m_nChangeFlags & EPersonaChange.k_EPersonaChangeAvatar) != 0)
		{
			_avatarUpdates.Add(new CSteamID(pCallback.m_ulSteamID));
			Instance.OnAvatarUpdateReceived(pCallback.m_ulSteamID);
		}
		else if ((pCallback.m_nChangeFlags & EPersonaChange.k_EPersonaChangeName) != 0)
		{
			_nameUpdates.Add(new CSteamID(pCallback.m_ulSteamID));
			Instance.OnNameUpdateReceived(new CSteamID(pCallback.m_ulSteamID).ToPlayerId());
		}
		else if ((pCallback.m_nChangeFlags & EPersonaChange.k_EPersonaChangeGamePlayed) != 0)
		{
			HandleOnUserStatusChanged(new CSteamID(pCallback.m_ulSteamID).ToPlayerId());
		}
	}

	private void GamepadTextInputDismissed(GamepadTextInputDismissed_t gamepadTextInputDismissedT)
	{
		if (gamepadTextInputDismissedT.m_bSubmitted)
		{
			SteamUtils.GetEnteredGamepadTextInput(out var pchText, SteamUtils.GetEnteredGamepadTextLength());
			this.OnTextEnteredFromPlatform?.Invoke(pchText);
		}
		else
		{
			this.OnTextCanceledFromPlatform?.Invoke();
		}
	}

	private static void HandleOnUserStatusChanged(PlayerId playerId)
	{
		Instance._steamFriendListService.HandleOnUserStatusChanged(playerId);
	}

	Task<ILoginAccessProvider> IPlatformServices.CreateLobbyClientLoginProvider()
	{
		return Task.FromResult((ILoginAccessProvider)new SteamLoginAccessProvider());
	}

	IFriendListService[] IPlatformServices.GetFriendListServices()
	{
		return _friendListServices;
	}

	bool IPlatformServices.UsePlatformInvitationService(PlayerId targetPlayerId)
	{
		return false;
	}
}
