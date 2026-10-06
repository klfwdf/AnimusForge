using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Epic.OnlineServices;
using Epic.OnlineServices.Achievements;
using Epic.OnlineServices.Auth;
using Epic.OnlineServices.Connect;
using Epic.OnlineServices.Friends;
using Epic.OnlineServices.Platform;
using Epic.OnlineServices.Presence;
using Epic.OnlineServices.Stats;
using Epic.OnlineServices.UserInfo;
using Newtonsoft.Json;
using TaleWorlds.AchievementSystem;
using TaleWorlds.ActivitySystem;
using TaleWorlds.Avatar.PlayerServices;
using TaleWorlds.Diamond;
using TaleWorlds.Diamond.AccessProvider.Epic;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.PlayerServices;
using TaleWorlds.PlayerServices.Avatar;

namespace TaleWorlds.PlatformService.Epic;

public class EpicPlatformServices : IPlatformServices
{
	private class IngestStatsQueueItem
	{
		public string Name { get; set; }

		public int Value { get; set; }
	}

	private class EpicAuthErrorResponse
	{
		[JsonProperty("errorCode")]
		public string ErrorCode { get; set; }

		[JsonProperty("errorMessage")]
		public string ErrorMessage { get; set; }

		[JsonProperty("numericErrorCode")]
		public int NumericErrorCode { get; set; }

		[JsonProperty("error_description")]
		public string ErrorDescription { get; set; }

		[JsonProperty("error")]
		public string Error { get; set; }
	}

	private class EpicAuthResponse
	{
		[JsonProperty("access_token")]
		public string AccessToken { get; set; }

		[JsonProperty("expires_in")]
		public int ExpiresIn { get; set; }

		[JsonProperty("expires_at")]
		public DateTime ExpiresAt { get; set; }

		[JsonProperty("token_type")]
		public string TokenType { get; set; }

		[JsonProperty("refresh_token")]
		public string RefreshToken { get; set; }

		[JsonProperty("refresh_expires")]
		public int RefreshExpires { get; set; }

		[JsonProperty("refresh_expires_at")]
		public DateTime RefreshExpiresAt { get; set; }

		[JsonProperty("account_id")]
		public string AccountId { get; set; }

		[JsonProperty("client_id")]
		public string ClientId { get; set; }

		[JsonProperty("internal_client")]
		public bool InternalClient { get; set; }

		[JsonProperty("client_service")]
		public string ClientService { get; set; }

		[JsonProperty("displayName")]
		public string DisplayName { get; set; }

		[JsonProperty("app")]
		public string App { get; set; }

		[JsonProperty("in_app_id")]
		public string InAppId { get; set; }

		[JsonProperty("device_id")]
		public string DeviceId { get; set; }

		[JsonProperty("product_id")]
		public string ProductId { get; set; }
	}

	[Serializable]
	[CompilerGenerated]
	private sealed class _003C_003Ec
	{
		public static readonly _003C_003Ec _003C_003E9 = new _003C_003Ec();

		public static OnQueryDefinitionsCompleteCallback _003C_003E9__73_0;

		public static OnQueryStatsCompleteCallback _003C_003E9__87_0;

		internal void _003CQueryDefinitions_003Eb__73_0(ref OnQueryDefinitionsCompleteCallbackInfo data)
		{
		}

		internal void _003CQueryStats_003Eb__87_0(ref OnQueryStatsCompleteCallbackInfo data)
		{
		}
	}

	private EpicAccountId _epicAccountId;

	private ProductUserId _localUserId;

	private string _accessToken;

	private string _epicUserName;

	private PlatformInterface _platform;

	private PlatformInitParams _initParams;

	private EpicFriendListService _epicFriendListService;

	private IFriendListService[] _friendListServices;

	private TextObject _initFailReason;

	private ulong _refreshConnectionCallbackId;

	private ConcurrentBag<IngestStatsQueueItem> _ingestStatsQueue = new ConcurrentBag<IngestStatsQueueItem>();

	private bool _writingStats;

	private DateTime _statsLastWrittenOn = DateTime.MinValue;

	private const int MinStatsWriteInterval = 5;

	public string UserId
	{
		get
		{
			if ((Handle)(object)_epicAccountId == (Handle)null)
			{
				return "";
			}
			return ((object)_epicAccountId).ToString();
		}
	}

	string IPlatformServices.UserDisplayName => _epicUserName;

	IReadOnlyCollection<PlayerId> IPlatformServices.BlockedUsers => new List<PlayerId>();

	bool IPlatformServices.IsPermanentMuteAvailable => true;

	private string ExchangeCode => (string)_initParams["ExchangeCode"];

	string IPlatformServices.ProviderName => "Epic";

	PlayerId IPlatformServices.PlayerId => EpicAccountIdToPlayerId(_epicAccountId);

	bool IPlatformServices.UserLoggedIn
	{
		get
		{
			throw new NotImplementedException();
		}
	}

	public event Action<AvatarData> OnAvatarUpdated;

	public event Action<string> OnNameUpdated;

	public event Action<bool, TextObject> OnSignInStateUpdated;

	public event Action OnBlockedUserListUpdated;

	public event Action<string> OnTextEnteredFromPlatform;

	public event Action OnTextCanceledFromPlatform;

	public EpicPlatformServices(PlatformInitParams initParams)
	{
		_initParams = initParams;
		AvatarServices.AddAvatarService(PlayerIdProvidedTypes.Epic, new EpicPlatformAvatarService());
		_epicFriendListService = new EpicFriendListService(this);
	}

	public bool Initialize(IFriendListService[] additionalFriendListServices)
	{
		//IL_00aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b4: Expected O, but got Unknown
		//IL_00b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00db: Expected O, but got Unknown
		_friendListServices = new IFriendListService[additionalFriendListServices.Length + 1];
		_friendListServices[0] = _epicFriendListService;
		for (int i = 0; i < additionalFriendListServices.Length; i++)
		{
			_friendListServices[i + 1] = additionalFriendListServices[i];
		}
		string text = (string)_initParams["PlatformInterface"];
		if (!long.TryParse(text, out var result))
		{
			_initFailReason = new TextObject("{=BJ1626h7}Epic platform initialization failed: {FAILREASON}.");
			_initFailReason.SetTextVariable("FAILREASON (Platform Interface Handle)", text);
			Debug.Print("Epic PlatformInterface.Initialize Failed (Platform Interface Handle):" + text);
			return false;
		}
		IntPtr intPtr = new IntPtr(result);
		_platform = new PlatformInterface(intPtr);
		AddNotifyFriendsUpdateOptions val = default(AddNotifyFriendsUpdateOptions);
		_platform.GetFriendsInterface().AddNotifyFriendsUpdate(ref val, (object)null, (OnFriendsUpdateCallback)delegate(ref OnFriendsUpdateInfo callbackInfo)
		{
			_epicFriendListService.UserStatusChanged(EpicAccountIdToPlayerId(((OnFriendsUpdateInfo)(ref callbackInfo)).TargetUserId));
		});
		_epicAccountId = EpicAccountId.FromString(Utf8String.op_Implicit((string)_initParams["EpicUserId"]));
		_epicUserName = (string)_initParams["EpicUserName"];
		if ((Handle)(object)_platform.GetAuthInterface() == (Handle)null)
		{
			Console.WriteLine("ERROR: Failed to get Auth interface!");
			_initFailReason = new TextObject("{=BJ1626h7}Failed to get Auth interface!.");
			Debug.Print("Failed to get Auth interface!");
			return false;
		}
		return Connect();
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

	private void RefreshConnection(ref AuthExpirationCallbackInfo clientData)
	{
		try
		{
			Connect();
		}
		catch (Exception ex)
		{
			Debug.Print("RefreshConnection:" + ex.Message + " " + Environment.StackTrace, 5);
		}
	}

	private bool Connect()
	{
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0086: Unknown result type (might be due to invalid IL or missing references)
		//IL_0090: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00be: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00de: Expected O, but got Unknown
		//IL_011e: Unknown result type (might be due to invalid IL or missing references)
		//IL_013f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0149: Expected O, but got Unknown
		bool failed = false;
		CopyUserAuthTokenOptions val = default(CopyUserAuthTokenOptions);
		Token? val2 = default(Token?);
		_platform.GetAuthInterface().CopyUserAuthToken(ref val, _epicAccountId, ref val2);
		if (!val2.HasValue)
		{
			_initFailReason = new TextObject("{=oGIdsL8h}Could not retrieve token");
			return false;
		}
		Token value = val2.Value;
		_accessToken = Utf8String.op_Implicit(((Token)(ref value)).AccessToken);
		_platform.GetConnectInterface().RemoveNotifyAuthExpiration(_refreshConnectionCallbackId);
		LoginOptions val3 = default(LoginOptions);
		Credentials value2 = default(Credentials);
		((Credentials)(ref value2)).Token = Utf8String.op_Implicit(_accessToken);
		((Credentials)(ref value2)).Type = (ExternalCredentialType)0;
		((LoginOptions)(ref val3)).Credentials = value2;
		LoginOptions val4 = val3;
		OnCreateUserCallback val8 = default(OnCreateUserCallback);
		_platform.GetConnectInterface().Login(ref val4, (object)null, (OnLoginCallback)delegate(ref LoginCallbackInfo data)
		{
			//IL_0001: Unknown result type (might be due to invalid IL or missing references)
			//IL_0007: Invalid comparison between Unknown and I4
			//IL_0059: Unknown result type (might be due to invalid IL or missing references)
			//IL_000b: Unknown result type (might be due to invalid IL or missing references)
			//IL_001e: Unknown result type (might be due to invalid IL or missing references)
			//IL_001f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0045: Unknown result type (might be due to invalid IL or missing references)
			//IL_004a: Unknown result type (might be due to invalid IL or missing references)
			//IL_004c: Expected O, but got Unknown
			//IL_0051: Expected O, but got Unknown
			if ((int)((LoginCallbackInfo)(ref data)).ResultCode == 3)
			{
				CreateUserOptions val6 = default(CreateUserOptions);
				((CreateUserOptions)(ref val6)).ContinuanceToken = ((LoginCallbackInfo)(ref data)).ContinuanceToken;
				CreateUserOptions val7 = val6;
				ConnectInterface connectInterface = _platform.GetConnectInterface();
				OnCreateUserCallback obj = val8;
				if (obj == null)
				{
					OnCreateUserCallback val9 = delegate(ref CreateUserCallbackInfo res)
					{
						//IL_0001: Unknown result type (might be due to invalid IL or missing references)
						if ((int)((CreateUserCallbackInfo)(ref res)).ResultCode != 0)
						{
							failed = true;
						}
						else
						{
							_localUserId = ((CreateUserCallbackInfo)(ref res)).LocalUserId;
						}
					};
					OnCreateUserCallback val10 = val9;
					val8 = val9;
					obj = val10;
				}
				connectInterface.CreateUser(ref val7, (object)null, obj);
			}
			else if ((int)((LoginCallbackInfo)(ref data)).ResultCode != 0)
			{
				failed = true;
			}
			else
			{
				_localUserId = ((LoginCallbackInfo)(ref data)).LocalUserId;
			}
		});
		while ((Handle)(object)_localUserId == (Handle)null && !failed)
		{
			_platform.Tick();
		}
		if (failed)
		{
			_initFailReason = new TextObject("{=KoKdRd1u}Could not login to Epic");
			return false;
		}
		AddNotifyAuthExpirationOptions val5 = default(AddNotifyAuthExpirationOptions);
		_refreshConnectionCallbackId = _platform.GetConnectInterface().AddNotifyAuthExpiration(ref val5, (object)val2, new OnAuthExpirationCallback(RefreshConnection));
		QueryStats();
		QueryDefinitions();
		return true;
	}

	public void Terminate()
	{
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		if ((Handle)(object)_platform != (Handle)null)
		{
			_platform.Release();
			_platform = null;
			PlatformInterface.Shutdown();
		}
	}

	public void Tick(float dt)
	{
		if ((Handle)(object)_platform != (Handle)null)
		{
			_platform.Tick();
			ProcessIngestStatsQueue();
		}
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
		throw new NotImplementedException();
	}

	Task<AvatarData> IPlatformServices.GetUserAvatar(PlayerId providedId)
	{
		return Task.FromResult<AvatarData>(null);
	}

	Task<bool> IPlatformServices.ShowOverlayForWebPage(string url)
	{
		return Task.FromResult(result: false);
	}

	Task<ILoginAccessProvider> IPlatformServices.CreateLobbyClientLoginProvider()
	{
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Expected O, but got Unknown
		return Task.FromResult((ILoginAccessProvider)new EpicLoginAccessProvider(_platform, _epicAccountId, _epicUserName, _accessToken, _initFailReason));
	}

	PlatformInitParams IPlatformServices.GetInitParams()
	{
		return _initParams;
	}

	IAchievementService IPlatformServices.GetAchievementService()
	{
		return new EpicAchievementService(this);
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

	void IPlatformServices.OnFocusGained()
	{
	}

	void IPlatformServices.GetPlatformId(PlayerId playerId, Action<object> callback)
	{
		callback(PlayerIdToEpicAccountId(playerId));
	}

	internal async Task<string> GetUserName(PlayerId providedId)
	{
		if (!providedId.IsValid || providedId.ProvidedType != PlayerIdProvidedTypes.Epic)
		{
			return null;
		}
		EpicAccountId targetUserId = PlayerIdToEpicAccountId(providedId);
		UserInfoData? val = await GetUserInfo(targetUserId);
		if (!val.HasValue)
		{
			return "";
		}
		UserInfoData value = val.Value;
		return Utf8String.op_Implicit(((UserInfoData)(ref value)).DisplayName);
	}

	internal async Task<bool> GetUserOnlineStatus(PlayerId providedId)
	{
		EpicAccountId targetUserId = PlayerIdToEpicAccountId(providedId);
		await GetUserInfo(targetUserId);
		Info? val = await GetUserPresence(targetUserId);
		if (!val.HasValue)
		{
			return false;
		}
		Info value = val.Value;
		return (int)((Info)(ref value)).Status == 1;
	}

	internal async Task<bool> IsPlayingThisGame(PlayerId providedId)
	{
		Info? val = await GetUserPresence(PlayerIdToEpicAccountId(providedId));
		if (!val.HasValue)
		{
			return false;
		}
		Info value = val.Value;
		return ((Info)(ref value)).ProductId == Utf8String.op_Implicit("6372ed7350f34ffc9ace219dff4b9f40");
	}

	internal Task<PlayerId> GetUserWithName(string name)
	{
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Expected O, but got Unknown
		TaskCompletionSource<PlayerId> tsc = new TaskCompletionSource<PlayerId>();
		QueryUserInfoByDisplayNameOptions val = default(QueryUserInfoByDisplayNameOptions);
		((QueryUserInfoByDisplayNameOptions)(ref val)).LocalUserId = _epicAccountId;
		((QueryUserInfoByDisplayNameOptions)(ref val)).DisplayName = Utf8String.op_Implicit(name);
		QueryUserInfoByDisplayNameOptions val2 = val;
		_platform.GetUserInfoInterface().QueryUserInfoByDisplayName(ref val2, (object)null, (OnQueryUserInfoByDisplayNameCallback)delegate(ref QueryUserInfoByDisplayNameCallbackInfo callbackInfo)
		{
			//IL_0001: Unknown result type (might be due to invalid IL or missing references)
			if ((int)((QueryUserInfoByDisplayNameCallbackInfo)(ref callbackInfo)).ResultCode == 0)
			{
				PlayerId result = EpicAccountIdToPlayerId(((QueryUserInfoByDisplayNameCallbackInfo)(ref callbackInfo)).TargetUserId);
				tsc.SetResult(result);
				return;
			}
			throw new Exception("Could not retrieve player from EOS");
		});
		return tsc.Task;
	}

	internal IEnumerable<PlayerId> GetAllFriends()
	{
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Expected O, but got Unknown
		List<PlayerId> friends = new List<PlayerId>();
		bool? success = null;
		QueryFriendsOptions val = default(QueryFriendsOptions);
		((QueryFriendsOptions)(ref val)).LocalUserId = _epicAccountId;
		QueryFriendsOptions val2 = val;
		_platform.GetFriendsInterface().QueryFriends(ref val2, (object)null, (OnQueryFriendsCallback)delegate(ref QueryFriendsCallbackInfo callbackInfo)
		{
			//IL_0001: Unknown result type (might be due to invalid IL or missing references)
			//IL_000d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0025: Unknown result type (might be due to invalid IL or missing references)
			//IL_0026: Unknown result type (might be due to invalid IL or missing references)
			//IL_0045: Unknown result type (might be due to invalid IL or missing references)
			//IL_0065: Unknown result type (might be due to invalid IL or missing references)
			//IL_0067: Unknown result type (might be due to invalid IL or missing references)
			if ((int)((QueryFriendsCallbackInfo)(ref callbackInfo)).ResultCode == 0)
			{
				GetFriendsCountOptions val3 = default(GetFriendsCountOptions);
				((GetFriendsCountOptions)(ref val3)).LocalUserId = _epicAccountId;
				GetFriendsCountOptions val4 = val3;
				int friendsCount = _platform.GetFriendsInterface().GetFriendsCount(ref val4);
				for (int i = 0; i < friendsCount; i++)
				{
					GetFriendAtIndexOptions val5 = default(GetFriendAtIndexOptions);
					((GetFriendAtIndexOptions)(ref val5)).LocalUserId = _epicAccountId;
					((GetFriendAtIndexOptions)(ref val5)).Index = i;
					GetFriendAtIndexOptions val6 = val5;
					EpicAccountId friendAtIndex = _platform.GetFriendsInterface().GetFriendAtIndex(ref val6);
					friends.Add(EpicAccountIdToPlayerId(friendAtIndex));
				}
				success = true;
			}
			else
			{
				success = false;
			}
		});
		while (!success.HasValue)
		{
			_platform.Tick();
			Task.Delay(5);
		}
		return friends;
	}

	public void QueryDefinitions()
	{
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Expected O, but got Unknown
		AchievementsInterface achievementsInterface = _platform.GetAchievementsInterface();
		QueryDefinitionsOptions val = default(QueryDefinitionsOptions);
		((QueryDefinitionsOptions)(ref val)).LocalUserId = _localUserId;
		QueryDefinitionsOptions val2 = val;
		object obj = _003C_003Ec._003C_003E9__73_0;
		if (obj == null)
		{
			OnQueryDefinitionsCompleteCallback val3 = delegate
			{
			};
			_003C_003Ec._003C_003E9__73_0 = val3;
			obj = (object)val3;
		}
		achievementsInterface.QueryDefinitions(ref val2, (object)null, (OnQueryDefinitionsCompleteCallback)obj);
	}

	internal bool SetStat(string name, int value)
	{
		_ingestStatsQueue.Add(new IngestStatsQueueItem
		{
			Name = name,
			Value = value
		});
		return true;
	}

	internal Task<int> GetStat(string name)
	{
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		StatsInterface statsInterface = _platform.GetStatsInterface();
		CopyStatByNameOptions val = default(CopyStatByNameOptions);
		((CopyStatByNameOptions)(ref val)).Name = Utf8String.op_Implicit(name);
		((CopyStatByNameOptions)(ref val)).TargetUserId = _localUserId;
		CopyStatByNameOptions val2 = val;
		Stat? val3 = default(Stat?);
		if ((int)statsInterface.CopyStatByName(ref val2, ref val3) == 0)
		{
			Stat value = val3.Value;
			return Task.FromResult(((Stat)(ref value)).Value);
		}
		return Task.FromResult(-1);
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

	private void ProcessIngestStatsQueue()
	{
		//IL_00d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_0104: Unknown result type (might be due to invalid IL or missing references)
		//IL_0106: Unknown result type (might be due to invalid IL or missing references)
		//IL_0113: Unknown result type (might be due to invalid IL or missing references)
		//IL_011d: Expected O, but got Unknown
		//IL_0094: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bb: Unknown result type (might be due to invalid IL or missing references)
		if (_writingStats || !(DateTime.Now.Subtract(_statsLastWrittenOn).TotalSeconds > 5.0) || _ingestStatsQueue.Count <= 0)
		{
			return;
		}
		_statsLastWrittenOn = DateTime.Now;
		_writingStats = true;
		StatsInterface statsInterface = _platform.GetStatsInterface();
		List<IngestData> stats = new List<IngestData>();
		while (_ingestStatsQueue.Count > 0)
		{
			if (_ingestStatsQueue.TryTake(out var result))
			{
				List<IngestData> list = stats;
				IngestData item = default(IngestData);
				((IngestData)(ref item)).StatName = Utf8String.op_Implicit(result.Name);
				((IngestData)(ref item)).IngestAmount = result.Value;
				list.Add(item);
			}
		}
		IngestStatOptions val = default(IngestStatOptions);
		((IngestStatOptions)(ref val)).Stats = stats.ToArray();
		((IngestStatOptions)(ref val)).LocalUserId = _localUserId;
		((IngestStatOptions)(ref val)).TargetUserId = _localUserId;
		IngestStatOptions val2 = val;
		statsInterface.IngestStat(ref val2, (object)null, (OnIngestStatCompleteCallback)delegate(ref IngestStatCompleteCallbackInfo data)
		{
			//IL_0001: Unknown result type (might be due to invalid IL or missing references)
			//IL_0018: Unknown result type (might be due to invalid IL or missing references)
			//IL_001d: Unknown result type (might be due to invalid IL or missing references)
			if ((int)((IngestStatCompleteCallbackInfo)(ref data)).ResultCode != 0)
			{
				foreach (IngestData item2 in stats)
				{
					IngestData current = item2;
					_ingestStatsQueue.Add(new IngestStatsQueueItem
					{
						Name = Utf8String.op_Implicit(((IngestData)(ref current)).StatName),
						Value = ((IngestData)(ref current)).IngestAmount
					});
				}
			}
			QueryStats();
			_writingStats = false;
		});
	}

	private static PlayerId EpicAccountIdToPlayerId(EpicAccountId epicAccountId)
	{
		return new PlayerId(3, ((object)epicAccountId).ToString());
	}

	private static EpicAccountId PlayerIdToEpicAccountId(PlayerId playerId)
	{
		byte[] b = Enumerable.ToArray(new ArraySegment<byte>(playerId.ToByteArray(), 16, 16));
		return EpicAccountId.FromString(Utf8String.op_Implicit(new Guid(b).ToString("N")));
	}

	private Task<UserInfoData?> GetUserInfo(EpicAccountId targetUserId)
	{
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_0062: Expected O, but got Unknown
		TaskCompletionSource<UserInfoData?> tsc = new TaskCompletionSource<UserInfoData?>();
		QueryUserInfoOptions val = default(QueryUserInfoOptions);
		((QueryUserInfoOptions)(ref val)).LocalUserId = _epicAccountId;
		((QueryUserInfoOptions)(ref val)).TargetUserId = targetUserId;
		QueryUserInfoOptions val2 = val;
		_platform.GetUserInfoInterface().QueryUserInfo(ref val2, (object)null, (OnQueryUserInfoCallback)delegate(ref QueryUserInfoCallbackInfo callbackInfo)
		{
			//IL_0001: Unknown result type (might be due to invalid IL or missing references)
			//IL_000a: Unknown result type (might be due to invalid IL or missing references)
			//IL_002f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0030: Unknown result type (might be due to invalid IL or missing references)
			//IL_0045: Unknown result type (might be due to invalid IL or missing references)
			if ((int)((QueryUserInfoCallbackInfo)(ref callbackInfo)).ResultCode == 0)
			{
				CopyUserInfoOptions val3 = default(CopyUserInfoOptions);
				((CopyUserInfoOptions)(ref val3)).LocalUserId = _epicAccountId;
				((CopyUserInfoOptions)(ref val3)).TargetUserId = targetUserId;
				CopyUserInfoOptions val4 = val3;
				UserInfoData? result = default(UserInfoData?);
				_platform.GetUserInfoInterface().CopyUserInfo(ref val4, ref result);
				tsc.SetResult(result);
			}
			else
			{
				tsc.SetResult(null);
			}
		});
		return tsc.Task;
	}

	private Task<Info?> GetUserPresence(EpicAccountId targetUserId)
	{
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_0062: Expected O, but got Unknown
		TaskCompletionSource<Info?> tsc = new TaskCompletionSource<Info?>();
		QueryPresenceOptions val = default(QueryPresenceOptions);
		((QueryPresenceOptions)(ref val)).LocalUserId = _epicAccountId;
		((QueryPresenceOptions)(ref val)).TargetUserId = targetUserId;
		QueryPresenceOptions val2 = val;
		_platform.GetPresenceInterface().QueryPresence(ref val2, (object)null, (OnQueryPresenceCompleteCallback)delegate(ref QueryPresenceCallbackInfo callbackInfo)
		{
			//IL_0001: Unknown result type (might be due to invalid IL or missing references)
			//IL_000d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0032: Unknown result type (might be due to invalid IL or missing references)
			//IL_0033: Unknown result type (might be due to invalid IL or missing references)
			//IL_004f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0074: Unknown result type (might be due to invalid IL or missing references)
			//IL_0076: Unknown result type (might be due to invalid IL or missing references)
			//IL_008b: Unknown result type (might be due to invalid IL or missing references)
			if ((int)((QueryPresenceCallbackInfo)(ref callbackInfo)).ResultCode == 0)
			{
				HasPresenceOptions val3 = default(HasPresenceOptions);
				((HasPresenceOptions)(ref val3)).LocalUserId = _epicAccountId;
				((HasPresenceOptions)(ref val3)).TargetUserId = targetUserId;
				HasPresenceOptions val4 = val3;
				if (_platform.GetPresenceInterface().HasPresence(ref val4))
				{
					CopyPresenceOptions val5 = default(CopyPresenceOptions);
					((CopyPresenceOptions)(ref val5)).LocalUserId = _epicAccountId;
					((CopyPresenceOptions)(ref val5)).TargetUserId = targetUserId;
					CopyPresenceOptions val6 = val5;
					Info? result = default(Info?);
					_platform.GetPresenceInterface().CopyPresence(ref val6, ref result);
					tsc.SetResult(result);
				}
				else
				{
					tsc.SetResult(null);
				}
			}
			else
			{
				tsc.SetResult(null);
			}
		});
		return tsc.Task;
	}

	private void QueryStats()
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Expected O, but got Unknown
		QueryStatsOptions val = default(QueryStatsOptions);
		((QueryStatsOptions)(ref val)).LocalUserId = _localUserId;
		((QueryStatsOptions)(ref val)).TargetUserId = _localUserId;
		QueryStatsOptions val2 = val;
		StatsInterface statsInterface = _platform.GetStatsInterface();
		object obj = _003C_003Ec._003C_003E9__87_0;
		if (obj == null)
		{
			OnQueryStatsCompleteCallback val3 = delegate
			{
			};
			_003C_003Ec._003C_003E9__87_0 = val3;
			obj = (object)val3;
		}
		statsInterface.QueryStats(ref val2, (object)null, (OnQueryStatsCompleteCallback)obj);
	}

	IFriendListService[] IPlatformServices.GetFriendListServices()
	{
		return _friendListServices;
	}

	public bool ShowGamepadTextInput(string descriptionText, string existingText, uint maxChars, bool isObfuscated)
	{
		return false;
	}

	bool IPlatformServices.UsePlatformInvitationService(PlayerId targetPlayerId)
	{
		return false;
	}
}
