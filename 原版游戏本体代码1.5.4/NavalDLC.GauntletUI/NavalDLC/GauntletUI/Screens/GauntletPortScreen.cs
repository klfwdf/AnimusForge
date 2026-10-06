using System;
using System.Collections.Generic;
using System.Linq;
using Helpers;
using NavalDLC.Missions.NavalPhysics;
using NavalDLC.Missions.Objects;
using NavalDLC.View;
using NavalDLC.ViewModelCollection.Port;
using NavalDLC.ViewModelCollection.Port.PortScreenHandlers;
using SandBox.View;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.Engine.Screens;
using TaleWorlds.GauntletUI;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Objects;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.Screens;
using TaleWorlds.ScreenSystem;
using TaleWorlds.TwoDimension;

namespace NavalDLC.GauntletUI.Screens;

[GameStateScreen(typeof(PortState))]
public class GauntletPortScreen : ScreenBase, IGameStateListener, IChangeableScreen
{
	private struct CameraParameters
	{
		public float Azimuth;

		public float Inclination;

		public float Distance;

		public float Deviation;

		public CameraParameters(float azimuth, float inclination, float distance, float deviation)
		{
			Azimuth = azimuth;
			Inclination = inclination;
			Distance = distance;
			Deviation = deviation;
		}
	}

	private struct StaticCameraParameters
	{
		public float HorizontalRotationSensitivity;

		public float VerticalRotationSensitivity;

		public float ZoomSensitivity;

		public float SensitivityMappingMultiplier;

		public float DeviationSensitivityAtMinDistance;

		public float DeviationSensitivityAtMaxDistance;

		public float MinCameraInclination;

		public float MaxCameraInclinationAtMinDistance;

		public float MaxCameraInclinationAtMaxDistance;

		public float MinCameraDistance;

		public float MaxCameraDistance;

		public float MinCameraDistanceWhileInspectingPiece;

		public float CameraDeviationLimit;

		public float FocusDistanceAtMinDistance;

		public float FocusDistanceAtMaxDistance;

		public float ExtraHeightAtMinDistance;

		public float ExtraHeightAtMaxDistance;

		public StaticCameraParameters(float horizontalRotationSensitivity, float verticalRotationSensitivity, float zoomSensitivity, float sensitivityMappingMultiplier, float deviationSensitivityAtMinDistance, float deviationSensitivityAtMaxDistance, float minCameraInclination, float maxCameraInclinationAtMinDistance, float maxCameraInclinationAtMaxDistance, float minCameraDistance, float maxCameraDistance, float minCameraDistanceWhileInspectingPiece, float cameraDeviationLimit, float focusDistanceAtMinDistance, float focusDistanceAtMaxDistance, float extraHeightAtMinDistance, float extraHeightAtMaxDistance)
		{
			HorizontalRotationSensitivity = horizontalRotationSensitivity;
			VerticalRotationSensitivity = verticalRotationSensitivity;
			ZoomSensitivity = zoomSensitivity;
			SensitivityMappingMultiplier = sensitivityMappingMultiplier;
			DeviationSensitivityAtMinDistance = deviationSensitivityAtMinDistance;
			DeviationSensitivityAtMaxDistance = deviationSensitivityAtMaxDistance;
			MinCameraInclination = minCameraInclination;
			MaxCameraInclinationAtMinDistance = maxCameraInclinationAtMinDistance;
			MaxCameraInclinationAtMaxDistance = maxCameraInclinationAtMaxDistance;
			MinCameraDistance = minCameraDistance;
			MaxCameraDistance = maxCameraDistance;
			MinCameraDistanceWhileInspectingPiece = minCameraDistanceWhileInspectingPiece;
			CameraDeviationLimit = cameraDeviationLimit;
			FocusDistanceAtMinDistance = focusDistanceAtMinDistance;
			FocusDistanceAtMaxDistance = focusDistanceAtMaxDistance;
			ExtraHeightAtMinDistance = extraHeightAtMinDistance;
			ExtraHeightAtMaxDistance = extraHeightAtMaxDistance;
		}
	}

	private struct PortShipVisualInfo
	{
		public GameEntity VisualEntity;

		public Vec3 InitialPosition;

		public Vec3 VisualCenterPosition;

		public bool IsHidden;

		public PortShipVisualInfo(GameEntity visualEntity, Vec3 initialPosition, Vec3 visualCenterPosition, bool isHidden = false)
		{
			VisualEntity = visualEntity;
			InitialPosition = initialPosition;
			VisualCenterPosition = visualCenterPosition;
			IsHidden = isHidden;
		}
	}

	private const string ShipWaterEffectsEntityName = "ship_water_effects";

	private const string ShipBurningSystemEntityName = "ship_burning_system";

	private SceneLayer _sceneLayer;

	private Scene _scene;

	private readonly PortState _portState;

	private GauntletLayer _gauntletLayer;

	private PortVM _dataSource;

	private GameEntity _shipSpawnPositionEntity;

	private readonly Dictionary<Ship, PortShipVisualInfo> _shipVisualInfos;

	private PortShipVisualInfo _currentShipVisualInfo;

	private SpriteCategory _portCategory;

	private SpriteCategory _shipPiecesCategory;

	private SpriteCategory _clanCategory;

	private SpriteCategory _characterdeveloperCategory;

	private Camera _sceneCamera;

	private SoundEvent _underwaterSoundEvent;

	private IViewDataTracker _viewDataTracker;

	private readonly bool _isInSettlementPort;

	private bool _isInitialized;

	private bool _isControllingCamera;

	private int _framesToWaitAfterInit;

	private CameraParameters _targetCameraValues;

	private CameraParameters _currentCameraValues;

	private CameraParameters _previousCameraValues;

	private readonly CameraParameters _initialCameraValues;

	private readonly StaticCameraParameters _staticCameraValues;

	private Vec3 _currentCameraTargetPosition;

	private GameEntity _currentSelectedSlotCameraEntity;

	private Vec3 _shipForwardDirection = Vec3.Forward;

	private Vec3 _shipSideDirection = Vec3.Side;

	public GauntletPortScreen(PortState portState)
	{
		_portState = portState;
		_initialCameraValues = new CameraParameters(2.2f, 1.45f, 40f, 0f);
		_staticCameraValues = new StaticCameraParameters(0.2f, 0.1f, 0.015f, 1920f, 15f, 25f, System.MathF.PI / 4f, System.MathF.PI * 2f / 3f, System.MathF.PI * 19f / 36f, 15f, 50f, 5f, 15f, 50f, 3000f, 0f, 6f);
		_shipVisualInfos = new Dictionary<Ship, PortShipVisualInfo>();
		_isInSettlementPort = Settlement.CurrentSettlement != null && Settlement.CurrentSettlement.HasPort && Settlement.CurrentSettlement.SiegeEvent == null;
	}

	protected override void OnInitialize()
	{
		base.OnInitialize();
		InformationManager.HideAllMessages();
	}

	protected override void OnFinalize()
	{
		base.OnFinalize();
	}

	protected override void OnFrameTick(float dt)
	{
		base.OnFrameTick(dt);
		if (_sceneLayer.SceneView.ReadyToRender() && _sceneLayer.SceneView.CheckSceneReadyToRender())
		{
			if (!_isInitialized)
			{
				_scene.WaitWaterRendererCPUSimulation();
				InitializeView();
				_isInitialized = true;
				_framesToWaitAfterInit = 10;
			}
			_dataSource.OnTick(dt);
			_scene.Tick(dt);
			if (_framesToWaitAfterInit > 0)
			{
				_framesToWaitAfterInit--;
				return;
			}
			if (LoadingWindow.IsLoadingWindowActive)
			{
				LoadingWindow.DisableGlobalLoadingWindow();
				return;
			}
			TickSceneInput(dt);
			TickDataSourceInput();
		}
	}

	protected override void OnActivate()
	{
		base.OnActivate();
		if (_gauntletLayer != null)
		{
			ScreenManager.SetSuspendLayer(_gauntletLayer, isSuspended: false);
		}
	}

	protected override void OnDeactivate()
	{
		base.OnDeactivate();
		if (_gauntletLayer != null)
		{
			ScreenManager.SetSuspendLayer(_gauntletLayer, isSuspended: true);
		}
	}

	void IGameStateListener.OnActivate()
	{
	}

	void IGameStateListener.OnDeactivate()
	{
	}

	private void InitializeView()
	{
		_shipPiecesCategory = UIResourceManager.LoadSpriteCategory("ui_naval_ship_pieces");
		_portCategory = UIResourceManager.LoadSpriteCategory("ui_port");
		_clanCategory = UIResourceManager.LoadSpriteCategory("ui_clan");
		_characterdeveloperCategory = UIResourceManager.LoadSpriteCategory("ui_characterdeveloper");
		_viewDataTracker = Campaign.Current?.GetCampaignBehavior<IViewDataTracker>();
		PortScreenHandler portScreenHandler = null;
		switch (_portState.PortScreenMode)
		{
		case PortScreenModes.Restricted:
			portScreenHandler = new PortScreenRestrictedModeHandler(_portState.LeftOwner, _portState.RightOwner);
			break;
		case PortScreenModes.TradeMode:
			portScreenHandler = new PortScreenTradeModeHandler(_portState.LeftOwner, _portState.RightOwner);
			break;
		case PortScreenModes.LootMode:
			portScreenHandler = new PortScreenLootModeHandler(GameTexts.FindText("str_loot"), _portState.RightOwner, _portState.LeftShips, _portState.RightShips);
			break;
		case PortScreenModes.Story:
			portScreenHandler = new PortScreenStoryModeHandler(_portState.LeftOwner, _portState.RightOwner);
			break;
		case PortScreenModes.Manage:
			portScreenHandler = new PortScreenManageFleetModeHandler(GameTexts.FindText("str_port_discard_ship"), _portState.RightOwner, _portState.LeftShips, _portState.RightShips);
			break;
		case PortScreenModes.ManageOther:
			portScreenHandler = new PortScreenManageOtherFleetModeHandler(_portState.LeftOwner);
			break;
		default:
			Debug.FailedAssert("Trying to initialize Port Screen with invalid PortScreenMode. Falling back to manage mode", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\NavalDLC.GauntletUI\\Screens\\GauntletPortScreen.cs", "InitializeView", 217);
			portScreenHandler = new PortScreenManageFleetModeHandler(GameTexts.FindText("str_port_discard_ship"), _portState.RightOwner, _portState.LeftShips, _portState.RightShips);
			break;
		}
		_dataSource = new PortVM(portScreenHandler, _portState.PortScreenMode, OnShipSelected, OnRostersRefreshed, RefreshShipVisual, OnUpgradeSlotSelected, _isInSettlementPort, Settlement.CurrentSettlement);
		InitializeShipVisuals();
		_dataSource.SelectFirstAvailableRosterAndShip();
		_dataSource.IsNight = _scene.TimeOfDay <= 4f || _scene.TimeOfDay >= 20f;
		_gauntletLayer = new GauntletLayer("PortScreen", 10);
		_gauntletLayer.LoadMovie("PortScreen", _dataSource);
		_gauntletLayer.InputRestrictions.SetInputRestrictions();
		_gauntletLayer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("PortHotKeyCategory"));
		_gauntletLayer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("GenericPanelGameKeyCategory"));
		_gauntletLayer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("GenericCampaignPanelsGameKeyCategory"));
		_dataSource.SetDoneInputKey(HotKeyManager.GetCategory("GenericPanelGameKeyCategory").GetHotKey("Confirm"));
		_dataSource.SetCancelInputKey(HotKeyManager.GetCategory("GenericPanelGameKeyCategory").GetHotKey("Exit"));
		_dataSource.SetResetInputKey(HotKeyManager.GetCategory("GenericPanelGameKeyCategory").GetHotKey("Reset"));
		_dataSource.SetSelectPreviousShipInputKey(HotKeyManager.GetCategory("GenericPanelGameKeyCategory").GetHotKey("SwitchToPreviousTab"));
		_dataSource.SetSelectNextShipInputKey(HotKeyManager.GetCategory("GenericPanelGameKeyCategory").GetHotKey("SwitchToNextTab"));
		_dataSource.SetSelectLeftRosterInputKey(HotKeyManager.GetCategory("PortHotKeyCategory").GetHotKey("SelectLeftRoster"));
		_dataSource.SetSelectRightRosterInputKey(HotKeyManager.GetCategory("PortHotKeyCategory").GetHotKey("SelectRightRoster"));
		_dataSource.AddGamepadCameraControlInputKey(HotKeyManager.GetCategory("PortHotKeyCategory").RegisteredGameAxisKeys.FirstOrDefault((GameAxisKey x) => x.Id == "MovementAxisX"));
		_dataSource.AddGamepadCameraControlInputKey(HotKeyManager.GetCategory("PortHotKeyCategory").RegisteredGameAxisKeys.FirstOrDefault((GameAxisKey x) => x.Id == "CameraAxisX"));
		_dataSource.AddGamepadCameraControlInputKey(HotKeyManager.GetCategory("PortHotKeyCategory").GetHotKey("ResetCamera"));
		_dataSource.SetGamepadToggleCameraInputKey(HotKeyManager.GetCategory("PortHotKeyCategory").GetHotKey("ToggleCameraMovement"));
		_dataSource.AddKeyboardMoveCameraInputKey(HotKeyManager.GetCategory("PortHotKeyCategory").RegisteredGameAxisKeys.FirstOrDefault((GameAxisKey x) => x.Id == "MovementAxisY").PositiveKey);
		_dataSource.AddKeyboardMoveCameraInputKey(HotKeyManager.GetCategory("PortHotKeyCategory").RegisteredGameAxisKeys.FirstOrDefault((GameAxisKey x) => x.Id == "MovementAxisX").NegativeKey);
		_dataSource.AddKeyboardMoveCameraInputKey(HotKeyManager.GetCategory("PortHotKeyCategory").RegisteredGameAxisKeys.FirstOrDefault((GameAxisKey x) => x.Id == "MovementAxisY").NegativeKey);
		_dataSource.AddKeyboardMoveCameraInputKey(HotKeyManager.GetCategory("PortHotKeyCategory").RegisteredGameAxisKeys.FirstOrDefault((GameAxisKey x) => x.Id == "MovementAxisX").PositiveKey);
		_dataSource.SetKeyboardRotateCameraInputKey(HotKeyManager.GetCategory("PortHotKeyCategory").GetHotKey("ToggleCameraMovement"));
		AddLayer(_gauntletLayer);
		ResetCamera(isInstant: true);
	}

	void IGameStateListener.OnInitialize()
	{
		LoadingWindow.EnableGlobalLoadingWindow();
		_isInitialized = false;
	}

	protected override void OnReady()
	{
		base.OnReady();
		if (!(_scene != null))
		{
			CreateScene();
		}
	}

	void IGameStateListener.OnFinalize()
	{
		if (_isInitialized)
		{
			_shipPiecesCategory.Unload();
			_portCategory.Unload();
			_clanCategory.Unload();
			_characterdeveloperCategory.Unload();
			RemoveLayer(_gauntletLayer);
			_dataSource.OnFinalize();
			_gauntletLayer = null;
			_dataSource = null;
			if (_underwaterSoundEvent != null)
			{
				_underwaterSoundEvent.Release();
				_underwaterSoundEvent = null;
				SoundManager.SetGlobalParameter("isUnderwater", 0f);
			}
		}
		if (_sceneLayer != null && _scene != null)
		{
			DestroyScene();
			Utilities.ClearOldResourcesAndObjects();
			Mission.DefragRenderBuffers();
		}
	}

	private void CreateScene()
	{
		_scene = Scene.CreateNewScene(initialize_physics: true, enable_decals: false);
		_scene.SetUseAdvancedWaterRendering(value: true);
		SceneInitializationData initData = new SceneInitializationData
		{
			InitPhysicsWorld = true,
			InitFloraNodes = true
		};
		_scene.Read(_isInSettlementPort ? "prototype_port_scene_wide" : "scn_port", ref initData);
		CampaignVec2 position = (_isInSettlementPort ? Settlement.CurrentSettlement.PortPosition : Campaign.Current.MainParty.Position);
		AtmosphereInfo atmosphereModel = Campaign.Current.Models.MapWeatherModel.GetAtmosphereModel(position);
		float num = TaleWorlds.Library.MathF.Max(4f, atmosphereModel.NauticalInfo.WindVector.Length);
		float waterStrength = TaleWorlds.Library.MathF.Max(2f, num / 4f);
		_scene.EnableFixedTick();
		_scene.SetClothSimulationState(state: true);
		_scene.EnableInclusiveAsyncPhysx();
		_scene.SetWaterStrength(waterStrength);
		_scene.SetGlobalWindVelocity(num * (_isInSettlementPort ? (-Vec2.Side) : Vec2.Forward));
		_scene.SetPhotoAtmosphereViaTod(atmosphereModel.TimeInfo.TimeOfDay, num > 20f);
		_sceneLayer = new SceneLayer();
		_sceneLayer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("PortHotKeyCategory"));
		_sceneLayer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("GenericPanelGameKeyCategory"));
		_sceneLayer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("GenericCampaignPanelsGameKeyCategory"));
		_sceneLayer.InputRestrictions.SetInputRestrictions(isMouseVisible: false);
		_sceneLayer.SceneView.SetScene(_scene);
		_sceneLayer.SceneView.SetSceneUsesShadows(value: true);
		_sceneLayer.SceneView.SetAcceptGlobalDebugRenderObjects(value: true);
		_sceneLayer.SceneView.SetRenderWithPostfx(value: true);
		_sceneLayer.SceneView.SetResolutionScaling(value: true);
		_shipSpawnPositionEntity = _scene.FindEntityWithName("ship_spawn_point");
		_shipSpawnPositionEntity?.SetPhysicsState(isEnabled: false, setChildren: true);
		_shipForwardDirection = _shipSpawnPositionEntity?.GetFrame().rotation.f.AsVec2.ToVec3() ?? Vec3.Forward;
		_shipSideDirection = Vec3.CrossProduct(Vec3.Up, _shipForwardDirection);
		InitializeCamera();
		AddLayer(_sceneLayer);
	}

	private void InitializeCamera()
	{
		GameEntity gameEntity = _scene.FindEntityWithName("camera_position");
		gameEntity.SetPhysicsState(isEnabled: false, setChildren: true);
		_sceneCamera = Camera.CreateCamera();
		_sceneCamera.Frame = gameEntity.GetFrame();
		_sceneCamera.SetFovHorizontal(System.MathF.PI / 2f, Screen.AspectRatio, 0.1f, 2000f);
		ResetCamera(isInstant: true);
		UpdateCamera(1f);
		_sceneLayer.SetCamera(_sceneCamera);
	}

	private void DestroyScene()
	{
		RemoveLayer(_sceneLayer);
		_sceneLayer.ClearAll();
		_scene.WaitWaterRendererCPUSimulation();
		_scene.ClearAll();
		_scene.ManualInvalidate();
		_scene = null;
		_shipSpawnPositionEntity = null;
		_shipVisualInfos.Clear();
		_sceneCamera = null;
	}

	private void InitializeShipVisuals()
	{
		Vec3 origin = _shipSpawnPositionEntity.GetFrame().origin;
		int num = ((!_isInSettlementPort) ? (-_dataSource.RightRoster.Ships.Count / 2) : 0);
		foreach (ShipItemVM ship in _dataSource.RightRoster.Ships)
		{
			SpawnShipVisual(ship.Ship, origin + GetPositionOffsetForIndex(num, isOppositeSide: false), GetExtraRotationInRadiansForIndex(num, isOppositeSide: false));
			num++;
		}
		if (_isInSettlementPort)
		{
			origin -= Vec3.Forward * 75f;
		}
		else
		{
			origin += Vec3.Forward * 100f;
		}
		num = ((!_isInSettlementPort) ? (-_dataSource.LeftRoster.Ships.Count / 2) : 0);
		foreach (ShipItemVM ship2 in _dataSource.LeftRoster.Ships)
		{
			SpawnShipVisual(ship2.Ship, origin + GetPositionOffsetForIndex(num, isOppositeSide: true), GetExtraRotationInRadiansForIndex(num, isOppositeSide: true));
			num++;
		}
	}

	private void SpawnShipVisual(Ship ship, Vec3 position, float rotation)
	{
		List<ShipVisualSlotInfo> shipVisualSlotInfos = ship.GetShipVisualSlotInfos();
		GameEntity shipEntity = NavalDLCViewHelpers.ShipVisualHelper.GetShipEntity(ship, _scene, shipVisualSlotInfos, createPhysics: true);
		RemoveAttachmentMachineEntities(shipEntity);
		if (Utilities.IsLockhartPlatform())
		{
			RemoveUnusedParticleSystemEntities(shipEntity);
		}
		MatrixFrame frame = _shipSpawnPositionEntity.GetFrame();
		frame.origin = position;
		frame.origin.z = _scene.GetWaterLevelAtPosition(frame.origin.AsVec2, useWaterRenderer: true, checkWaterBodyEntities: false) - shipEntity.GetFirstScriptOfType<NavalPhysics>().StabilitySubmergedHeightOfShip;
		frame.rotation.RotateAboutUp(rotation);
		shipEntity.SetPhysicsState(isEnabled: true, setChildren: false);
		shipEntity.SetFrame(ref frame);
		shipEntity.GetFirstScriptOfType<NavalPhysics>().SetAnchor(isAnchored: true, anchorInPlace: true);
		RotateOars(shipEntity);
		RotateSails(shipEntity);
		_shipVisualInfos.Add(ship, new PortShipVisualInfo(shipEntity, frame.origin, frame.origin + GetVisualCenterOffsetForShip(shipEntity)));
	}

	private void RemoveAttachmentMachineEntities(GameEntity shipEntity)
	{
		List<GameEntity> list = new List<GameEntity>();
		foreach (GameEntity child in shipEntity.GetChildren())
		{
			if (child.Name.Equals("attachment_machine_holder"))
			{
				list.Add(child);
			}
		}
		foreach (GameEntity item in list)
		{
			if (item.Parent != null)
			{
				item.Parent.RemoveChild(item, keepPhysics: false, keepScenePointer: false, callScriptCallbacks: false, 32);
			}
		}
	}

	private void RemoveUnusedParticleSystemEntities(GameEntity shipEntity)
	{
		List<GameEntity> children = new List<GameEntity>();
		shipEntity.GetChildrenRecursive(ref children);
		List<GameEntity> list = new List<GameEntity>();
		foreach (GameEntity item in children)
		{
			if (item.Name.Equals("ship_water_effects") || item.Name.Equals("ship_burning_system"))
			{
				list.Add(item);
			}
		}
		foreach (GameEntity item2 in list)
		{
			if (item2.Parent != null)
			{
				item2.Parent.RemoveChild(item2, keepPhysics: false, keepScenePointer: false, callScriptCallbacks: false, 32);
			}
		}
	}

	private void RotateOars(GameEntity visualShip)
	{
		foreach (GameEntity item in visualShip.CollectChildrenEntitiesWithTag("oar"))
		{
			MatrixFrame frame = item.GetFrame();
			frame.Rotate(-System.MathF.PI / 3f, in Vec3.Side);
			item.SetFrame(ref frame);
		}
	}

	private void RotateSails(GameEntity visualShip)
	{
		ShipVisual firstScriptOfType = visualShip.GetFirstScriptOfType<ShipVisual>();
		if (firstScriptOfType == null)
		{
			return;
		}
		foreach (ScriptComponentBehavior sailVisual2 in firstScriptOfType.SailVisuals)
		{
			SailVisual sailVisual = sailVisual2 as SailVisual;
			if (sailVisual.Type == SailVisual.SailType.LateenSail)
			{
				MatrixFrame frame = sailVisual.SailYawRotationEntity.GetLocalFrame();
				frame.rotation = Mat3.Identity;
				frame.rotation.RotateAboutUp(0.87266463f);
				sailVisual.SailYawRotationEntity.SetLocalFrame(ref frame, isTeleportation: false);
			}
		}
	}

	private Vec3 GetPositionOffsetForIndex(int i, bool isOppositeSide)
	{
		Vec3 vec;
		Vec3 vec2;
		if (_isInSettlementPort)
		{
			vec = Vec3.Forward * 45f * (i % 4);
			vec2 = Vec3.Side * -60f * (i / 4);
		}
		else
		{
			vec2 = Vec3.Side * -45f * i;
			vec = Vec3.Forward * -20f * TaleWorlds.Library.MathF.Abs(i);
		}
		if (isOppositeSide)
		{
			vec *= -1f;
		}
		Vec3 vec3 = (MBRandom.RandomFloatWithSeed((uint)i, (uint)(i + (isOppositeSide ? 1 : 0))) - 0.5f) * 8f * Vec3.Side + (MBRandom.RandomFloatWithSeed((uint)i, (uint)(i + (isOppositeSide ? 3 : 2))) - 0.5f) * 8f * Vec3.Forward;
		return vec2 + vec + vec3;
	}

	private float GetExtraRotationInRadiansForIndex(int i, bool isOppositeSide)
	{
		return (MBRandom.RandomFloatWithSeed((uint)i, (uint)(i + (isOppositeSide ? 1 : 0))) - 0.5f) * 20f * (System.MathF.PI / 180f);
	}

	private Vec3 GetVisualCenterOffsetForShip(GameEntity shipEntity)
	{
		MetaMesh metaMesh = shipEntity.GetFirstChildEntityWithTagRecursive("body_mesh")?.GetMetaMesh(0);
		if (metaMesh != null)
		{
			BoundingBox boundingBox = metaMesh.GetBoundingBox();
			return new Vec3(boundingBox.center.AsVec2, TaleWorlds.Library.MathF.Lerp(boundingBox.center.Z, boundingBox.max.Z, 0.4f));
		}
		return new Vec3(0f, 0f, 2.5f);
	}

	private void RecalculateShipVisibilities()
	{
		foreach (KeyValuePair<Ship, PortShipVisualInfo> item in _shipVisualInfos.ToList())
		{
			Ship key = item.Key;
			bool flag = ShouldShipBeHidden(key);
			if (item.Value.IsHidden != flag)
			{
				_shipVisualInfos[key] = new PortShipVisualInfo(item.Value.VisualEntity, item.Value.InitialPosition, item.Value.VisualCenterPosition, flag);
			}
			item.Value.VisualEntity.SetVisibilityExcludeParents(!flag);
		}
	}

	private bool ShouldShipBeHidden(Ship ship)
	{
		if (!_dataSource.LeftRoster.Ships.Any((ShipItemVM x) => x.Ship == ship))
		{
			return !_dataSource.RightRoster.Ships.Any((ShipItemVM x) => x.Ship == ship);
		}
		return false;
	}

	private void RecalculateShipPositions()
	{
		Vec3 origin = _shipSpawnPositionEntity.GetFrame().origin;
		int num = ((!_isInSettlementPort) ? (-_dataSource.RightRoster.Ships.Count / 2) : 0);
		foreach (ShipItemVM ship in _dataSource.RightRoster.Ships)
		{
			RecalculateShipPosition(ship.Ship, origin + GetPositionOffsetForIndex(num, isOppositeSide: false), GetExtraRotationInRadiansForIndex(num, isOppositeSide: false));
			num++;
		}
		if (_isInSettlementPort)
		{
			origin -= Vec3.Forward * 75f;
		}
		else
		{
			origin += Vec3.Forward * 100f;
		}
		num = ((!_isInSettlementPort) ? (-_dataSource.LeftRoster.Ships.Count / 2) : 0);
		foreach (ShipItemVM ship2 in _dataSource.LeftRoster.Ships)
		{
			RecalculateShipPosition(ship2.Ship, origin + GetPositionOffsetForIndex(num, isOppositeSide: true), GetExtraRotationInRadiansForIndex(num, isOppositeSide: true));
			num++;
		}
	}

	private void RecalculateShipPosition(Ship ship, Vec3 position, float rotation)
	{
		if (!_shipVisualInfos.ContainsKey(ship))
		{
			return;
		}
		PortShipVisualInfo portShipVisualInfo = _shipVisualInfos[ship];
		if (portShipVisualInfo.InitialPosition.AsVec2 != position.AsVec2)
		{
			GameEntity visualEntity = portShipVisualInfo.VisualEntity;
			MatrixFrame frame = _shipSpawnPositionEntity.GetFrame();
			frame.origin = position;
			frame.origin.z = _scene.GetWaterLevelAtPosition(frame.origin.AsVec2, useWaterRenderer: true, checkWaterBodyEntities: false) - visualEntity.GetFirstScriptOfType<NavalPhysics>().StabilitySubmergedHeightOfShip;
			frame.rotation.RotateAboutUp(rotation);
			visualEntity.GetFirstScriptOfType<NavalPhysics>().SetAnchor(isAnchored: false);
			visualEntity.SetFrame(ref frame);
			visualEntity.GetFirstScriptOfType<NavalPhysics>().SetAnchor(isAnchored: true, anchorInPlace: true);
			_shipVisualInfos[ship] = new PortShipVisualInfo(visualEntity, frame.origin, frame.origin + GetVisualCenterOffsetForShip(visualEntity), portShipVisualInfo.IsHidden);
			if (_currentShipVisualInfo.VisualEntity == visualEntity)
			{
				_currentShipVisualInfo = _shipVisualInfos[ship];
			}
		}
	}

	private void RefreshShipVisuals()
	{
		foreach (ShipItemVM allShip in _dataSource.AllShips)
		{
			RefreshShipVisual(allShip);
		}
	}

	private void RefreshShipVisual(ShipItemVM shipItem)
	{
		Ship ship = shipItem.Ship;
		if (!_shipVisualInfos.ContainsKey(ship))
		{
			return;
		}
		List<ShipVisualSlotInfo> list = new List<ShipVisualSlotInfo>();
		foreach (ShipUpgradeSlotBaseVM upgradeSlot in shipItem.Upgrades.UpgradeSlots)
		{
			if (upgradeSlot is ShipUpgradeSlotVM)
			{
				list.Add(new ShipVisualSlotInfo(upgradeSlot.ShipSlotTag, (upgradeSlot.SelectedPiece as ShipUpgradePieceVM)?.Piece.SlotPrefabChildTagId ?? string.Empty));
			}
			else if (upgradeSlot is ShipFigureheadSlotVM)
			{
				list.Add(new ShipVisualSlotInfo(upgradeSlot.ShipSlotTag, (upgradeSlot.SelectedPiece as ShipFigureheadVM)?.Figurehead.StringId ?? string.Empty));
			}
		}
		uint item;
		uint item2;
		Banner shipBannerForParty;
		if (_dataSource.LeftRoster.Ships.Contains(shipItem))
		{
			(uint sailColor1, uint sailColor2) sailColorsForParty = ShipHelper.GetSailColorsForParty(_dataSource.LeftRoster.Owner);
			item = sailColorsForParty.sailColor1;
			item2 = sailColorsForParty.sailColor2;
			shipBannerForParty = ShipHelper.GetShipBannerForParty(_dataSource.LeftRoster.Owner);
		}
		else
		{
			(uint sailColor1, uint sailColor2) sailColorsForParty2 = ShipHelper.GetSailColorsForParty(_dataSource.RightRoster.Owner);
			item = sailColorsForParty2.sailColor1;
			item2 = sailColorsForParty2.sailColor2;
			shipBannerForParty = ShipHelper.GetShipBannerForParty(_dataSource.RightRoster.Owner);
		}
		NavalDLCViewHelpers.ShipVisualHelper.RefreshShipVisuals(_shipVisualInfos[ship].VisualEntity, list, item, item2, shipBannerForParty, shipItem.CurrentHp / shipItem.MaxHp);
	}

	private void OnShipSelected(Ship shipItem)
	{
		if (shipItem == null)
		{
			return;
		}
		if (_shipVisualInfos.ContainsKey(shipItem))
		{
			_currentShipVisualInfo = _shipVisualInfos[shipItem];
			foreach (KeyValuePair<Ship, PortShipVisualInfo> shipVisualInfo in _shipVisualInfos)
			{
				if (shipVisualInfo.Value.VisualEntity != _currentShipVisualInfo.VisualEntity)
				{
					shipVisualInfo.Value.VisualEntity.AddBodyFlags(BodyFlags.DoNotCollideWithRaycast);
				}
				else
				{
					shipVisualInfo.Value.VisualEntity.RemoveBodyFlags(BodyFlags.DoNotCollideWithRaycast);
				}
			}
		}
		else
		{
			Debug.FailedAssert("Selected ship item's visual has not been spawned!", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\NavalDLC.GauntletUI\\Screens\\GauntletPortScreen.cs", "OnShipSelected", 767);
		}
		_targetCameraValues.Deviation = _initialCameraValues.Deviation;
	}

	private void OnRostersRefreshed()
	{
		if (_dataSource == null)
		{
			return;
		}
		Vec3 origin = _shipSpawnPositionEntity.GetFrame().origin;
		for (int i = 0; i < _dataSource.RightRoster.Ships.Count; i++)
		{
			Ship ship = _dataSource.RightRoster.Ships[i].Ship;
			if (!_shipVisualInfos.ContainsKey(ship))
			{
				SpawnShipVisual(ship, origin + GetPositionOffsetForIndex(i, isOppositeSide: false), GetExtraRotationInRadiansForIndex(i, isOppositeSide: false));
			}
		}
		RecalculateShipVisibilities();
		RecalculateShipPositions();
		RefreshShipVisuals();
	}

	private void OnUpgradeSlotSelected()
	{
		if (_dataSource.IsAnyUpgradeSlotSelected)
		{
			string shipSlotTag = _dataSource.SelectedUpgradeSlot.ShipSlotTag;
			string slotTypeId = _dataSource.SelectedUpgradeSlot.SlotTypeId;
			if (_currentSelectedSlotCameraEntity == null)
			{
				_previousCameraValues = _currentCameraValues;
			}
			_currentSelectedSlotCameraEntity = _currentShipVisualInfo.VisualEntity.GetFirstChildEntityWithTagRecursive(shipSlotTag + "_point");
			if (_currentSelectedSlotCameraEntity == null)
			{
				Debug.FailedAssert("Slot camera point entity not found!", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\NavalDLC.GauntletUI\\Screens\\GauntletPortScreen.cs", "OnUpgradeSlotSelected", 814);
				return;
			}
			_targetCameraValues.Azimuth = GetCameraAzimuthForSlot();
			_targetCameraValues.Inclination = GetCameraInclinationForSlotType(slotTypeId);
			_targetCameraValues.Distance = GetCameraDistanceForSlotType(slotTypeId);
			_targetCameraValues.Deviation = 0f;
		}
		else
		{
			FreeCameraFromUpgradeSlot();
		}
	}

	private void FreeCameraFromUpgradeSlot()
	{
		if (_currentSelectedSlotCameraEntity != null)
		{
			_currentSelectedSlotCameraEntity = null;
			_targetCameraValues = _previousCameraValues;
		}
	}

	private float GetCameraAzimuthForSlot()
	{
		Vec3 point = GetStableSlotPosition() - _shipSideDirection;
		Vec3 closestPointOnLineSegmentToPoint = MBMath.GetClosestPointOnLineSegmentToPoint(_currentShipVisualInfo.VisualCenterPosition - _shipForwardDirection * _staticCameraValues.CameraDeviationLimit, _currentShipVisualInfo.VisualCenterPosition + _shipForwardDirection * _staticCameraValues.CameraDeviationLimit, in point);
		Vec3 vec = point - closestPointOnLineSegmentToPoint;
		if (TaleWorlds.Library.MathF.Abs(Vec3.DotProduct(vec.NormalizedCopy(), Vec3.Up)).ApproximatelyEqualsTo(1f))
		{
			return _initialCameraValues.Azimuth;
		}
		return TaleWorlds.Library.MathF.Atan2(vec.y, vec.x);
	}

	private float GetCameraInclinationForSlotType(string slotType)
	{
		return 1.3962634f;
	}

	private float GetCameraDistanceForSlotType(string slotType)
	{
		if (slotType == "hull" || slotType == "sail")
		{
			return _initialCameraValues.Distance;
		}
		return _staticCameraValues.MinCameraDistance;
	}

	private void TickDataSourceInput()
	{
		PortVM dataSource = _dataSource;
		if (dataSource != null && dataSource.ShipSelectionPopup?.IsOpen == true)
		{
			if (IsHotKeyReleasedInAnyLayer("Confirm"))
			{
				if (_dataSource.ShipSelectionPopup.CanTakeToParty)
				{
					UISoundsHelper.PlayUISound("event:/ui/port/confirm_ship");
					_dataSource.ShipSelectionPopup.ExecuteTakeToParty();
				}
			}
			else if (IsHotKeyReleasedInAnyLayer("Exit"))
			{
				UISoundsHelper.PlayUISound("event:/ui/default");
				_dataSource.ShipSelectionPopup.ExecuteCancel();
			}
		}
		else if (IsHotKeyReleasedInAnyLayer("Confirm"))
		{
			if (!_dataSource.IsConfirmDisabled)
			{
				UISoundsHelper.PlayUISound("event:/ui/port/confirm_ship");
				_dataSource.ExecuteConfirm();
			}
		}
		else if (IsHotKeyReleasedInAnyLayer("Exit"))
		{
			if (_dataSource.IsAnyUpgradeSlotSelected)
			{
				UISoundsHelper.PlayUISound("event:/ui/default");
				_dataSource.SelectedUpgradeSlot.ExecuteDeselect();
			}
			else
			{
				UISoundsHelper.PlayUISound("event:/ui/default");
				_dataSource.ExecuteCancel(showCancelInquiry: true);
			}
		}
		else if (IsGameKeyPressedInAnyLayer(45))
		{
			UISoundsHelper.PlayUISound("event:/ui/default");
			_dataSource.ExecuteCancel(showCancelInquiry: true);
		}
		else if (IsHotKeyReleasedInAnyLayer("Reset"))
		{
			UISoundsHelper.PlayUISound("event:/ui/default");
			_dataSource.ExecuteReset();
		}
		else if (IsHotKeyReleasedInAnyLayer("SwitchToPreviousTab"))
		{
			if (!_isControllingCamera && _dataSource.ExecuteSelectPreviousShip())
			{
				UISoundsHelper.PlayUISound("event:/ui/port/choose_ship");
			}
		}
		else if (IsHotKeyReleasedInAnyLayer("SwitchToNextTab"))
		{
			if (!_isControllingCamera && _dataSource.ExecuteSelectNextShip())
			{
				UISoundsHelper.PlayUISound("event:/ui/port/choose_ship");
			}
		}
		else if (IsHotKeyReleasedInAnyLayer("SelectLeftRoster"))
		{
			if (!_isControllingCamera && !_dataSource.LeftRoster.IsSelected && _dataSource.LeftRoster.HasAnyShips)
			{
				UISoundsHelper.PlayUISound("event:/ui/default");
				_dataSource.LeftRoster.ExecuteSelectRoster();
			}
		}
		else if (IsHotKeyReleasedInAnyLayer("SelectRightRoster") && !_isControllingCamera && !_dataSource.RightRoster.IsSelected && _dataSource.RightRoster.HasAnyShips)
		{
			UISoundsHelper.PlayUISound("event:/ui/default");
			_dataSource.RightRoster.ExecuteSelectRoster();
		}
	}

	private bool IsHotKeyPressedInAnyLayer(string hotkey)
	{
		if (!_gauntletLayer.Input.IsHotKeyPressed(hotkey))
		{
			return _sceneLayer.Input.IsHotKeyPressed(hotkey);
		}
		return true;
	}

	private bool IsHotKeyReleasedInAnyLayer(string hotkey)
	{
		if (!_gauntletLayer.Input.IsHotKeyReleased(hotkey))
		{
			return _sceneLayer.Input.IsHotKeyReleased(hotkey);
		}
		return true;
	}

	private bool IsGameKeyPressedInAnyLayer(int gameKey)
	{
		if (!_gauntletLayer.Input.IsGameKeyPressed(gameKey))
		{
			return _sceneLayer.Input.IsGameKeyPressed(gameKey);
		}
		return true;
	}

	private void TickSceneInput(float dt)
	{
		if (_sceneLayer.IsHitThisFrame && ScreenManager.FocusedLayer == _gauntletLayer)
		{
			_gauntletLayer.IsFocusLayer = false;
			ScreenManager.TryLoseFocus(_gauntletLayer);
			_sceneLayer.IsFocusLayer = true;
			ScreenManager.TrySetFocus(_sceneLayer);
		}
		else if (!_sceneLayer.IsHitThisFrame && ScreenManager.FocusedLayer == _sceneLayer)
		{
			_sceneLayer.IsFocusLayer = false;
			ScreenManager.TryLoseFocus(_sceneLayer);
			_gauntletLayer.IsFocusLayer = true;
			ScreenManager.TrySetFocus(_gauntletLayer);
		}
		int num;
		if (_sceneLayer.IsHitThisFrame || _gauntletLayer.IsHitThisFrame)
		{
			PortVM dataSource = _dataSource;
			num = ((dataSource != null && dataSource.ShipSelectionPopup?.IsOpen == false) ? 1 : 0);
		}
		else
		{
			num = 0;
		}
		bool flag = (byte)num != 0;
		if (Input.IsGamepadActive)
		{
			if (flag && IsHotKeyPressedInAnyLayer("ToggleCameraMovement"))
			{
				_isControllingCamera = !_isControllingCamera;
			}
		}
		else if (_sceneLayer.Input.IsHotKeyPressed("ToggleCameraMovement"))
		{
			_isControllingCamera = true;
		}
		else if (_sceneLayer.Input.IsHotKeyReleased("ToggleCameraMovement"))
		{
			_isControllingCamera = false;
		}
		_dataSource.IsControllingCamera = _isControllingCamera;
		_dataSource.ShowPortScreenGamepadInputs = flag;
		_dataSource.CanToggleCamera = flag;
		_dataSource.IsMapBarExtended = _viewDataTracker?.GetMapBarExtendedState() ?? false;
		_dataSource.CanUseGamepadInputs = Input.IsGamepadActive;
		_dataSource.CanUseKeyboardInputs = !Input.IsGamepadActive && _sceneLayer.IsHitThisFrame;
		if (_isControllingCamera)
		{
			MBWindowManager.DontChangeCursorPos();
			_gauntletLayer.InputRestrictions.ResetInputRestrictions();
		}
		else
		{
			_gauntletLayer.InputRestrictions.SetInputRestrictions();
		}
		if (_sceneLayer.Input.IsHotKeyPressed("ResetCamera"))
		{
			ResetCamera(isInstant: false);
		}
		Vec2 vec = new Vec2(_sceneLayer.Input.GetNormalizedMouseMoveX() * 1920f, _sceneLayer.Input.GetNormalizedMouseMoveY() * 1080f);
		float num2 = 0f;
		if (Input.IsGamepadActive)
		{
			if (_isControllingCamera)
			{
				float inputValue = _sceneLayer.Input.GetGameKeyAxis("MovementAxisY") * -1f;
				NormalizeControllerInputForDeadZone(ref inputValue, 0.1f);
				if (_sceneLayer.Input.IsHotKeyDown("ControllerZoomOut"))
				{
					inputValue += 1f;
				}
				if (_sceneLayer.Input.IsHotKeyDown("ControllerZoomIn"))
				{
					inputValue -= 1f;
				}
				inputValue = TaleWorlds.Library.MathF.Clamp(inputValue, -1f, 1f);
				num2 = inputValue * _staticCameraValues.ZoomSensitivity * _staticCameraValues.SensitivityMappingMultiplier * dt;
			}
		}
		else
		{
			float num3 = _sceneLayer.Input.GetDeltaMouseScroll() * -1f;
			float num4 = _sceneLayer.Input.GetGameKeyAxis("MovementAxisY") * -1f;
			num2 = num3 * _staticCameraValues.ZoomSensitivity + num4 * _staticCameraValues.ZoomSensitivity * _staticCameraValues.SensitivityMappingMultiplier * dt;
		}
		_targetCameraValues.Distance = TaleWorlds.Library.MathF.Clamp(_targetCameraValues.Distance + num2, GetTargetMinDistance(), _staticCameraValues.MaxCameraDistance);
		float num5;
		if (Input.IsGamepadActive)
		{
			float inputValue2 = (_isControllingCamera ? (_sceneLayer.Input.GetGameKeyAxis("CameraAxisX") * -1f) : 0f);
			NormalizeControllerInputForDeadZone(ref inputValue2, 0.1f);
			num5 = inputValue2 * _staticCameraValues.HorizontalRotationSensitivity * _sceneLayer.Input.GetMouseSensitivity() * _staticCameraValues.SensitivityMappingMultiplier * dt;
		}
		else
		{
			num5 = (_isControllingCamera ? (vec.x * -1f) : 0f) * _staticCameraValues.HorizontalRotationSensitivity * _sceneLayer.Input.GetMouseSensitivity();
		}
		_targetCameraValues.Azimuth = MBMath.WrapAngle(_targetCameraValues.Azimuth + num5 * (System.MathF.PI / 180f));
		float num6;
		if (Input.IsGamepadActive)
		{
			float inputValue3 = (_isControllingCamera ? _sceneLayer.Input.GetGameKeyAxis("CameraAxisY") : 0f);
			NormalizeControllerInputForDeadZone(ref inputValue3, 0.1f);
			num6 = inputValue3 * _staticCameraValues.VerticalRotationSensitivity * _sceneLayer.Input.GetMouseSensitivity() * _staticCameraValues.SensitivityMappingMultiplier * dt;
		}
		else
		{
			num6 = (_isControllingCamera ? (vec.y * -1f) : 0f) * _staticCameraValues.VerticalRotationSensitivity * _sceneLayer.Input.GetMouseSensitivity();
		}
		if (NativeConfig.InvertMouse)
		{
			num6 *= -1f;
		}
		float amount = (_targetCameraValues.Distance - GetTargetMinDistance()) / (_staticCameraValues.MaxCameraDistance - GetTargetMinDistance());
		float maxValue = TaleWorlds.Library.MathF.Lerp(_staticCameraValues.MaxCameraInclinationAtMinDistance, _staticCameraValues.MaxCameraInclinationAtMaxDistance, amount);
		_targetCameraValues.Inclination = TaleWorlds.Library.MathF.Clamp(_targetCameraValues.Inclination + num6 * (System.MathF.PI / 180f), _staticCameraValues.MinCameraInclination, maxValue);
		float num7 = 0f;
		if (Input.IsGamepadActive)
		{
			if (_isControllingCamera)
			{
				num7 = _sceneLayer.Input.GetGameKeyAxis("MovementAxisX");
				NormalizeControllerInputForDeadZone(ref num7, 0.1f);
				if (_sceneLayer.Input.IsHotKeyDown("ControllerDeviateRight"))
				{
					num7 += 1f;
				}
				if (_sceneLayer.Input.IsHotKeyDown("ControllerDeviateLeft"))
				{
					num7 -= 1f;
				}
				num7 = TaleWorlds.Library.MathF.Clamp(num7, -1f, 1f);
			}
		}
		else
		{
			num7 = _sceneLayer.Input.GetGameKeyAxis("MovementAxisX");
		}
		float num8 = TaleWorlds.Library.MathF.Lerp(_staticCameraValues.DeviationSensitivityAtMinDistance, _staticCameraValues.DeviationSensitivityAtMaxDistance, amount);
		float num9 = TaleWorlds.Library.MathF.Clamp(TaleWorlds.Library.MathF.Pow(TaleWorlds.Library.MathF.Cos(_currentCameraValues.Azimuth - Vec3.AngleBetweenTwoVectors(Vec3.Forward, _shipForwardDirection)), 3f) * 2f, -1f, 1f);
		float num10 = num7 * num8 * dt * num9;
		_targetCameraValues.Deviation = TaleWorlds.Library.MathF.Clamp(_targetCameraValues.Deviation + num10, 0f - _staticCameraValues.CameraDeviationLimit, _staticCameraValues.CameraDeviationLimit);
		if (num10 != 0f)
		{
			FreeCameraFromUpgradeSlot();
		}
		UpdateCamera(dt);
	}

	bool IChangeableScreen.AnyUnsavedChanges()
	{
		if (_isInitialized)
		{
			return _dataSource.AreThereAnyChanges();
		}
		return false;
	}

	bool IChangeableScreen.CanChangesBeApplied()
	{
		return !_dataSource.IsConfirmDisabled;
	}

	void IChangeableScreen.ApplyChanges()
	{
		_dataSource.ExecuteConfirm();
	}

	void IChangeableScreen.ResetChanges()
	{
		_dataSource.ExecuteReset();
	}

	private void UpdateCamera(float dt)
	{
		float amount = TaleWorlds.Library.MathF.Min(1f, 10f * dt);
		float amount2 = TaleWorlds.Library.MathF.Min(1f, 5f * dt);
		float maxAmount = ((_currentSelectedSlotCameraEntity != null) ? (System.MathF.PI * 2f * dt) : (100f * dt));
		_currentCameraValues.Azimuth = LerpAngleWithMax(_currentCameraValues.Azimuth, _targetCameraValues.Azimuth, amount, maxAmount);
		_currentCameraValues.Inclination = LerpAngleWithMax(_currentCameraValues.Inclination, _targetCameraValues.Inclination, amount, maxAmount);
		_currentCameraValues.Deviation = TaleWorlds.Library.MathF.Lerp(_currentCameraValues.Deviation, _targetCameraValues.Deviation, amount);
		_currentCameraValues.Distance = TaleWorlds.Library.MathF.Lerp(_currentCameraValues.Distance, _targetCameraValues.Distance, amount);
		float value = (_currentCameraValues.Distance - GetTargetMinDistance()) / (_staticCameraValues.MaxCameraDistance - GetTargetMinDistance());
		value = TaleWorlds.Library.MathF.Clamp(value, 0f, 1f);
		_currentCameraTargetPosition = LerpVec3WithMax(_currentCameraTargetPosition, GetCameraTargetPosition(), amount2, 500f * dt);
		Vec3 currentCameraTargetPosition = _currentCameraTargetPosition;
		currentCameraTargetPosition += _currentCameraValues.Deviation * _shipForwardDirection;
		float amount3 = AnimationInterpolation.Ease(AnimationInterpolation.Type.EaseOut, AnimationInterpolation.Function.Sine, value);
		currentCameraTargetPosition.z += TaleWorlds.Library.MathF.Lerp(_staticCameraValues.ExtraHeightAtMinDistance, _staticCameraValues.ExtraHeightAtMaxDistance, amount3);
		HandleCameraCollision(currentCameraTargetPosition);
		MatrixFrame identity = MatrixFrame.Identity;
		identity.origin = currentCameraTargetPosition;
		identity.origin.x += _currentCameraValues.Distance * TaleWorlds.Library.MathF.Sin(_currentCameraValues.Inclination) * TaleWorlds.Library.MathF.Cos(_currentCameraValues.Azimuth);
		identity.origin.y += _currentCameraValues.Distance * TaleWorlds.Library.MathF.Sin(_currentCameraValues.Inclination) * TaleWorlds.Library.MathF.Sin(_currentCameraValues.Azimuth);
		identity.origin.z += _currentCameraValues.Distance * TaleWorlds.Library.MathF.Cos(_currentCameraValues.Inclination);
		_sceneCamera.LookAt(identity.origin, currentCameraTargetPosition, Vec3.Up);
		_sceneCamera.SetFovHorizontal(System.MathF.PI / 2f, Screen.AspectRatio, 0.1f, 2000f);
		_scene.SetDepthOfFieldFocus(_currentCameraValues.Distance);
		float amount4 = AnimationInterpolation.Ease(AnimationInterpolation.Type.EaseIn, AnimationInterpolation.Function.Cubic, value);
		float num = TaleWorlds.Library.MathF.Lerp(_staticCameraValues.FocusDistanceAtMinDistance, _staticCameraValues.FocusDistanceAtMaxDistance, amount4);
		_scene.SetDepthOfFieldParameters(num, num, isVignetteOn: true);
		_sceneLayer.SetCamera(_sceneCamera);
		SoundManager.SetListenerFrame(_sceneCamera.Frame);
		HandleIsCameraUnderwater();
		HandleShipEntityVisibilities();
	}

	private float LerpAngleWithMax(float current, float target, float amount, float maxAmount)
	{
		float num = TaleWorlds.Library.MathF.AngleLerp(current, target, amount);
		float num2 = (num - current) % (System.MathF.PI * 2f);
		float f = 2f * num2 % (System.MathF.PI * 2f) - num2;
		if (TaleWorlds.Library.MathF.Abs(f) > maxAmount)
		{
			num = TaleWorlds.Library.MathF.AngleClamp(current + (float)TaleWorlds.Library.MathF.Sign(f) * maxAmount);
		}
		return num;
	}

	private Vec3 LerpVec3WithMax(Vec3 current, Vec3 target, float amount, float maxAmount)
	{
		Vec3 vec = Vec3.Lerp(current, target, amount);
		if (vec.Distance(current) > maxAmount)
		{
			vec = current + (vec - current).NormalizedCopy() * maxAmount;
		}
		return vec;
	}

	private Vec3 GetCameraTargetPosition()
	{
		if (_currentShipVisualInfo.VisualEntity != null)
		{
			if (_currentSelectedSlotCameraEntity != null)
			{
				return GetStableSlotPosition();
			}
			return _currentShipVisualInfo.VisualCenterPosition;
		}
		return _shipSpawnPositionEntity.GetFrame().origin + new Vec3(0f, 0f, 2.5f);
	}

	private Vec3 GetStableSlotPosition()
	{
		return _currentSelectedSlotCameraEntity.GlobalPosition - _currentShipVisualInfo.VisualEntity.GlobalPosition + _currentShipVisualInfo.InitialPosition;
	}

	private void NormalizeControllerInputForDeadZone(ref float inputValue, float controllerDeadZone)
	{
		if (TaleWorlds.Library.MathF.Abs(inputValue) < controllerDeadZone)
		{
			inputValue = 0f;
		}
		else
		{
			inputValue = (inputValue - (float)TaleWorlds.Library.MathF.Sign(inputValue) * controllerDeadZone) / (1f - controllerDeadZone);
		}
	}

	private void HandleCameraCollision(Vec3 cameraTargetPos)
	{
		if (_scene.RayCastForClosestEntityOrTerrain(_sceneCamera.Position, cameraTargetPos, out var collisionDistance))
		{
			float num = _currentCameraValues.Distance - collisionDistance + 1f;
			if (_currentCameraValues.Distance < num)
			{
				_currentCameraValues.Distance = num;
				_targetCameraValues.Distance = num;
			}
		}
	}

	private void HandleIsCameraUnderwater()
	{
		Vec3 position = _sceneCamera.Position;
		float waterLevelAtPosition = _scene.GetWaterLevelAtPosition(position.AsVec2, useWaterRenderer: true, checkWaterBodyEntities: false);
		if (position.Z < waterLevelAtPosition)
		{
			if (_underwaterSoundEvent == null)
			{
				_underwaterSoundEvent = SoundManager.CreateEvent("snapshot:/Underwater", _scene);
				_underwaterSoundEvent.Play();
				SoundManager.SetGlobalParameter("isUnderwater", 1f);
			}
		}
		else if (_underwaterSoundEvent != null)
		{
			_underwaterSoundEvent.Release();
			_underwaterSoundEvent = null;
			SoundManager.SetGlobalParameter("isUnderwater", 0f);
		}
	}

	private void ResetCamera(bool isInstant)
	{
		if (isInstant)
		{
			_currentCameraTargetPosition = GetCameraTargetPosition();
			_currentCameraValues = _initialCameraValues;
		}
		_targetCameraValues = _initialCameraValues;
	}

	private void HandleShipEntityVisibilities()
	{
		foreach (KeyValuePair<Ship, PortShipVisualInfo> shipVisualInfo in _shipVisualInfos)
		{
			GameEntity visualEntity = shipVisualInfo.Value.VisualEntity;
			bool isHidden = shipVisualInfo.Value.IsHidden;
			if (visualEntity == _currentShipVisualInfo.VisualEntity)
			{
				visualEntity.SetVisibilityExcludeParents(!isHidden);
				continue;
			}
			float num = 6f;
			(Vec3, Vec3) tuple = visualEntity.ComputeGlobalPhysicsBoundingBoxMinMax();
			Vec3 item = tuple.Item1;
			Vec3 item2 = tuple.Item2;
			float num2 = TaleWorlds.Library.MathF.Min(item.X, item2.X) - num;
			float num3 = TaleWorlds.Library.MathF.Max(item.X, item2.X) + num;
			float num4 = TaleWorlds.Library.MathF.Min(item.Y, item2.Y) - num;
			float num5 = TaleWorlds.Library.MathF.Max(item.Y, item2.Y) + num;
			float num6 = TaleWorlds.Library.MathF.Min(item.Z, item2.Z) - num;
			float num7 = TaleWorlds.Library.MathF.Max(item.Z, item2.Z) + num;
			bool flag = _sceneCamera.Position.X > num2 && _sceneCamera.Position.X < num3 && _sceneCamera.Position.Y > num4 && _sceneCamera.Position.Y < num5 && _sceneCamera.Position.Z > num6 && _sceneCamera.Position.Z < num7;
			visualEntity.SetVisibilityExcludeParents(!isHidden && !flag);
		}
	}

	private float GetTargetMinDistance()
	{
		if (!(_currentSelectedSlotCameraEntity != null))
		{
			return _staticCameraValues.MinCameraDistance;
		}
		return _staticCameraValues.MinCameraDistanceWhileInspectingPiece;
	}
}
