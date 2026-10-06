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

public class TestPlatformServices : IPlatformServices
{
	private readonly string _userName;

	private readonly PlayerId _playerId;

	private TestLoginAccessProvider _loginAccessProvider;

	private TestFriendListService _testFriendListService;

	string IPlatformServices.ProviderName => "Test";

	string IPlatformServices.UserId => _playerId.ToString();

	PlayerId IPlatformServices.PlayerId => _playerId;

	string IPlatformServices.UserDisplayName => _userName;

	IReadOnlyCollection<PlayerId> IPlatformServices.BlockedUsers => new List<PlayerId>();

	bool IPlatformServices.IsPermanentMuteAvailable => true;

	bool IPlatformServices.UserLoggedIn => true;

	public event Action<AvatarData> OnAvatarUpdated;

	public event Action<string> OnNameUpdated;

	public event Action<bool, TextObject> OnSignInStateUpdated;

	public event Action OnBlockedUserListUpdated;

	public event Action<string> OnTextEnteredFromPlatform;

	public event Action OnTextCanceledFromPlatform;

	public TestPlatformServices(string userName)
	{
		_userName = userName;
		_loginAccessProvider = new TestLoginAccessProvider();
		ILoginAccessProvider loginAccessProvider = _loginAccessProvider;
		loginAccessProvider.Initialize(_userName, null);
		_playerId = loginAccessProvider.GetPlayerId();
		_testFriendListService = new TestFriendListService(userName, _playerId);
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

	void IPlatformServices.LoginUser()
	{
	}

	void IPlatformServices.ShowPlayerProfileCard(PlayerId providedId)
	{
	}

	Task<AvatarData> IPlatformServices.GetUserAvatar(PlayerId providedId)
	{
		return Task.FromResult<AvatarData>(null);
	}

	IFriendListService[] IPlatformServices.GetFriendListServices()
	{
		return new IFriendListService[1] { _testFriendListService };
	}

	IAchievementService IPlatformServices.GetAchievementService()
	{
		return new TestAchievementService();
	}

	IActivityService IPlatformServices.GetActivityService()
	{
		return new TestActivityService();
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
		if (this.OnSignInStateUpdated != null)
		{
			this.OnSignInStateUpdated(arg1: false, null);
		}
		if (this.OnBlockedUserListUpdated != null)
		{
			this.OnBlockedUserListUpdated();
		}
		if (this.OnTextEnteredFromPlatform != null)
		{
			this.OnTextEnteredFromPlatform(null);
		}
		if (this.OnTextCanceledFromPlatform != null)
		{
			this.OnTextCanceledFromPlatform();
		}
	}

	public void Tick(float dt)
	{
	}

	PlatformInitParams IPlatformServices.GetInitParams()
	{
		return new PlatformInitParams();
	}

	public void ActivateFriendList()
	{
	}

	Task<ILoginAccessProvider> IPlatformServices.CreateLobbyClientLoginProvider()
	{
		return Task.FromResult((ILoginAccessProvider)_loginAccessProvider);
	}

	void IPlatformServices.CheckPrivilege(Privilege privilege, bool displayResolveUI, PrivilegeResult callback)
	{
		callback(result: true);
	}

	void IPlatformServices.CheckPermissionWithUser(Permission privilege, PlayerId targetPlayerId, PermissionResult callback)
	{
		callback(result: true);
	}

	Task<bool> IPlatformServices.VerifyString(string content)
	{
		return Task.FromResult(result: true);
	}

	void IPlatformServices.GetPlatformId(PlayerId playerId, Action<object> callback)
	{
		callback(0);
	}

	void IPlatformServices.ShowRestrictedInformation()
	{
	}

	void IPlatformServices.OnFocusGained()
	{
	}

	bool IPlatformServices.RegisterPermissionChangeEvent(PlayerId targetPlayerId, Permission permission, PermissionChanged callback)
	{
		return true;
	}

	bool IPlatformServices.UnregisterPermissionChangeEvent(PlayerId targetPlayerId, Permission permission, PermissionChanged callback)
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
