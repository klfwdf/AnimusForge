using System;
using System.Collections.Generic;
using SandBox.AdvancedStartOptions;
using SandBox.View.Conversation;
using SandBox.View.Map;
using SandBox.View.Map.Managers;
using SandBox.View.Map.Visuals;
using SandBox.View.Missions.NameMarkers;
using SandBox.View.OrderProviders;
using SandBox.View.Overlay;
using SandBox.ViewModelCollection.Missions.NameMarker;
using Sandbox.View.GameStates;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.BattleWreckages;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.Inventory;
using TaleWorlds.CampaignSystem.Map;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Buildings;
using TaleWorlds.CampaignSystem.Settlements.Workshops;
using TaleWorlds.CampaignSystem.Siege;
using TaleWorlds.CampaignSystem.ViewModelCollection;
using TaleWorlds.CampaignSystem.ViewModelCollection.GameMenu.Overlay;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Core.ViewModelCollection.Information.RundownTooltip;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View.Tableaus;
using TaleWorlds.MountAndBlade.ViewModelCollection.Order.Visual;
using TaleWorlds.SaveSystem;
using TaleWorlds.SaveSystem.Load;
using TaleWorlds.ScreenSystem;

namespace SandBox.View;

public class SandBoxViewSubModule : MBSubModuleBase
{
	private bool _latestSaveLoaded;

	private TextObject _sandBoxAchievementsHint = new TextObject("{=j09m7S2E}Achievements are disabled in SandBox mode!");

	private bool _isInitialized;

	private HideoutVisualOrderProvider _hideoutVisualOrderProvider;

	private ConversationViewManager _conversationViewManager;

	private SandBoxViewVisualManager _sandBoxViewVisualManager;

	private IMapConversationDataProvider _mapConversationDataProvider;

	private IGameMenuOverlayProvider _gameMenuOverlayProvider;

	private Dictionary<UIntPtr, MapEntityVisual> _visualsOfEntities;

	private Dictionary<UIntPtr, Tuple<MatrixFrame, SettlementVisual>> _frameAndVisualOfEngines;

	private SandBox.AdvancedStartOptions.AdvancedStartOptions _startingOptionsCache;

	private static SandBoxViewSubModule _instance;

	public static SandBoxViewVisualManager SandBoxViewVisualManager => _instance._sandBoxViewVisualManager;

	public static ConversationViewManager ConversationViewManager => _instance._conversationViewManager;

	public static IMapConversationDataProvider MapConversationDataProvider => _instance._mapConversationDataProvider;

	internal static Dictionary<UIntPtr, MapEntityVisual> VisualsOfEntities => _instance._visualsOfEntities;

	internal static Dictionary<UIntPtr, Tuple<MatrixFrame, SettlementVisual>> FrameAndVisualOfEngines => _instance._frameAndVisualOfEngines;

	protected override void OnSubModuleLoad()
	{
		base.OnSubModuleLoad();
		_instance = this;
		RegisterTooltipTypes();
		Module.CurrentModule.AddInitialStateOption(new InitialStateOption("CampaignResumeGame", new TextObject("{=6mN03uTP}Saved Games"), 0, delegate
		{
			ScreenManager.PushScreen(SandBoxViewCreator.CreateSaveLoadScreen(isSaving: false));
		}, () => IsSavedGamesDisabled()));
		Module.CurrentModule.AddInitialStateOption(new InitialStateOption("ContinueCampaign", new TextObject("{=0tJ1oarX}Continue Campaign"), 1, delegate
		{
			PreloadState gameState = GameStateManager.Current.CreateState<PreloadState>(new object[1] { BannerlordConfig.LatestSaveGameName });
			GameStateManager.Current.PushState(gameState);
		}, () => IsContinueCampaignDisabled(BannerlordConfig.LatestSaveGameName)));
		Module.CurrentModule.AddInitialStateOption(new InitialStateOption("SandBoxNewGame", new TextObject("{=171fTtIN}SandBox"), 3, delegate
		{
			StartGame();
		}, () => IsSandboxDisabled(), _sandBoxAchievementsHint));
		SandBoxSaveHelper.OnStateChange += OnSaveHelperStateChange;
		Module.CurrentModule.ImguiProfilerTick += OnImguiProfilerTick;
		_gameMenuOverlayProvider = new DefaultGameMenuOverlayProvider();
		GameMenuOverlayFactory.RegisterProvider(_gameMenuOverlayProvider);
		MissionNameMarkerFactory.DefaultContext.AddProvider<DefaultMissionNameMarkerHandler>();
		MissionNameMarkerFactory.DefaultContext.AddProvider<StealthNameMarkerProvider>();
		_mapConversationDataProvider = new DefaultMapConversationDataProvider();
		_hideoutVisualOrderProvider = new HideoutVisualOrderProvider();
		VisualOrderFactory.RegisterProvider(_hideoutVisualOrderProvider);
	}

	protected override void OnSubModuleUnloaded()
	{
		Module.CurrentModule.ImguiProfilerTick -= OnImguiProfilerTick;
		SandBoxSaveHelper.OnStateChange -= OnSaveHelperStateChange;
		GameMenuOverlayFactory.UnregisterProvider(_gameMenuOverlayProvider);
		VisualOrderFactory.UnregisterProvider(_hideoutVisualOrderProvider);
		UnregisterTooltipTypes();
		_instance = null;
		base.OnSubModuleUnloaded();
	}

	protected override void OnApplicationTick(float dt)
	{
		base.OnApplicationTick(dt);
		if (!_isInitialized)
		{
			CampaignOptionsManager.Initialize();
			_isInitialized = true;
		}
		if (GameStateManager.Current?.ActiveState is MapState)
		{
			_startingOptionsCache = null;
		}
	}

	protected override void OnBeforeInitialModuleScreenSetAsRoot()
	{
		base.OnBeforeInitialModuleScreenSetAsRoot();
		SandBox.AdvancedStartOptions.AdvancedStartOptions startingOptionsCache = _startingOptionsCache;
		if (startingOptionsCache != null && startingOptionsCache.HasAnyChange())
		{
			ScreenManager.AddGlobalLayer(SandBoxViewCreator.CreateCampaignAdvancedStartOptions(_startingOptionsCache, OnStartingOptionsConfirmed, OnStartingOptionsClosed), isFocusable: true);
		}
	}

	public override void OnCampaignStart(Game game, object starterObject)
	{
		base.OnCampaignStart(game, starterObject);
		if (Campaign.Current != null)
		{
			_conversationViewManager = new ConversationViewManager();
			_sandBoxViewVisualManager = new SandBoxViewVisualManager();
		}
	}

	public override void OnGameLoaded(Game game, object initializerObject)
	{
		_conversationViewManager = new ConversationViewManager();
		_sandBoxViewVisualManager = new SandBoxViewVisualManager();
	}

	public override void OnAfterGameInitializationFinished(Game game, object starterObject)
	{
		base.OnAfterGameInitializationFinished(game, starterObject);
	}

	public override void BeginGameStart(Game game)
	{
		base.BeginGameStart(game);
		if (Campaign.Current != null)
		{
			_visualsOfEntities = new Dictionary<UIntPtr, MapEntityVisual>();
			_frameAndVisualOfEngines = new Dictionary<UIntPtr, Tuple<MatrixFrame, SettlementVisual>>();
			Campaign.Current.SaveHandler.MainHeroVisualSupplier = new MainHeroSaveVisualSupplier();
			ThumbnailCacheManager.InitializeSandboxValues();
		}
	}

	public override void OnGameEnd(Game game)
	{
		if (_visualsOfEntities != null)
		{
			foreach (MapEntityVisual value in _visualsOfEntities.Values)
			{
				value.ReleaseResources();
			}
		}
		_visualsOfEntities = null;
		_frameAndVisualOfEngines = null;
		_conversationViewManager = null;
		_sandBoxViewVisualManager = null;
		if (Campaign.Current != null)
		{
			Campaign.Current.SaveHandler.MainHeroVisualSupplier = null;
			ThumbnailCacheManager.ReleaseSandboxValues();
		}
	}

	private (bool, TextObject) IsSavedGamesDisabled()
	{
		if (Module.CurrentModule.IsOnlyCoreContentEnabled)
		{
			return (true, new TextObject("{=V8BXjyYq}Disabled during installation."));
		}
		if (MBSaveLoad.NumberOfCurrentSaves == 0)
		{
			return (true, new TextObject("{=XcVVE1mp}No saved games found."));
		}
		return (false, null);
	}

	private (bool, TextObject) IsContinueCampaignDisabled(string saveName)
	{
		if (Module.CurrentModule.IsOnlyCoreContentEnabled)
		{
			return (true, new TextObject("{=V8BXjyYq}Disabled during installation."));
		}
		if (string.IsNullOrEmpty(saveName))
		{
			return (true, new TextObject("{=aWMZQKXZ}Save the game at least once to continue"));
		}
		SaveGameFileInfo saveFileWithName = MBSaveLoad.GetSaveFileWithName(saveName);
		if (saveFileWithName == null)
		{
			return (true, new TextObject("{=60LTq0tQ}Can't find the save file for the latest save game."));
		}
		TextObject reason;
		return (SandBoxSaveHelper.GetIsDisabledWithReason(saveFileWithName, out reason), reason);
	}

	private (bool, TextObject) IsSandboxDisabled()
	{
		if (Module.CurrentModule.IsOnlyCoreContentEnabled)
		{
			return (true, new TextObject("{=V8BXjyYq}Disabled during installation."));
		}
		return (false, null);
	}

	private void OnStartingOptionsClosed()
	{
		_startingOptionsCache = null;
	}

	private void OnStartingOptionsConfirmed(SandBox.AdvancedStartOptions.AdvancedStartOptions options)
	{
		_startingOptionsCache = options;
	}

	private void ContinueCampaign(string saveName)
	{
		SandBoxSaveHelper.TryLoadSave(MBSaveLoad.GetSaveFileWithName(saveName), StartGame);
	}

	public override void OnInitialState()
	{
		base.OnInitialState();
		if (!Module.CurrentModule.StartupInfo.IsContinueGame || _latestSaveLoaded)
		{
			return;
		}
		_latestSaveLoaded = true;
		string latestSaveGameName = BannerlordConfig.LatestSaveGameName;
		if (!string.IsNullOrEmpty(latestSaveGameName))
		{
			(bool, TextObject) tuple = IsContinueCampaignDisabled(latestSaveGameName);
			if (!tuple.Item1)
			{
				ContinueCampaign(latestSaveGameName);
			}
			else
			{
				InformationManager.ShowInquiry(new InquiryData(new TextObject("{=oZrVNUOk}Error").ToString(), tuple.Item2.ToString(), isAffirmativeOptionShown: true, isNegativeOptionShown: false, new TextObject("{=yS7PvrTD}OK").ToString(), string.Empty, null, null));
			}
		}
	}

	private void StartGame(LoadResult loadResult)
	{
		MBGameManager.StartNewGame(new SandBoxGameManager(loadResult));
	}

	private void StartGame()
	{
		SandBox.AdvancedStartOptions.AdvancedStartOptions options = AdvancedStartOptionsManager.CreateCampaignStartOptions();
		if (!options.IsEmpty())
		{
			ScreenManager.AddGlobalLayer(SandBoxViewCreator.CreateCampaignAdvancedStartOptions(options, OnStartingOptionsConfirmed, OnStartingOptionsClosed), isFocusable: true);
			return;
		}
		MBGameManager.StartNewGame(new SandBoxGameManager(() => new Campaign(CampaignGameMode.Campaign, options.GetChangedOptions())));
	}

	private void OnImguiProfilerTick()
	{
		if (Campaign.Current == null)
		{
			return;
		}
		MBReadOnlyList<MobileParty> all = MobileParty.All;
		int num = 0;
		int num2 = 0;
		int num3 = 0;
		List<EntityVisualManagerBase<PartyBase>> components = SandBoxViewVisualManager.GetComponents<EntityVisualManagerBase<PartyBase>>();
		foreach (MobileParty item in all)
		{
			if (item.IsMilitia || item.IsGarrison)
			{
				continue;
			}
			if (item.IsVisible)
			{
				num++;
			}
			MapEntityVisual<PartyBase> mapEntityVisual = null;
			foreach (EntityVisualManagerBase<PartyBase> item2 in components)
			{
				MapEntityVisual<PartyBase> visualOfEntity = item2.GetVisualOfEntity(PartyBase.MainParty);
				if (visualOfEntity != null)
				{
					mapEntityVisual = visualOfEntity;
				}
			}
			if (mapEntityVisual == null)
			{
				continue;
			}
			if (mapEntityVisual is MobilePartyVisual mobilePartyVisual)
			{
				if (mobilePartyVisual.HumanAgentVisuals != null)
				{
					num2++;
				}
				if (mobilePartyVisual.MountAgentVisuals != null)
				{
					num2++;
				}
				if (mobilePartyVisual.CaravanMountAgentVisuals != null)
				{
					num2++;
				}
			}
			num3++;
		}
		Imgui.BeginMainThreadScope();
		Imgui.Begin("Bannerlord Campaign Statistics");
		Imgui.Columns(2);
		Imgui.Text("Name");
		Imgui.NextColumn();
		Imgui.Text("Count");
		Imgui.NextColumn();
		Imgui.Separator();
		Imgui.Text("Total Mobile Party");
		Imgui.NextColumn();
		Imgui.Text(num3.ToString());
		Imgui.NextColumn();
		Imgui.Text("Visible Mobile Party");
		Imgui.NextColumn();
		Imgui.Text(num.ToString());
		Imgui.NextColumn();
		Imgui.Text("Total Agent Visuals");
		Imgui.NextColumn();
		Imgui.Text(num2.ToString());
		Imgui.NextColumn();
		Imgui.End();
		Imgui.EndMainThreadScope();
	}

	private void RegisterTooltipTypes()
	{
		InformationManager.RegisterTooltip<List<MobileParty>, PropertyBasedTooltipVM>(TooltipRefresherCollection.RefreshEncounterTooltip, "PropertyBasedTooltip");
		InformationManager.RegisterTooltip<Track, PropertyBasedTooltipVM>(TooltipRefresherCollection.RefreshTrackTooltip, "PropertyBasedTooltip");
		InformationManager.RegisterTooltip<MapEvent, PropertyBasedTooltipVM>(TooltipRefresherCollection.RefreshMapEventTooltip, "PropertyBasedTooltip");
		InformationManager.RegisterTooltip<SiegeEvent, PropertyBasedTooltipVM>(TooltipRefresherCollection.RefreshSiegeEventTooltip, "PropertyBasedTooltip");
		InformationManager.RegisterTooltip<Army, PropertyBasedTooltipVM>(TooltipRefresherCollection.RefreshArmyTooltip, "PropertyBasedTooltip");
		InformationManager.RegisterTooltip<MobileParty, PropertyBasedTooltipVM>(TooltipRefresherCollection.RefreshMobilePartyTooltip, "PropertyBasedTooltip");
		InformationManager.RegisterTooltip<Hero, PropertyBasedTooltipVM>(TooltipRefresherCollection.RefreshHeroTooltip, "PropertyBasedTooltip");
		InformationManager.RegisterTooltip<Settlement, PropertyBasedTooltipVM>(TooltipRefresherCollection.RefreshSettlementTooltip, "PropertyBasedTooltip");
		InformationManager.RegisterTooltip<CharacterObject, PropertyBasedTooltipVM>(TooltipRefresherCollection.RefreshCharacterTooltip, "PropertyBasedTooltip");
		InformationManager.RegisterTooltip<WeaponDesignElement, PropertyBasedTooltipVM>(TooltipRefresherCollection.RefreshCraftingPartTooltip, "PropertyBasedTooltip");
		InformationManager.RegisterTooltip<InventoryLogic, PropertyBasedTooltipVM>(TooltipRefresherCollection.RefreshInventoryTooltip, "PropertyBasedTooltip");
		InformationManager.RegisterTooltip<ItemObject, PropertyBasedTooltipVM>(TooltipRefresherCollection.RefreshItemTooltip, "PropertyBasedTooltip");
		InformationManager.RegisterTooltip<Building, PropertyBasedTooltipVM>(TooltipRefresherCollection.RefreshBuildingTooltip, "PropertyBasedTooltip");
		InformationManager.RegisterTooltip<Workshop, PropertyBasedTooltipVM>(TooltipRefresherCollection.RefreshWorkshopTooltip, "PropertyBasedTooltip");
		InformationManager.RegisterTooltip<Clan, PropertyBasedTooltipVM>(TooltipRefresherCollection.RefreshClanTooltip, "PropertyBasedTooltip");
		InformationManager.RegisterTooltip<Kingdom, PropertyBasedTooltipVM>(TooltipRefresherCollection.RefreshKingdomTooltip, "PropertyBasedTooltip");
		InformationManager.RegisterTooltip<MapMarker, PropertyBasedTooltipVM>(TooltipRefresherCollection.RefreshMapMarkerTooltip, "PropertyBasedTooltip");
		InformationManager.RegisterTooltip<ExplainedNumber, RundownTooltipVM>(TooltipRefresherCollection.RefreshExplainedNumberTooltip, "RundownTooltip");
		InformationManager.RegisterTooltip<BattleWreckage, PropertyBasedTooltipVM>(TooltipRefresherCollection.RefreshBattleWreckageTooltip, "PropertyBasedTooltip");
	}

	private void UnregisterTooltipTypes()
	{
		InformationManager.UnregisterTooltip<List<MobileParty>>();
		InformationManager.UnregisterTooltip<Track>();
		InformationManager.UnregisterTooltip<MapEvent>();
		InformationManager.UnregisterTooltip<Army>();
		InformationManager.UnregisterTooltip<MobileParty>();
		InformationManager.UnregisterTooltip<Hero>();
		InformationManager.UnregisterTooltip<Settlement>();
		InformationManager.UnregisterTooltip<CharacterObject>();
		InformationManager.UnregisterTooltip<WeaponDesignElement>();
		InformationManager.UnregisterTooltip<InventoryLogic>();
		InformationManager.UnregisterTooltip<ItemObject>();
		InformationManager.UnregisterTooltip<Building>();
		InformationManager.UnregisterTooltip<Workshop>();
		InformationManager.UnregisterTooltip<Clan>();
		InformationManager.UnregisterTooltip<Kingdom>();
		InformationManager.UnregisterTooltip<BattleWreckage>();
		InformationManager.UnregisterTooltip<ExplainedNumber>();
	}

	public static void SetMapConversationDataProvider(IMapConversationDataProvider mapConversationDataProvider)
	{
		_instance._mapConversationDataProvider = mapConversationDataProvider;
	}

	private static void OnSaveHelperStateChange(SandBoxSaveHelper.SaveHelperState currentState)
	{
		switch (currentState)
		{
		case SandBoxSaveHelper.SaveHelperState.Start:
		case SandBoxSaveHelper.SaveHelperState.LoadGame:
			LoadingWindow.EnableGlobalLoadingWindow();
			break;
		case SandBoxSaveHelper.SaveHelperState.Inquiry:
			LoadingWindow.DisableGlobalLoadingWindow();
			break;
		default:
			Debug.FailedAssert("Undefined save state for listener!", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\SandBox.View\\SandBoxViewSubModule.cs", "OnSaveHelperStateChange", 728);
			break;
		}
	}
}
