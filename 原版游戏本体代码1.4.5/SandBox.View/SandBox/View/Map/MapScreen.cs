using System;
using System.Collections.Generic;
using System.Linq;
using Helpers;
using SandBox.Missions.BattleScore;
using SandBox.View.Map.Managers;
using SandBox.View.Map.Visuals;
using SandBox.View.Menu;
using SandBox.ViewModelCollection;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Election;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.Incidents;
using TaleWorlds.CampaignSystem.Map;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Siege;
using TaleWorlds.CampaignSystem.ViewModelCollection;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Engine.Options;
using TaleWorlds.Engine.Screens;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Library.EventSystem;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.Screens;
using TaleWorlds.MountAndBlade.View.Scripts;
using TaleWorlds.MountAndBlade.View.Tableaus;
using TaleWorlds.MountAndBlade.View.Tableaus.Thumbnails;
using TaleWorlds.MountAndBlade.ViewModelCollection.EscapeMenu;
using TaleWorlds.ObjectSystem;
using TaleWorlds.ScreenSystem;

namespace SandBox.View.Map;

[GameStateScreen(typeof(MapState))]
public class MapScreen : ScreenBase, IMapStateHandler, IGameStateListener, IChatLogHandlerScreen
{
	public enum MapOverlayType
	{
		None,
		Army
	}

	public struct DecalEntity
	{
		public GameEntity GameEntity { get; set; }

		public Decal Decal { get; set; }

		public DecalEntity(GameEntity gameEntity, Decal decal)
		{
			GameEntity = gameEntity;
			Decal = decal;
		}

		public static DecalEntity Create(Scene scene, string material, string entityName = null)
		{
			GameEntity gameEntity = GameEntity.CreateEmpty(scene);
			gameEntity.Name = entityName ?? "Entity";
			Decal decal = Decal.CreateDecal();
			Material fromResource = Material.GetFromResource(material);
			if (fromResource != null)
			{
				decal.SetMaterial(fromResource);
			}
			scene.AddDecalInstance(decal, "editor_set", deletable: false);
			gameEntity.AddComponent(decal);
			return new DecalEntity(gameEntity, decal);
		}
	}

	private struct MouseInputState
	{
		public bool IsLeftMouseDown;

		public bool IsLeftMousePressed;

		public bool IsLeftMouseReleased;

		public bool IsMiddleMouseDown;

		public bool IsMiddleMousePressed;

		public bool IsMiddleMouseReleased;

		public bool IsRightMouseDown;

		public bool IsRightMousePressed;

		public bool IsRightMouseReleased;
	}

	public class MainMapCameraMoveEvent : EventBase
	{
		public bool RotationChanged { get; private set; }

		public bool PositionChanged { get; private set; }

		public MainMapCameraMoveEvent(bool rotationChanged, bool positionChanged)
		{
			RotationChanged = rotationChanged;
			PositionChanged = positionChanged;
		}
	}

	private const float DoubleClickTimeLimit = 0.3f;

	private INavigationHandler _navigationHandler;

	private const int _frameDelayAmountForRenderActivation = 5;

	private MenuViewContext _menuViewContext;

	private MenuContext _latestMenuContext;

	public readonly Dictionary<Tuple<Material, Banner>, Material> CharacterBannerMaterialCache = new Dictionary<Tuple<Material, Banner>, Material>();

	private bool _partyIconNeedsRefreshing;

	private uint _tooltipTargetHash;

	private object _tooltipTargetObject;

	private MapViewsContainer _mapViewsContainer;

	private MapView _encounterOverlay;

	public static bool DisableVisualTicks;

	private MapReadyView _mapReadyView;

	private MapView _armyOverlay;

	public IMapTracksCampaignBehavior MapTracksCampaignBehavior;

	private double _lastReleaseTime;

	private double _lastPressTime;

	private MapView _marriageOfferPopupView;

	private Vec3 _clickedPosition;

	private Vec2 _clickedPositionPixel;

	private double _secondLastPressTime;

	private bool _leftButtonDoubleClickOnSceneWidget;

	private bool _ignoreNextTimeToggle;

	private MapView _heirSelectionPopupView;

	private Ray _mouseRay;

	private float _timeToggleTimer = float.MaxValue;

	private float _waitForDoubleClickUntilTime;

	private MapView _campaignOptionsView;

	private MapView _mapCheatsView;

	private MapView _battleSimulationView;

	private MapView _escapeMenuView;

	private bool _leftButtonDraggingMode;

	private MapConversationView _conversationView;

	private MapEntityVisual _preVisualOfSelectedEntity;

	private Vec2 _oldMousePosition;

	private int _activatedFrameNo = Utilities.EngineFrameNo;

	private bool _exitOnSaveOver;

	private bool _isSceneViewEnabled;

	private bool _isReadyForRender;

	private bool _gpuMemoryCleared;

	private bool _focusLost;

	private bool _isKingdomDecisionsDirty;

	private float _cheatPressTimer;

	private DecalEntity _pointTargetWindDirectionDecal;

	private DecalEntity _pointTargetInnerDecal;

	private DecalEntity _pointTargetOuterDecal;

	private DecalEntity _partyHoverOutlineDecal;

	private DecalEntity _townCircleDecal;

	private DecalEntity _settlementHoverOutlineDecal;

	private float _targetCircleRotationStartTime;

	private float _soundCalculationTime;

	private const float SoundCalculationInterval = 0.2f;

	private Dictionary<Tuple<Material, Banner>, Material> _bannerTexturedMaterialCache;

	public const uint EnemyPartyDecalColor = 4292093218u;

	public const uint SameFactionPartyDecalColor = 4284183827u;

	public const uint NeutralPartyDecalColor = 4291596077u;

	public const uint AllyPartyDecalColor = 4279386828u;

	private bool _mapSceneCursorWanted = true;

	private bool _mapSceneCursorActive;

	private TutorialContexts _currentTutorialContext = TutorialContexts.MapWindow;

	private MapColorGradeManager _colorGradeManager;

	private int _mapScreenTickCount;

	private int _sceneReadyFrameCounter;

	public bool TooltipHandlingDisabled;

	private readonly UIntPtr[] _intersectedEntityIDs = new UIntPtr[128];

	private readonly Intersection[] _intersectionInfos = new Intersection[128];

	private GameEntity[] _tickedMapEntities;

	private Mesh[] _tickedMapMeshes;

	private readonly List<MBCampaignEvent> _periodicCampaignUIEvents;

	private bool _ignoreLeftMouseRelease;

	public IInputContext Input => SceneLayer.Input;

	public static MapScreen Instance { get; private set; }

	public bool IsReady => _isReadyForRender;

	public INavigationHandler NavigationHandler
	{
		get
		{
			return _navigationHandler;
		}
		set
		{
			if (_navigationHandler != null && value != null && value != _navigationHandler)
			{
				Debug.FailedAssert("Navigation handler should not be changed after map bar initialization", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\SandBox.View\\Map\\MapScreen.cs", "NavigationHandler", 127);
			}
			else
			{
				_navigationHandler = value;
			}
		}
	}

	public MapEntityVisual CurrentVisualOfTooltip { get; private set; }

	public CampaignMapSiegePrefabEntityCache PrefabEntityCache { get; private set; }

	public MapEncyclopediaView EncyclopediaScreenManager { get; private set; }

	public bool IsEscapeMenuOpened { get; private set; }

	public MapNotificationView MapNotificationView { get; private set; }

	public Dictionary<Tuple<Material, Banner>, Material> BannerTexturedMaterialCache => _bannerTexturedMaterialCache ?? (_bannerTexturedMaterialCache = new Dictionary<Tuple<Material, Banner>, Material>());

	public bool IsInMenu => _menuViewContext != null;

	public SceneLayer SceneLayer { get; private set; }

	public MapCameraView MapCameraView { get; private set; }

	public bool MapSceneCursorActive
	{
		get
		{
			return _mapSceneCursorActive;
		}
		set
		{
			if (_mapSceneCursorActive != value)
			{
				_mapSceneCursorActive = value;
			}
		}
	}

	public GameEntity ContourMaskEntity { get; private set; }

	public MapCursor MapCursor { get; private set; } = new MapCursor();

	public List<Mesh> InactiveLightMeshes { get; private set; }

	public List<Mesh> ActiveLightMeshes { get; private set; }

	public Scene MapScene { get; private set; }

	public MapState MapState { get; private set; }

	public bool IsInBattleSimulation { get; private set; }

	public bool IsInTownManagement { get; private set; }

	public bool IsInHideoutTroopManage { get; private set; }

	public bool IsInArmyManagement { get; private set; }

	public bool IsInRecruitment { get; private set; }

	public bool IsBarExtended { get; private set; }

	public bool IsInCampaignOptions { get; private set; }

	public bool IsMarriageOfferPopupActive { get; private set; }

	public bool IsMapCheatsActive { get; private set; }

	public bool IsMapIncidentActive { get; private set; }

	public bool IsHeirSelectionPopupActive { get; private set; }

	public bool IsOverlayContextMenuEnabled { get; private set; }

	public bool IsSoundOn { get; private set; } = true;

	public static Dictionary<UIntPtr, MapEntityVisual> VisualsOfEntities => SandBoxViewSubModule.VisualsOfEntities;

	internal static Dictionary<UIntPtr, Tuple<MatrixFrame, SettlementVisual>> FrameAndVisualOfEngines => SandBoxViewSubModule.FrameAndVisualOfEngines;

	public MapScreen(MapState mapState)
	{
		MapState = mapState;
		mapState.Handler = this;
		_periodicCampaignUIEvents = new List<MBCampaignEvent>();
		InitializeVisuals();
		CampaignMusicHandler.Create();
		_mapViewsContainer = new MapViewsContainer();
		MapCameraView = (MapCameraView)AddMapView<MapCameraView>(Array.Empty<object>());
		AddMapView<MapBarView>(Array.Empty<object>());
		AddMapView<MapConversationView>(Array.Empty<object>());
		_conversationView = GetMapView<MapConversationView>();
		MapTracksCampaignBehavior = Campaign.Current.GetCampaignBehavior<IMapTracksCampaignBehavior>();
	}

	public void OnHoverMapEntity(MapEntityVisual mapEntityVisual)
	{
		uint hashCode = (uint)mapEntityVisual.GetHashCode();
		if (_tooltipTargetHash != hashCode)
		{
			_tooltipTargetHash = hashCode;
			_tooltipTargetObject = null;
			mapEntityVisual.OnHover();
		}
	}

	public void RemoveMapTooltip()
	{
		if (_tooltipTargetObject != null || _tooltipTargetHash != 0)
		{
			_tooltipTargetObject = null;
			_tooltipTargetHash = 0u;
			MBInformationManager.HideInformations();
			CurrentVisualOfTooltip?.OnHoverEnd();
		}
	}

	private static void PreloadTextures()
	{
		List<string> list = new List<string>();
		list.Add("gui_map_circle_enemy");
		list.Add("gui_map_circle_enemy_selected");
		list.Add("gui_map_circle_neutral");
		list.Add("gui_map_circle_neutral_selected");
		for (int i = 2; i <= 5; i++)
		{
			list.Add("gui_map_circle_enemy_selected_" + i);
			list.Add("gui_map_circle_neutral_selected_" + i);
		}
		for (int j = 0; j < list.Count; j++)
		{
			Texture.GetFromResource(list[j]).PreloadTexture(blocking: false);
		}
		list.Clear();
	}

	private void SetCameraOfSceneLayer()
	{
		SceneLayer.SetCamera(MapCameraView.Camera);
		Vec3 center = MapCameraView.CameraFrame.origin;
		center.z = 0f;
		SceneLayer.SetFocusedShadowmap(enable: false, ref center, 0f);
	}

	protected override void OnResume()
	{
		base.OnResume();
		PreloadTextures();
		IsSoundOn = true;
		RestartAmbientSounds();
		if (_gpuMemoryCleared)
		{
			_gpuMemoryCleared = false;
		}
		_mapViewsContainer.ForeachReverse(delegate(MapView view)
		{
			view.OnResume();
		});
		MenuContext menuContext = MapState.MenuContext;
		if (_menuViewContext != null)
		{
			if (menuContext != null && menuContext != _menuViewContext.MenuContext)
			{
				_menuViewContext.UpdateMenuContext(menuContext);
			}
			else if (menuContext == null)
			{
				ExitMenuContext();
			}
		}
		_menuViewContext?.OnResume();
		(Campaign.Current.MapSceneWrapper as MapScene).ValidateAgentVisualsReseted();
	}

	protected override void OnPause()
	{
		base.OnPause();
		MBInformationManager.HideInformations();
		PauseAmbientSounds();
		IsSoundOn = false;
		_activatedFrameNo = Utilities.EngineFrameNo;
		HandleIfSceneIsReady();
	}

	void IMapStateHandler.OnGameLoadFinished()
	{
		SandBoxViewVisualManager.OnGameLoadFinished();
	}

	protected override void OnActivate()
	{
		base.OnActivate();
		_mapViewsContainer.ForeachReverse(delegate(MapView view)
		{
			view.OnActivate();
		});
		MapCameraView.OnActivate(_leftButtonDraggingMode, _clickedPosition);
		_activatedFrameNo = Utilities.EngineFrameNo;
		HandleIfSceneIsReady();
		Game.Current.EventManager.TriggerEvent(new TutorialContextChangedEvent(TutorialContexts.MapWindow));
		SetCameraOfSceneLayer();
		RestartAmbientSounds();
		MenuContext menuContext = MapState.MenuContext;
		if (_menuViewContext != null)
		{
			if (menuContext != null && menuContext != _menuViewContext.MenuContext)
			{
				_menuViewContext.UpdateMenuContext(menuContext);
			}
			else if (menuContext == null)
			{
				ExitMenuContext();
			}
		}
		_menuViewContext?.OnResume();
		PartyBase.MainParty.SetVisualAsDirty();
		for (int num = base.Layers.Count - 1; num >= 0; num--)
		{
			if (base.Layers[num].IsActive && base.Layers[num].IsFocusLayer)
			{
				ScreenManager.TrySetFocus(base.Layers[num]);
			}
		}
	}

	public void ClearGPUMemory()
	{
		if (true)
		{
			SceneLayer.ClearRuntimeGPUMemory(remove_terrain: true);
			SceneLayer.SceneView.GetScene().DeleteWaterWakeRenderer();
		}
		SandBoxViewVisualManager.ClearVisualMemory();
		ThumbnailCacheManager.Current.ForceClearAllCache(releaseImmediately: true);
		Texture.ReleaseGpuMemories();
		_gpuMemoryCleared = true;
	}

	protected override void OnDeactivate()
	{
		_sceneReadyFrameCounter = 0;
		Game.Current?.EventManager.TriggerEvent(new TutorialContextChangedEvent(TutorialContexts.None));
		PauseAmbientSounds();
		_menuViewContext?.OnDeactivate();
		MBInformationManager.HideInformations();
		_mapViewsContainer.ForeachReverse(delegate(MapView view)
		{
			view.OnDeactivate();
		});
		base.OnDeactivate();
	}

	public override void OnFocusChangeOnGameWindow(bool focusGained)
	{
		base.OnFocusChangeOnGameWindow(focusGained);
		if (!focusGained && BannerlordConfig.StopGameOnFocusLost && !InformationManager.IsAnyInquiryActive())
		{
			MapEncyclopediaView encyclopediaScreenManager = EncyclopediaScreenManager;
			if ((encyclopediaScreenManager == null || !encyclopediaScreenManager.IsEncyclopediaOpen) && _mapViewsContainer.IsOpeningEscapeMenuOnFocusChangeAllowedForAll() && !ScreenFadeController.IsFadeActive)
			{
				OnEscapeMenuToggled(isOpened: true);
			}
		}
		_focusLost = !focusGained;
	}

	public MapView AddMapView<T>(params object[] parameters) where T : MapView, new()
	{
		T mapViewWithType = _mapViewsContainer.GetMapViewWithType<T>();
		if (mapViewWithType != null)
		{
			Debug.FailedAssert("Map view already added to the list", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\SandBox.View\\Map\\MapScreen.cs", "AddMapView", 549);
			Debug.Print("Map view already added to the list: " + typeof(T).Name + ". Returning existing view instead of creating new one.");
			return mapViewWithType;
		}
		MapView mapView = SandBoxViewCreator.CreateMapView<T>(parameters);
		mapView.MapScreen = this;
		mapView.MapState = MapState;
		_mapViewsContainer.Add(mapView);
		mapView.CreateLayout();
		return mapView;
	}

	public T GetMapView<T>() where T : MapView
	{
		return _mapViewsContainer.GetMapViewWithType<T>();
	}

	public void RemoveMapView(MapView mapView)
	{
		mapView.OnDeactivate();
		mapView.OnFinalize();
		_mapViewsContainer.Remove(mapView);
	}

	public void AddEncounterOverlay(GameMenu.MenuOverlayType type)
	{
		if (_encounterOverlay == null)
		{
			_encounterOverlay = AddMapView<MapOverlayView>(new object[1] { type });
			_mapViewsContainer.Foreach(delegate(MapView view)
			{
				view.OnOverlayCreated();
			});
		}
	}

	public void AddArmyOverlay(MapOverlayType type)
	{
		if (_armyOverlay == null)
		{
			_armyOverlay = AddMapView<MapOverlayView>(new object[1] { type });
			_mapViewsContainer.ForeachReverse(delegate(MapView view)
			{
				view.OnOverlayCreated();
			});
		}
	}

	public void RemoveEncounterOverlay()
	{
		if (_encounterOverlay != null)
		{
			RemoveMapView(_encounterOverlay);
			_encounterOverlay = null;
			_mapViewsContainer.ForeachReverse(delegate(MapView view)
			{
				view.OnOverlayClosed();
			});
		}
	}

	public void RemoveArmyOverlay()
	{
		if (_armyOverlay != null)
		{
			RemoveMapView(_armyOverlay);
			_armyOverlay = null;
			_mapViewsContainer.ForeachReverse(delegate(MapView view)
			{
				view.OnOverlayClosed();
			});
		}
	}

	protected override void OnInitialize()
	{
		base.OnInitialize();
		if (MBDebug.TestModeEnabled)
		{
			CheckValidityOfItems();
		}
		Instance = this;
		ThumbnailCacheManager.Current.ForceClearAllCache(releaseImmediately: true);
		MapCameraView.Initialize();
		ViewSubModule.BannerTexturedMaterialCache = BannerTexturedMaterialCache;
		SceneLayer = new SceneLayer(clearSceneOnFinalize: true, autoToggleSceneView: false);
		SceneLayer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("Generic"));
		SceneLayer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("GenericPanelGameKeyCategory"));
		SceneLayer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("GenericCampaignPanelsGameKeyCategory"));
		SceneLayer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("MapHotKeyCategory"));
		AddLayer(SceneLayer);
		MapScene = ((MapScene)Campaign.Current.MapSceneWrapper).Scene;
		Utilities.SetAllocationAlwaysValidScene(null);
		SceneLayer.SetScene(MapScene);
		SceneLayer.SceneView.SetEnable(value: false);
		SceneLayer.SetSceneUsesShadows(value: true);
		SceneLayer.SetRenderWithPostfx(value: true);
		SceneLayer.SetSceneUsesContour(value: true);
		SceneLayer.SceneView.SetAcceptGlobalDebugRenderObjects(value: true);
		SceneLayer.SceneView.SetResolutionScaling(value: true);
		CollectTickableMapMeshes();
		MapNotificationView = AddMapView<MapNotificationView>(Array.Empty<object>()) as MapNotificationView;
		AddMapView<MapBasicView>(Array.Empty<object>());
		AddMapView<MapPartyNameplateView>(Array.Empty<object>());
		AddMapView<MapSettlementNameplateView>(Array.Empty<object>());
		AddMapView<MapEventVisualsView>(Array.Empty<object>());
		AddMapView<MapTrackersView>(Array.Empty<object>());
		AddMapView<MapSaveView>(Array.Empty<object>());
		AddMapView<MapGamepadEffectsView>(Array.Empty<object>());
		EncyclopediaScreenManager = AddMapView<MapEncyclopediaView>(Array.Empty<object>()) as MapEncyclopediaView;
		_mapReadyView = AddMapView<MapReadyView>(Array.Empty<object>()) as MapReadyView;
		_mapReadyView.SetIsMapSceneReady(isReady: false);
		_mouseRay = new Ray(Vec3.Zero, Vec3.Up);
		if (PlayerSiege.PlayerSiegeEvent != null)
		{
			((IMapStateHandler)this)?.OnPlayerSiegeActivated();
		}
		PrefabEntityCache = SceneLayer.SceneView.GetScene().GetFirstEntityWithScriptComponent<CampaignMapSiegePrefabEntityCache>().GetFirstScriptOfType<CampaignMapSiegePrefabEntityCache>();
		CampaignEvents.OnSaveOverEvent.AddNonSerializedListener(this, OnSaveOver);
		CampaignEvents.OnMarriageOfferedToPlayerEvent.AddNonSerializedListener(this, OnMarriageOfferedToPlayer);
		CampaignEvents.OnMarriageOfferCanceledEvent.AddNonSerializedListener(this, OnMarriageOfferCanceled);
		CampaignEvents.OnHeirSelectionRequestedEvent.AddNonSerializedListener(this, OnHeirSelectionRequested);
		CampaignEvents.OnHeirSelectionOverEvent.AddNonSerializedListener(this, OnHeirSelectionOver);
		Game.Current.EventManager.RegisterEvent<TutorialContextChangedEvent>(OnTutorialContextChanged);
		GameEntity firstEntityWithScriptComponent = MapScene.GetFirstEntityWithScriptComponent<MapColorGradeManager>();
		if (firstEntityWithScriptComponent != null)
		{
			_colorGradeManager = firstEntityWithScriptComponent.GetFirstScriptOfType<MapColorGradeManager>();
		}
	}

	private void OnSaveOver(bool isSuccessful, string newSaveGameName)
	{
		if (_exitOnSaveOver)
		{
			if (isSuccessful)
			{
				OnExit();
			}
			_exitOnSaveOver = false;
		}
	}

	private void OnMarriageOfferedToPlayer(Hero suitor, Hero maiden)
	{
		_marriageOfferPopupView = AddMapView<MarriageOfferPopupView>(new object[2] { suitor, maiden });
	}

	public void CloseMarriageOfferPopup()
	{
		if (_marriageOfferPopupView != null)
		{
			RemoveMapView(_marriageOfferPopupView);
			_marriageOfferPopupView = null;
		}
	}

	protected override void OnFinalize()
	{
		_mapViewsContainer.ForeachReverse(delegate(MapView view)
		{
			view.OnFinalize();
		});
		List<EntityVisualManagerBase> components = SandBoxViewSubModule.SandBoxViewVisualManager.GetComponents<EntityVisualManagerBase>();
		for (int num = components.Count - 1; num >= 0; num--)
		{
			SandBoxViewSubModule.SandBoxViewVisualManager.Finalize(components[num]);
		}
		base.OnFinalize();
		if (MapScene != null)
		{
			MapScene.ClearAll();
		}
		Common.MemoryCleanupGC();
		CharacterBannerMaterialCache.Clear();
		ViewSubModule.BannerTexturedMaterialCache = null;
		MBMusicManager.Current.DeactivateCampaignMode();
		MBMusicManager.Current.OnCampaignMusicHandlerFinalize();
		CampaignEvents.OnSaveOverEvent.ClearListeners(this);
		CampaignEvents.OnMarriageOfferedToPlayerEvent.ClearListeners(this);
		CampaignEvents.OnMarriageOfferCanceledEvent.ClearListeners(this);
		Game.Current.EventManager.UnregisterEvent<TutorialContextChangedEvent>(OnTutorialContextChanged);
		BannerPersistentTextureCache.Current?.FlushCache();
		MapScene = null;
		MapCameraView = null;
		Instance = null;
	}

	public void OnHourlyTick()
	{
		_mapViewsContainer.ForeachReverse(delegate(MapView view)
		{
			view.OnHourlyTick();
		});
		_isKingdomDecisionsDirty = Clan.PlayerClan.Kingdom?.UnresolvedDecisions.FirstOrDefault((KingdomDecision d) => d.NotifyPlayer && d.IsEnforced && d.IsPlayerParticipant && !d.ShouldBeCancelled()) != null;
	}

	private void OnMarriageOfferCanceled(Hero suitor, Hero maiden)
	{
		CloseMarriageOfferPopup();
	}

	private void OnHeirSelectionRequested(Dictionary<Hero, int> heirApparents)
	{
		_heirSelectionPopupView = AddMapView<HeirSelectionPopupView>(new object[1] { heirApparents });
	}

	public void BeginParleyWith(PartyBase party)
	{
		if (GetMapView<MapParleyAnimationView>() == null)
		{
			AddMapView<MapParleyAnimationView>(new object[1] { party });
		}
	}

	private void OnHeirSelectionOver(Hero selectedHeir)
	{
		if (_heirSelectionPopupView != null)
		{
			RemoveMapView(_heirSelectionPopupView);
			_heirSelectionPopupView = null;
		}
	}

	private void ShowNextKingdomDecisionPopup()
	{
		KingdomDecision kingdomDecision = Clan.PlayerClan.Kingdom?.UnresolvedDecisions.FirstOrDefault((KingdomDecision d) => d.NotifyPlayer && d.IsEnforced && d.IsPlayerParticipant && !d.ShouldBeCancelled());
		if (kingdomDecision != null)
		{
			InquiryData data = new InquiryData(new TextObject("{=A7349NHy}Critical Kingdom Decision").ToString(), kingdomDecision.GetChooseTitle().ToString(), isAffirmativeOptionShown: true, isNegativeOptionShown: false, new TextObject("{=bFzZwwjT}Examine").ToString(), "", delegate
			{
				OpenKingdom();
			}, null);
			kingdomDecision.NotifyPlayer = false;
			InformationManager.ShowInquiry(data, pauseGameActiveState: true);
			_isKingdomDecisionsDirty = false;
		}
		else
		{
			Debug.FailedAssert("There is no dirty decision but still demanded one", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\SandBox.View\\Map\\MapScreen.cs", "ShowNextKingdomDecisionPopup", 827);
		}
	}

	void IMapStateHandler.OnMenuModeTick(float dt)
	{
		UpdateTutorialContext();
		_mapViewsContainer.ForeachReverse(delegate(MapView view)
		{
			view.OnMenuModeTick(dt);
		});
	}

	private void HandleIfBlockerStatesDisabled()
	{
		_ = _isReadyForRender;
		bool flag = SceneLayer.SceneView.ReadyToRender() && SceneLayer.SceneView.CheckSceneReadyToRender();
		bool flag2 = (_isSceneViewEnabled && flag) || _conversationView.IsConversationActive;
		if (flag2)
		{
			if (LoadingWindow.IsLoadingWindowActive)
			{
				if (_sceneReadyFrameCounter == 3)
				{
					LoadingWindow.DisableGlobalLoadingWindow();
					_sceneReadyFrameCounter = 0;
				}
				else
				{
					_sceneReadyFrameCounter++;
				}
			}
		}
		else if (!flag && !LoadingWindow.IsLoadingWindowActive)
		{
			LoadingWindow.EnableGlobalLoadingWindow();
		}
		if (flag)
		{
			_mapReadyView.SetIsMapSceneReady(flag2);
			_isReadyForRender = flag2;
		}
	}

	private void UpdateTutorialContext()
	{
		if (!base.IsActive)
		{
			return;
		}
		TutorialContexts tutorialContexts = TutorialContexts.MapWindow;
		if (IsInMenu)
		{
			for (int num = _menuViewContext.MenuViews.Count - 1; num >= 0; num--)
			{
				TutorialContexts tutorialContext = _menuViewContext.MenuViews[num].GetTutorialContext();
				if (tutorialContext != TutorialContexts.MapWindow)
				{
					tutorialContexts = tutorialContext;
					break;
				}
			}
		}
		if (tutorialContexts == TutorialContexts.MapWindow)
		{
			tutorialContexts = _mapViewsContainer.GetContextToChangeTo();
		}
		if (_currentTutorialContext != tutorialContexts)
		{
			Game.Current.EventManager.TriggerEvent(new TutorialContextChangedEvent(tutorialContexts));
		}
	}

	private void CheckCursorState()
	{
		Vec3 worldMouseNear = Vec3.Zero;
		Vec3 worldMouseFar = Vec3.Zero;
		SceneLayer.SceneView.TranslateMouse(ref worldMouseNear, ref worldMouseFar);
		PathFaceRecord currentFace = PathFaceRecord.NullFaceRecord;
		GetCursorIntersectionPoint(ref worldMouseNear, ref worldMouseFar, out var _, out var intersectionPoint, ref currentFace, out var isOnland);
		SceneLayer.ActiveCursor = (NavigationHelper.CanPlayerNavigateToPosition(new CampaignVec2(intersectionPoint.AsVec2, isOnland), out var _) ? CursorType.Default : CursorType.Disabled);
	}

	private void HandleIfSceneIsReady()
	{
		int num = Utilities.EngineFrameNo - _activatedFrameNo;
		bool isSceneViewEnabled = _isSceneViewEnabled;
		if (num < 5)
		{
			isSceneViewEnabled = false;
			_colorGradeManager?.ApplyAtmosphere(forceLoadTextures: true);
		}
		else
		{
			bool isConversationActive = _conversationView.IsConversationActive;
			bool flag = ScreenManager.TopScreen == this;
			isSceneViewEnabled = !isConversationActive && flag;
		}
		if (isSceneViewEnabled != _isSceneViewEnabled)
		{
			_isSceneViewEnabled = isSceneViewEnabled;
			SceneLayer.SceneView.SetEnable(_isSceneViewEnabled);
			if (_isSceneViewEnabled)
			{
				MapScene.CheckResources(checkInvisibleEntities: false);
				if (MapScene.SceneHadWaterWakeRenderer())
				{
					MapScene.EnsureWaterWakeRenderer();
					MapScene.SetWaterWakeWorldSize(128f, 0.994f);
					MapScene.SetWaterWakeCameraOffset(8f);
				}
				_sceneReadyFrameCounter = 0;
				if (_focusLost && !IsEscapeMenuOpened)
				{
					OnFocusChangeOnGameWindow(focusGained: false);
				}
			}
		}
		HandleIfBlockerStatesDisabled();
	}

	void IMapStateHandler.StartCameraAnimation(CampaignVec2 targetPosition, float animationStopDuration)
	{
		MapCameraView.StartCameraAnimation(targetPosition, animationStopDuration);
	}

	private void OnTutorialContextChanged(TutorialContextChangedEvent evnt)
	{
		_currentTutorialContext = evnt.NewContext;
	}

	void IMapStateHandler.BeforeTick(float dt)
	{
		UpdateTutorialContext();
		HandleIfSceneIsReady();
		bool flag = MobileParty.MainParty != null && PartyBase.MainParty.IsValid;
		if (flag && !MapCameraView.CameraAnimationInProgress)
		{
			if (!IsInMenu && SceneLayer.Input.IsHotKeyPressed("MapChangeCursorMode"))
			{
				_mapSceneCursorWanted = !_mapSceneCursorWanted;
			}
			if (IsMapClickPressed())
			{
				_secondLastPressTime = _lastPressTime;
				_lastPressTime = Time.ApplicationTime;
			}
			_leftButtonDoubleClickOnSceneWidget = false;
			if (IsMapClickReleased())
			{
				Vec2 mousePositionPixel = SceneLayer.Input.GetMousePositionPixel();
				float applicationTime = Time.ApplicationTime;
				_leftButtonDoubleClickOnSceneWidget = (double)applicationTime - _lastReleaseTime < 0.30000001192092896 && (double)applicationTime - _secondLastPressTime < 0.44999998807907104 && mousePositionPixel.Distance(_oldMousePosition) < 10f;
				if (_leftButtonDoubleClickOnSceneWidget)
				{
					_waitForDoubleClickUntilTime = 0f;
				}
				_oldMousePosition = SceneLayer.Input.GetMousePositionPixel();
				_lastReleaseTime = applicationTime;
			}
			if (IsReady)
			{
				HandleMouse(dt);
			}
		}
		MapSceneCursorActive = !SceneLayer.Input.GetIsMouseActive() && !IsInMenu && ScreenManager.FocusedLayer == SceneLayer && _mapSceneCursorWanted;
		float deltaMouseScroll = SceneLayer.Input.GetDeltaMouseScroll();
		Vec3 worldMouseNear = Vec3.Zero;
		Vec3 worldMouseFar = Vec3.Zero;
		SceneLayer.SceneView.TranslateMouse(ref worldMouseNear, ref worldMouseFar);
		float gameKeyAxis = SceneLayer.Input.GetGameKeyAxis("CameraAxisX");
		float collisionDistance;
		Vec3 closestPoint;
		bool rayCastForClosestEntityOrTerrainCondition = MapScene.RayCastForClosestEntityOrTerrain(worldMouseNear, worldMouseFar, out collisionDistance, out closestPoint, 0.01f, BodyFlags.CameraCollisionRayCastExludeFlags);
		float rX = 0f;
		float rY = 0f;
		float num = 1f;
		bool num2 = !TaleWorlds.InputSystem.Input.IsGamepadActive && !IsInMenu && ScreenManager.FocusedLayer == SceneLayer;
		bool flag2 = TaleWorlds.InputSystem.Input.IsGamepadActive && MapSceneCursorActive;
		if (num2 || flag2)
		{
			if (SceneLayer.Input.IsGameKeyDown(55))
			{
				num = MapCameraView.CameraFastMoveMultiplier;
			}
			rX = SceneLayer.Input.GetGameKeyAxis("MapMovementAxisX") * num;
			rY = SceneLayer.Input.GetGameKeyAxis("MapMovementAxisY") * num;
		}
		_ignoreLeftMouseRelease = false;
		MouseInputState mouseInputState = GetMouseInputState();
		if (mouseInputState.IsLeftMousePressed)
		{
			_clickedPositionPixel = SceneLayer.Input.GetMousePositionPixel();
			MapScene.RayCastForClosestEntityOrTerrain(_mouseRay.Origin, _mouseRay.EndPoint, out collisionDistance, out _clickedPosition, 0.01f, BodyFlags.CameraCollisionRayCastExludeFlags);
			if (CurrentVisualOfTooltip != null)
			{
				RemoveMapTooltip();
			}
			_leftButtonDraggingMode = false;
		}
		else if (mouseInputState.IsLeftMouseDown && !mouseInputState.IsLeftMouseReleased && (SceneLayer.Input.GetMousePositionPixel().DistanceSquared(_clickedPositionPixel) > 300f || _leftButtonDraggingMode) && !IsInMenu)
		{
			_leftButtonDraggingMode = true;
		}
		else if (_leftButtonDraggingMode)
		{
			_leftButtonDraggingMode = false;
			_ignoreLeftMouseRelease = true;
		}
		if (mouseInputState.IsMiddleMouseDown)
		{
			MBWindowManager.DontChangeCursorPos();
		}
		if (mouseInputState.IsLeftMouseReleased)
		{
			_clickedPositionPixel = SceneLayer.Input.GetMousePositionPixel();
		}
		MapCameraView.InputInformation inputInformation = default(MapCameraView.InputInformation);
		inputInformation.IsMainPartyValid = flag;
		inputInformation.IsMapReady = IsReady;
		inputInformation.IsControlDown = SceneLayer.Input.IsControlDown();
		inputInformation.IsMouseActive = SceneLayer.Input.GetIsMouseActive();
		inputInformation.CheatModeEnabled = Game.Current.CheatMode;
		inputInformation.DeltaMouseScroll = deltaMouseScroll;
		inputInformation.LeftMouseButtonPressed = mouseInputState.IsLeftMousePressed;
		inputInformation.LeftMouseButtonDown = mouseInputState.IsLeftMouseDown;
		inputInformation.LeftMouseButtonReleased = mouseInputState.IsLeftMouseReleased;
		inputInformation.MiddleMouseButtonDown = mouseInputState.IsMiddleMouseDown;
		inputInformation.RightMouseButtonDown = mouseInputState.IsRightMouseDown;
		inputInformation.RotateLeftKeyDown = SceneLayer.Input.IsGameKeyDown(58);
		inputInformation.RotateRightKeyDown = SceneLayer.Input.IsGameKeyDown(59);
		inputInformation.PartyMoveUpKey = SceneLayer.Input.IsGameKeyDown(50);
		inputInformation.PartyMoveDownKey = SceneLayer.Input.IsGameKeyDown(51);
		inputInformation.PartyMoveLeftKey = SceneLayer.Input.IsGameKeyDown(52);
		inputInformation.PartyMoveRightKey = SceneLayer.Input.IsGameKeyDown(53);
		inputInformation.MapZoomIn = SceneLayer.Input.GetGameKeyState(56);
		inputInformation.MapZoomOut = SceneLayer.Input.GetGameKeyState(57);
		inputInformation.CameraFollowModeKeyPressed = SceneLayer.Input.IsGameKeyPressed(64);
		inputInformation.MousePositionPixel = SceneLayer.Input.GetMousePositionPixel();
		inputInformation.ClickedPositionPixel = _clickedPositionPixel;
		inputInformation.ClickedPosition = _clickedPosition;
		inputInformation.LeftButtonDraggingMode = _leftButtonDraggingMode;
		inputInformation.IsInMenu = IsInMenu;
		inputInformation.WorldMouseNear = worldMouseNear;
		inputInformation.WorldMouseFar = worldMouseFar;
		inputInformation.MouseSensitivity = SceneLayer.Input.GetMouseSensitivity();
		inputInformation.MouseMoveX = SceneLayer.Input.GetMouseMoveX();
		inputInformation.MouseMoveY = SceneLayer.Input.GetMouseMoveY();
		inputInformation.HorizontalCameraInput = gameKeyAxis;
		inputInformation.RayCastForClosestEntityOrTerrainCondition = rayCastForClosestEntityOrTerrainCondition;
		inputInformation.ProjectedPosition = closestPoint;
		inputInformation.RX = rX;
		inputInformation.RY = rY;
		inputInformation.RS = num;
		inputInformation.Dt = dt;
		MapCameraView.OnBeforeTick(in inputInformation);
		MapCursor.SetVisible(MapSceneCursorActive);
		if (flag && !Campaign.Current.TimeControlModeLock)
		{
			if (!MapState.AtMenu)
			{
				goto IL_0655;
			}
			if (Campaign.Current.CurrentMenuContext != null)
			{
				GameMenu gameMenu = Campaign.Current.CurrentMenuContext.GameMenu;
				if (gameMenu != null && gameMenu.IsWaitActive)
				{
					goto IL_0655;
				}
			}
		}
		goto IL_08f0;
		IL_0655:
		float applicationTime2 = Time.ApplicationTime;
		if (SceneLayer.Input.IsGameKeyPressed(63) && _timeToggleTimer == float.MaxValue)
		{
			_timeToggleTimer = applicationTime2;
		}
		if (SceneLayer.Input.IsGameKeyPressed(63) && applicationTime2 - _timeToggleTimer > 0.4f)
		{
			if (Campaign.Current.TimeControlMode == CampaignTimeControlMode.StoppablePlay || Campaign.Current.TimeControlMode == CampaignTimeControlMode.UnstoppablePlay)
			{
				Campaign.Current.SetTimeSpeed(2);
			}
			else if (Campaign.Current.TimeControlMode == CampaignTimeControlMode.StoppableFastForward || Campaign.Current.TimeControlMode == CampaignTimeControlMode.UnstoppableFastForward)
			{
				Campaign.Current.SetTimeSpeed(1);
			}
			else if (Campaign.Current.TimeControlMode == CampaignTimeControlMode.Stop)
			{
				Campaign.Current.SetTimeSpeed(1);
			}
			else if (Campaign.Current.TimeControlMode == CampaignTimeControlMode.FastForwardStop)
			{
				Campaign.Current.SetTimeSpeed(2);
			}
			_timeToggleTimer = float.MaxValue;
			_ignoreNextTimeToggle = true;
		}
		else if (SceneLayer.Input.IsGameKeyPressed(63))
		{
			if (_ignoreNextTimeToggle)
			{
				_ignoreNextTimeToggle = false;
			}
			else
			{
				_waitForDoubleClickUntilTime = 0f;
				if (Campaign.Current.TimeControlMode == CampaignTimeControlMode.UnstoppableFastForward || Campaign.Current.TimeControlMode == CampaignTimeControlMode.UnstoppablePlay || ((Campaign.Current.TimeControlMode == CampaignTimeControlMode.StoppableFastForward || Campaign.Current.TimeControlMode == CampaignTimeControlMode.StoppablePlay) && !Campaign.Current.IsMainPartyWaiting))
				{
					Campaign.Current.SetTimeSpeed(0);
				}
				else if (Campaign.Current.TimeControlMode == CampaignTimeControlMode.Stop || Campaign.Current.TimeControlMode == CampaignTimeControlMode.StoppablePlay)
				{
					Campaign.Current.SetTimeSpeed(1);
				}
				else if (Campaign.Current.TimeControlMode == CampaignTimeControlMode.FastForwardStop || Campaign.Current.TimeControlMode == CampaignTimeControlMode.StoppableFastForward)
				{
					Campaign.Current.SetTimeSpeed(2);
				}
			}
			_timeToggleTimer = float.MaxValue;
		}
		else if (SceneLayer.Input.IsGameKeyPressed(60))
		{
			_waitForDoubleClickUntilTime = 0f;
			Campaign.Current.SetTimeSpeed(0);
		}
		else if (SceneLayer.Input.IsGameKeyPressed(61))
		{
			_waitForDoubleClickUntilTime = 0f;
			Campaign.Current.SetTimeSpeed(1);
		}
		else if (SceneLayer.Input.IsGameKeyPressed(62))
		{
			_waitForDoubleClickUntilTime = 0f;
			Campaign.Current.SetTimeSpeed(2);
		}
		else if (SceneLayer.Input.IsGameKeyPressed(65))
		{
			if (Campaign.Current.TimeControlMode == CampaignTimeControlMode.UnstoppableFastForward || Campaign.Current.TimeControlMode == CampaignTimeControlMode.StoppableFastForward)
			{
				Campaign.Current.SetTimeSpeed(0);
			}
			else
			{
				Campaign.Current.SetTimeSpeed(2);
			}
		}
		goto IL_08f0;
		IL_08f0:
		if (!flag && CurrentVisualOfTooltip != null)
		{
			RemoveMapTooltip();
			CurrentVisualOfTooltip = null;
		}
		SetCameraOfSceneLayer();
		if (!SceneLayer.Input.GetIsMouseActive() && Campaign.Current.GameStarted)
		{
			MapCursor.BeforeTick(dt);
		}
	}

	void IMapStateHandler.Tick(float dt)
	{
		if (!IsInMenu)
		{
			if (_isKingdomDecisionsDirty)
			{
				ShowNextKingdomDecisionPopup();
			}
			else
			{
				if (ViewModel.UIDebugMode && base.DebugInput.IsHotKeyDown("UIExtendedDebugKey") && base.DebugInput.IsHotKeyPressed("MapScreenHotkeyOpenEncyclopedia"))
				{
					OpenEncyclopedia();
				}
				bool cheatMode = Game.Current.CheatMode;
				if (cheatMode && base.DebugInput.IsHotKeyPressed("MapScreenHotkeySwitchCampaignTrueSight"))
				{
					Campaign.Current.TrueSight = !Campaign.Current.TrueSight;
				}
				if (cheatMode)
				{
					base.DebugInput.IsHotKeyPressed("MapScreenPrintMultiLineText");
				}
				_mapViewsContainer.ForeachReverse(delegate(MapView view)
				{
					view.OnFrameTick(dt);
				});
			}
		}
		SandBoxViewVisualManager.OnTick(dt, Campaign.Current.CampaignDt);
	}

	void IMapStateHandler.OnIdleTick(float dt)
	{
		UpdateTutorialContext();
		HandleIfSceneIsReady();
		RemoveMapTooltip();
		_mapViewsContainer.ForeachReverse(delegate(MapView view)
		{
			view.OnIdleTick(dt);
		});
	}

	protected override void OnFrameTick(float dt)
	{
		base.OnFrameTick(dt);
		MBDebug.SetErrorReportScene(MapScene);
		UpdateMenuView();
		TextObject disabledReason;
		if (IsInMenu)
		{
			_menuViewContext.OnFrameTick(dt);
			if (SceneLayer.Input.IsGameKeyPressed(4))
			{
				GameMenuOption leaveMenuOption = Campaign.Current.GameMenuManager.GetLeaveMenuOption(_menuViewContext.MenuContext);
				if (leaveMenuOption != null)
				{
					UISoundsHelper.PlayUISound("event:/ui/default");
					if (_menuViewContext.MenuContext.GameMenu.IsWaitMenu)
					{
						_menuViewContext.MenuContext.GameMenu.EndWait();
					}
					leaveMenuOption.RunConsequence(_menuViewContext.MenuContext);
				}
			}
		}
		else if (Campaign.Current != null && !IsInBattleSimulation && !IsInArmyManagement && !IsMarriageOfferPopupActive && !IsHeirSelectionPopupActive && !IsMapCheatsActive && !IsMapIncidentActive && !IsOverlayContextMenuEnabled && !EncyclopediaScreenManager.IsEncyclopediaOpen && CampaignUIHelper.GetMapScreenActionIsEnabledWithReason(out disabledReason) && Clan.PlayerClan.Kingdom?.UnresolvedDecisions?.FirstOrDefault((KingdomDecision d) => d.NeedsPlayerResolution && !d.ShouldBeCancelled()) != null)
		{
			OpenKingdom();
		}
		if (_partyIconNeedsRefreshing)
		{
			_partyIconNeedsRefreshing = false;
			PartyBase.MainParty.SetVisualAsDirty();
		}
		_mapViewsContainer.ForeachReverse(delegate(MapView view)
		{
			view.OnMapScreenUpdate(dt);
		});
		SandBoxViewVisualManager.OnFrameTick(Campaign.Current.CampaignDt);
	}

	protected override void OnPostFrameTick(float dt)
	{
		base.OnPostFrameTick(dt);
		if (Campaign.Current.CurrentTickCount != _mapScreenTickCount)
		{
			Campaign.Current.CampaignLateAITickTask?.Invoke();
			_mapScreenTickCount = Campaign.Current.CurrentTickCount;
		}
	}

	private void UpdateMenuView()
	{
		if (_latestMenuContext == null && IsInMenu)
		{
			ExitMenuContext();
		}
		else if ((!IsInMenu && _latestMenuContext != null) || (IsInMenu && _menuViewContext.MenuContext != _latestMenuContext))
		{
			EnterMenuContext(_latestMenuContext);
		}
	}

	private void EnterMenuContext(MenuContext menuContext)
	{
		if (!Hero.MainHero.IsPrisoner)
		{
			MapCameraView.SetCameraMode(MapCameraView.CameraFollowMode.FollowParty);
			Campaign.Current.CameraFollowParty = PartyBase.MainParty;
		}
		if (!IsInMenu)
		{
			_menuViewContext = CreateMenuViewContext(menuContext);
		}
		else
		{
			_menuViewContext.UpdateMenuContext(menuContext);
		}
		_menuViewContext.OnInitialize();
		_menuViewContext.OnActivate();
		if (_conversationView.IsConversationActive)
		{
			_menuViewContext.OnMapConversationActivated();
		}
	}

	private void ExitMenuContext()
	{
		_menuViewContext.OnGameStateDeactivate();
		_menuViewContext.OnDeactivate();
		_menuViewContext.OnFinalize();
		_menuViewContext = null;
	}

	private void OpenBannerEditorScreen()
	{
		if (Campaign.Current.IsBannerEditorEnabled)
		{
			_partyIconNeedsRefreshing = true;
			Game.Current.GameStateManager.PushState(Game.Current.GameStateManager.CreateState<BannerEditorState>());
		}
	}

	private void OpenFaceGeneratorScreen()
	{
		if (Campaign.Current.IsFaceGenEnabled)
		{
			IFaceGeneratorCustomFilter faceGeneratorFilter = CharacterHelper.GetFaceGeneratorFilter();
			BarberState gameState = Game.Current.GameStateManager.CreateState<BarberState>(new object[2]
			{
				Hero.MainHero.CharacterObject,
				faceGeneratorFilter
			});
			GameStateManager.Current.PushState(gameState);
		}
	}

	public void OnExit()
	{
		MapCameraView.OnExit();
		MBGameManager.EndGame();
	}

	private void OnEscapeMenuToggled(bool isOpened = false)
	{
		MapCameraView.OnEscapeMenuToggled(isOpened);
		if (IsEscapeMenuOpened != isOpened)
		{
			IsEscapeMenuOpened = isOpened;
			if (isOpened)
			{
				List<EscapeMenuItemVM> escapeMenuItems = GetEscapeMenuItems();
				Game.Current.GameStateManager.RegisterActiveStateDisableRequest(this);
				_escapeMenuView = AddMapView<MapEscapeMenuView>(new object[1] { escapeMenuItems });
			}
			else
			{
				RemoveMapView(_escapeMenuView);
				_escapeMenuView = null;
				Game.Current.GameStateManager.UnregisterActiveStateDisableRequest(this);
			}
		}
	}

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

	public void GetCursorIntersectionPoint(ref Vec3 clippedMouseNear, ref Vec3 clippedMouseFar, out float closestDistanceSquared, out Vec3 intersectionPoint, ref PathFaceRecord currentFace, out bool isOnland, BodyFlags excludedBodyFlags = BodyFlags.CommonFocusRayCastExcludeFlags)
	{
		(clippedMouseFar - clippedMouseNear).Normalize();
		Vec3 vec = clippedMouseFar - clippedMouseNear;
		float maxDistance = vec.Normalize();
		_mouseRay.Reset(clippedMouseNear, vec, maxDistance);
		intersectionPoint = Vec3.Zero;
		closestDistanceSquared = 1E+12f;
		if (SceneLayer.SceneView.RayCastForClosestEntityOrTerrain(clippedMouseNear, clippedMouseFar, out var collisionDistance, out var _, 0.01f, excludedBodyFlags))
		{
			closestDistanceSquared = collisionDistance * collisionDistance;
			intersectionPoint = clippedMouseNear + vec * collisionDistance;
		}
		currentFace = new CampaignVec2(intersectionPoint.AsVec2, isOnLand: true).Face;
		isOnland = true;
		if (!currentFace.IsValid())
		{
			currentFace = new CampaignVec2(intersectionPoint.AsVec2, isOnLand: false).Face;
			isOnland = false;
		}
	}

	public void FastMoveCameraToPosition(CampaignVec2 target)
	{
		MapCameraView.FastMoveCameraToPosition(target, IsInMenu);
	}

	private void HandleMouse(float dt)
	{
		if (!Campaign.Current.GameStarted)
		{
			return;
		}
		Vec3 worldMouseNear = Vec3.Zero;
		Vec3 worldMouseFar = Vec3.Zero;
		SceneLayer.SceneView.TranslateMouse(ref worldMouseNear, ref worldMouseFar);
		Vec3 clippedMouseNear = worldMouseNear;
		Vec3 clippedMouseFar = worldMouseFar;
		PathFaceRecord currentFace = PathFaceRecord.NullFaceRecord;
		GetCursorIntersectionPoint(ref clippedMouseNear, ref clippedMouseFar, out var closestDistanceSquared, out var _, ref currentFace, out var isOnland);
		GetCursorIntersectionPoint(ref clippedMouseNear, ref clippedMouseFar, out closestDistanceSquared, out var intersectionPoint2, ref currentFace, out var _, BodyFlags.CommonFocusRayCastExcludeFlags | BodyFlags.Moveable);
		int num = MapScene.SelectEntitiesCollidedWith(ref _mouseRay, _intersectionInfos, _intersectedEntityIDs);
		MapEntityVisual hoveredVisual = null;
		MapEntityVisual selectedVisual = null;
		MBList<CampaignEntityVisualComponent> components = SandBoxViewSubModule.SandBoxViewVisualManager.GetComponents();
		for (int i = 0; i < components.Count && !components[i].OnVisualIntersected(_mouseRay, _intersectedEntityIDs, _intersectionInfos, num, worldMouseNear, worldMouseFar, intersectionPoint2, ref hoveredVisual, ref selectedVisual); i++)
		{
		}
		Array.Clear(_intersectedEntityIDs, 0, num);
		Array.Clear(_intersectionInfos, 0, num);
		if (hoveredVisual != null && !hoveredVisual.IsMobileEntity)
		{
			SceneLayer.ActiveCursor = CursorType.Default;
		}
		else
		{
			CheckCursorState();
		}
		float gameKeyAxis = SceneLayer.Input.GetGameKeyAxis("CameraAxisY");
		bool flag = SceneLayer.IsHitThisFrame && SceneLayer.Input.IsKeyDown(InputKey.RightMouseButton);
		MapCameraView.HandleMouse(flag, gameKeyAxis, SceneLayer.Input.GetMouseMoveY(), dt);
		if (flag)
		{
			MBWindowManager.DontChangeCursorPos();
		}
		if (ScreenManager.FirstHitLayer == SceneLayer && IsMapClickReleased() && !_leftButtonDraggingMode && !_ignoreLeftMouseRelease)
		{
			HandleLeftMouseButtonClick(intersectionPoint: new CampaignVec2(intersectionPoint2.AsVec2, isOnland), visualOfSelectedEntity: _leftButtonDoubleClickOnSceneWidget ? _preVisualOfSelectedEntity : selectedVisual, mouseOverFaceIndex: currentFace, isDoubleClick: _leftButtonDoubleClickOnSceneWidget);
			_preVisualOfSelectedEntity = selectedVisual;
		}
		if (BannerlordConfig.MapDoubleClickBehavior == 0 && Campaign.Current.TimeControlMode == CampaignTimeControlMode.StoppableFastForward && _waitForDoubleClickUntilTime > 0f && _waitForDoubleClickUntilTime < Time.ApplicationTime)
		{
			Campaign.Current.TimeControlMode = CampaignTimeControlMode.StoppablePlay;
			_waitForDoubleClickUntilTime = 0f;
		}
		if (ScreenManager.FirstHitLayer == SceneLayer)
		{
			if (hoveredVisual != null)
			{
				if (CurrentVisualOfTooltip != hoveredVisual)
				{
					RemoveMapTooltip();
				}
				if (SceneLayer.Input.IsGameKeyPressed(67))
				{
					hoveredVisual.OnOpenEncyclopedia();
					MapCursor.SetVisible(value: false);
				}
				if (SceneLayer.Input.IsGameKeyPressed(66))
				{
					hoveredVisual.OnTrackAction();
				}
				OnHoverMapEntity(hoveredVisual);
				CurrentVisualOfTooltip = hoveredVisual;
			}
			else if (!TooltipHandlingDisabled)
			{
				RemoveMapTooltip();
				CurrentVisualOfTooltip = null;
			}
		}
		else
		{
			RemoveMapTooltip();
			CurrentVisualOfTooltip = null;
		}
	}

	private MouseInputState GetMouseInputState()
	{
		if (!SceneLayer.IsHitThisFrame)
		{
			return default(MouseInputState);
		}
		return new MouseInputState
		{
			IsLeftMousePressed = SceneLayer.Input.IsKeyPressed(InputKey.LeftMouseButton),
			IsLeftMouseDown = SceneLayer.Input.IsKeyDown(InputKey.LeftMouseButton),
			IsLeftMouseReleased = SceneLayer.Input.IsKeyReleased(InputKey.LeftMouseButton),
			IsMiddleMousePressed = SceneLayer.Input.IsKeyPressed(InputKey.MiddleMouseButton),
			IsMiddleMouseDown = SceneLayer.Input.IsKeyDown(InputKey.MiddleMouseButton),
			IsMiddleMouseReleased = SceneLayer.Input.IsKeyReleased(InputKey.MiddleMouseButton),
			IsRightMousePressed = SceneLayer.Input.IsKeyPressed(InputKey.RightMouseButton),
			IsRightMouseDown = SceneLayer.Input.IsKeyDown(InputKey.RightMouseButton),
			IsRightMouseReleased = SceneLayer.Input.IsKeyReleased(InputKey.RightMouseButton)
		};
	}

	private bool IsMapClickPressed()
	{
		if (!SceneLayer.Input.IsHotKeyPressed("MapClick"))
		{
			if (SceneLayer.Input.IsHotKeyPressed("MapTouchpadClick"))
			{
				return NativeOptions.GetConfig(NativeOptions.NativeOptionsType.EnableTouchpadMouse) != 0f;
			}
			return false;
		}
		return true;
	}

	private bool IsMapClickReleased()
	{
		if (!SceneLayer.Input.IsHotKeyReleased("MapClick"))
		{
			if (SceneLayer.Input.IsHotKeyReleased("MapTouchpadClick"))
			{
				return NativeOptions.GetConfig(NativeOptions.NativeOptionsType.EnableTouchpadMouse) != 0f;
			}
			return false;
		}
		return true;
	}

	private void HandleLeftMouseButtonClick(MapEntityVisual visualOfSelectedEntity, CampaignVec2 intersectionPoint, PathFaceRecord mouseOverFaceIndex, bool isDoubleClick)
	{
		bool flag = false;
		bool flag2 = false;
		bool flag3 = false;
		bool flag4 = Input.IsControlDown() && Game.Current.CheatMode;
		if (!MapState.AtMenu)
		{
			if (visualOfSelectedEntity != null)
			{
				if (visualOfSelectedEntity.IsMainEntity)
				{
					MobileParty.MainParty.SetMoveModeHold();
				}
				else
				{
					PathFaceRecord face = visualOfSelectedEntity.InteractionPositionForPlayer.Face;
					flag2 = MapScene.DoesPathExistBetweenFaces(face.FaceIndex, MobileParty.MainParty.CurrentNavigationFace.FaceIndex, ignoreDisabled: false);
					if (flag2 && MapCameraView.ProcessCameraInput && PartyBase.MainParty.MapEvent == null)
					{
						flag = visualOfSelectedEntity.OnMapClick(SceneLayer.Input.IsHotKeyDown("MapFollowModifier"));
						if (flag)
						{
							HandleClickTimeChange(isDoubleClick);
							if (TaleWorlds.InputSystem.Input.IsGamepadActive)
							{
								if (visualOfSelectedEntity.IsMobileEntity)
								{
									if (visualOfSelectedEntity.IsInSameFaction(PartyBase.MainParty.MapFaction))
									{
										UISoundsHelper.PlayUISound("event:/ui/campaign/click_party");
									}
									else
									{
										UISoundsHelper.PlayUISound("event:/ui/campaign/click_party_enemy");
									}
								}
								else if (visualOfSelectedEntity.IsInSameFaction(PartyBase.MainParty.MapFaction))
								{
									UISoundsHelper.PlayUISound("event:/ui/campaign/click_settlement");
								}
								else
								{
									UISoundsHelper.PlayUISound("event:/ui/campaign/click_settlement_enemy");
								}
							}
						}
						MobileParty.MainParty.ForceAiNoPathMode = false;
					}
				}
			}
			else if (mouseOverFaceIndex.IsValid() || flag4)
			{
				if (!MobileParty.MainParty.IsInRaftState)
				{
					if (flag4)
					{
						MobileParty.MainParty.Position = intersectionPoint;
						MobileParty.MainParty.SetMoveModeHold();
						if (NavigationHelper.IsPositionValidForNavigationType(new CampaignVec2(intersectionPoint.ToVec2(), isOnLand: true), MobileParty.MainParty.IsCurrentlyAtSea ? MobileParty.NavigationType.Default : MobileParty.NavigationType.Naval) || NavigationHelper.IsPositionValidForNavigationType(new CampaignVec2(intersectionPoint.ToVec2(), isOnLand: false), MobileParty.MainParty.IsCurrentlyAtSea ? MobileParty.NavigationType.Default : MobileParty.NavigationType.Naval))
						{
							MobileParty.MainParty.ChangeIsCurrentlyAtSeaCheat();
						}
						if (MobileParty.MainParty.Army != null)
						{
							foreach (MobileParty attachedParty in MobileParty.MainParty.Army.LeaderParty.AttachedParties)
							{
								attachedParty.Position = intersectionPoint;
							}
						}
						foreach (MobileParty item in MobileParty.All)
						{
							item.Party.UpdateVisibilityAndInspected(MobileParty.MainParty.Position);
						}
						foreach (Settlement item2 in Settlement.All)
						{
							item2.Party.UpdateVisibilityAndInspected(MobileParty.MainParty.Position);
						}
						MBDebug.Print("main party cheat move! - " + intersectionPoint.X + " " + intersectionPoint.Y);
						flag2 = true;
						flag3 = true;
					}
					else
					{
						flag2 = NavigationHelper.CanPlayerNavigateToPosition(intersectionPoint, out var _);
					}
				}
				if (flag2 && MapCameraView.ProcessCameraInput && MobileParty.MainParty.MapEvent == null)
				{
					if (!flag3)
					{
						MapState.ProcessTravel(intersectionPoint);
					}
					HandleClickTimeChange(isDoubleClick);
				}
				OnTerrainClick();
			}
		}
		Vec3 intersectionPoint2 = intersectionPoint.AsVec3();
		if (!SandBoxViewVisualManager.OnMouseClick(visualOfSelectedEntity, intersectionPoint2, mouseOverFaceIndex, isDoubleClick) && !flag)
		{
			OnTerrainClick();
		}
		if (flag2)
		{
			MapCameraView.HandleLeftMouseButtonClick(SceneLayer.Input.GetIsMouseActive());
		}
	}

	private void OnTerrainClick()
	{
		_mapViewsContainer.Foreach(delegate(MapView view)
		{
			view.OnMapTerrainClick();
		});
		MapCursor.OnMapTerrainClick();
	}

	public void OnSiegeEngineFrameClick(MatrixFrame siegeFrame)
	{
		_mapViewsContainer.Foreach(delegate(MapView view)
		{
			view.OnSiegeEngineClick(siegeFrame);
		});
	}

	private void HandleClickTimeChange(bool isDoubleClick)
	{
		switch (BannerlordConfig.MapDoubleClickBehavior)
		{
		case 0:
			if (!isDoubleClick && Campaign.Current.TimeControlMode == CampaignTimeControlMode.StoppableFastForward)
			{
				_waitForDoubleClickUntilTime = Time.ApplicationTime + 0.3f;
				Campaign.Current.TimeControlMode = CampaignTimeControlMode.StoppableFastForward;
			}
			else
			{
				Campaign.Current.TimeControlMode = (isDoubleClick ? CampaignTimeControlMode.StoppableFastForward : CampaignTimeControlMode.StoppablePlay);
			}
			break;
		case 1:
			if (isDoubleClick)
			{
				Campaign.Current.TimeControlMode = ((Campaign.Current.TimeControlMode == CampaignTimeControlMode.StoppableFastForward) ? CampaignTimeControlMode.StoppablePlay : CampaignTimeControlMode.StoppableFastForward);
			}
			else
			{
				Campaign.Current.TimeControlMode = ((Campaign.Current.TimeControlMode == CampaignTimeControlMode.StoppableFastForward) ? CampaignTimeControlMode.StoppableFastForward : CampaignTimeControlMode.StoppablePlay);
			}
			break;
		case 2:
			Campaign.Current.TimeControlMode = ((Campaign.Current.TimeControlMode == CampaignTimeControlMode.StoppableFastForward) ? CampaignTimeControlMode.StoppableFastForward : CampaignTimeControlMode.StoppablePlay);
			break;
		}
	}

	void IMapStateHandler.AfterTick(float dt)
	{
		if (ScreenManager.TopScreen == this)
		{
			TickVisuals(dt);
			SceneLayer sceneLayer = SceneLayer;
			if (sceneLayer != null && sceneLayer.Input.IsGameKeyPressed(54))
			{
				Campaign.Current.SaveHandler.QuickSaveCurrentGame();
			}
		}
		base.DebugInput.IsHotKeyPressed("MapScreenHotkeyShowPos");
	}

	protected virtual MenuViewContext CreateMenuViewContext(MenuContext menuContext)
	{
		return new MenuViewContext(this, menuContext);
	}

	protected virtual bool TickNavigationInput(float dt)
	{
		if (SceneLayer.Input.IsShiftDown() || SceneLayer.Input.IsControlDown())
		{
			return false;
		}
		bool flag = false;
		if (SceneLayer.Input.IsGameKeyPressed(38) && _navigationHandler.GetPermission(MapNavigationItemType.Inventory).IsAuthorized)
		{
			OpenInventory();
			flag = true;
		}
		else if (SceneLayer.Input.IsGameKeyPressed(43) && _navigationHandler.GetPermission(MapNavigationItemType.Party).IsAuthorized)
		{
			OpenParty();
			flag = true;
		}
		else if (SceneLayer.Input.IsGameKeyPressed(39) && !IsInArmyManagement && !IsMapCheatsActive && !IsMapIncidentActive && !IsOverlayContextMenuEnabled)
		{
			OpenEncyclopedia();
			flag = true;
		}
		else if (SceneLayer.Input.IsGameKeyPressed(36) && !IsInArmyManagement && !IsMarriageOfferPopupActive && !IsHeirSelectionPopupActive && !IsMapCheatsActive && !IsMapIncidentActive && !EncyclopediaScreenManager.IsEncyclopediaOpen && !IsOverlayContextMenuEnabled)
		{
			OpenBannerEditorScreen();
			flag = true;
		}
		else if (SceneLayer.Input.IsGameKeyPressed(40) && _navigationHandler.GetPermission(MapNavigationItemType.Kingdom).IsAuthorized)
		{
			OpenKingdom();
			flag = true;
		}
		else if (SceneLayer.Input.IsGameKeyPressed(42) && _navigationHandler.GetPermission(MapNavigationItemType.Quest).IsAuthorized)
		{
			OpenQuestsScreen();
			flag = true;
		}
		else if (SceneLayer.Input.IsGameKeyPressed(41) && _navigationHandler.GetPermission(MapNavigationItemType.Clan).IsAuthorized)
		{
			OpenClanScreen();
			flag = true;
		}
		else if (SceneLayer.Input.IsGameKeyPressed(37) && _navigationHandler.GetPermission(MapNavigationItemType.CharacterDeveloper).IsAuthorized)
		{
			OpenCharacterDevelopmentScreen();
			flag = true;
		}
		else if (SceneLayer.Input.IsHotKeyReleased("ToggleEscapeMenu"))
		{
			if (!_mapViewsContainer.IsThereAnyViewIsEscaped() && !ScreenFadeController.IsFadeActive)
			{
				OpenEscapeMenu();
				flag = true;
			}
		}
		else if (SceneLayer.Input.IsGameKeyPressed(44))
		{
			OpenFaceGeneratorScreen();
			flag = true;
		}
		else if (TaleWorlds.InputSystem.Input.IsGamepadActive)
		{
			flag = HandleCheatMenuInput(dt);
		}
		if (flag)
		{
			MapCursor.SetVisible(value: false);
		}
		return flag;
	}

	void IMapStateHandler.AfterWaitTick(float dt)
	{
		TickNavigationInput(dt);
	}

	private bool HandleCheatMenuInput(float dt)
	{
		if (!IsMapCheatsActive && Input.IsKeyDown(InputKey.ControllerLBumper) && Input.IsKeyDown(InputKey.ControllerRTrigger) && Input.IsKeyDown(InputKey.ControllerLDown))
		{
			_cheatPressTimer += dt;
			if (_cheatPressTimer > 0.55f)
			{
				OpenGameplayCheats();
			}
			return true;
		}
		_cheatPressTimer = 0f;
		return false;
	}

	void IMapStateHandler.OnRefreshState()
	{
		if (!(Game.Current.GameStateManager.ActiveState is MapState))
		{
			return;
		}
		if (MobileParty.MainParty.Army != null && _armyOverlay == null)
		{
			AddArmyOverlay(MapOverlayType.Army);
		}
		else if (MobileParty.MainParty.Army == null && _armyOverlay != null)
		{
			_mapViewsContainer.ForeachReverse(delegate(MapView view)
			{
				view.OnArmyLeft();
			});
			_mapViewsContainer.ForeachReverse(delegate(MapView view)
			{
				view.OnDispersePlayerLeadedArmy();
			});
		}
	}

	void IMapStateHandler.OnExitingMenuMode()
	{
		_latestMenuContext = null;
	}

	void IMapStateHandler.OnEnteringMenuMode(MenuContext menuContext)
	{
		_latestMenuContext = menuContext;
	}

	void IMapStateHandler.OnMainPartyEncounter()
	{
		_mapViewsContainer.ForeachReverse(delegate(MapView view)
		{
			view.OnMainPartyEncounter();
		});
	}

	void IMapStateHandler.OnIncidentStarted(Incident incident)
	{
		if (GetMapView<MapIncidentView>() == null)
		{
			AddMapView<MapIncidentView>(new object[1] { incident });
		}
	}

	void IMapStateHandler.OnSignalPeriodicEvents()
	{
		DeleteMarkedPeriodicEvents();
	}

	void IMapStateHandler.OnBattleSimulationStarted(BattleSimulation battleSimulation)
	{
		IsInBattleSimulation = true;
		_battleSimulationView = AddMapView<BattleSimulationMapView>(new object[1] { CreateSimulationScoreboardDatasource(battleSimulation) });
	}

	protected virtual SPScoreboardVM CreateSimulationScoreboardDatasource(BattleSimulation battleSimulation)
	{
		return new SPScoreboardVM(new SandboxSimulationBattleScoreContext(battleSimulation), battleSimulation);
	}

	void IMapStateHandler.OnBattleSimulationEnded()
	{
		IsInBattleSimulation = false;
		RemoveMapView(_battleSimulationView);
		_battleSimulationView = null;
	}

	void IMapStateHandler.OnSiegeEngineClick(MatrixFrame siegeEngineFrame)
	{
		MapCameraView.SiegeEngineClick(siegeEngineFrame);
	}

	void IGameStateListener.OnInitialize()
	{
	}

	void IMapStateHandler.OnPlayerSiegeActivated()
	{
	}

	void IMapStateHandler.OnPlayerSiegeDeactivated()
	{
	}

	public void SetIsMapCheatsActive(bool isMapCheatsActive)
	{
		if (IsMapCheatsActive != isMapCheatsActive)
		{
			IsMapCheatsActive = isMapCheatsActive;
			_cheatPressTimer = 0f;
		}
	}

	void IMapStateHandler.OnGameplayCheatsEnabled()
	{
		OpenGameplayCheats();
	}

	void IGameStateListener.OnActivate()
	{
	}

	void IGameStateListener.OnDeactivate()
	{
	}

	void IMapStateHandler.OnMapConversationStarts(ConversationCharacterData playerCharacterData, ConversationCharacterData conversationPartnerData)
	{
		HandleMapConversationInit(playerCharacterData, conversationPartnerData);
	}

	private void HandleMapConversationInit(ConversationCharacterData playerCharacterData, ConversationCharacterData conversationPartnerData)
	{
		_mapViewsContainer.ForeachReverse(delegate(MapView view)
		{
			view.OnMapConversationStart();
		});
		_menuViewContext?.OnMapConversationActivated();
		_conversationView.InitializeConversation(playerCharacterData, conversationPartnerData);
		MapCursor.SetVisible(value: false);
		HandleIfSceneIsReady();
	}

	void IMapStateHandler.OnMapConversationOver()
	{
		_mapViewsContainer.ForeachReverse(delegate(MapView view)
		{
			view.OnMapConversationOver();
		});
		_menuViewContext?.OnMapConversationDeactivated();
		_conversationView.FinalizeConversation();
		_activatedFrameNo = Utilities.EngineFrameNo;
		HandleIfSceneIsReady();
	}

	private void InitializeVisuals()
	{
		InactiveLightMeshes = new List<Mesh>();
		ActiveLightMeshes = new List<Mesh>();
		MapScene mapScene = Campaign.Current.MapSceneWrapper as MapScene;
		MapCursor.Initialize(this);
		_pointTargetWindDirectionDecal = DecalEntity.Create(mapScene.Scene, "decal_map_circle_wind", "MainPartyTargetLocationWindIndicatorDecal");
		_pointTargetInnerDecal = DecalEntity.Create(mapScene.Scene, "map_circle_decal", "InnerPointTarget");
		_pointTargetOuterDecal = DecalEntity.Create(mapScene.Scene, "map_circle_decal", "OuterPointTarget");
		_partyHoverOutlineDecal = DecalEntity.Create(mapScene.Scene, "map_circle_decal", "MapOutlineDecal");
		_settlementHoverOutlineDecal = DecalEntity.Create(mapScene.Scene, "decal_city_circle_a", "SettlementOutlineDecal");
		_townCircleDecal = DecalEntity.Create(mapScene.Scene, "decal_city_circle_a", "TownCircle");
		SandBoxViewSubModule.SandBoxViewVisualManager.AddEntityComponent<MapTracksVisualManager>();
		SandBoxViewSubModule.SandBoxViewVisualManager.AddEntityComponent<MapWeatherVisualManager>();
		SandBoxViewSubModule.SandBoxViewVisualManager.AddEntityComponent<MapAudioManager>();
		SandBoxViewSubModule.SandBoxViewVisualManager.AddEntityComponent<MobilePartyVisualManager>();
		SandBoxViewSubModule.SandBoxViewVisualManager.AddEntityComponent<SettlementVisualManager>();
		ContourMaskEntity = GameEntity.CreateEmpty(mapScene.Scene);
		ContourMaskEntity.Name = "aContourMask";
	}

	public void SetIsInTownManagement(bool isInTownManagement)
	{
		if (IsInTownManagement != isInTownManagement)
		{
			IsInTownManagement = isInTownManagement;
		}
	}

	public void SetIsInHideoutTroopManage(bool isInHideoutTroopManage)
	{
		if (IsInHideoutTroopManage != isInHideoutTroopManage)
		{
			IsInHideoutTroopManage = isInHideoutTroopManage;
		}
	}

	public void SetIsInArmyManagement(bool isInArmyManagement)
	{
		if (IsInArmyManagement != isInArmyManagement)
		{
			IsInArmyManagement = isInArmyManagement;
			if (!IsInArmyManagement)
			{
				_menuViewContext?.OnResume();
			}
		}
	}

	public void SetIsOverlayContextMenuActive(bool isOverlayContextMenuEnabled)
	{
		if (IsOverlayContextMenuEnabled != isOverlayContextMenuEnabled)
		{
			IsOverlayContextMenuEnabled = isOverlayContextMenuEnabled;
		}
	}

	public void SetIsInRecruitment(bool isInRecruitment)
	{
		if (IsInRecruitment != isInRecruitment)
		{
			IsInRecruitment = isInRecruitment;
		}
	}

	public void SetIsBarExtended(bool isBarExtended)
	{
		if (IsBarExtended != isBarExtended)
		{
			IsBarExtended = isBarExtended;
		}
	}

	public void SetIsMarriageOfferPopupActive(bool isMarriageOfferPopupActive)
	{
		if (IsMarriageOfferPopupActive != isMarriageOfferPopupActive)
		{
			IsMarriageOfferPopupActive = isMarriageOfferPopupActive;
		}
	}

	public void SetIsInCampaignOptions(bool isInCampaignOptions)
	{
		if (IsInCampaignOptions != isInCampaignOptions)
		{
			IsInCampaignOptions = isInCampaignOptions;
		}
	}

	public void SetIsMapIncidentActive(bool isMapIncidentActive)
	{
		if (IsMapIncidentActive != isMapIncidentActive)
		{
			IsMapIncidentActive = isMapIncidentActive;
		}
	}

	private void TickVisuals(float realDt)
	{
		if (!MapScene.IsLoadingFinished())
		{
			MapScene.HandleCurrentFrameTickEntities();
			return;
		}
		if (DisableVisualTicks)
		{
			MapScene.ClearCurrentFrameTickEntities();
			return;
		}
		MapScene.TimeOfDay = CampaignTime.Now.CurrentHourInDay;
		Campaign.Current.Models.MapWeatherModel.GetSeasonTimeFactorOfCampaignTime(CampaignTime.Now, out var timeFactorForSnow, out var _, snapCampaignTimeToWeatherPeriod: false);
		MBMapScene.SetSeasonTimeFactor(MapScene, timeFactorForSnow);
		MBMapScene.TickVisuals(MapScene, Campaign.CurrentTime % (float)CampaignTime.HoursInDay, _tickedMapMeshes);
		if (IsReady)
		{
			SandBoxViewVisualManager.VisualTick(this, realDt, Campaign.Current.CampaignDt);
			TickStepSounds(realDt);
			TickCircles();
		}
		MBWindowManager.PreDisplay();
	}

	public void SetMouseVisible(bool value)
	{
		SceneLayer.InputRestrictions.SetMouseVisibility(value);
	}

	public void SetIsHeirSelectionPopupActive(bool isHeirSelectionPopupActive)
	{
		if (IsHeirSelectionPopupActive != isHeirSelectionPopupActive)
		{
			IsHeirSelectionPopupActive = isHeirSelectionPopupActive;
		}
	}

	public bool GetMouseVisible()
	{
		return MBMapScene.GetMouseVisible();
	}

	public void RestartAmbientSounds()
	{
		if (MapScene != null)
		{
			MapScene.ResumeSceneSounds();
		}
	}

	void IGameStateListener.OnFinalize()
	{
	}

	public void PauseAmbientSounds()
	{
		if (MapScene != null)
		{
			MapScene.PauseSceneSounds();
		}
	}

	private void CollectTickableMapMeshes()
	{
		_tickedMapEntities = MapScene.FindEntitiesWithTag("ticked_map_entity").ToArray();
		_tickedMapMeshes = new Mesh[_tickedMapEntities.Length];
		for (int i = 0; i < _tickedMapEntities.Length; i++)
		{
			_tickedMapMeshes[i] = _tickedMapEntities[i].GetFirstMesh();
		}
	}

	public MBCampaignEvent CreatePeriodicUIEvent(CampaignTime triggerPeriod, CampaignTime initialWait)
	{
		MBCampaignEvent mBCampaignEvent = new MBCampaignEvent(triggerPeriod, initialWait);
		_periodicCampaignUIEvents.Add(mBCampaignEvent);
		return mBCampaignEvent;
	}

	private void DeleteMarkedPeriodicEvents()
	{
		for (int num = _periodicCampaignUIEvents.Count - 1; num >= 0; num--)
		{
			if (_periodicCampaignUIEvents[num].isEventDeleted)
			{
				_periodicCampaignUIEvents.RemoveAt(num);
			}
		}
	}

	public void DeletePeriodicUIEvent(MBCampaignEvent campaignEvent)
	{
		campaignEvent.isEventDeleted = true;
	}

	private static float CalculateCameraElevation(float cameraDistance)
	{
		return cameraDistance * 0.5f * 0.015f + 0.35f;
	}

	public void OpenOptions()
	{
		ScreenManager.PushScreen(ViewCreator.CreateOptionsScreen(fromMainMenu: false));
	}

	public void OpenEncyclopedia()
	{
		Campaign.Current.EncyclopediaManager.GoToLink("LastPage", "");
	}

	public void OpenSaveLoad(bool isSaving)
	{
		ScreenManager.PushScreen(SandBoxViewCreator.CreateSaveLoadScreen(isSaving));
	}

	public void CloseEscapeMenu()
	{
		OnEscapeMenuToggled();
	}

	public void OpenEscapeMenu()
	{
		OnEscapeMenuToggled(isOpened: true);
	}

	private void OpenGameplayCheats()
	{
		_mapCheatsView = AddMapView<MapCheatsView>(Array.Empty<object>());
		IsMapCheatsActive = true;
	}

	public void CloseGameplayCheats()
	{
		if (_mapCheatsView != null)
		{
			RemoveMapView(_mapCheatsView);
		}
		else
		{
			Debug.FailedAssert("Requested remove map cheats but cheats is not enabled", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\SandBox.View\\Map\\MapScreen.cs", "CloseGameplayCheats", 2577);
		}
	}

	public void CloseCampaignOptions()
	{
		if (_campaignOptionsView == null)
		{
			Debug.FailedAssert("Trying to close campaign options when it's not set", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\SandBox.View\\Map\\MapScreen.cs", "CloseCampaignOptions", 2585);
			_campaignOptionsView = GetMapView<MapCampaignOptionsView>();
			if (_campaignOptionsView == null)
			{
				Debug.FailedAssert("Trying to close campaign options when it's not open", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\SandBox.View\\Map\\MapScreen.cs", "CloseCampaignOptions", 2590);
				IsInCampaignOptions = false;
				_campaignOptionsView = null;
				return;
			}
		}
		if (_campaignOptionsView != null)
		{
			RemoveMapView(_campaignOptionsView);
		}
		_campaignOptionsView = null;
		IsInCampaignOptions = false;
	}

	private List<EscapeMenuItemVM> GetEscapeMenuItems()
	{
		bool isMapConversationActive = _conversationView.IsConversationActive;
		bool cannotQuickSave = MBSaveLoad.IsMaxNumberOfSavesReached() && !MBSaveLoad.IsSaveGameFileExists(MBSaveLoad.ActiveSaveSlotName);
		if (cannotQuickSave && CampaignOptions.IsIronmanMode)
		{
			string activeSaveSlotName = MBSaveLoad.ActiveSaveSlotName;
			string[] saveFileNames = MBSaveLoad.GetSaveFileNames();
			for (int i = 0; i < saveFileNames.Length; i++)
			{
				if (saveFileNames[i] == activeSaveSlotName)
				{
					cannotQuickSave = false;
					break;
				}
			}
		}
		return new List<EscapeMenuItemVM>
		{
			new EscapeMenuItemVM(new TextObject("{=e139gKZc}Return to the Game"), delegate
			{
				OnEscapeMenuToggled();
			}, null, () => new Tuple<bool, TextObject>(item1: false, null), isPositiveBehaviored: true),
			new EscapeMenuItemVM(new TextObject("{=PXT6aA4J}Campaign Options"), delegate
			{
				_campaignOptionsView = AddMapView<MapCampaignOptionsView>(Array.Empty<object>());
				IsInCampaignOptions = true;
			}, null, () => new Tuple<bool, TextObject>(item1: false, null)),
			new EscapeMenuItemVM(new TextObject("{=NqarFr4P}Options"), delegate
			{
				OnEscapeMenuToggled();
				OpenOptions();
			}, null, () => new Tuple<bool, TextObject>(item1: false, null)),
			new EscapeMenuItemVM(new TextObject("{=bV75iwKa}Save"), delegate
			{
				OnEscapeMenuToggled();
				Campaign.Current.SaveHandler.QuickSaveCurrentGame();
			}, null, () => GetIsEscapeMenuOptionDisabledReason(isMapConversationActive, isIronmanMode: false, cannotQuickSave)),
			new EscapeMenuItemVM(new TextObject("{=e0KdfaNe}Save As"), delegate
			{
				OnEscapeMenuToggled();
				OpenSaveLoad(isSaving: true);
			}, null, () => GetIsEscapeMenuOptionDisabledReason(isMapConversationActive, CampaignOptions.IsIronmanMode, cannotQuickSave: false)),
			new EscapeMenuItemVM(new TextObject("{=9NuttOBC}Load"), delegate
			{
				OnEscapeMenuToggled();
				OpenSaveLoad(isSaving: false);
			}, null, () => GetIsEscapeMenuOptionDisabledReason(isMapConversationActive, CampaignOptions.IsIronmanMode, cannotQuickSave: false)),
			new EscapeMenuItemVM(new TextObject("{=AbEh2y8o}Save And Exit"), delegate
			{
				Campaign.Current.SaveHandler.QuickSaveCurrentGame();
				OnEscapeMenuToggled();
				InformationManager.HideInquiry();
				_exitOnSaveOver = true;
			}, null, () => GetIsEscapeMenuOptionDisabledReason(isMapConversationActive, isIronmanMode: false, cannotQuickSave)),
			new EscapeMenuItemVM(new TextObject("{=RamV6yLM}Exit to Main Menu"), delegate
			{
				InformationManager.ShowInquiry(new InquiryData(GameTexts.FindText("str_exit").ToString(), GameTexts.FindText("str_mission_exit_query").ToString(), isAffirmativeOptionShown: true, isNegativeOptionShown: true, GameTexts.FindText("str_yes").ToString(), GameTexts.FindText("str_no").ToString(), OnExitToMainMenu, delegate
				{
					OnEscapeMenuToggled();
				}));
			}, null, () => GetIsEscapeMenuOptionDisabledReason(isMapConversationActive: false, CampaignOptions.IsIronmanMode, cannotQuickSave: false))
		};
	}

	private Tuple<bool, TextObject> GetIsEscapeMenuOptionDisabledReason(bool isMapConversationActive, bool isIronmanMode, bool cannotQuickSave)
	{
		if (isIronmanMode)
		{
			return new Tuple<bool, TextObject>(item1: true, GameTexts.FindText("str_pause_menu_disabled_hint", "IronmanMode"));
		}
		if (isMapConversationActive)
		{
			return new Tuple<bool, TextObject>(item1: true, GameTexts.FindText("str_pause_menu_disabled_hint", "OngoingConversation"));
		}
		if (cannotQuickSave)
		{
			return new Tuple<bool, TextObject>(item1: true, GameTexts.FindText("str_pause_menu_disabled_hint", "SaveLimitReached"));
		}
		return new Tuple<bool, TextObject>(item1: false, null);
	}

	private void OpenParty()
	{
		if (Hero.MainHero != null && !Hero.MainHero.IsPrisoner && !Hero.MainHero.IsDead)
		{
			PartyScreenHelper.OpenScreenAsNormal();
		}
	}

	public void OpenInventory()
	{
		if (Hero.MainHero != null)
		{
			Hero mainHero = Hero.MainHero;
			if (mainHero != null && !mainHero.IsDead)
			{
				InventoryScreenHelper.OpenScreenAsInventory();
			}
		}
	}

	private void OpenKingdom()
	{
		if (Hero.MainHero != null)
		{
			Hero mainHero = Hero.MainHero;
			if (mainHero != null && !mainHero.IsDead && Hero.MainHero.MapFaction.IsKingdomFaction)
			{
				KingdomState gameState = Game.Current.GameStateManager.CreateState<KingdomState>();
				Game.Current.GameStateManager.PushState(gameState);
			}
		}
	}

	private void OnExitToMainMenu()
	{
		OnEscapeMenuToggled();
		InformationManager.HideInquiry();
		OnExit();
	}

	private void OpenQuestsScreen()
	{
		if (Hero.MainHero != null)
		{
			Hero mainHero = Hero.MainHero;
			if (mainHero != null && !mainHero.IsDead)
			{
				Game.Current.GameStateManager.PushState(Game.Current.GameStateManager.CreateState<QuestsState>());
			}
		}
	}

	private void OpenClanScreen()
	{
		if (Hero.MainHero != null)
		{
			Hero mainHero = Hero.MainHero;
			if (mainHero != null && !mainHero.IsDead)
			{
				Game.Current.GameStateManager.PushState(Game.Current.GameStateManager.CreateState<ClanState>());
			}
		}
	}

	private void OpenCharacterDevelopmentScreen()
	{
		if (Hero.MainHero != null)
		{
			Hero mainHero = Hero.MainHero;
			if (mainHero != null && !mainHero.IsDead)
			{
				Game.Current.GameStateManager.PushState(Game.Current.GameStateManager.CreateState<CharacterDeveloperState>());
			}
		}
	}

	public void OpenFacegenScreenAux()
	{
		OpenFaceGeneratorScreen();
	}

	public bool IsCameraLockedToPlayerParty()
	{
		return MapCameraView.IsCameraLockedToPlayerParty();
	}

	public void FastMoveCameraToMainParty()
	{
		MapCameraView.FastMoveCameraToMainParty();
	}

	public void ResetCamera(bool resetDistance, bool teleportToMainParty)
	{
		MapCameraView.ResetCamera(resetDistance, teleportToMainParty);
	}

	public void TeleportCameraToMainParty()
	{
		MapCameraView.TeleportCameraToMainParty();
	}

	void IChatLogHandlerScreen.TryUpdateChatLogLayerParameters(ref bool isTeamChatAvailable, ref bool inputEnabled, ref bool isToggleChatHintAvailable, ref bool isMouseVisible, ref InputContext inputContext)
	{
		if (SceneLayer != null)
		{
			inputEnabled = true;
			isToggleChatHintAvailable = true;
			inputContext = SceneLayer.Input;
			isMouseVisible = SceneLayer.InputRestrictions.MouseVisibility;
		}
	}

	private void TickCircles()
	{
		bool flag = false;
		bool flag2 = false;
		bool flag3 = false;
		float num = 0.5f;
		float num2 = 0.5f;
		int num3 = 0;
		int num4 = 0;
		uint factor1Linear = 4293199122u;
		uint factor1Linear2 = 4293199122u;
		uint factor1Linear3 = 4293199122u;
		bool flag4 = false;
		bool flag5 = false;
		MatrixFrame frame = MatrixFrame.Identity;
		PartyBase partyBase = null;
		if (MobileParty.MainParty.PartyMoveMode == MoveModeType.Point && MobileParty.MainParty.DefaultBehavior != AiBehavior.GoToSettlement && MobileParty.MainParty.DefaultBehavior != AiBehavior.Hold && !MobileParty.MainParty.ForceAiNoPathMode && MobileParty.MainParty.Ai.AiBehaviorInteractable == null && MobileParty.MainParty.MapEvent == null && MobileParty.MainParty.TargetPosition.DistanceSquared(MobileParty.MainParty.Position) > 0.01f)
		{
			flag3 = true;
			flag = true;
			num = 0.238846f;
			num2 = 0.278584f;
			num3 = 4;
			num4 = 5;
			factor1Linear = 4293993473u;
			factor1Linear2 = 4293993473u;
			frame.origin = MobileParty.MainParty.TargetPosition.AsVec3();
			flag5 = true;
		}
		else
		{
			if (MobileParty.MainParty.PartyMoveMode == MoveModeType.Party && MobileParty.MainParty.MoveTargetParty != null && MobileParty.MainParty.MoveTargetParty.IsVisible)
			{
				partyBase = ((MobileParty.MainParty.MoveTargetParty.CurrentSettlement != null && !MobileParty.MainParty.MoveTargetParty.CurrentSettlement.IsHideout) ? MobileParty.MainParty.MoveTargetParty.CurrentSettlement.Party : MobileParty.MainParty.MoveTargetParty.Party);
			}
			else if (MobileParty.MainParty.DefaultBehavior == AiBehavior.GoToSettlement && MobileParty.MainParty.TargetSettlement != null)
			{
				partyBase = MobileParty.MainParty.TargetSettlement.Party;
			}
			if (partyBase != null)
			{
				if (partyBase.IsMobile)
				{
					MapEntityVisual<PartyBase> partyVisual = GetPartyVisual(partyBase);
					if (partyVisual != null)
					{
						frame = partyVisual.CircleLocalFrame;
						flag3 = true;
						num3 = GetCircleIndex();
						float num5 = 1.2f;
						if (partyBase.MobileParty.IsCurrentlyAtSea)
						{
							num5 = 2.5f;
						}
						factor1Linear = GetDecalColorForParty(partyBase);
						num = frame.rotation.GetScaleVector().x * num5;
					}
				}
				else
				{
					frame = SettlementVisualManager.Current.GetSettlementVisual(partyBase.Settlement).CircleLocalFrame;
					if (partyBase.IsSettlement && partyBase.Settlement.IsFortification)
					{
						flag4 = true;
						flag2 = true;
						factor1Linear3 = GetDecalColorForParty(partyBase);
						num = frame.rotation.GetScaleVector().x * 1.3f;
					}
					else
					{
						flag3 = true;
						num3 = 5;
						factor1Linear = GetDecalColorForParty(partyBase);
						num = frame.rotation.GetScaleVector().x * 1.2f;
					}
				}
				if (!flag4)
				{
					frame.origin = partyBase.Position.AsVec3();
					if (partyBase.IsMobile)
					{
						frame.origin += (partyBase.MobileParty.EventPositionAdder + partyBase.MobileParty.ArmyPositionAdder).ToVec3();
					}
				}
			}
		}
		if (flag5)
		{
			float value = (Instance.MapCameraView.CameraDistance + 80f) * (Instance.MapCameraView.CameraDistance + 80f) / 5000f;
			value = TaleWorlds.Library.MathF.Clamp(value, 0.2f, 45f);
			num *= value;
			num2 *= value;
		}
		if (partyBase == null)
		{
			_targetCircleRotationStartTime = 0f;
		}
		else if (_targetCircleRotationStartTime == 0f)
		{
			_targetCircleRotationStartTime = MBCommon.GetApplicationTime();
		}
		Vec3 normalAt = Instance.MapScene.GetNormalAt(frame.origin.AsVec2);
		MatrixFrame frame2 = MatrixFrame.Identity;
		frame2.origin = frame.origin;
		MobileParty mainParty = MobileParty.MainParty;
		bool flag6 = mainParty != null && !mainParty.TargetPosition.IsOnLand;
		bool flag7 = partyBase != null;
		frame2.rotation.u = normalAt;
		MatrixFrame frame3 = frame2;
		frame2.rotation.ApplyScaleLocal(new Vec3(num, num, num));
		frame3.rotation.ApplyScaleLocal(new Vec3(num2, num2, num2));
		_townCircleDecal.GameEntity.SetVisibilityExcludeParents(flag2);
		_pointTargetInnerDecal.GameEntity.SetVisibilityExcludeParents(flag3 && (!flag6 || flag7));
		_pointTargetOuterDecal.GameEntity.SetVisibilityExcludeParents(flag && (!flag6 || flag7));
		_pointTargetWindDirectionDecal.GameEntity.SetVisibilityExcludeParents(flag3 && flag6 && !flag7);
		if (flag3)
		{
			if (flag6 && !flag7)
			{
				float num6 = num + 0.15f;
				MatrixFrame frame4 = frame2;
				frame4.rotation = Mat3.CreateMat3WithForward(Campaign.Current.Models.MapWeatherModel.GetWindForPosition(MobileParty.MainParty.TargetPosition).ToVec3().NormalizedCopy());
				frame4.rotation.ApplyScaleLocal(new Vec3(num6, num6, num6));
				frame4.rotation.RotateAboutUp(System.MathF.PI / 2f);
				_pointTargetWindDirectionDecal.Decal.SetFactor1Linear(factor1Linear);
				_pointTargetWindDirectionDecal.Decal.SetVectorArgument(1f, 1f, 0f, 0f);
				_pointTargetWindDirectionDecal.GameEntity.SetGlobalFrame(in frame4);
			}
			else
			{
				_pointTargetInnerDecal.Decal.SetVectorArgument(0.166f, 1f, 0.166f * (float)num3, 0f);
				_pointTargetInnerDecal.Decal.SetFactor1Linear(factor1Linear);
				_pointTargetInnerDecal.GameEntity.SetGlobalFrame(in frame2);
			}
		}
		if (flag)
		{
			_pointTargetOuterDecal.Decal.SetVectorArgument(0.166f, 1f, 0.166f * (float)num4, 0f);
			_pointTargetOuterDecal.Decal.SetFactor1Linear(factor1Linear2);
			_pointTargetOuterDecal.GameEntity.SetGlobalFrame(in frame3);
		}
		if (flag2)
		{
			_townCircleDecal.Decal.SetVectorArgument(1f, 1f, 0f, 0f);
			_townCircleDecal.Decal.SetFactor1Linear(factor1Linear3);
			_townCircleDecal.GameEntity.SetGlobalFrame(in frame);
		}
		MatrixFrame frame5 = MatrixFrame.Identity;
		if (Instance.CurrentVisualOfTooltip != null && (partyBase == null || Instance.CurrentVisualOfTooltip != GetPartyVisual(partyBase)) && Instance.CurrentVisualOfTooltip is MapEntityVisual<PartyBase> mapEntityVisual)
		{
			Instance.MapCursor.OnAnotherEntityHighlighted();
			if (mapEntityVisual != null)
			{
				flag4 = mapEntityVisual.MapEntity.IsSettlement && mapEntityVisual.MapEntity.Settlement.IsFortification;
				if (flag4)
				{
					frame5 = mapEntityVisual.CircleLocalFrame;
					_settlementHoverOutlineDecal.Decal.SetFactor1Linear(GetDecalColorForParty(mapEntityVisual.MapEntity));
				}
				else
				{
					Vec3 origin = _settlementHoverOutlineDecal.GameEntity.GetGlobalFrame().origin;
					frame5.origin = mapEntityVisual.GetVisualPosition() + mapEntityVisual.CircleLocalFrame.origin;
					frame5.rotation = mapEntityVisual.CircleLocalFrame.rotation;
					_partyHoverOutlineDecal.Decal.SetFactor1Linear(GetDecalColorForParty(mapEntityVisual.MapEntity));
					_partyHoverOutlineDecal.Decal.SetVectorArgument(0.166f, 1f, 0.83f, 0f);
					ref Vec3 origin2 = ref frame5.origin;
					float z;
					if (!(origin.AsVec2 != frame5.origin.AsVec2))
					{
						z = origin.z;
					}
					else
					{
						PartyBase mapEntity = mapEntityVisual.MapEntity;
						z = ((mapEntity != null && mapEntity.MobileParty?.IsCurrentlyAtSea == true) ? frame5.origin.z : Instance.MapScene.GetTerrainHeight(frame5.origin.AsVec2));
					}
					origin2.z = z;
				}
				if (flag4)
				{
					_settlementHoverOutlineDecal.GameEntity.SetGlobalFrame(in frame5);
					_settlementHoverOutlineDecal.GameEntity.SetVisibilityExcludeParents(visible: true);
					_partyHoverOutlineDecal.GameEntity.SetVisibilityExcludeParents(visible: false);
					return;
				}
				if (mapEntityVisual.MapEntity.IsMobile && mapEntityVisual.MapEntity.MobileParty.IsCurrentlyAtSea)
				{
					frame5.Scale(Vec3.One * 2.5f);
				}
				_partyHoverOutlineDecal.GameEntity.SetGlobalFrame(in frame5);
				_settlementHoverOutlineDecal.GameEntity.SetVisibilityExcludeParents(visible: false);
				_partyHoverOutlineDecal.GameEntity.SetVisibilityExcludeParents(visible: true);
			}
			else
			{
				_settlementHoverOutlineDecal.GameEntity.SetVisibilityExcludeParents(visible: false);
				_partyHoverOutlineDecal.GameEntity.SetVisibilityExcludeParents(visible: false);
			}
		}
		else
		{
			_settlementHoverOutlineDecal.GameEntity.SetVisibilityExcludeParents(visible: false);
			_partyHoverOutlineDecal.GameEntity.SetVisibilityExcludeParents(visible: false);
		}
	}

	private int GetCircleIndex()
	{
		int num = (int)((MBCommon.GetApplicationTime() - _targetCircleRotationStartTime) / 0.1f) % 10;
		if (num >= 5)
		{
			num = 10 - num - 1;
		}
		return num;
	}

	private MapEntityVisual<PartyBase> GetPartyVisual(PartyBase party)
	{
		MapEntityVisual<PartyBase> mapEntityVisual = null;
		foreach (EntityVisualManagerBase<PartyBase> component in SandBoxViewSubModule.SandBoxViewVisualManager.GetComponents<EntityVisualManagerBase<PartyBase>>())
		{
			mapEntityVisual = component.GetVisualOfEntity(party);
			if (mapEntityVisual != null)
			{
				break;
			}
		}
		return mapEntityVisual;
	}

	private void TickStepSounds(float realDt)
	{
		if (NativeConfig.DisableSound || !(ScreenManager.TopScreen is MapScreen))
		{
			return;
		}
		_soundCalculationTime += realDt;
		if (IsSoundOn && Campaign.Current.CampaignDt > 0f)
		{
			MobileParty mainParty = MobileParty.MainParty;
			LocatableSearchData<MobileParty> data = MobileParty.StartFindingLocatablesAroundPosition(radius: mainParty.SeeingRange + 25f, position: mainParty.Position.ToVec2());
			for (MobileParty mobileParty = MobileParty.FindNextLocatable(ref data); mobileParty != null; mobileParty = MobileParty.FindNextLocatable(ref data))
			{
				if (!mobileParty.IsMilitia && !mobileParty.IsGarrison && !mobileParty.IsCurrentlyAtSea)
				{
					StepSounds(mobileParty);
				}
			}
		}
		if (_soundCalculationTime > 0.2f)
		{
			_soundCalculationTime -= 0.2f;
		}
	}

	private void StepSounds(MobileParty party)
	{
		if (!party.IsVisible || party.MemberRoster.TotalManCount <= 0)
		{
			return;
		}
		MobilePartyVisual partyVisual = MobilePartyVisualManager.Current.GetPartyVisual(party.Party);
		if (partyVisual.HumanAgentVisuals == null)
		{
			return;
		}
		TerrainType faceTerrainType = Campaign.Current.MapSceneWrapper.GetFaceTerrainType(party.CurrentNavigationFace);
		AgentVisuals agentVisuals = null;
		TerrainTypeSoundSlot soundType = TerrainTypeSoundSlot.Dismounted;
		if (partyVisual.CaravanMountAgentVisuals != null)
		{
			soundType = TerrainTypeSoundSlot.Caravan;
			agentVisuals = partyVisual.CaravanMountAgentVisuals;
		}
		else if (partyVisual.HumanAgentVisuals != null)
		{
			if (partyVisual.MountAgentVisuals != null)
			{
				soundType = TerrainTypeSoundSlot.Mounted;
				if (party.Army != null && party.AttachedParties.Count > 0)
				{
					soundType = TerrainTypeSoundSlot.ArmyMounted;
				}
				agentVisuals = partyVisual.MountAgentVisuals;
			}
			else
			{
				soundType = TerrainTypeSoundSlot.Dismounted;
				if (party.Army != null && party.AttachedParties.Count > 0)
				{
					soundType = TerrainTypeSoundSlot.ArmyDismounted;
				}
				agentVisuals = partyVisual.HumanAgentVisuals;
			}
		}
		if (party.AttachedTo == null)
		{
			MBMapScene.TickStepSound(MapScene, agentVisuals.GetVisuals(), (int)faceTerrainType, soundType, party.AttachedParties.Count);
		}
	}

	private uint GetDecalColorForParty(PartyBase targetParty)
	{
		if (FactionManager.IsAtWarAgainstFaction(targetParty.MapFaction, Hero.MainHero.MapFaction))
		{
			return 4292093218u;
		}
		if (DiplomacyHelper.IsSameFactionAndNotEliminated(targetParty.MapFaction, Hero.MainHero.MapFaction))
		{
			return 4284183827u;
		}
		if (DiplomacyHelper.HasAllianceWithFaction(targetParty.MapFaction, Hero.MainHero.MapFaction))
		{
			return 4279386828u;
		}
		return 4291596077u;
	}
}
