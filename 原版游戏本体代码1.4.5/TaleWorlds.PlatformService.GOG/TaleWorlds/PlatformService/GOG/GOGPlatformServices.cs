using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using Galaxy.Api;
using TaleWorlds.AchievementSystem;
using TaleWorlds.ActivitySystem;
using TaleWorlds.Avatar.PlayerServices;
using TaleWorlds.Diamond;
using TaleWorlds.Diamond.AccessProvider.GOG;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.PlayerServices;
using TaleWorlds.PlayerServices.Avatar;

namespace TaleWorlds.PlatformService.GOG;

public class GOGPlatformServices : IPlatformServices
{
	private readonly struct AchievementData
	{
		public readonly string AchievementName;

		public readonly IReadOnlyList<(string StatName, int Threshold)> RequiredStats;

		public AchievementData(string achievementName, IReadOnlyList<(string StatName, int Threshold)> requiredStats)
		{
			AchievementName = achievementName;
			RequiredStats = requiredStats;
		}
	}

	private const string ClientID = "53550366963454221";

	private const string ClientSecret = "c17786edab4b6b3915ab55cfc5bb5a9a0a80b9a2d55d22c0767c9c18477efdb9";

	private PlatformInitParams _initParams;

	private GOGFriendListService _gogFriendListService;

	private IFriendListService[] _friendListServices;

	private bool _initialized;

	private DateTime? _statsLastInvalidated;

	private DateTime _statsLastStored = DateTime.MinValue;

	private UserStatsAndAchievementsRetrieveListener _achievementRetrieveListener;

	private StatsAndAchievementsStoreListener _statsAndAchievementsStoreListener;

	private List<AchievementData> _achievementDatas;

	private Dictionary<PlayerId, AvatarData> _avatarCache = new Dictionary<PlayerId, AvatarData>();

	private static GOGPlatformServices Instance => PlatformServices.Instance as GOGPlatformServices;

	string IPlatformServices.ProviderName => "GOG";

	string IPlatformServices.UserId => GalaxyInstance.User().GetGalaxyID().ToUint64()
		.ToString();

	PlayerId IPlatformServices.PlayerId => GalaxyInstance.User().GetGalaxyID().ToPlayerId();

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
			if (!_initialized)
			{
				return string.Empty;
			}
			return GalaxyInstance.Friends().GetPersonaName();
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

	public GOGPlatformServices(PlatformInitParams initParams)
	{
		LoadAchievementDataFromXml((string)initParams["AchievementDataXmlPath"]);
		_initParams = initParams;
		AvatarServices.AddAvatarService(PlayerIdProvidedTypes.GOG, new GOGPlatformAvatarService(this));
		_gogFriendListService = new GOGFriendListService(this);
	}

	void IPlatformServices.LoginUser()
	{
		throw new NotImplementedException();
	}

	IAchievementService IPlatformServices.GetAchievementService()
	{
		return new GOGAchievementService(this);
	}

	IActivityService IPlatformServices.GetActivityService()
	{
		return new TestActivityService();
	}

	Task<bool> IPlatformServices.VerifyString(string content)
	{
		return Task.FromResult(result: true);
	}

	void IPlatformServices.GetPlatformId(PlayerId playerId, Action<object> callback)
	{
		callback(new GalaxyID(playerId.Part4));
	}

	bool IPlatformServices.Initialize(IFriendListService[] additionalFriendListServices)
	{
		if (!_initialized)
		{
			_friendListServices = new IFriendListService[additionalFriendListServices.Length + 1];
			_friendListServices[0] = _gogFriendListService;
			for (int i = 0; i < additionalFriendListServices.Length; i++)
			{
				_friendListServices[i + 1] = additionalFriendListServices[i];
			}
			_initialized = false;
			InitParams initpParams = new InitParams("53550366963454221", "c17786edab4b6b3915ab55cfc5bb5a9a0a80b9a2d55d22c0767c9c18477efdb9");
			Debug.Print("Initializing GalaxyPeer instance...");
			try
			{
				GalaxyInstance.Init(initpParams);
				try
				{
					IUser user = GalaxyInstance.User();
					AuthenticationListener authenticationListener = new AuthenticationListener(this);
					user.SignInGalaxy(requireOnline: true, authenticationListener);
					while (!authenticationListener.GotResult)
					{
						GalaxyInstance.ProcessData();
						Thread.Sleep(5);
					}
					_gogFriendListService.RequestFriendList();
				}
				catch (GalaxyInstance.Error error)
				{
					Debug.Print("SignInGalaxy failed for reason " + error);
				}
				InitListeners();
				RequestUserStatsAndAchievements();
				_initialized = true;
			}
			catch (GalaxyInstance.Error error2)
			{
				Debug.Print("Galaxy Init failed for reason " + error2);
				_initialized = false;
			}
		}
		return _initialized;
	}

	void IPlatformServices.Tick(float dt)
	{
		GalaxyInstance.ProcessData();
		if (_initialized)
		{
			CheckStoreStats();
		}
	}

	void IPlatformServices.Terminate()
	{
		GalaxyInstance.Shutdown(unloadModule: true);
	}

	private void InvalidateStats()
	{
		_statsLastInvalidated = DateTime.Now;
	}

	private void CheckStoreStats()
	{
		if (_statsLastInvalidated.HasValue && DateTime.Now.Subtract(_statsLastInvalidated.Value).TotalSeconds > 5.0 && DateTime.Now.Subtract(_statsLastStored).TotalSeconds > 30.0)
		{
			_statsLastStored = DateTime.Now;
			GalaxyInstance.Stats().StoreStatsAndAchievements();
			_statsLastInvalidated = null;
		}
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

	bool IPlatformServices.IsPlayerProfileCardAvailable(PlayerId providedId)
	{
		return false;
	}

	void IPlatformServices.ShowPlayerProfileCard(PlayerId providedId)
	{
	}

	internal void ClearAvatarCache()
	{
		_avatarCache.Clear();
	}

	async Task<AvatarData> IPlatformServices.GetUserAvatar(PlayerId providedId)
	{
		if (providedId.ProvidedType == PlayerIdProvidedTypes.GOGReal)
		{
			GalaxyID galaxyID = new GalaxyID(providedId.Part4);
			if (_avatarCache.ContainsKey(providedId))
			{
				return _avatarCache[providedId];
			}
			UserInformationRetrieveListener listener = new UserInformationRetrieveListener();
			GalaxyInstance.Friends().RequestUserInformation(galaxyID, 4u, listener);
			while (!listener.GotResult)
			{
				await Task.Delay(5);
			}
			uint num = 184u;
			uint num2 = 184u;
			uint num3 = 4 * num * num2;
			byte[] array = new byte[num3];
			GalaxyInstance.Friends().GetFriendAvatarImageRGBA(galaxyID, AvatarType.AVATAR_TYPE_LARGE, array, num3);
			AvatarData avatarData = new AvatarData(array, num, num2);
			lock (_avatarCache)
			{
				if (!_avatarCache.ContainsKey(providedId))
				{
					_avatarCache.Add(providedId, avatarData);
				}
			}
			return avatarData;
		}
		return null;
	}

	PlatformInitParams IPlatformServices.GetInitParams()
	{
		return _initParams;
	}

	internal Task<PlayerId> GetUserWithName(string name)
	{
		return Task.FromResult(PlayerId.Empty);
	}

	Task<bool> IPlatformServices.ShowOverlayForWebPage(string url)
	{
		bool flag = false;
		Debug.Print("Opening overlay with web page " + url);
		try
		{
			GalaxyInstance.Utils().ShowOverlayWithWebPage(url);
			Debug.Print("Opened overlay with web page " + url);
			flag = true;
		}
		catch (GalaxyInstance.Error error)
		{
			Debug.Print("Could not open overlay with web page " + url + " for reason " + error);
			flag = false;
		}
		return Task.FromResult(flag);
	}

	Task<ILoginAccessProvider> IPlatformServices.CreateLobbyClientLoginProvider()
	{
		return Task.FromResult((ILoginAccessProvider)new GOGLoginAccessProvider());
	}

	IFriendListService[] IPlatformServices.GetFriendListServices()
	{
		return _friendListServices;
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

	void IPlatformServices.OnFocusGained()
	{
	}

	private void LoadAchievementDataFromXml(string xmlPath)
	{
		_achievementDatas = new List<AchievementData>();
		XmlDocument xmlDocument = new XmlDocument();
		xmlDocument.Load(xmlPath);
		XmlNodeList elementsByTagName = xmlDocument.GetElementsByTagName("Achievement");
		for (int i = 0; i < elementsByTagName.Count; i++)
		{
			XmlNode xmlNode = elementsByTagName[i];
			string innerText = xmlNode.Attributes.GetNamedItem("name").InnerText;
			List<(string, int)> list = new List<(string, int)>();
			XmlNodeList xmlNodeList = null;
			for (int j = 0; j < xmlNode.ChildNodes.Count; j++)
			{
				if (xmlNode.ChildNodes[j].Name == "Requirements")
				{
					xmlNodeList = xmlNode.ChildNodes[j].ChildNodes;
					break;
				}
			}
			for (int k = 0; k < xmlNodeList.Count; k++)
			{
				XmlNode? xmlNode2 = xmlNodeList[k];
				string value = xmlNode2.Attributes["statName"].Value;
				int item = int.Parse(xmlNode2.Attributes["threshold"].Value);
				list.Add((value, item));
			}
			_achievementDatas.Add(new AchievementData(innerText, list));
		}
	}

	internal async Task<string> GetUserName(PlayerId providedId)
	{
		if (!providedId.IsValid || providedId.ProvidedType != PlayerIdProvidedTypes.GOG)
		{
			return null;
		}
		GalaxyID gogId = providedId.ToGOGID();
		IFriends friends = GalaxyInstance.Friends();
		UserInformationRetrieveListener informationRetriever = new UserInformationRetrieveListener();
		friends.RequestUserInformation(gogId, 0u, informationRetriever);
		while (!informationRetriever.GotResult)
		{
			await Task.Delay(5);
		}
		string friendPersonaName = friends.GetFriendPersonaName(gogId);
		if (!string.IsNullOrEmpty(friendPersonaName))
		{
			return friendPersonaName;
		}
		return null;
	}

	internal bool SetStat(string name, int value)
	{
		try
		{
			Debug.Print("trying to set stat:" + name + " to value:" + value);
			GalaxyInstance.Stats().SetStatInt(name, value);
			for (int i = 0; i < _achievementDatas.Count; i++)
			{
				AchievementData achievementData = _achievementDatas[i];
				foreach (var requiredStat in achievementData.RequiredStats)
				{
					if (requiredStat.StatName == name)
					{
						if (value >= requiredStat.Threshold)
						{
							CheckStatsAndUnlockAchievement(in achievementData);
						}
						break;
					}
				}
			}
			InvalidateStats();
			return true;
		}
		catch (Exception ex)
		{
			Debug.Print("Could not set stat: " + ex.Message);
			return false;
		}
	}

	internal Task<int> GetStat(string name)
	{
		int result = -1;
		try
		{
			result = GalaxyInstance.Stats().GetStatInt(name);
		}
		catch (Exception ex)
		{
			Debug.Print("Could not get stat: " + ex.Message);
		}
		return Task.FromResult(result);
	}

	internal Task<int[]> GetStats(string[] names)
	{
		List<int> list = new List<int>();
		foreach (string name in names)
		{
			list.Add(GetStat(name).Result);
		}
		return Task.FromResult(list.ToArray());
	}

	private void RequestUserStatsAndAchievements()
	{
		try
		{
			GalaxyInstance.Stats().RequestUserStatsAndAchievements();
		}
		catch (GalaxyInstance.Error error)
		{
			Debug.Print("Achievements definitions could not be retrieved: " + error);
		}
	}

	private GOGAchievement GetGOGAchievement(string name, GalaxyID galaxyID)
	{
		bool unlocked = false;
		uint unlockTime = 0u;
		GalaxyInstance.Stats().GetAchievement(name, ref unlocked, ref unlockTime, GalaxyInstance.User().GetGalaxyID());
		return new GOGAchievement
		{
			AchievementName = name,
			Name = GalaxyInstance.Stats().GetAchievementDisplayName(name),
			Description = GalaxyInstance.Stats().GetAchievementDescription(name),
			Achieved = unlocked
		};
	}

	private void CheckStatsAndUnlockAchievements()
	{
		for (int i = 0; i < _achievementDatas.Count; i++)
		{
			CheckStatsAndUnlockAchievement(_achievementDatas[i]);
		}
	}

	private void CheckStatsAndUnlockAchievement(in AchievementData achievementData)
	{
		if (GetGOGAchievement(achievementData.AchievementName, GalaxyInstance.User().GetGalaxyID()).Achieved)
		{
			return;
		}
		bool flag = true;
		foreach (var requiredStat in achievementData.RequiredStats)
		{
			if (GalaxyInstance.Stats().GetStatInt(requiredStat.StatName) < requiredStat.Threshold)
			{
				flag = false;
				break;
			}
		}
		if (flag)
		{
			Debug.Print("trying to set achievement:" + achievementData.AchievementName);
			GalaxyInstance.Stats().SetAchievement(achievementData.AchievementName);
		}
	}

	bool IPlatformServices.UsePlatformInvitationService(PlayerId targetPlayerId)
	{
		return false;
	}

	private void InitListeners()
	{
		_achievementRetrieveListener = new UserStatsAndAchievementsRetrieveListener();
		_achievementRetrieveListener.OnUserStatsAndAchievementsRetrieved += OnUserStatsAndAchievementsRetrieved;
		_statsAndAchievementsStoreListener = new StatsAndAchievementsStoreListener();
		_statsAndAchievementsStoreListener.OnUserStatsAndAchievementsStored += OnUserStatsAndAchievementsStored;
	}

	private void OnUserStatsAndAchievementsStored(bool success, IStatsAndAchievementsStoreListener.FailureReason? failureReason)
	{
		if (!success)
		{
			Debug.Print("Failed to store user stats and achievements: " + failureReason.ToString());
		}
	}

	private void OnUserStatsAndAchievementsRetrieved(GalaxyID userID, bool success, IUserStatsAndAchievementsRetrieveListener.FailureReason? failureReason)
	{
		if (success)
		{
			CheckStatsAndUnlockAchievements();
		}
		else
		{
			Debug.Print("Failed to receive user stats and achievements: " + failureReason.ToString());
		}
	}

	bool IPlatformServices.ShowGamepadTextInput(string descriptionText, string existingText, uint maxLine, bool isObfuscated)
	{
		return false;
	}
}
