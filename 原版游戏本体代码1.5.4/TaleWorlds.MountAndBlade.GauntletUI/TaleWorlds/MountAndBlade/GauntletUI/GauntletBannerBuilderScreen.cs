using System;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.Engine.Screens;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.Screens;
using TaleWorlds.MountAndBlade.View.Tableaus.Thumbnails;
using TaleWorlds.MountAndBlade.ViewModelCollection.BannerBuilder;
using TaleWorlds.ObjectSystem;
using TaleWorlds.ScreenSystem;
using TaleWorlds.TwoDimension;

namespace TaleWorlds.MountAndBlade.GauntletUI;

[GameStateScreen(typeof(BannerBuilderState))]
public class GauntletBannerBuilderScreen : ScreenBase, IGameStateListener
{
	private BannerBuilderVM _dataSource;

	private GauntletLayer _gauntletLayer;

	private GauntletMovieIdentifier _movie;

	private SpriteCategory _bannerIconsCategory;

	private SpriteCategory _bannerBuilderCategory;

	private BannerBuilderState _state;

	private bool _isFinalized;

	private Camera _camera;

	private AgentVisuals[] _agentVisuals;

	private Scene _scene;

	private MBAgentRendererSceneController _agentRendererSceneController;

	private MatrixFrame _characterFrame;

	private Equipment _weaponEquipment;

	private Banner _currentBanner;

	private float _cameraCurrentRotation;

	private float _cameraTargetRotation;

	private float _cameraCurrentDistanceAdder;

	private float _cameraTargetDistanceAdder;

	private float _cameraCurrentElevationAdder;

	private float _cameraTargetElevationAdder;

	private int _agentVisualToShowIndex;

	private bool _refreshCharacterAndShieldNextFrame;

	private bool _refreshBannersNextFrame;

	private bool _checkWhetherAgentVisualIsReady;

	private bool _firstCharacterRender = true;

	private BasicCharacterObject _character;

	private const string DefaultBannerKey = "11.163.166.1528.1528.764.764.1.0.0.133.171.171.483.483.764.764.0.0.0";

	public SceneLayer SceneLayer { get; private set; }

	public GauntletBannerBuilderScreen(BannerBuilderState state)
	{
		_state = state;
		_character = MBObjectManager.Instance.GetObject<BasicCharacterObject>("main_hero");
	}

	protected override void OnInitialize()
	{
		base.OnInitialize();
		_bannerIconsCategory = UIResourceManager.LoadSpriteCategory("ui_bannericons");
		_bannerBuilderCategory = UIResourceManager.LoadSpriteCategory("ui_bannerbuilder");
		_agentVisuals = new AgentVisuals[2];
		string initialKey = (string.IsNullOrWhiteSpace(_state.DefaultBannerKey) ? "11.163.166.1528.1528.764.764.1.0.0.133.171.171.483.483.764.764.0.0.0" : _state.DefaultBannerKey);
		_dataSource = new BannerBuilderVM(_character, initialKey, Exit, Refresh, CopyBannerCode);
		_gauntletLayer = new GauntletLayer("BannerBuilder", 100);
		_gauntletLayer.IsFocusLayer = true;
		AddLayer(_gauntletLayer);
		_gauntletLayer.InputRestrictions.SetInputRestrictions();
		ScreenManager.TrySetFocus(_gauntletLayer);
		_movie = _gauntletLayer.LoadMovie("BannerBuilderScreen", _dataSource);
		_gauntletLayer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("GenericPanelGameKeyCategory"));
		_gauntletLayer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("FaceGenHotkeyCategory"));
		_dataSource.SetCancelInputKey(HotKeyManager.GetCategory("GenericPanelGameKeyCategory").GetHotKey("Exit"));
		_dataSource.SetDoneInputKey(HotKeyManager.GetCategory("GenericPanelGameKeyCategory").GetHotKey("Confirm"));
		CreateScene();
		AddLayer(SceneLayer);
		_checkWhetherAgentVisualIsReady = true;
		_firstCharacterRender = true;
		RefreshShieldAndCharacter();
		InformationManager.HideAllMessages();
	}

	private void Refresh()
	{
		RefreshShieldAndCharacter();
	}

	protected override void OnFrameTick(float dt)
	{
		base.OnFrameTick(dt);
		if (_isFinalized)
		{
			return;
		}
		HandleUserInput(dt);
		if (_isFinalized)
		{
			return;
		}
		UpdateCamera(dt);
		SceneLayer sceneLayer = SceneLayer;
		if (sceneLayer != null && sceneLayer.ReadyToRender())
		{
			LoadingWindow.DisableGlobalLoadingWindow();
		}
		_scene?.Tick(dt);
		if (_refreshBannersNextFrame)
		{
			UpdateBanners();
			_refreshBannersNextFrame = false;
		}
		if (_refreshCharacterAndShieldNextFrame)
		{
			RefreshShieldAndCharacterAux();
			_refreshCharacterAndShieldNextFrame = false;
		}
		if (!_checkWhetherAgentVisualIsReady)
		{
			return;
		}
		int num = (_agentVisualToShowIndex + 1) % 2;
		if (_agentVisuals[_agentVisualToShowIndex].GetEntity().CheckResources(_firstCharacterRender, checkFaceResources: true))
		{
			_agentVisuals[num].SetVisible(value: false);
			_agentVisuals[_agentVisualToShowIndex].SetVisible(value: true);
			_checkWhetherAgentVisualIsReady = false;
			_firstCharacterRender = false;
		}
		else
		{
			if (!_firstCharacterRender)
			{
				_agentVisuals[num].SetVisible(value: true);
			}
			_agentVisuals[_agentVisualToShowIndex].SetVisible(value: false);
		}
	}

	private void CreateScene()
	{
		_scene = Scene.CreateNewScene(initialize_physics: true, enable_decals: true, DecalAtlasGroup.Battle);
		_scene.SetName("BannerBuilderScreen");
		SceneInitializationData initData = new SceneInitializationData
		{
			InitPhysicsWorld = false
		};
		_scene.Read("banner_editor_scene", ref initData);
		_scene.SetShadow(shadowEnabled: true);
		_scene.DisableStaticShadows(value: true);
		_scene.SetDynamicShadowmapCascadesRadiusMultiplier(0.1f);
		_agentRendererSceneController = MBAgentRendererSceneController.CreateNewAgentRendererSceneController(_scene);
		float aspectRatio = Screen.AspectRatio;
		GameEntity gameEntity = _scene.FindEntityWithTag("spawnpoint_player");
		_characterFrame = gameEntity.GetFrame();
		_characterFrame.rotation.OrthonormalizeAccordingToForwardAndKeepUpAsZAxis();
		_cameraTargetDistanceAdder = 3.5f;
		_cameraCurrentDistanceAdder = _cameraTargetDistanceAdder;
		_cameraTargetElevationAdder = 1.15f;
		_cameraCurrentElevationAdder = _cameraTargetElevationAdder;
		_camera = Camera.CreateCamera();
		_camera.SetFovVertical(0.6981317f, aspectRatio, 0.2f, 200f);
		SceneLayer = new SceneLayer();
		SceneLayer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("FaceGenHotkeyCategory"));
		SceneLayer.SetScene(_scene);
		UpdateCamera(0f);
		SceneLayer.SetSceneUsesShadows(value: true);
		SceneLayer.SceneView.SetResolutionScaling(value: true);
		int num = -1;
		num &= -5;
		SceneLayer.SetPostfxConfigParams(num);
		AddCharacterEntities(in ActionIndexCache.act_walk_idle_1h_with_shield_left_stance);
	}

	private void AddCharacterEntities(in ActionIndexCache action)
	{
		_weaponEquipment = new Equipment();
		for (int i = 0; i < 12; i++)
		{
			EquipmentElement equipmentFromSlot = _character.Equipment.GetEquipmentFromSlot((EquipmentIndex)i);
			if (equipmentFromSlot.Item?.PrimaryWeapon == null || (!equipmentFromSlot.Item.PrimaryWeapon.IsShield && !equipmentFromSlot.Item.ItemFlags.HasAllFlags(ItemFlags.DropOnWeaponChange)))
			{
				_weaponEquipment.AddEquipmentToSlotWithoutAgent((EquipmentIndex)i, equipmentFromSlot);
			}
		}
		_weaponEquipment.AddEquipmentToSlotWithoutAgent((EquipmentIndex)_dataSource.ShieldSlotIndex, _dataSource.ShieldRosterElement.EquipmentElement);
		Monster baseMonsterFromRace = TaleWorlds.Core.FaceGen.GetBaseMonsterFromRace(_character.Race);
		_agentVisuals[0] = AgentVisuals.Create(new AgentVisualsData().Equipment(_weaponEquipment).BodyProperties(_character.GetBodyProperties(_weaponEquipment)).Frame(_characterFrame)
			.ActionSet(MBGlobals.GetActionSetWithSuffix(baseMonsterFromRace, _character.IsFemale, "_facegen"))
			.ActionCode(in action)
			.Scene(_scene)
			.Monster(baseMonsterFromRace)
			.SkeletonType(_character.IsFemale ? SkeletonType.Female : SkeletonType.Male)
			.Race(_character.Race)
			.PrepareImmediately(prepareImmediately: true)
			.RightWieldedItemIndex(-1)
			.LeftWieldedItemIndex(_dataSource.ShieldSlotIndex)
			.ClothColor1(_dataSource.CurrentBanner.GetPrimaryColor())
			.ClothColor2(_dataSource.CurrentBanner.GetFirstIconColor())
			.Banner(_dataSource.CurrentBanner)
			.UseMorphAnims(useMorphAnims: true), "BannerEditorChar", isRandomProgress: false, needBatchedVersionForWeaponMeshes: false, forceUseFaceCache: true);
		_agentVisuals[0].SetAgentLodZeroOrMaxExternal(makeZero: true);
		_agentVisuals[0].Refresh(needBatchedVersionForWeaponMeshes: false, _agentVisuals[0].GetCopyAgentVisualsData(), forceUseFaceCache: true);
		MissionWeapon shieldWeapon = new MissionWeapon(_dataSource.ShieldRosterElement.EquipmentElement.Item, _dataSource.ShieldRosterElement.EquipmentElement.ItemModifier, _dataSource.CurrentBanner);
		Action<TaleWorlds.Engine.Texture> setAction = delegate(TaleWorlds.Engine.Texture tex)
		{
			shieldWeapon.GetWeaponData(needBatchedVersionForMeshes: false).TableauMaterial.SetTexture(TaleWorlds.Engine.Material.MBTextureType.DiffuseMap2, tex);
		};
		_dataSource.CurrentBanner.GetTableauTextureLarge(BannerDebugInfo.CreateManual(GetType().Name), setAction);
		_agentVisuals[0].SetVisible(value: false);
		_agentVisuals[0].GetEntity().CheckResources(addToQueue: true, checkFaceResources: true);
		_agentVisuals[1] = AgentVisuals.Create(new AgentVisualsData().Equipment(_weaponEquipment).BodyProperties(_character.GetBodyProperties(_weaponEquipment)).Frame(_characterFrame)
			.ActionSet(MBGlobals.GetActionSetWithSuffix(baseMonsterFromRace, _character.IsFemale, "_facegen"))
			.ActionCode(in action)
			.Scene(_scene)
			.Race(_character.Race)
			.Monster(baseMonsterFromRace)
			.SkeletonType(_character.IsFemale ? SkeletonType.Female : SkeletonType.Male)
			.PrepareImmediately(prepareImmediately: true)
			.RightWieldedItemIndex(-1)
			.LeftWieldedItemIndex(_dataSource.ShieldSlotIndex)
			.Banner(_dataSource.CurrentBanner)
			.ClothColor1(_dataSource.CurrentBanner.GetPrimaryColor())
			.ClothColor2(_dataSource.CurrentBanner.GetFirstIconColor())
			.UseMorphAnims(useMorphAnims: true), "BannerEditorChar", isRandomProgress: false, needBatchedVersionForWeaponMeshes: false, forceUseFaceCache: true);
		_agentVisuals[1].SetAgentLodZeroOrMaxExternal(makeZero: true);
		_agentVisuals[1].Refresh(needBatchedVersionForWeaponMeshes: false, _agentVisuals[1].GetCopyAgentVisualsData(), forceUseFaceCache: true);
		_agentVisuals[1].SetVisible(value: false);
		_agentVisuals[1].GetEntity().CheckResources(addToQueue: true, checkFaceResources: true);
		UpdateBanners();
	}

	private void UpdateBanners()
	{
		Banner currentBanner = _dataSource.CurrentBanner;
		_dataSource.CurrentBanner.GetTableauTextureLarge(BannerDebugInfo.CreateManual(GetType().Name), delegate(TaleWorlds.Engine.Texture resultTexture)
		{
			OnNewBannerReadyForBanners(currentBanner, resultTexture);
		}, out var _);
	}

	private void OnNewBannerReadyForBanners(Banner bannerOfTexture, TaleWorlds.Engine.Texture newTexture)
	{
		if (_isFinalized || !(_scene != null) || !(_currentBanner?.BannerCode == bannerOfTexture.BannerCode))
		{
			return;
		}
		GameEntity gameEntity = _scene.FindEntityWithTag("banner");
		if (gameEntity != null)
		{
			Mesh firstMesh = gameEntity.GetFirstMesh();
			if (firstMesh != null && _dataSource.CurrentBanner != null)
			{
				firstMesh.GetMaterial().SetTexture(TaleWorlds.Engine.Material.MBTextureType.DiffuseMap2, newTexture);
			}
		}
		else
		{
			gameEntity = _scene.FindEntityWithTag("banner_2");
			Mesh firstMesh2 = gameEntity.GetFirstMesh();
			if (firstMesh2 != null && _dataSource.CurrentBanner != null)
			{
				firstMesh2.GetMaterial().SetTexture(TaleWorlds.Engine.Material.MBTextureType.DiffuseMap2, newTexture);
			}
		}
		_refreshCharacterAndShieldNextFrame = true;
	}

	protected override void OnFinalize()
	{
		base.OnFinalize();
		_bannerIconsCategory.Unload();
		_bannerBuilderCategory.Unload();
		_dataSource.OnFinalize();
		_isFinalized = true;
	}

	private void RefreshShieldAndCharacter()
	{
		_currentBanner = _dataSource.CurrentBanner;
		_dataSource.BannerCodeAsString = _currentBanner.BannerCode;
		_refreshBannersNextFrame = true;
	}

	private void RefreshShieldAndCharacterAux()
	{
		_ = _agentVisualToShowIndex;
		_agentVisualToShowIndex = (_agentVisualToShowIndex + 1) % 2;
		AgentVisualsData copyAgentVisualsData = _agentVisuals[_agentVisualToShowIndex].GetCopyAgentVisualsData();
		copyAgentVisualsData.Equipment(_weaponEquipment).RightWieldedItemIndex(-1).LeftWieldedItemIndex(_dataSource.ShieldSlotIndex)
			.Banner(_dataSource.CurrentBanner)
			.Frame(_characterFrame)
			.BodyProperties(_character.GetBodyProperties(_weaponEquipment))
			.ClothColor1(_dataSource.CurrentBanner.GetPrimaryColor())
			.ClothColor2(_dataSource.CurrentBanner.GetFirstIconColor());
		_agentVisuals[_agentVisualToShowIndex].Refresh(needBatchedVersionForWeaponMeshes: false, copyAgentVisualsData, forceUseFaceCache: true);
		_agentVisuals[_agentVisualToShowIndex].GetEntity().CheckResources(addToQueue: true, checkFaceResources: true);
		_agentVisuals[_agentVisualToShowIndex].GetVisuals().GetSkeleton().TickAnimationsAndForceUpdate(0.001f, _characterFrame, tickAnimsForChildren: true);
		_agentVisuals[_agentVisualToShowIndex].SetVisible(value: false);
		_agentVisuals[_agentVisualToShowIndex].SetVisible(value: true);
		_checkWhetherAgentVisualIsReady = true;
	}

	private void HandleUserInput(float dt)
	{
		if (_gauntletLayer.IsFocusedOnInput())
		{
			return;
		}
		if (_gauntletLayer.Input.IsHotKeyReleased("Confirm") || SceneLayer.Input.IsHotKeyReleased("Confirm"))
		{
			_dataSource.ExecuteDone();
			return;
		}
		if (_gauntletLayer.Input.IsHotKeyReleased("Exit") || SceneLayer.Input.IsHotKeyReleased("Exit"))
		{
			_dataSource.ExecuteCancel();
			return;
		}
		if (SceneLayer.IsHitThisFrame && ScreenManager.FocusedLayer == _gauntletLayer)
		{
			_gauntletLayer.IsFocusLayer = false;
			ScreenManager.TryLoseFocus(_gauntletLayer);
			SceneLayer.IsFocusLayer = true;
			ScreenManager.TrySetFocus(SceneLayer);
		}
		else if (!SceneLayer.IsHitThisFrame && ScreenManager.FocusedLayer == SceneLayer)
		{
			SceneLayer.IsFocusLayer = false;
			ScreenManager.TryLoseFocus(SceneLayer);
			_gauntletLayer.IsFocusLayer = true;
			ScreenManager.TrySetFocus(_gauntletLayer);
		}
		Vec2 vec = new Vec2(SceneLayer.Input.GetNormalizedMouseMoveX() * 1920f, SceneLayer.Input.GetNormalizedMouseMoveY() * 1080f);
		bool flag = SceneLayer.Input.IsHotKeyDown("Zoom");
		bool flag2 = SceneLayer.Input.IsHotKeyDown("Rotate");
		bool flag3 = SceneLayer.Input.IsHotKeyDown("Ascend");
		if (flag || flag2 || flag3)
		{
			MBWindowManager.DontChangeCursorPos();
			_gauntletLayer.InputRestrictions.SetMouseVisibility(isVisible: false);
		}
		else
		{
			_gauntletLayer.InputRestrictions.SetMouseVisibility(isVisible: true);
		}
		float gameKeyState = SceneLayer.Input.GetGameKeyState(56);
		float inputValue = SceneLayer.Input.GetGameKeyState(57) - gameKeyState;
		float num;
		if (Input.IsGamepadActive)
		{
			NormalizeControllerInputForDeadZone(ref inputValue, 0.1f);
			num = inputValue * 5f * dt;
		}
		else
		{
			float num2 = SceneLayer.Input.GetDeltaMouseScroll() * -1f;
			float num3 = (flag ? (vec.y * -1f) : 0f);
			num = num2 * 0.002f + num3 * 0.004f;
		}
		_cameraTargetDistanceAdder = MBMath.ClampFloat(_cameraTargetDistanceAdder + num, 1.5f, 5f);
		float num4;
		if (Input.IsGamepadActive)
		{
			float inputValue2 = SceneLayer.Input.GetGameKeyAxis("CameraAxisX") * -1f;
			NormalizeControllerInputForDeadZone(ref inputValue2, 0.1f);
			num4 = inputValue2 * 600f * SceneLayer.Input.GetMouseSensitivity() * dt;
		}
		else
		{
			num4 = (flag2 ? (vec.x * -1f) : 0f) * 0.3f * SceneLayer.Input.GetMouseSensitivity();
		}
		_cameraTargetRotation = MBMath.WrapAngle(_cameraTargetRotation + num4 * (System.MathF.PI / 180f));
		float num5;
		if (Input.IsGamepadActive)
		{
			float inputValue3 = SceneLayer.Input.GetGameKeyAxis("CameraAxisY");
			NormalizeControllerInputForDeadZone(ref inputValue3, 0.1f);
			num5 = inputValue3 * 2f * dt;
		}
		else
		{
			num5 = (flag3 ? vec.y : 0f) * 0.002f;
		}
		_cameraTargetElevationAdder = MBMath.ClampFloat(_cameraTargetElevationAdder + num5, 0.5f, 1.9f * _agentVisuals[_agentVisualToShowIndex].GetScale());
		if (Input.DebugInput.IsHotKeyPressed("Copy"))
		{
			CopyBannerCode();
		}
		if (Input.DebugInput.IsHotKeyPressed("Duplicate"))
		{
			_dataSource.ExecuteDuplicateCurrentLayer();
		}
		if (Input.DebugInput.IsHotKeyPressed("Paste"))
		{
			_dataSource.SetBannerCode(Input.GetClipboardText());
			RefreshShieldAndCharacter();
		}
		if (Input.DebugInput.IsKeyPressed(InputKey.Delete))
		{
			_dataSource.DeleteCurrentLayer();
		}
		Vec2 moveDirection = new Vec2(0f, 0f);
		if (Input.DebugInput.IsKeyReleased(InputKey.Left))
		{
			moveDirection.x = -1f;
		}
		else if (Input.DebugInput.IsKeyReleased(InputKey.Right))
		{
			moveDirection.x = 1f;
		}
		if (Input.DebugInput.IsKeyReleased(InputKey.Down))
		{
			moveDirection.y = 1f;
		}
		else if (Input.DebugInput.IsKeyReleased(InputKey.Up))
		{
			moveDirection.y = -1f;
		}
		if (moveDirection.x != 0f || moveDirection.y != 0f)
		{
			_dataSource.TranslateCurrentLayerWith(moveDirection);
		}
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

	private void UpdateCamera(float dt)
	{
		float amount = TaleWorlds.Library.MathF.Min(1f, 10f * dt);
		_cameraCurrentRotation = TaleWorlds.Library.MathF.AngleLerp(_cameraCurrentRotation, _cameraTargetRotation, amount);
		_cameraCurrentElevationAdder = TaleWorlds.Library.MathF.Lerp(_cameraCurrentElevationAdder, _cameraTargetElevationAdder, amount);
		_cameraCurrentDistanceAdder = TaleWorlds.Library.MathF.Lerp(_cameraCurrentDistanceAdder, _cameraTargetDistanceAdder, amount);
		MatrixFrame characterFrame = _characterFrame;
		characterFrame.rotation.RotateAboutUp(_cameraCurrentRotation);
		characterFrame.origin += _cameraCurrentElevationAdder * characterFrame.rotation.u + _cameraCurrentDistanceAdder * characterFrame.rotation.f;
		characterFrame.rotation.RotateAboutSide(-System.MathF.PI / 2f);
		characterFrame.rotation.RotateAboutUp(System.MathF.PI);
		characterFrame.rotation.RotateAboutForward(System.MathF.PI * 3f / 50f);
		_camera.Frame = characterFrame;
		SceneLayer.SetCamera(_camera);
		SoundManager.SetListenerFrame(characterFrame);
	}

	private void CopyBannerCode()
	{
		Input.SetClipboardText(_dataSource.GetBannerCode());
		InformationManager.DisplayMessage(new InformationMessage("Banner code copied to the clipboard."));
	}

	public void Exit(bool isCancel)
	{
		MouseManager.ActivateMouseCursor(CursorType.Default);
		GameStateManager.Current.PopState();
	}

	void IGameStateListener.OnActivate()
	{
	}

	void IGameStateListener.OnDeactivate()
	{
		_agentVisuals[0].Reset();
		_agentVisuals[1].Reset();
		MBAgentRendererSceneController.DestructAgentRendererSceneController(_scene, _agentRendererSceneController, deleteThisFrame: false);
		_agentRendererSceneController = null;
		_scene?.ManualInvalidate();
		_scene = null;
	}

	void IGameStateListener.OnInitialize()
	{
	}

	void IGameStateListener.OnFinalize()
	{
	}
}
