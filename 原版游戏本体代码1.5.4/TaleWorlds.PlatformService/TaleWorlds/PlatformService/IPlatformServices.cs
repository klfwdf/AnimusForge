using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TaleWorlds.AchievementSystem;
using TaleWorlds.ActivitySystem;
using TaleWorlds.Diamond;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.PlayerServices;
using TaleWorlds.PlayerServices.Avatar;

namespace TaleWorlds.PlatformService;

public interface IPlatformServices
{
	string ProviderName { get; }

	string UserId { get; }

	PlayerId PlayerId { get; }

	string UserDisplayName { get; }

	bool UserLoggedIn { get; }

	bool IsPermanentMuteAvailable { get; }

	IReadOnlyCollection<PlayerId> BlockedUsers { get; }

	event Action<AvatarData> OnAvatarUpdated;

	event Action<string> OnNameUpdated;

	event Action<bool, TextObject> OnSignInStateUpdated;

	event Action<string> OnTextEnteredFromPlatform;

	event Action OnTextCanceledFromPlatform;

	event Action OnBlockedUserListUpdated;

	void LoginUser();

	bool Initialize(IFriendListService[] additionalFriendListServices);

	PlatformInitParams GetInitParams();

	void Terminate();

	void Tick(float dt);

	Task<AvatarData> GetUserAvatar(PlayerId providedId);

	Task<bool> ShowOverlayForWebPage(string url);

	Task<ILoginAccessProvider> CreateLobbyClientLoginProvider();

	IFriendListService[] GetFriendListServices();

	IAchievementService GetAchievementService();

	IActivityService GetActivityService();

	void CheckPrivilege(Privilege privilege, bool displayResolveUI, PrivilegeResult callback);

	void CheckPermissionWithUser(Permission permission, PlayerId targetPlayerId, PermissionResult callback);

	bool IsPlayerProfileCardAvailable(PlayerId providedId);

	void ShowPlayerProfileCard(PlayerId providedId);

	bool ShowGamepadTextInput(string descriptionText, string existingText, uint maxChars, bool isObfuscated);

	void GetPlatformId(PlayerId playerId, Action<object> callback);

	void OnFocusGained();

	void ShowRestrictedInformation();

	Task<bool> VerifyString(string content);

	bool RegisterPermissionChangeEvent(PlayerId targetPlayerId, Permission permission, PermissionChanged callback);

	bool UnregisterPermissionChangeEvent(PlayerId targetPlayerId, Permission permission, PermissionChanged callback);

	bool UsePlatformInvitationService(PlayerId targetPlayerId);
}
