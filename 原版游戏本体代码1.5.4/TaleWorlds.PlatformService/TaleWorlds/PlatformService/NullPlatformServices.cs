using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TaleWorlds.AchievementSystem;
using TaleWorlds.ActivitySystem;
using TaleWorlds.Diamond;
using TaleWorlds.Diamond.AccessProvider.Test;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.PlayerServices;
using TaleWorlds.PlayerServices.Avatar;

namespace TaleWorlds.PlatformService;

public class NullPlatformServices : IPlatformServices
{
	private TestFriendListService _testFriendListService;

	string IPlatformServices.ProviderName => "Null";

	string IPlatformServices.UserId => "";

	PlayerId IPlatformServices.PlayerId => PlayerId.Empty;

	string IPlatformServices.UserDisplayName => "";

	IReadOnlyCollection<PlayerId> IPlatformServices.BlockedUsers => new List<PlayerId>();

	bool IPlatformServices.IsPermanentMuteAvailable => true;

	bool IPlatformServices.UserLoggedIn => false;

	public event Action<PlayerId> OnUserStatusChanged;

	public event Action<AvatarData> OnAvatarUpdated;

	public event Action OnBlockedUserListUpdated;

	public event Action<string> OnNameUpdated;

	public event Action<bool, TextObject> OnSignInStateUpdated;

	public event Action<string> OnTextEnteredFromPlatform;

	public event Action OnTextCanceledFromPlatform;

	public NullPlatformServices()
	{
		_testFriendListService = new TestFriendListService("NULL", default(PlayerId));
	}

	bool IPlatformServices.Initialize(IFriendListService[] additionalFriendListServices)
	{
		return false;
	}

	void IPlatformServices.Terminate()
	{
	}

	bool IPlatformServices.IsPlayerProfileCardAvailable(PlayerId providedId)
	{
		return false;
	}

	void IPlatformServices.ShowPlayerProfileCard(PlayerId providedId)
	{
	}

	void IPlatformServices.LoginUser()
	{
	}

	Task<AvatarData> IPlatformServices.GetUserAvatar(PlayerId providedId)
	{
		return Task.FromResult<AvatarData>(null);
	}

	Task<bool> IPlatformServices.ShowOverlayForWebPage(string url)
	{
		return Task.FromResult(result: false);
	}

	private void Dummy()
	{
		if (this.OnAvatarUpdated != null)
		{
			this.OnAvatarUpdated(null);
		}
		if (this.OnNameUpdated != null)
		{
			this.OnNameUpdated(null);
		}
		if (this.OnUserStatusChanged != null)
		{
			this.OnUserStatusChanged(default(PlayerId));
		}
		if (this.OnSignInStateUpdated != null)
		{
			this.OnSignInStateUpdated(arg1: false, null);
		}
		if (this.OnTextEnteredFromPlatform != null)
		{
			this.OnTextEnteredFromPlatform(null);
		}
		if (this.OnTextCanceledFromPlatform != null)
		{
			this.OnTextCanceledFromPlatform();
		}
		if (this.OnBlockedUserListUpdated != null)
		{
			this.OnBlockedUserListUpdated();
		}
	}

	public void Tick(float dt)
	{
	}

	PlatformInitParams IPlatformServices.GetInitParams()
	{
		return new PlatformInitParams();
	}

	IFriendListService[] IPlatformServices.GetFriendListServices()
	{
		return new IFriendListService[1] { _testFriendListService };
	}

	public void ActivateFriendList()
	{
	}

	Task<ILoginAccessProvider> IPlatformServices.CreateLobbyClientLoginProvider()
	{
		return Task.FromResult((ILoginAccessProvider)new TestLoginAccessProvider());
	}

	IAchievementService IPlatformServices.GetAchievementService()
	{
		return new TestAchievementService();
	}

	IActivityService IPlatformServices.GetActivityService()
	{
		return new TestActivityService();
	}

	void IPlatformServices.CheckPrivilege(Privilege privilege, bool displayResolveUI, PrivilegeResult callback)
	{
		callback(result: true);
	}

	void IPlatformServices.CheckPermissionWithUser(Permission privilege, PlayerId targetPlayerId, PermissionResult callback)
	{
		callback(result: true);
	}

	void IPlatformServices.GetPlatformId(PlayerId playerId, Action<object> callback)
	{
		callback(-1);
	}

	Task<bool> IPlatformServices.VerifyString(string content)
	{
		return Task.FromResult(result: true);
	}

	void IPlatformServices.ShowRestrictedInformation()
	{
	}

	void IPlatformServices.OnFocusGained()
	{
	}

	bool IPlatformServices.RegisterPermissionChangeEvent(PlayerId targetPlayerId, Permission permission, PermissionChanged Callback)
	{
		return true;
	}

	bool IPlatformServices.UnregisterPermissionChangeEvent(PlayerId targetPlayerId, Permission permission, PermissionChanged Callback)
	{
		return true;
	}

	bool IPlatformServices.ShowGamepadTextInput(string descriptionText, string existingText, uint maxLine, bool isObfuscated)
	{
		return false;
	}

	bool IPlatformServices.UsePlatformInvitationService(PlayerId targetPlayerId)
	{
		return false;
	}
}
