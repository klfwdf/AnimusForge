using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using TaleWorlds.AchievementSystem;
using TaleWorlds.ActivitySystem;
using TaleWorlds.Avatar.PlayerServices;
using TaleWorlds.Core;
using TaleWorlds.Diamond.ClientApplication;
using TaleWorlds.DotNet;
using TaleWorlds.Engine;
using TaleWorlds.Engine.Options;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ModuleManager;
using TaleWorlds.ObjectSystem;
using TaleWorlds.PlatformService;
using TaleWorlds.SaveSystem;
using TaleWorlds.ScreenSystem;

namespace TaleWorlds.MountAndBlade;

public sealed class Module : DotNetObject, IGameStateManagerOwner
{
	public enum XmlInformationType
	{
		Parameters,
		MbObjectType
	}

	private enum StartupType
	{
		None,
		TestMode,
		GameServer,
		Singleplayer,
		Multiplayer,
		Count
	}

	private bool _enableCoreContentOnReturnToRoot;

	private List<MissionInfo> _missionInfos;

	private TestContext _testContext;

	private SingleThreadedSynchronizationContext _synchronizationContext;

	private readonly Dictionary<SubModuleInfo, MBSubModuleBase> _subModuleBases;

	private bool _splashScreenPlayed;

	private List<InitialStateOption> _initialStateOptions;

	private IEditorMissionTester _editorMissionTester;

	private Dictionary<string, MultiplayerGameMode> _multiplayerGameModesWithNames;

	private MBList<MultiplayerGameTypeInfo> _multiplayerGameTypes = new MBList<MultiplayerGameTypeInfo>();

	private bool _isShuttingDown;

	public static Module CurrentModule { get; private set; }

	public GameStateManager GlobalGameStateManager { get; private set; }

	public bool MultiplayerRequested
	{
		get
		{
			if (StartupInfo.StartupType != GameStartupType.Multiplayer && PlatformServices.SessionInvitationType != SessionInvitationType.Multiplayer)
			{
				return PlatformServices.IsPlatformRequestedMultiplayer;
			}
			return true;
		}
	}

	public bool ReturnToEditorState { get; private set; }

	public bool LoadingFinished { get; private set; }

	public GameTextManager GlobalTextManager { get; private set; }

	public bool IsOnlyCoreContentEnabled { get; private set; }

	public JobManager JobManager { get; private set; }

	public GameStartupInfo StartupInfo { get; private set; }

	public event Action SkinsXMLHasChanged;

	public event Action ImguiProfilerTick;

	private Module()
	{
		MBDebug.Print("Creating module...");
		StartupInfo = new GameStartupInfo();
		_testContext = new TestContext();
		_subModuleBases = new Dictionary<SubModuleInfo, MBSubModuleBase>();
		GlobalGameStateManager = new GameStateManager(this, GameStateManager.GameStateManagerType.Global);
		GameStateManager.Current = GlobalGameStateManager;
		GlobalTextManager = new GameTextManager();
		JobManager = new JobManager();
	}

	public MBReadOnlyList<MBSubModuleBase> CollectSubModules()
	{
		MBList<MBSubModuleBase> mBList = new MBList<MBSubModuleBase>();
		foreach (ModuleInfo allModule in ModuleHelper.GetAllModules())
		{
			if (!allModule.IsActive)
			{
				continue;
			}
			foreach (SubModuleInfo subModule in allModule.SubModules)
			{
				MBSubModuleBase subModuleBase = GetSubModuleBase(subModule);
				if (subModuleBase != null)
				{
					mBList.Add(subModuleBase);
				}
			}
		}
		return mBList;
	}

	internal static void CreateModule()
	{
		CurrentModule = new Module();
		Utilities.SetLoadingScreenPercentage(0.4f);
	}

	private AssemblyLoader.AssemblyLoadResult AddSubModule(SubModuleInfo subModuleInfo, Assembly subModuleAssembly)
	{
		ConstructorInfo constructor = subModuleAssembly.GetType(subModuleInfo.SubModuleClassTypeName).GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.CreateInstance, null, new Type[0], null);
		Dictionary<string, Type> types;
		AssemblyLoader.AssemblyLoadResult assemblyLoadResult = CollectModuleAssemblyTypes(subModuleInfo, subModuleAssembly, out types);
		if (assemblyLoadResult == AssemblyLoader.AssemblyLoadResult.Success)
		{
			Managed.AddTypes(types);
			MBSubModuleBase value = (MBSubModuleBase)constructor.Invoke(new object[0]);
			_subModuleBases.Add(subModuleInfo, value);
		}
		return assemblyLoadResult;
	}

	private AssemblyLoader.AssemblyLoadResult CollectModuleAssemblyTypes(SubModuleInfo subModule, Assembly moduleAssembly, out Dictionary<string, Type> types)
	{
		try
		{
			types = new Dictionary<string, Type>();
			Type[] types2 = moduleAssembly.GetTypes();
			foreach (Type type in types2)
			{
				if (typeof(ManagedObject).IsAssignableFrom(type) || typeof(DotNetObject).IsAssignableFrom(type))
				{
					types.Add(type.Name, type);
				}
			}
			return AssemblyLoader.AssemblyLoadResult.Success;
		}
		catch (Exception ex)
		{
			MBDebug.Print("Error while getting types and loading" + ex.Message + "\nException: " + ex.GetType().Name);
			if (ex is ReflectionTypeLoadException ex2)
			{
				string text = "";
				Exception[] loaderExceptions = ex2.LoaderExceptions;
				foreach (Exception ex3 in loaderExceptions)
				{
					MBDebug.Print("Loader Exceptions: " + ex3.Message);
					text = text + ex3.Message + Environment.NewLine;
				}
				TaleWorlds.Library.Debug.SetCrashReportCustomString(text);
				Type[] types2 = ex2.Types;
				foreach (Type type2 in types2)
				{
					if (type2 != null)
					{
						MBDebug.Print("Loaded Types: " + type2.FullName);
					}
				}
			}
			if (ex.InnerException != null)
			{
				MBDebug.Print("Inner excetion: " + ex.StackTrace);
			}
			types = null;
			return AssemblyLoader.AssemblyLoadResult.CriticalError;
		}
	}

	private void InitializeSubModuleBases()
	{
		Managed.AddConstructorDelegateOfClass<SpawnedItemEntity>();
		foreach (MBSubModuleBase value in _subModuleBases.Values)
		{
			value.OnSubModuleLoad();
		}
	}

	private void OnNewModuleLoaded()
	{
		foreach (MBSubModuleBase value in _subModuleBases.Values)
		{
			value.OnNewModuleLoad();
		}
	}

	private MBSubModuleBase GetSubModuleBase(SubModuleInfo subModuleInfo)
	{
		if (_subModuleBases.TryGetValue(subModuleInfo, out var value))
		{
			return value;
		}
		return null;
	}

	private void FinalizeSubModulesBases()
	{
		foreach (MBSubModuleBase value in _subModuleBases.Values)
		{
			value.OnSubModuleUnloaded();
		}
	}

	[MBCallback(null, false)]
	internal void LoadSingleModule(string modulePath)
	{
		List<ModuleInfo> list = new List<ModuleInfo>();
		list.Add(ModuleHelper.InitializeSingleModule(modulePath));
		LocalizedTextManager.AddLocalizationXml(modulePath);
		LoadSubModules(list, loadNewModules: true);
		BannerManager.ResetAndLoad();
	}

	[MBCallback(null, false)]
	internal void Initialize()
	{
		MBDebug.Print("Module Initialize begin...");
		TWParallel.InitializeAndSetImplementation(new NativeParallelDriver());
		MBSaveLoad.SetSaveDriver(new AsyncFileSaveDriver());
		ProcessApplicationArguments();
		SetWindowTitle();
		_initialStateOptions = new List<InitialStateOption>();
		FillMultiplayerGameTypes();
		if (!GameNetwork.IsDedicatedServer && !MBDebug.TestModeEnabled)
		{
			MBDebug.Print("Loading platform services...");
			LoadPlatformServices();
		}
		string[] platformModulePaths = null;
		ModuleHelper.InitializeModules(Utilities.GetModulesNames(), platformModulePaths);
		LoadLocalizationXmls();
		GlobalTextManager.LoadDefaultTexts();
		IsOnlyCoreContentEnabled = Utilities.IsOnlyCoreContentEnabled();
		NativeConfig.OnConfigChanged();
		List<ModuleInfo> modules = ModuleHelper.GetModules();
		LoadSubModules(modules, loadNewModules: false);
		MBDebug.Print("Adding trace listener...");
		MBDebug.Print("MBModuleBase Initialize begin...");
		MBDebug.Print("MBModuleBase Initialize end...");
		GameNetwork.FindGameNetworkMessages();
		GameNetwork.FindSynchedMissionObjectTypes();
		HasTableauCache.CollectTableauCacheTypes();
		MBDebug.Print("Module Initialize end...");
		MBDebug.TestModeEnabled = Utilities.CommandLineArgumentExists("/runTest");
		FindMissions();
		NativeOptions.ReadRGLConfigFiles();
		BannerlordConfig.Initialize();
		EngineController.ConfigChange += OnConfigChanged;
		EngineController.OnConstrainedStateChanged += OnConstrainedStateChange;
		ScreenManager.FocusGained += OnFocusGained;
		ScreenManager.PlatformTextRequested += OnPlatformTextRequested;
		PlatformServices.Instance.OnTextEnteredFromPlatform += OnTextEnteredFromPlatform;
		PlatformServices.Instance.OnTextCanceledFromPlatform += OnTextCanceledFromPlatform;
		SaveManager.InitializeGlobalDefinitionContext();
		EnsureAsyncJobsAreFinished();
	}

	private bool OnPlatformTextRequested(string initialText, string descriptionText, int maxLength, int keyboardTypeEnum)
	{
		return PlatformServices.Instance?.ShowGamepadTextInput(descriptionText, initialText, (uint)maxLength, keyboardTypeEnum == 2) ?? false;
	}

	private void LoadLocalizationXmls()
	{
		List<string> list = new List<string>();
		foreach (ModuleInfo module in ModuleHelper.GetModules())
		{
			list.Add(module.FolderPath);
		}
		LocalizedTextManager.LoadLocalizationXmls(list.ToArray());
	}

	private void SetWindowTitle()
	{
		string applicationName = Utilities.GetApplicationName();
		string title = ((StartupInfo.StartupType == GameStartupType.Singleplayer) ? (applicationName + " - Singleplayer") : ((StartupInfo.StartupType == GameStartupType.Multiplayer) ? (applicationName + " - Multiplayer") : ((StartupInfo.StartupType != GameStartupType.GameServer) ? applicationName : ("[" + Utilities.GetCurrentProcessID() + "] " + applicationName + " Dedicated Server Port:" + StartupInfo.ServerPort))));
		title = Utilities.ProcessWindowTitle(title);
		Utilities.SetWindowTitle(title);
	}

	private void EnsureAsyncJobsAreFinished()
	{
		if (!GameNetwork.IsDedicatedServer)
		{
			while (!MBMusicManager.IsCreationCompleted())
			{
				Thread.Sleep(1);
			}
		}
		if (!GameNetwork.IsDedicatedServer && !MBDebug.TestModeEnabled)
		{
			while (!AchievementManager.AchievementService.IsInitializationCompleted())
			{
				Thread.Sleep(1);
			}
		}
	}

	private void ProcessApplicationArguments()
	{
		StartupInfo.StartupType = GameStartupType.None;
		string[] array = Utilities.GetFullCommandLineString().Split(new char[1] { ' ' });
		for (int i = 0; i < array.Length; i++)
		{
			string text = array[i].ToLowerInvariant();
			if (text == "/dedicatedmatchmakingserver".ToLower())
			{
				int serverPort = Convert.ToInt32(array[i + 1]);
				string serverRegion = array[i + 2];
				sbyte serverPriority = Convert.ToSByte(array[i + 3]);
				string text2 = array[i + 4];
				i += 4;
				StartupInfo.StartupType = GameStartupType.GameServer;
				StartupInfo.DedicatedServerType = DedicatedServerType.Matchmaker;
				StartupInfo.ServerPort = serverPort;
				StartupInfo.ServerRegion = serverRegion;
				StartupInfo.ServerPriority = serverPriority;
				StartupInfo.ServerGameMode = text2.Trim();
			}
			else if (text == "/dedicatedcustomserver".ToLower())
			{
				int serverPort2 = Convert.ToInt32(array[i + 1]);
				string serverRegion2 = array[i + 2];
				int permission = Convert.ToInt32(array[i + 3]);
				i += 3;
				StartupInfo.StartupType = GameStartupType.GameServer;
				StartupInfo.DedicatedServerType = DedicatedServerType.Custom;
				StartupInfo.ServerPort = serverPort2;
				StartupInfo.ServerRegion = serverRegion2;
				StartupInfo.Permission = permission;
			}
			else if (text == "/dedicatedcommunityserver".ToLower())
			{
				int serverPort3 = Convert.ToInt32(array[i + 1]);
				i++;
				StartupInfo.StartupType = GameStartupType.GameServer;
				StartupInfo.DedicatedServerType = DedicatedServerType.Community;
				StartupInfo.ServerPort = serverPort3;
			}
			else if (text == "/dedicatedcustomserverconfigfile".ToLower())
			{
				string customGameServerConfigFile = array[i + 1];
				i++;
				StartupInfo.CustomGameServerConfigFile = customGameServerConfigFile;
			}
			else if (text == "/dedicatedcustomservernameoverride".ToLower())
			{
				string customGameServerNameOverride = array[i + 1];
				i++;
				StartupInfo.CustomGameServerNameOverride = customGameServerNameOverride;
			}
			else if (text == "/dedicatedcustomserverpasswordoverride".ToLower())
			{
				string customGameServerPasswordOverride = array[i + 1];
				i++;
				StartupInfo.CustomGameServerPasswordOverride = customGameServerPasswordOverride;
			}
			else if (text == "/dedicatedcustomserverauthtoken".ToLower())
			{
				string customGameServerAuthToken = array[i + 1];
				i++;
				StartupInfo.CustomGameServerAuthToken = customGameServerAuthToken;
			}
			else if (text == "/dedicatedcustomserverDontAllowOptionalModules".ToLower())
			{
				StartupInfo.CustomGameServerAllowsOptionalModules = false;
			}
			else if (text == "/playerHostedDedicatedServer".ToLower())
			{
				StartupInfo.PlayerHostedDedicatedServer = true;
			}
			else if (text == "/singleplatform")
			{
				StartupInfo.IsSinglePlatformServer = true;
			}
			else if (text == "/customserverhost")
			{
				string customServerHostIP = array[i + 1];
				i++;
				StartupInfo.CustomServerHostIP = customServerHostIP;
			}
			else if (text == "/singleplayer".ToLower())
			{
				StartupInfo.StartupType = GameStartupType.Singleplayer;
			}
			else if (text == "/multiplayer".ToLower())
			{
				StartupInfo.StartupType = GameStartupType.Multiplayer;
			}
			else if (text == "/clientConfigurationCategory".ToLower())
			{
				ClientApplicationConfiguration.SetDefaultConfigurationCategory(array[i + 1]);
				i++;
			}
			else if (text == "/overridenusername".ToLower())
			{
				string overridenUserName = array[i + 1];
				StartupInfo.OverridenUserName = overridenUserName;
				i++;
			}
			else if (text.StartsWith("-PlatformInterface".ToLowerInvariant()))
			{
				StartupInfo.PlatformInterface = text.Split(new char[1] { '=' })[1];
			}
			else if (text.StartsWith("-epicuserid".ToLowerInvariant()))
			{
				StartupInfo.EpicUserId = text.Split(new char[1] { '=' })[1];
			}
			else if (text.StartsWith("-epicusername".ToLowerInvariant()))
			{
				StartupInfo.EpicUserName = text.Split(new char[1] { '=' })[1];
			}
			else if (text == "/continuegame".ToLower())
			{
				StartupInfo.IsContinueGame = true;
			}
			else if (text == "/serverbandwidthlimitmbps".ToLower())
			{
				double serverBandwidthLimitInMbps = Convert.ToDouble(array[i + 1]);
				StartupInfo.ServerBandwidthLimitInMbps = serverBandwidthLimitInMbps;
				i++;
			}
			else if (text == "/tickrate".ToLower())
			{
				int serverTickRate = Convert.ToInt32(array[i + 1]);
				StartupInfo.ServerTickRate = serverTickRate;
				i++;
			}
		}
	}

	internal void OnApplicationTick(float dt)
	{
		bool isOnlyCoreContentEnabled = IsOnlyCoreContentEnabled;
		IsOnlyCoreContentEnabled = Utilities.IsOnlyCoreContentEnabled();
		if (isOnlyCoreContentEnabled != IsOnlyCoreContentEnabled && isOnlyCoreContentEnabled)
		{
			InitialState initialState;
			if ((initialState = GameStateManager.Current.ActiveState as InitialState) != null)
			{
				Utilities.DisableCoreGame();
				InformationManager.ShowInquiry(new InquiryData(new TextObject("{=CaSafuAH}Content Download Complete").ToString(), new TextObject("{=1nKa4pQX}Rest of the game content has been downloaded.").ToString(), isAffirmativeOptionShown: true, isNegativeOptionShown: false, new TextObject("{=yS7PvrTD}OK").ToString(), null, delegate
				{
					initialState.RefreshContentState();
				}, null));
			}
			else
			{
				InformationManager.ShowInquiry(new InquiryData(new TextObject("{=CaSafuAH}Content Download Complete").ToString(), new TextObject("{=BFhMw4bl}Rest of the game content has been downloaded. Do you want to return to the main menu?").ToString(), isAffirmativeOptionShown: true, isNegativeOptionShown: true, new TextObject("{=aeouhelq}Yes").ToString(), new TextObject("{=8OkPHu4f}No").ToString(), OnConfirmReturnToMainMenu, null));
				_enableCoreContentOnReturnToRoot = true;
			}
		}
		if (_synchronizationContext == null)
		{
			_synchronizationContext = new SingleThreadedSynchronizationContext();
			SynchronizationContext.SetSynchronizationContext(_synchronizationContext);
		}
		_testContext.OnApplicationTick(dt);
		if (!GameNetwork.MultiplayerDisabled)
		{
			OnNetworkTick(dt);
		}
		if (GameStateManager.Current == null)
		{
			GameStateManager.Current = GlobalGameStateManager;
		}
		if (GameStateManager.Current == GlobalGameStateManager)
		{
			if (LoadingFinished && GlobalGameStateManager.ActiveState == null)
			{
				if (ReturnToEditorState)
				{
					ReturnToEditorState = false;
					SetEditorScreenAsRootScreen();
				}
				else
				{
					SetInitialModuleScreenAsRootScreen();
				}
			}
			GlobalGameStateManager.OnTick(dt);
		}
		Utilities.RunJobs();
		PlatformServices.Instance?.Tick(dt);
		_synchronizationContext.Tick();
		if (GameManagerBase.Current != null)
		{
			GameManagerBase.Current.OnTick(dt);
		}
		foreach (MBSubModuleBase item in CollectSubModules())
		{
			item.OnApplicationTick(dt);
		}
		JobManager.OnTick(dt);
		AvatarServices.UpdateAvatarServices(dt);
	}

	private void OnConfirmReturnToMainMenu()
	{
		MBGameManager.EndGame();
	}

	private void OnNetworkTick(float dt)
	{
		foreach (MBSubModuleBase item in CollectSubModules())
		{
			item.OnNetworkTick(dt);
		}
	}

	[MBCallback(null, false)]
	internal void RunTest(string commandLine)
	{
		MBDebug.Print(" TEST MODE ENABLED. Command line string: " + commandLine);
		_testContext.RunTestAux(commandLine);
	}

	[MBCallback(null, true)]
	internal void TickTest(float dt)
	{
		_testContext.TickTest(dt);
	}

	[MBCallback(null, false)]
	internal void OnDumpCreated()
	{
		if (TestCommonBase.BaseInstance != null)
		{
			TestCommonBase.BaseInstance.ToggleTimeoutTimer();
			TestCommonBase.BaseInstance.StartTimeoutTimer();
		}
	}

	[MBCallback(null, false)]
	internal void OnDumpCreationStarted()
	{
		if (TestCommonBase.BaseInstance != null)
		{
			TestCommonBase.BaseInstance.ToggleTimeoutTimer();
		}
	}

	public static void GetMetaMeshPackageMapping(Dictionary<string, string> metaMeshPackageMappings)
	{
		foreach (ItemObject objectType in Game.Current.ObjectManager.GetObjectTypeList<ItemObject>())
		{
			if (objectType.HasArmorComponent)
			{
				string value = ((objectType.Culture != null) ? objectType.Culture.StringId : "shared") + "_armor";
				metaMeshPackageMappings[objectType.MultiMeshName] = value;
				metaMeshPackageMappings[objectType.MultiMeshName + "_converted"] = value;
				metaMeshPackageMappings[objectType.MultiMeshName + "_converted_slim"] = value;
				metaMeshPackageMappings[objectType.MultiMeshName + "_slim"] = value;
			}
			if (objectType.WeaponComponent != null)
			{
				string value2 = ((objectType.Culture != null) ? objectType.Culture.StringId : "shared") + "_weapon";
				metaMeshPackageMappings[objectType.MultiMeshName] = value2;
				if (objectType.HolsterMeshName != null)
				{
					metaMeshPackageMappings[objectType.HolsterMeshName] = value2;
				}
				if (objectType.HolsterWithWeaponMeshName != null)
				{
					metaMeshPackageMappings[objectType.HolsterWithWeaponMeshName] = value2;
				}
			}
			if (objectType.HasHorseComponent)
			{
				string value3 = "horses";
				metaMeshPackageMappings[objectType.MultiMeshName] = value3;
			}
			if (objectType.IsFood)
			{
				string value4 = "food";
				metaMeshPackageMappings[objectType.MultiMeshName] = value4;
			}
		}
		foreach (CraftingPiece objectType2 in Game.Current.ObjectManager.GetObjectTypeList<CraftingPiece>())
		{
			string value5 = ((objectType2.Culture != null) ? objectType2.Culture.StringId : "shared") + "_crafting";
			metaMeshPackageMappings[objectType2.MeshName] = value5;
		}
	}

	public static void GetItemMeshNames(HashSet<string> itemMeshNames)
	{
		foreach (ItemObject objectType in Game.Current.ObjectManager.GetObjectTypeList<ItemObject>())
		{
			if (!objectType.IsCraftedWeapon)
			{
				itemMeshNames.Add(objectType.MultiMeshName);
			}
			if (objectType.PrimaryWeapon != null)
			{
				if (objectType.FlyingMeshName != null && !objectType.FlyingMeshName.IsEmpty())
				{
					itemMeshNames.Add(objectType.FlyingMeshName);
				}
				if (objectType.HolsterMeshName != null && !objectType.HolsterMeshName.IsEmpty())
				{
					itemMeshNames.Add(objectType.HolsterMeshName);
				}
				if (objectType.HolsterWithWeaponMeshName != null && !objectType.HolsterWithWeaponMeshName.IsEmpty())
				{
					itemMeshNames.Add(objectType.HolsterWithWeaponMeshName);
				}
			}
			if (!objectType.HasHorseComponent)
			{
				continue;
			}
			foreach (KeyValuePair<string, bool> additionalMeshesName in objectType.HorseComponent.AdditionalMeshesNameList)
			{
				if (additionalMeshesName.Key != null && !additionalMeshesName.Key.IsEmpty())
				{
					itemMeshNames.Add(additionalMeshesName.Key);
				}
			}
		}
	}

	[MBCallback(null, false)]
	internal string GetMetaMeshPackageMapping()
	{
		Dictionary<string, string> dictionary = new Dictionary<string, string>();
		GetMetaMeshPackageMapping(dictionary);
		string text = "";
		foreach (string key in dictionary.Keys)
		{
			text = text + key + "|" + dictionary[key] + ",";
		}
		return text;
	}

	[MBCallback(null, false)]
	internal string GetItemMeshNames()
	{
		HashSet<string> hashSet = new HashSet<string>();
		GetItemMeshNames(hashSet);
		foreach (CraftingPiece objectType in MBObjectManager.Instance.GetObjectTypeList<CraftingPiece>())
		{
			hashSet.Add(objectType.MeshName);
			if (objectType.BladeData != null)
			{
				hashSet.Add(objectType.BladeData.HolsterMeshName);
			}
		}
		foreach (BannerIconGroup bannerIconGroup in BannerManager.Instance.BannerIconGroups)
		{
			foreach (KeyValuePair<int, BannerIconData> allIcon in bannerIconGroup.AllIcons)
			{
				if (allIcon.Value.MaterialName != "")
				{
					hashSet.Add(allIcon.Value.MaterialName + allIcon.Value.TextureIndex);
				}
			}
		}
		string text = "";
		foreach (string item in hashSet)
		{
			if (item != null && !item.IsEmpty())
			{
				text = text + item + "#";
			}
		}
		return text;
	}

	[CommandLineFunctionality.CommandLineArgumentFunction("get_item_mesh_names", "module")]
	public static string GetCraftedItemMeshNames(List<string> arguments)
	{
		HashSet<string> hashSet = new HashSet<string>();
		foreach (CraftingPiece objectType in MBObjectManager.Instance.GetObjectTypeList<CraftingPiece>())
		{
			hashSet.Add(objectType.MeshName);
			if (objectType.BladeData != null)
			{
				hashSet.Add(objectType.BladeData.HolsterMeshName);
			}
		}
		string text = "";
		foreach (string item in hashSet)
		{
			if (item != null && !item.IsEmpty())
			{
				text = text + item + "#";
			}
		}
		return text;
	}

	[MBCallback(null, false)]
	internal string GetHorseMaterialNames()
	{
		HashSet<string> hashSet = new HashSet<string>();
		string text = "";
		foreach (ItemObject objectType in Game.Current.ObjectManager.GetObjectTypeList<ItemObject>())
		{
			if (!objectType.HasHorseComponent || objectType.HorseComponent.HorseMaterialNames == null || objectType.HorseComponent.HorseMaterialNames.Count <= 0)
			{
				continue;
			}
			foreach (HorseComponent.MaterialProperty horseMaterialName in objectType.HorseComponent.HorseMaterialNames)
			{
				hashSet.Add(horseMaterialName.Name);
			}
		}
		foreach (string item in hashSet)
		{
			if (item != null && !item.IsEmpty())
			{
				text = text + item + "#";
			}
		}
		return text;
	}

	public void SetInitialModuleScreenAsRootScreen()
	{
		if (GameStateManager.Current != GlobalGameStateManager)
		{
			GameStateManager.Current = GlobalGameStateManager;
		}
		foreach (MBSubModuleBase value in _subModuleBases.Values)
		{
			value.OnBeforeInitialModuleScreenSetAsRoot();
		}
		if (GameNetwork.IsDedicatedServer)
		{
			return;
		}
		string text = ModuleHelper.GetModuleFullPath("Native") + "Videos/TWLogo_and_Partners.ivf";
		string text2 = ModuleHelper.GetModuleFullPath("Native") + "Videos/TWLogo_and_Partners.ogg";
		if (!_splashScreenPlayed && File.Exists(text) && (text2 == "" || File.Exists(text2)) && !Debugger.IsAttached)
		{
			VideoPlaybackState videoPlaybackState = GlobalGameStateManager.CreateState<VideoPlaybackState>();
			videoPlaybackState.SetStartingParameters(text, text2, string.Empty);
			videoPlaybackState.SetOnVideoFinisedDelegate(delegate
			{
				OnInitialModuleScreenActivated(isFromSplashScreenVideo: true);
			});
			GlobalGameStateManager.CleanAndPushState(videoPlaybackState);
			_splashScreenPlayed = true;
		}
		else
		{
			OnInitialModuleScreenActivated(isFromSplashScreenVideo: false);
		}
	}

	private void OnInitialModuleScreenActivated(bool isFromSplashScreenVideo)
	{
		Utilities.EnableGlobalLoadingWindow();
		LoadingWindow.EnableGlobalLoadingWindow();
		if (!StartupInfo.IsContinueGame)
		{
			StartupInfo.IsContinueGame = PlatformServices.IsPlatformRequestedContinueGame && !IsOnlyCoreContentEnabled;
		}
		if (_enableCoreContentOnReturnToRoot)
		{
			Utilities.DisableCoreGame();
			_enableCoreContentOnReturnToRoot = false;
		}
		if (IsOnlyCoreContentEnabled && PlatformServices.SessionInvitationType == SessionInvitationType.Multiplayer)
		{
			PlatformServices.OnSessionInvitationHandled();
		}
		if (IsOnlyCoreContentEnabled && PlatformServices.IsPlatformRequestedMultiplayer)
		{
			PlatformServices.OnPlatformMultiplayerRequestHandled();
		}
		foreach (ModuleInfo module in ModuleHelper.GetModules((ModuleInfo x) => !x.IsActive))
		{
			ActivateModule(module.Id);
		}
		if (IsOnlyCoreContentEnabled || !MultiplayerRequested)
		{
			GlobalGameStateManager.CleanAndPushState(GlobalGameStateManager.CreateState<InitialState>());
		}
		foreach (MBSubModuleBase value in _subModuleBases.Values)
		{
			value.OnInitialState();
		}
	}

	private void OnSignInStateUpdated(bool isLoggedIn, TextObject message)
	{
		if (!isLoggedIn && !(GlobalGameStateManager.ActiveState is ProfileSelectionState))
		{
			GlobalGameStateManager.CleanAndPushState(GlobalGameStateManager.CreateState<ProfileSelectionState>());
		}
	}

	[MBCallback(null, false)]
	internal bool SetEditorScreenAsRootScreen()
	{
		if (GameStateManager.Current != GlobalGameStateManager)
		{
			GameStateManager.Current = GlobalGameStateManager;
		}
		if (!(GlobalGameStateManager.ActiveState is EditorState))
		{
			GlobalGameStateManager.CleanAndPushState(GameStateManager.Current.CreateState<EditorState>());
			return true;
		}
		return false;
	}

	private bool CheckAssemblyForMissionMethods(Assembly assembly)
	{
		Assembly assembly2 = Assembly.GetAssembly(typeof(MissionMethod));
		if (assembly == assembly2)
		{
			return true;
		}
		AssemblyName[] referencedAssemblies = assembly.GetReferencedAssemblies();
		for (int i = 0; i < referencedAssemblies.Length; i++)
		{
			if (referencedAssemblies[i].FullName == assembly2.FullName)
			{
				return true;
			}
		}
		return false;
	}

	private void FindMissions()
	{
		MBDebug.Print("Searching Mission Methods");
		_missionInfos = new List<MissionInfo>();
		Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
		List<Type> list = new List<Type>();
		Assembly[] array = assemblies;
		foreach (Assembly assembly in array)
		{
			if (!CheckAssemblyForMissionMethods(assembly))
			{
				continue;
			}
			foreach (Type item in assembly.GetTypesSafe())
			{
				object[] customAttributesSafe = item.GetCustomAttributesSafe(typeof(MissionManager), inherit: true);
				if (customAttributesSafe != null && customAttributesSafe.Length != 0)
				{
					list.Add(item);
				}
			}
		}
		MBDebug.Print("Found " + list.Count + " mission managers");
		foreach (Type item2 in list)
		{
			MethodInfo[] methods = item2.GetMethods(BindingFlags.Static | BindingFlags.Public);
			foreach (MethodInfo methodInfo in methods)
			{
				object[] customAttributesSafe2 = methodInfo.GetCustomAttributesSafe(typeof(MissionMethod), inherit: true);
				if (customAttributesSafe2 != null && customAttributesSafe2.Length != 0)
				{
					MissionMethod missionMethod = customAttributesSafe2[0] as MissionMethod;
					MissionInfo missionInfo = new MissionInfo();
					missionInfo.Creator = methodInfo;
					missionInfo.Manager = item2;
					missionInfo.UsableByEditor = missionMethod.UsableByEditor;
					missionInfo.Name = methodInfo.Name;
					if (missionInfo.Name.StartsWith("Open"))
					{
						missionInfo.Name = missionInfo.Name.Substring(4);
					}
					if (missionInfo.Name.EndsWith("Mission"))
					{
						missionInfo.Name = missionInfo.Name.Substring(0, missionInfo.Name.Length - 7);
					}
					missionInfo.Name = missionInfo.Name + "[" + item2.Name + "]";
					_missionInfos.Add(missionInfo);
				}
			}
		}
		MBDebug.Print("Found " + _missionInfos.Count + " missions");
	}

	[MBCallback(null, false)]
	internal string GetMissionControllerClassNames()
	{
		string text = "";
		for (int i = 0; i < _missionInfos.Count; i++)
		{
			MissionInfo missionInfo = _missionInfos[i];
			if (missionInfo.UsableByEditor)
			{
				text += missionInfo.Name;
				if (i + 1 != _missionInfos.Count)
				{
					text += " ";
				}
			}
		}
		return text;
	}

	private void LoadPlatformServices()
	{
		IPlatformServices platformServices = null;
		Assembly assembly = null;
		string fullModulePath = EngineApplicationInterface.IUtil.GetFullModulePath("Native");
		PlatformInitParams platformInitParams = new PlatformInitParams();
		if (ApplicationPlatform.CurrentPlatform == Platform.WindowsSteam)
		{
			assembly = AssemblyLoader.LoadFrom(ManagedDllFolder.Name + "TaleWorlds.PlatformService.Steam.dll");
		}
		else if (ApplicationPlatform.CurrentPlatform == Platform.WindowsEpic)
		{
			assembly = AssemblyLoader.LoadFrom(ManagedDllFolder.Name + "TaleWorlds.PlatformService.Epic.dll");
			platformInitParams.Add("PlatformInterface", StartupInfo.PlatformInterface);
			platformInitParams.Add("EpicUserId", StartupInfo.EpicUserId);
			platformInitParams.Add("EpicUserName", StartupInfo.EpicUserName);
		}
		else if (ApplicationPlatform.CurrentPlatform == Platform.WindowsGOG)
		{
			assembly = AssemblyLoader.LoadFrom(ManagedDllFolder.Name + "TaleWorlds.PlatformService.GOG.dll");
			platformInitParams.Add("AchievementDataXmlPath", System.IO.Path.Combine(fullModulePath, "ModuleData", "AchievementData", "gog_achievement_data.xml"));
		}
		else if (ApplicationPlatform.CurrentPlatform == Platform.GDKDesktop || ApplicationPlatform.CurrentPlatform == Platform.Durango)
		{
			assembly = AssemblyLoader.LoadFrom(ManagedDllFolder.Name + "TaleWorlds.PlatformService.GDK.dll");
		}
		else if (ApplicationPlatform.CurrentPlatform == Platform.Orbis)
		{
			assembly = AssemblyLoader.LoadFrom(ManagedDllFolder.Name + "TaleWorlds.PlatformService.PS.dll");
			platformInitParams.Add("AchievementDataXmlPath", System.IO.Path.Combine(fullModulePath, "ModuleData", "AchievementData", "ps_achievement_data.xml"));
		}
		else if (ApplicationPlatform.CurrentPlatform == Platform.WindowsNoPlatform)
		{
			string userName = "TestUser" + DateTime.Now.Ticks % 10000;
			if (!string.IsNullOrEmpty(StartupInfo.OverridenUserName))
			{
				userName = StartupInfo.OverridenUserName;
			}
			platformServices = new TestPlatformServices(userName);
		}
		if (assembly != null)
		{
			List<Type> typesSafe = assembly.GetTypesSafe();
			Type type = null;
			foreach (Type item in typesSafe)
			{
				if (item.GetInterfaces().Contains(typeof(IPlatformServices)))
				{
					type = item;
					break;
				}
			}
			platformServices = (IPlatformServices)type.GetConstructor(new Type[1] { typeof(PlatformInitParams) }).Invoke(new object[1] { platformInitParams });
		}
		if (platformServices != null)
		{
			PlatformServices.Setup(platformServices);
			PlatformServices.OnSessionInvitationAccepted = (Action<SessionInvitationType>)Delegate.Combine(PlatformServices.OnSessionInvitationAccepted, new Action<SessionInvitationType>(OnSessionInvitationAccepted));
			PlatformServices.OnPlatformRequestedMultiplayer = (Action)Delegate.Combine(PlatformServices.OnPlatformRequestedMultiplayer, new Action(OnPlatformRequestedMultiplayer));
			BannerlordFriendListService bannerlordFriendListService = new BannerlordFriendListService();
			ClanFriendListService clanFriendListService = new ClanFriendListService();
			RecentPlayersFriendListService recentPlayersFriendListService = new RecentPlayersFriendListService();
			PlatformServices.Initialize(new IFriendListService[3] { bannerlordFriendListService, clanFriendListService, recentPlayersFriendListService });
			AchievementManager.AchievementService = platformServices.GetAchievementService();
			ActivityManager.ActivityService = platformServices.GetActivityService();
		}
	}

	private void OnSessionInvitationAccepted(SessionInvitationType targetGameType)
	{
		if (targetGameType == SessionInvitationType.Multiplayer)
		{
			if (IsOnlyCoreContentEnabled)
			{
				PlatformServices.OnSessionInvitationHandled();
			}
			else
			{
				JobManager.AddJob(new OnSessionInvitationAcceptedJob(targetGameType));
			}
		}
	}

	private void OnPlatformRequestedMultiplayer()
	{
		if (IsOnlyCoreContentEnabled)
		{
			PlatformServices.OnPlatformMultiplayerRequestHandled();
		}
		else
		{
			JobManager.AddJob(new OnPlatformRequestedMultiplayerJob());
		}
	}

	private void LoadSubModules(List<ModuleInfo> modules, bool loadNewModules)
	{
		MBDebug.Print("Loading submodules...");
		foreach (ModuleInfo module in modules)
		{
			XmlResource.GetMbprojxmls(module.Id);
			XmlResource.GetXmlListAndApply(module.Id);
		}
		List<SubModuleInfo> list = new List<SubModuleInfo>();
		new List<ModuleInfo>();
		foreach (ModuleInfo module2 in modules)
		{
			foreach (SubModuleInfo subModule in module2.SubModules)
			{
				if (!CheckIfSubmoduleCanBeLoadable(subModule) || _subModuleBases.ContainsKey(subModule))
				{
					continue;
				}
				string path = System.IO.Path.Combine(module2.FolderPath, "bin", Common.ConfigName);
				string text = System.IO.Path.Combine(path, subModule.DLLName);
				string text2 = ManagedDllFolder.Name + subModule.DLLName;
				MBList<(string, AssemblyLoader.AssemblyLoadResult)> mBList = new MBList<(string, AssemblyLoader.AssemblyLoadResult)>();
				foreach (string assembly in subModule.Assemblies)
				{
					string text3 = System.IO.Path.Combine(path, assembly);
					string text4 = ManagedDllFolder.Name + assembly;
					AssemblyLoader.LoadFrom(File.Exists(text3) ? text3 : text4, out var result);
					if (result != AssemblyLoader.AssemblyLoadResult.Success)
					{
						mBList.Add((assembly, result));
					}
				}
				string text5 = (File.Exists(text) ? text : (File.Exists(text2) ? text2 : string.Empty));
				AssemblyLoader.AssemblyLoadResult result2 = AssemblyLoader.AssemblyLoadResult.Success;
				if (!string.IsNullOrEmpty(text5))
				{
					Assembly subModuleAssembly = AssemblyLoader.LoadFrom(text5, out result2);
					if (result2 != AssemblyLoader.AssemblyLoadResult.CriticalError)
					{
						result2 = AddSubModule(subModule, subModuleAssembly);
						if (result2 == AssemblyLoader.AssemblyLoadResult.Success && loadNewModules)
						{
							list.Add(subModule);
						}
					}
					if (result2 != AssemblyLoader.AssemblyLoadResult.Success)
					{
						HandleSubmoduleLoadError(module2, subModule, result2, mBList);
					}
					else if (mBList.Count > 0)
					{
						HandleSubmoduleLoadError(module2, null, AssemblyLoader.AssemblyLoadResult.LoadedWithErrors, mBList);
					}
				}
				else
				{
					string lpText = "Cannot find: " + text;
					string lpCaption = "Error";
					TaleWorlds.Library.Debug.ShowMessageBox(lpText, lpCaption, 4u);
				}
			}
		}
		if (loadNewModules)
		{
			foreach (SubModuleInfo item in list)
			{
				MBSubModuleBase value = null;
				if (_subModuleBases.TryGetValue(item, out value))
				{
					value.OnSubModuleLoad();
				}
			}
			OnNewModuleLoaded();
		}
		else
		{
			InitializeSubModuleBases();
		}
	}

	private void HandleSubmoduleLoadError(ModuleInfo module, SubModuleInfo subModule, AssemblyLoader.AssemblyLoadResult result, MBList<(string, AssemblyLoader.AssemblyLoadResult)> assemblyLoadResults)
	{
		TaleWorlds.Library.Debug.Print(module.Id + " could not be loaded correctly.");
		string lpCaption = "Error while loading " + module.Name;
		string assemblyLoadResultsMessage = GetAssemblyLoadResultsMessage(module, subModule, result, assemblyLoadResults);
		switch (result)
		{
		case AssemblyLoader.AssemblyLoadResult.CriticalError:
			TaleWorlds.Library.Debug.Print(assemblyLoadResultsMessage);
			if (!module.IsOfficial)
			{
				TaleWorlds.Library.Debug.ShowMessageBox(assemblyLoadResultsMessage, lpCaption, 256u);
			}
			throw new Exception();
		case AssemblyLoader.AssemblyLoadResult.LoadedWithErrors:
			TaleWorlds.Library.Debug.Print(assemblyLoadResultsMessage);
			if (!module.IsOfficial)
			{
				TaleWorlds.Library.Debug.ShowMessageBox(assemblyLoadResultsMessage, lpCaption, 256u);
			}
			break;
		}
	}

	private string GetAssemblyLoadResultsMessage(ModuleInfo module, SubModuleInfo subModule, AssemblyLoader.AssemblyLoadResult result, MBList<(string, AssemblyLoader.AssemblyLoadResult)> assemblyLoadResults)
	{
		string text = ((subModule != null) ? ("\"" + module.Name + "." + subModule.Name + "\" submodule") : ("\"" + module.Name + "\" module"));
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine(text + " could not be loaded correctly due to a dependency conflict. This may cause the game to experience stability issues.");
		return stringBuilder.ToString();
	}

	public Type GetSubModuleType(string name)
	{
		foreach (KeyValuePair<SubModuleInfo, MBSubModuleBase> subModuleBasis in _subModuleBases)
		{
			if (subModuleBasis.Key.SubModuleClassTypeName == name)
			{
				return subModuleBasis.Value.GetType();
			}
		}
		return null;
	}

	public bool CheckIfSubmoduleCanBeLoadable(SubModuleInfo subModuleInfo)
	{
		if (subModuleInfo.Tags.Count > 0)
		{
			foreach (Tuple<SubModuleInfo.SubModuleTags, string> tag in subModuleInfo.Tags)
			{
				if (!GetSubModuleValiditiy(tag.Item1, tag.Item2))
				{
					return false;
				}
			}
		}
		return true;
	}

	private bool GetSubModuleValiditiy(SubModuleInfo.SubModuleTags tag, string value)
	{
		Platform result;
		switch (tag)
		{
		case SubModuleInfo.SubModuleTags.ExclusivePlatform:
			if (Enum.TryParse<Platform>(value, out result))
			{
				return ApplicationPlatform.CurrentPlatform == result;
			}
			break;
		case SubModuleInfo.SubModuleTags.RejectedPlatform:
			if (Enum.TryParse<Platform>(value, out result))
			{
				return ApplicationPlatform.CurrentPlatform != result;
			}
			break;
		case SubModuleInfo.SubModuleTags.DedicatedServerType:
			switch (value.ToLower())
			{
			case "none":
				if (StartupInfo.DedicatedServerType == DedicatedServerType.None)
				{
					return true;
				}
				return false;
			case "both":
			case "all":
				if (StartupInfo.DedicatedServerType != DedicatedServerType.None)
				{
					return true;
				}
				return false;
			case "custom":
				if (StartupInfo.DedicatedServerType == DedicatedServerType.Custom)
				{
					return true;
				}
				return false;
			case "matchmaker":
				if (StartupInfo.DedicatedServerType == DedicatedServerType.Matchmaker)
				{
					return true;
				}
				return false;
			case "community":
				if (StartupInfo.DedicatedServerType == DedicatedServerType.Community)
				{
					return true;
				}
				return false;
			}
			break;
		case SubModuleInfo.SubModuleTags.IsNoRenderModeElement:
			return value.Equals("false");
		case SubModuleInfo.SubModuleTags.DependantRuntimeLibrary:
		{
			if (Enum.TryParse<Runtime>(value, out var result2))
			{
				return ApplicationPlatform.CurrentRuntimeLibrary == result2;
			}
			break;
		}
		case SubModuleInfo.SubModuleTags.PlayerHostedDedicatedServer:
		{
			string text = value.ToLower();
			if (StartupInfo.PlayerHostedDedicatedServer)
			{
				return text.Equals("true");
			}
			return text.Equals("false");
		}
		}
		return true;
	}

	[MBCallback(null, false)]
	internal static void MBThrowException()
	{
		TaleWorlds.Library.Debug.FailedAssert("MBThrowException", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade\\Module.cs", "MBThrowException", 1614);
	}

	[MBCallback(null, false)]
	internal void OnEnterEditMode(bool isFirstTime)
	{
	}

	[MBCallback(null, false)]
	internal static Module GetInstance()
	{
		return CurrentModule;
	}

	[MBCallback(null, false)]
	internal static string GetGameStatus()
	{
		if (TestCommonBase.BaseInstance != null)
		{
			return TestCommonBase.BaseInstance.GetGameStatus();
		}
		return "";
	}

	private void FinalizeModule()
	{
		if (Game.Current != null)
		{
			Game.Current.OnFinalize();
		}
		if (TestCommonBase.BaseInstance != null)
		{
			TestCommonBase.BaseInstance.OnFinalize();
		}
		_testContext.FinalizeContext();
		MBInformationManager.Clear();
		InformationManager.Clear();
		ScreenManager.OnFinalize();
		BannerlordConfig.Save();
		FinalizeSubModulesBases();
		PlatformServices.Instance?.Terminate();
		Common.MemoryCleanupGC();
		GC.WaitForPendingFinalizers();
	}

	internal static void FinalizeCurrentModule()
	{
		CurrentModule.FinalizeModule();
		CurrentModule = null;
	}

	[MBCallback(null, false)]
	internal void SetLoadingFinished()
	{
		LoadingFinished = true;
	}

	[MBCallback(null, false)]
	internal void OnCloseSceneEditorPresentation()
	{
		GameStateManager.Current.PopState();
	}

	[MBCallback(null, false)]
	internal void OnSceneEditorModeOver()
	{
		GameStateManager.Current.PopState();
	}

	private void OnConfigChanged()
	{
		foreach (MBSubModuleBase value in _subModuleBases.Values)
		{
			value.OnConfigChanged();
		}
	}

	private void OnConstrainedStateChange(bool isConstrained)
	{
		if (!isConstrained)
		{
			PlatformServices.Instance.OnFocusGained();
		}
	}

	private void OnFocusGained()
	{
		PlatformServices.Instance.OnFocusGained();
	}

	private void OnTextEnteredFromPlatform(string text)
	{
		ScreenManager.OnOnscreenKeyboardDone(text);
	}

	private void OnTextCanceledFromPlatform()
	{
		ScreenManager.OnOnscreenKeyboardCanceled();
	}

	[MBCallback(null, false)]
	internal void OnSkinsXMLHasChanged()
	{
		if (this.SkinsXMLHasChanged != null)
		{
			this.SkinsXMLHasChanged();
		}
	}

	[MBCallback(null, false)]
	internal void OnImguiProfilerTick()
	{
		if (this.ImguiProfilerTick != null)
		{
			this.ImguiProfilerTick();
		}
	}

	[MBCallback(null, false)]
	internal static string CreateProcessedSkinsXMLForNative(out string baseSkinsXmlPath)
	{
		List<string> usedPaths;
		XmlDocument mergedXmlForNative = MBObjectManager.GetMergedXmlForNative("soln_skins", out usedPaths);
		System.IO.StringWriter stringWriter = new System.IO.StringWriter();
		XmlTextWriter w = new XmlTextWriter(stringWriter);
		mergedXmlForNative.WriteTo(w);
		baseSkinsXmlPath = usedPaths[0];
		return stringWriter.ToString();
	}

	[MBCallback(null, false)]
	internal static string CreateProcessedItemHolstersXMLForNative(out string baseItemHolstersPath)
	{
		List<string> usedPaths;
		XmlDocument mergedXmlForNative = MBObjectManager.GetMergedXmlForNative("soln_item_holsters", out usedPaths);
		System.IO.StringWriter stringWriter = new System.IO.StringWriter();
		XmlTextWriter w = new XmlTextWriter(stringWriter);
		mergedXmlForNative.WriteTo(w);
		baseItemHolstersPath = usedPaths[0];
		return stringWriter.ToString();
	}

	[MBCallback(null, true)]
	internal static string CreateProcessedActionSetsXMLForNative()
	{
		XmlDocument mergedXmlForNative = MBObjectManager.GetMergedXmlForNative("soln_action_sets", out var _);
		Dictionary<string, XElement> dictionary = new Dictionary<string, XElement>();
		XDocument xDocument = MBObjectManager.ToXDocument(mergedXmlForNative);
		IEnumerable<XElement> source = xDocument.Descendants("action_set");
		for (int i = 0; i < source.Count(); i++)
		{
			XElement xElement = source.ElementAt(i);
			string key = xElement.FirstAttribute.ToString();
			if (dictionary.ContainsKey(key))
			{
				dictionary[key].Add(xElement.Descendants());
				xElement.Remove();
				i--;
			}
			else
			{
				dictionary.Add(key, xElement);
			}
		}
		mergedXmlForNative = MBObjectManager.ToXmlDocument(xDocument);
		System.IO.StringWriter stringWriter = new System.IO.StringWriter();
		XmlTextWriter w = new XmlTextWriter(stringWriter);
		mergedXmlForNative.WriteTo(w);
		return stringWriter.ToString();
	}

	[MBCallback(null, true)]
	internal static string CreateProcessedActionTypesXMLForNative()
	{
		List<string> usedPaths;
		XmlDocument mergedXmlForNative = MBObjectManager.GetMergedXmlForNative("soln_action_types", out usedPaths);
		System.IO.StringWriter stringWriter = new System.IO.StringWriter();
		XmlTextWriter w = new XmlTextWriter(stringWriter);
		mergedXmlForNative.WriteTo(w);
		return stringWriter.ToString();
	}

	[MBCallback(null, true)]
	internal static string CreateProcessedAnimationsXMLForNative(out string animationsXmlPaths)
	{
		List<string> usedPaths;
		XmlDocument mergedXmlForNative = MBObjectManager.GetMergedXmlForNative("soln_animations", out usedPaths);
		System.IO.StringWriter stringWriter = new System.IO.StringWriter();
		XmlTextWriter w = new XmlTextWriter(stringWriter);
		mergedXmlForNative.WriteTo(w);
		animationsXmlPaths = "";
		for (int i = 0; i < usedPaths.Count; i++)
		{
			animationsXmlPaths += usedPaths[i];
			if (i != usedPaths.Count - 1)
			{
				animationsXmlPaths += "\n";
			}
		}
		return stringWriter.ToString();
	}

	[MBCallback(null, true)]
	internal static string CreateProcessedVoiceDefinitionsXMLForNative()
	{
		XmlDocument mergedXmlForNative = MBObjectManager.GetMergedXmlForNative("soln_voice_definitions", out var _);
		XDocument xDocument = MBObjectManager.ToXDocument(mergedXmlForNative);
		XElement xElement = xDocument.Descendants("voice_type_declarations").First();
		int num;
		for (num = 1; num < xDocument.Descendants("voice_type_declarations").Count(); num++)
		{
			xElement.Add(xDocument.Descendants("voice_type_declarations").ElementAt(num).Descendants());
			xDocument.Descendants("voice_type_declarations").ElementAt(num).Remove();
			num--;
		}
		for (int i = 0; i < xDocument.Descendants("voice_definition").Count(); i++)
		{
			for (int j = i + 1; j < xDocument.Descendants("voice_definition").Count(); j++)
			{
				if (xDocument.Descendants("voice_definition").ElementAt(i).FirstAttribute.ToString() == xDocument.Descendants("voice_definition").ElementAt(j).FirstAttribute.ToString())
				{
					xDocument.Descendants("voice_definition").ElementAt(i).Add(xDocument.Descendants("voice_definition").ElementAt(j).Descendants());
					xDocument.Descendants("voice_definition").ElementAt(j).Remove();
					j--;
				}
			}
		}
		mergedXmlForNative = MBObjectManager.ToXmlDocument(xDocument);
		System.IO.StringWriter stringWriter = new System.IO.StringWriter();
		XmlTextWriter w = new XmlTextWriter(stringWriter);
		mergedXmlForNative.WriteTo(w);
		return stringWriter.ToString();
	}

	[MBCallback(null, true)]
	internal static string CreateProcessedSoundEventDataXMLForNative()
	{
		List<string> usedPaths;
		XmlDocument mergedXmlForNative = MBObjectManager.GetMergedXmlForNative("soln_sound_event_data", out usedPaths);
		System.IO.StringWriter stringWriter = new System.IO.StringWriter();
		XmlTextWriter w = new XmlTextWriter(stringWriter);
		mergedXmlForNative.WriteTo(w);
		return stringWriter.ToString();
	}

	[MBCallback(null, true)]
	internal static string CreateProcessedSoundParamsXMLForNative()
	{
		List<string> usedPaths;
		XmlDocument mergedXmlForNative = MBObjectManager.GetMergedXmlForNative("soln_sound_parameter_data", out usedPaths);
		System.IO.StringWriter stringWriter = new System.IO.StringWriter();
		XmlTextWriter w = new XmlTextWriter(stringWriter);
		mergedXmlForNative.WriteTo(w);
		return stringWriter.ToString();
	}

	[MBCallback(null, false)]
	internal static string CreateProcessedModuleDataXMLForNative(string xmlType)
	{
		List<string> usedPaths;
		XmlDocument xmlDocument = MBObjectManager.GetMergedXmlForNative("soln_" + xmlType, out usedPaths);
		if (xmlType == "full_movement_sets")
		{
			XDocument xDocument = MBObjectManager.ToXDocument(xmlDocument);
			for (int i = 0; i < xDocument.Descendants("full_movement_set").Count(); i++)
			{
				for (int j = i + 1; j < xDocument.Descendants("full_movement_set").Count(); j++)
				{
					if (xDocument.Descendants("full_movement_set").ElementAt(i).FirstAttribute.ToString() == xDocument.Descendants("full_movement_set").ElementAt(j).FirstAttribute.ToString())
					{
						xDocument.Descendants("full_movement_set").ElementAt(i).Add(xDocument.Descendants("full_movement_set").ElementAt(j).Descendants());
						xDocument.Descendants("full_movement_set").ElementAt(j).Remove();
						j--;
					}
				}
			}
			xmlDocument = MBObjectManager.ToXmlDocument(xDocument);
		}
		System.IO.StringWriter stringWriter = new System.IO.StringWriter();
		XmlTextWriter w = new XmlTextWriter(stringWriter);
		xmlDocument.WriteTo(w);
		return stringWriter.ToString();
	}

	public void ClearStateOptions()
	{
		_initialStateOptions.Clear();
	}

	public void AddInitialStateOption(InitialStateOption initialStateOption)
	{
		_initialStateOptions.Add(initialStateOption);
	}

	public void OverrideInitialStateOption(string id, InitialStateOption newInitialStateOption)
	{
		for (int i = 0; i < _initialStateOptions.Count; i++)
		{
			if (_initialStateOptions[i].Id == id)
			{
				_initialStateOptions[i] = newInitialStateOption;
				break;
			}
		}
	}

	public IEnumerable<InitialStateOption> GetInitialStateOptions()
	{
		return _initialStateOptions.OrderBy((InitialStateOption s) => s.OrderIndex);
	}

	public InitialStateOption GetInitialStateOptionWithId(string id)
	{
		foreach (InitialStateOption initialStateOption in _initialStateOptions)
		{
			if (initialStateOption.Id == id)
			{
				return initialStateOption;
			}
		}
		return null;
	}

	public void ExecuteInitialStateOptionWithId(string id)
	{
		GetInitialStateOptionWithId(id)?.DoAction();
	}

	public void SetCanLoadModules(bool canLoadModules)
	{
		EngineApplicationInterface.IUtil.SetCanLoadModules(canLoadModules);
	}

	void IGameStateManagerOwner.OnStateStackEmpty()
	{
	}

	void IGameStateManagerOwner.OnStateChanged(GameState oldState)
	{
	}

	public void SetEditorMissionTester(IEditorMissionTester editorMissionTester)
	{
		_editorMissionTester = editorMissionTester;
	}

	[MBCallback(null, false)]
	internal void StartMissionForEditor(string missionName, string sceneName, string levels)
	{
		if (_editorMissionTester != null)
		{
			_editorMissionTester.StartMissionForEditor(missionName, sceneName, levels);
		}
	}

	[MBCallback(null, false)]
	internal void StartMissionForReplayEditor(string missionName, string sceneName, string levels, string fileName, bool record, float startTime, float endTime)
	{
		if (_editorMissionTester != null)
		{
			_editorMissionTester.StartMissionForReplayEditor(missionName, sceneName, levels, fileName, record, startTime, endTime);
		}
	}

	public void StartMissionForEditorAux(string missionName, string sceneName, string levels, bool forReplay, string replayFileName, bool isRecord)
	{
		GameStateManager.Current = Game.Current.GameStateManager;
		ReturnToEditorState = true;
		MissionInfo missionInfo = _missionInfos.Find((MissionInfo mi) => mi.Name == missionName);
		if (missionInfo == null)
		{
			missionInfo = _missionInfos.Find((MissionInfo mi) => mi.Name.Contains(missionName));
		}
		if (forReplay)
		{
			missionInfo.Creator.Invoke(null, new object[2] { replayFileName, isRecord });
		}
		else
		{
			missionInfo.Creator.Invoke(null, new object[2] { sceneName, levels });
		}
	}

	private void FillMultiplayerGameTypes()
	{
		_multiplayerGameModesWithNames = new Dictionary<string, MultiplayerGameMode>();
		_multiplayerGameTypes = new MBList<MultiplayerGameTypeInfo>();
	}

	public MultiplayerGameMode GetMultiplayerGameMode(string gameType)
	{
		if (_multiplayerGameModesWithNames.TryGetValue(gameType, out var value))
		{
			return value;
		}
		return null;
	}

	public void AddMultiplayerGameMode(MultiplayerGameMode multiplayerGameMode)
	{
		_multiplayerGameModesWithNames.Add(multiplayerGameMode.Name, multiplayerGameMode);
		_multiplayerGameTypes.Add(new MultiplayerGameTypeInfo("Native", multiplayerGameMode.Name));
	}

	public MBReadOnlyList<MultiplayerGameTypeInfo> GetMultiplayerGameTypes()
	{
		return _multiplayerGameTypes;
	}

	public bool StartMultiplayerGame(string multiplayerGameType, string scene)
	{
		if (_multiplayerGameModesWithNames.TryGetValue(multiplayerGameType, out var value))
		{
			value.StartMultiplayerGame(scene);
			return true;
		}
		return false;
	}

	public async void ShutDownWithDelay(string reason, int seconds)
	{
		if (!_isShuttingDown)
		{
			_isShuttingDown = true;
			for (int i = 0; i < seconds; i++)
			{
				int num = seconds - i;
				string text = "Shutting down in " + num + " seconds with reason '" + reason + "'";
				TaleWorlds.Library.Debug.Print(text);
				Console.WriteLine(text);
				await Task.Delay(1000);
			}
			if (Game.Current != null)
			{
				TaleWorlds.Library.Debug.Print("Active game exist during ShutDownWithDelay");
				MBGameManager.EndGame();
			}
			Utilities.QuitGame();
		}
	}

	public void DeactiveModule(string moduleId)
	{
		ModuleInfo moduleInfo = ModuleHelper.GetModuleInfo(moduleId);
		if (moduleInfo == null || !moduleInfo.IsActive || moduleInfo.IsNative)
		{
			return;
		}
		TaleWorlds.Library.Debug.Print("Deactivating Module: " + moduleId, 0, TaleWorlds.Library.Debug.DebugColor.Green);
		ModuleHelper.OnModuleDeactivated(moduleId);
		foreach (SubModuleInfo subModule in moduleInfo.SubModules)
		{
			if (_subModuleBases.TryGetValue(subModule, out var value))
			{
				value.OnSubModuleDeactivated();
			}
		}
	}

	public void ActivateModule(string moduleId)
	{
		ModuleInfo moduleInfo = ModuleHelper.GetModuleInfo(moduleId);
		if (moduleInfo == null || moduleInfo.IsActive)
		{
			return;
		}
		TaleWorlds.Library.Debug.Print("Activating Module: " + moduleId, 0, TaleWorlds.Library.Debug.DebugColor.Green);
		ModuleHelper.OnModuleActivated(moduleId);
		foreach (SubModuleInfo subModule in moduleInfo.SubModules)
		{
			if (_subModuleBases.TryGetValue(subModule, out var value))
			{
				value.OnSubModuleActivated();
			}
		}
	}

	internal void OnBeforeGameStart(MBGameManager mbGameManager)
	{
		List<string> list = new List<string>();
		foreach (MBSubModuleBase value in _subModuleBases.Values)
		{
			value.OnBeforeGameStart(mbGameManager, list);
		}
		foreach (string item in list)
		{
			ModuleInfo moduleInfo = ModuleHelper.GetModuleInfo(item);
			if (moduleInfo != null && moduleInfo.IsActive)
			{
				DeactiveModule(item);
			}
		}
		MBList<ModuleInfo> mBList = new MBList<ModuleInfo>();
		foreach (ModuleInfo activeModule in ModuleHelper.GetActiveModules())
		{
			foreach (DependedModule dependedModule in activeModule.DependedModules)
			{
				foreach (string item2 in list)
				{
					ModuleInfo moduleInfo2 = ModuleHelper.GetModuleInfo(item2);
					if (moduleInfo2 != null && activeModule != moduleInfo2 && dependedModule.ModuleId == item2)
					{
						mBList.Add(moduleInfo2);
					}
				}
			}
		}
		if (mBList.Any((ModuleInfo x) => !x.IsOfficial))
		{
			string.Join("\n", from x in mBList
				where !x.IsOfficial
				select x.Name);
		}
		InformationManager.ClearAllMessages();
	}

	internal void OnGameEnd()
	{
		foreach (ModuleInfo module in ModuleHelper.GetModules((ModuleInfo x) => !x.IsActive))
		{
			ActivateModule(module.Id);
		}
	}
}
