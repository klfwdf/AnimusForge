using System;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Engine.InputSystem;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade.GameKeyCategory;
using TaleWorlds.MountAndBlade.View.CustomBattle;
using TaleWorlds.MountAndBlade.View.Screens;
using TaleWorlds.MountAndBlade.View.Tableaus;
using TaleWorlds.MountAndBlade.View.Tableaus.Thumbnails;
using TaleWorlds.MountAndBlade.View.VisualOrders;
using TaleWorlds.MountAndBlade.ViewModelCollection.Order.Visual;
using TaleWorlds.ScreenSystem;

namespace TaleWorlds.MountAndBlade.View;

public class ViewSubModule : MBSubModuleBase
{
	private Dictionary<Tuple<Material, Banner>, Material> _bannerTexturedMaterialCache;

	private GameStateScreenManager _gameStateScreenManager;

	private bool _newGameInitialization;

	private VisualOrderProvider _visualOrderProvider;

	private static ViewSubModule _instance;

	private bool _initialized;

	private DLCInstallationQueryView _dlcInstallationQueryView;

	public static Dictionary<Tuple<Material, Banner>, Material> BannerTexturedMaterialCache
	{
		get
		{
			return _instance._bannerTexturedMaterialCache;
		}
		set
		{
			_instance._bannerTexturedMaterialCache = value;
		}
	}

	public static GameStateScreenManager GameStateScreenManager => _instance._gameStateScreenManager;

	private void InitializeHotKeyManager()
	{
		string fileName = "BannerlordGameKeys.xml";
		HotKeyManager.Initialize(new PlatformFilePath(EngineFilePaths.ConfigsPath, fileName), !ScreenManager.IsEnterButtonRDown);
		HotKeyManager.RegisterInitialContexts(new List<GameKeyContext>
		{
			new GenericGameKeyContext(),
			new GenericCampaignPanelsGameKeyCategory(),
			new GenericPanelGameKeyCategory(),
			new ArmyManagementHotkeyCategory(),
			new BoardGameHotkeyCategory(),
			new ChatLogHotKeyCategory(),
			new CombatHotKeyCategory(),
			new CraftingHotkeyCategory(),
			new FaceGenHotkeyCategory(),
			new InventoryHotKeyCategory(),
			new PartyHotKeyCategory(),
			new MapHotKeyCategory(),
			new MapNotificationHotKeyCategory(),
			new MissionOrderHotkeyCategory(),
			new OrderOfBattleHotKeyCategory(),
			new MultiplayerHotkeyCategory(),
			new ScoreboardHotKeyCategory(),
			new ConversationHotKeyCategory(),
			new CheatsHotKeyCategory(),
			new PhotoModeHotKeyCategory(),
			new PollHotkeyCategory()
		});
	}

	private void InitializeBannerVisualManager()
	{
		if (BannerManager.Instance == null)
		{
			BannerManager.Initialize();
			BannerManager.Instance.LoadBannerIcons();
		}
	}

	protected override void OnSubModuleLoad()
	{
		base.OnSubModuleLoad();
		_instance = this;
		InitializeHotKeyManager();
		InitializeBannerVisualManager();
		CraftedDataViewManager.Initialize();
		_visualOrderProvider = new DefaultVisualOrderProvider();
		VisualOrderFactory.RegisterProvider(_visualOrderProvider);
		_gameStateScreenManager = new GameStateScreenManager();
		Module.CurrentModule.GlobalGameStateManager.RegisterListener(_gameStateScreenManager);
		MBMusicManager.Create();
		TextObject coreContentDisabledReason = new TextObject("{=V8BXjyYq}Disabled during installation.");
		if (Utilities.EditModeEnabled)
		{
			Module.CurrentModule.AddInitialStateOption(new InitialStateOption("Editor", new TextObject("{=bUh0x6rA}Editor"), -1, delegate
			{
				MBInitialScreenBase.OnEditModeEnterPress();
			}, () => (Module.CurrentModule.IsOnlyCoreContentEnabled, coreContentDisabledReason)));
		}
		Module.CurrentModule.AddInitialStateOption(new InitialStateOption("CustomBattle", new TextObject("{=4gOGGbeQ}Custom Battle"), 5000, CustomBattleFactory.StartCustomBattle, () => ((bool, TextObject))(false, null), null, () => CustomBattleFactory.GetProviderCount() == 0));
		Module.CurrentModule.AddInitialStateOption(new InitialStateOption("Options", new TextObject("{=NqarFr4P}Options"), 9998, delegate
		{
			ScreenManager.PushScreen(ViewCreator.CreateOptionsScreen(fromMainMenu: true));
		}, () => ((bool, TextObject))(false, null)));
		Module.CurrentModule.AddInitialStateOption(new InitialStateOption("Credits", new TextObject("{=ODQmOrIw}Credits"), 9999, delegate
		{
			ScreenManager.PushScreen(ViewCreator.CreateCreditsScreen());
		}, () => ((bool, TextObject))(false, null)));
		Module.CurrentModule.AddInitialStateOption(new InitialStateOption("Exit", new TextObject("{=YbpzLHzk}Exit Game"), 10000, delegate
		{
			MBInitialScreenBase.DoExitButtonAction();
		}, () => (Module.CurrentModule.IsOnlyCoreContentEnabled, coreContentDisabledReason)));
		ViewModel.RefreshPropertyAndMethodInfos();
		Module.CurrentModule.ImguiProfilerTick += OnImguiProfilerTick;
		ScreenManager.OnPushScreen += OnScreenManagerPushScreen;
		EngineController.OnConstrainedStateChanged += OnConstrainedStateChange;
		HyperlinkTexts.IsPlayStationGamepadActive = GetIsPlaystationGamepadActive;
		_dlcInstallationQueryView = new DLCInstallationQueryView();
		_dlcInstallationQueryView.Initialize();
	}

	private void OnModuleStructureChanged()
	{
		ViewModel.RefreshPropertyAndMethodInfos();
	}

	private void OnConstrainedStateChange(bool isConstrained)
	{
		ScreenManager.OnConstrainStateChanged(isConstrained);
	}

	private bool GetIsPlaystationGamepadActive()
	{
		bool flag = Input.ControllerType.IsPlaystation();
		return Input.IsGamepadActive && flag;
	}

	protected override void OnSubModuleUnloaded()
	{
		_dlcInstallationQueryView?.OnFinalize();
		_dlcInstallationQueryView = null;
		VisualOrderFactory.UnregisterProvider(_visualOrderProvider);
		ThumbnailCacheManager.ClearManager();
		BannerlordTableauManager.ClearManager();
		CraftedDataViewManager.Clear();
		Module.CurrentModule.ImguiProfilerTick -= OnImguiProfilerTick;
		ScreenManager.OnPushScreen -= OnScreenManagerPushScreen;
		EngineController.OnConstrainedStateChanged -= OnConstrainedStateChange;
		_instance = null;
		base.OnSubModuleUnloaded();
	}

	protected override void OnBeforeInitialModuleScreenSetAsRoot()
	{
		if (_initialized)
		{
			BannerPersistentTextureCache.Current?.FlushCache();
		}
		if (!_initialized)
		{
			BannerlordTableauManager.InitializeCharacterTableauRenderSystem();
			ThumbnailCacheManager.InitializeManager();
			ThumbnailCacheManager.Current.RegisterThumbnailCache(new AvatarThumbnailCache(75));
			ThumbnailCacheManager.Current.RegisterThumbnailCache(new BannerThumbnailCache(100));
			ThumbnailCacheManager.Current.RegisterThumbnailCache(new BannerPersistentTextureCache());
			ThumbnailCacheManager.Current.RegisterThumbnailCache(new BannerEditorTextureCache(5));
			ThumbnailCacheManager.Current.RegisterThumbnailCache(new CharacterThumbnailCache(75));
			ThumbnailCacheManager.Current.RegisterThumbnailCache(new CraftingPieceThumbnailCache(75));
			ThumbnailCacheManager.Current.RegisterThumbnailCache(new ItemThumbnailCache(75));
			_initialized = true;
		}
	}

	protected override void OnNewModuleLoad()
	{
		ViewCreatorManager.CollectTypes();
		ViewModel.RefreshPropertyAndMethodInfos();
		_gameStateScreenManager.CollectTypes();
	}

	protected override void OnApplicationTick(float dt)
	{
		base.OnApplicationTick(dt);
		if (Input.DebugInput.IsHotKeyPressed("ToggleUI"))
		{
			MBDebug.DisableUI(new List<string>());
		}
		HotKeyManager.Tick(dt);
		MBMusicManager.Current?.Update(dt);
		ThumbnailCacheManager.Current?.Tick(dt);
	}

	protected override void AfterAsyncTickTick(float dt)
	{
		MBMusicManager.Current?.Update(dt);
	}

	protected override void OnGameStart(Game game, IGameStarter gameStarterObject)
	{
		MissionWeapon.OnGetWeaponDataHandler = ItemCollectionElementViewExtensions.OnGetWeaponData;
	}

	public override void OnCampaignStart(Game game, object starterObject)
	{
		Game.Current.GameStateManager.RegisterListener(_gameStateScreenManager);
		_newGameInitialization = false;
	}

	public override void OnMultiplayerGameStart(Game game, object starterObject)
	{
		Game.Current.GameStateManager.RegisterListener(_gameStateScreenManager);
	}

	public override void OnGameLoaded(Game game, object initializerObject)
	{
		Game.Current.GameStateManager.RegisterListener(_gameStateScreenManager);
	}

	public override void OnGameInitializationFinished(Game game)
	{
		base.OnGameInitializationFinished(game);
		foreach (ItemObject objectType in Game.Current.ObjectManager.GetObjectTypeList<ItemObject>())
		{
			if (objectType.MultiMeshName != "")
			{
				MBUnusedResourceManager.SetMeshUsed(objectType.MultiMeshName);
			}
			HorseComponent horseComponent = objectType.HorseComponent;
			if (horseComponent != null)
			{
				foreach (KeyValuePair<string, bool> additionalMeshesName in horseComponent.AdditionalMeshesNameList)
				{
					MBUnusedResourceManager.SetMeshUsed(additionalMeshesName.Key);
				}
			}
			if (objectType.PrimaryWeapon != null)
			{
				MBUnusedResourceManager.SetMeshUsed(objectType.HolsterMeshName);
				MBUnusedResourceManager.SetMeshUsed(objectType.HolsterWithWeaponMeshName);
				MBUnusedResourceManager.SetMeshUsed(objectType.FlyingMeshName);
				MBUnusedResourceManager.SetBodyUsed(objectType.BodyName);
				MBUnusedResourceManager.SetBodyUsed(objectType.HolsterBodyName);
				MBUnusedResourceManager.SetBodyUsed(objectType.CollisionBodyName);
			}
		}
	}

	public override void BeginGameStart(Game game)
	{
		base.BeginGameStart(game);
		Game.Current.BannerVisualCreator = new BannerVisualCreator();
	}

	public override bool DoLoading(Game game)
	{
		if (_newGameInitialization)
		{
			return true;
		}
		_newGameInitialization = true;
		return _newGameInitialization;
	}

	public override void OnGameEnd(Game game)
	{
		MissionWeapon.OnGetWeaponDataHandler = null;
		CraftedDataViewManager.Clear();
	}

	private void OnImguiProfilerTick()
	{
	}

	private void OnScreenManagerPushScreen(ScreenBase pushedScreen)
	{
	}
}
