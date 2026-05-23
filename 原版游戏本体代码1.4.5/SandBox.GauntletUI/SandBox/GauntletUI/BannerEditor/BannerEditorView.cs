using System;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.ViewModelCollection;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection;
using TaleWorlds.Engine;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.Engine.Screens;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.Tableaus;
using TaleWorlds.MountAndBlade.View.Tableaus.Thumbnails;
using TaleWorlds.ScreenSystem;
using TaleWorlds.TwoDimension;

namespace SandBox.GauntletUI.BannerEditor;

public class BannerEditorView
{
	private GauntletMovieIdentifier _gauntletmovie;

	private readonly SpriteCategory _spriteCategory;

	private bool _isFinalized;

	private float _cameraCurrentRotation;

	private float _cameraTargetRotation;

	private float _cameraCurrentDistanceAdder;

	private float _cameraTargetDistanceAdder;

	private float _cameraCurrentElevationAdder;

	private float _cameraTargetElevationAdder;

	private readonly BasicCharacterObject _character;

	private Scene _scene;

	private MBAgentRendererSceneController _agentRendererSceneController;

	private AgentVisuals[] _agentVisuals;

	private int _agentVisualToShowIndex;

	private bool _checkWhetherAgentVisualIsReady;

	private bool _firstCharacterRender = true;

	private bool _refreshBannersNextFrame;

	private bool _refreshCharacterAndShieldNextFrame;

	private MatrixFrame _characterFrame;

	private MissionWeapon _shieldWeapon;

	private Equipment _weaponEquipment;

	private Banner _currentBanner;

	private Camera _camera;

	private BannerEditorTextureCreationData _latestBannerTextureCreationData;

	private BannerEditorTextureCreationData _latestShieldTextureCreationData;

	private bool _isOpenedFromCharacterCreation;

	private ControlCharacterCreationStage _affirmativeAction;

	private ControlCharacterCreationStage _negativeAction;

	private ControlCharacterCreationStageWithInt _goToIndexAction;

	public GauntletLayer GauntletLayer { get; private set; }

	public BannerEditorVM DataSource { get; private set; }

	public Banner Banner { get; private set; }

	private ItemRosterElement ShieldRosterElement => DataSource.ShieldRosterElement;

	private int ShieldSlotIndex => DataSource.ShieldSlotIndex;

	public SceneLayer SceneLayer { get; private set; }

	public BannerEditorView(BasicCharacterObject character, Banner banner, ControlCharacterCreationStage affirmativeAction, TextObject affirmativeActionText, ControlCharacterCreationStage negativeAction, TextObject negativeActionText, ControlCharacterCreationStage onRefresh = null, ControlCharacterCreationStageReturnInt getCurrentStageIndexAction = null, ControlCharacterCreationStageReturnInt getTotalStageCountAction = null, ControlCharacterCreationStageReturnInt getFurthestIndexAction = null, ControlCharacterCreationStageWithInt goToIndexAction = null)
	{
		BannerEditorTextureCache.Current?.FlushCache();
		_spriteCategory = UIResourceManager.LoadSpriteCategory("ui_bannericons");
		_character = character;
		Banner = banner;
		_goToIndexAction = goToIndexAction;
		if (getCurrentStageIndexAction == null || getTotalStageCountAction == null || getFurthestIndexAction == null)
		{
			DataSource = new BannerEditorVM(_character, Banner, Exit, RefreshShieldAndCharacter, 0, 0, 0, GoToIndex);
			DataSource.Description = new TextObject("{=3ZO5cMLu}Customize your banner's sigil").ToString();
			_isOpenedFromCharacterCreation = true;
		}
		else
		{
			DataSource = new BannerEditorVM(_character, Banner, Exit, RefreshShieldAndCharacter, getCurrentStageIndexAction(), getTotalStageCountAction(), getFurthestIndexAction(), GoToIndex);
			DataSource.Description = new TextObject("{=312lNJTM}Customize your personal banner by choosing your clan's sigil").ToString();
			_isOpenedFromCharacterCreation = false;
		}
		DataSource.DoneText = affirmativeActionText.ToString();
		DataSource.CancelText = negativeActionText.ToString();
		GauntletLayer = new GauntletLayer("BanerEditor", 1);
		GauntletLayer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("GenericPanelGameKeyCategory"));
		GauntletLayer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("FaceGenHotkeyCategory"));
		GauntletLayer.InputRestrictions.SetInputRestrictions();
		GauntletLayer.IsFocusLayer = true;
		ScreenManager.TrySetFocus(GauntletLayer);
		DataSource.SetCancelInputKey(HotKeyManager.GetCategory("GenericPanelGameKeyCategory").GetHotKey("Exit"));
		DataSource.SetDoneInputKey(HotKeyManager.GetCategory("GenericPanelGameKeyCategory").GetHotKey("Confirm"));
		DataSource.AddCameraControlInputKey(HotKeyManager.GetCategory("FaceGenHotkeyCategory").GetGameKey(56));
		DataSource.AddCameraControlInputKey(HotKeyManager.GetCategory("FaceGenHotkeyCategory").GetGameKey(57));
		GameAxisKey gameAxisKey = HotKeyManager.GetCategory("FaceGenHotkeyCategory").RegisteredGameAxisKeys.FirstOrDefault((GameAxisKey x) => x.Id == "CameraAxisX");
		GameAxisKey gameAxisKey2 = HotKeyManager.GetCategory("FaceGenHotkeyCategory").RegisteredGameAxisKeys.FirstOrDefault((GameAxisKey x) => x.Id == "CameraAxisY");
		DataSource.AddCameraControlInputKey(gameAxisKey, Module.CurrentModule.GlobalTextManager.FindText("str_key_name", typeof(FaceGenHotkeyCategory).Name + "_" + gameAxisKey.Id));
		DataSource.AddCameraControlInputKey(gameAxisKey2, Module.CurrentModule.GlobalTextManager.FindText("str_key_name", typeof(FaceGenHotkeyCategory).Name + "_" + gameAxisKey2.Id));
		_affirmativeAction = affirmativeAction;
		_negativeAction = negativeAction;
		_agentVisuals = new AgentVisuals[2];
		_currentBanner = Banner;
		CreateScene();
		Input.ClearKeys();
		_weaponEquipment.AddEquipmentToSlotWithoutAgent((EquipmentIndex)ShieldSlotIndex, ShieldRosterElement.EquipmentElement);
		_shieldWeapon = new MissionWeapon(ShieldRosterElement.EquipmentElement.Item, ShieldRosterElement.EquipmentElement.ItemModifier, Banner);
		AgentVisualsData copyAgentVisualsData = _agentVisuals[0].GetCopyAgentVisualsData();
		copyAgentVisualsData.Equipment(_weaponEquipment).RightWieldedItemIndex(-1).LeftWieldedItemIndex(ShieldSlotIndex)
			.Banner(Banner)
			.ClothColor1(Banner.GetPrimaryColor())
			.ClothColor2(Banner.GetFirstIconColor());
		_agentVisuals[0].Refresh(needBatchedVersionForWeaponMeshes: false, copyAgentVisualsData, forceUseFaceCache: true);
		_agentVisuals[0].SetVisible(value: false);
		_agentVisuals[0].GetEntity().CheckResources(addToQueue: true, checkFaceResources: true);
		AgentVisualsData copyAgentVisualsData2 = _agentVisuals[1].GetCopyAgentVisualsData();
		copyAgentVisualsData2.Equipment(_weaponEquipment).RightWieldedItemIndex(-1).LeftWieldedItemIndex(ShieldSlotIndex)
			.Banner(Banner)
			.ClothColor1(Banner.GetPrimaryColor())
			.ClothColor2(Banner.GetFirstIconColor());
		_agentVisuals[1].Refresh(needBatchedVersionForWeaponMeshes: false, copyAgentVisualsData2, forceUseFaceCache: true);
		_agentVisuals[1].SetVisible(value: false);
		_agentVisuals[1].GetEntity().CheckResources(addToQueue: true, checkFaceResources: true);
		_checkWhetherAgentVisualIsReady = true;
		_firstCharacterRender = true;
	}

	public void OnTick(float dt)
	{
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
			if (_gauntletmovie == null)
			{
				_gauntletmovie = GauntletLayer.LoadMovie("BannerEditor", DataSource);
			}
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

	public void OnFinalize()
	{
		BannerEditorTextureCache.Current?.FlushCache();
		if (!_isOpenedFromCharacterCreation)
		{
			_spriteCategory.Unload();
		}
		DataSource?.OnFinalize();
		_isFinalized = true;
	}

	public void Exit(bool isCancel)
	{
		MouseManager.ActivateMouseCursor(CursorType.Default);
		_gauntletmovie = null;
		if (isCancel)
		{
			_negativeAction();
			return;
		}
		SetMapIconAsDirtyForAllPlayerClanParties();
		_affirmativeAction();
	}

	private void SetMapIconAsDirtyForAllPlayerClanParties()
	{
		foreach (Hero aliveLord in Clan.PlayerClan.AliveLords)
		{
			foreach (CaravanPartyComponent ownedCaravan in aliveLord.OwnedCaravans)
			{
				ownedCaravan.MobileParty.Party?.SetVisualAsDirty();
				ownedCaravan.MobileParty.SetNavalVisualAsDirty();
			}
		}
		foreach (Hero companion in Clan.PlayerClan.Companions)
		{
			foreach (CaravanPartyComponent ownedCaravan2 in companion.OwnedCaravans)
			{
				ownedCaravan2.MobileParty.Party?.SetVisualAsDirty();
				ownedCaravan2.MobileParty.SetNavalVisualAsDirty();
			}
		}
		foreach (WarPartyComponent warPartyComponent in Clan.PlayerClan.WarPartyComponents)
		{
			warPartyComponent.MobileParty.Party?.SetVisualAsDirty();
			warPartyComponent.MobileParty.SetNavalVisualAsDirty();
		}
		foreach (Settlement settlement in Clan.PlayerClan.Settlements)
		{
			if (settlement.IsVillage && settlement.Village.VillagerPartyComponent != null)
			{
				settlement.Village.VillagerPartyComponent.MobileParty.Party?.SetVisualAsDirty();
			}
			else if ((settlement.IsCastle || settlement.IsTown) && settlement.Town.GarrisonParty != null)
			{
				settlement.Town.GarrisonParty.Party?.SetVisualAsDirty();
			}
		}
	}

	private void CreateScene()
	{
		_scene = Scene.CreateNewScene(initialize_physics: true, enable_decals: true, DecalAtlasGroup.Battle);
		_scene.SetName("MBBannerEditorScreen");
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
		SceneLayer.IsFocusLayer = true;
		SceneLayer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("GenericPanelGameKeyCategory"));
		SceneLayer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("FaceGenHotkeyCategory"));
		SceneLayer.SetScene(_scene);
		UpdateCamera(0f);
		SceneLayer.SetSceneUsesShadows(value: true);
		SceneLayer.SceneView.SetResolutionScaling(value: true);
		int num = -1;
		num &= -5;
		SceneLayer.SetPostfxConfigParams(num);
		AddCharacterEntity(in ActionIndexCache.act_walk_idle_1h_with_shield_left_stance);
	}

	private void AddCharacterEntity(in ActionIndexCache action)
	{
		_weaponEquipment = new Equipment();
		for (int i = 0; i < 12; i++)
		{
			EquipmentElement equipmentFromSlot = _character.Equipment.GetEquipmentFromSlot((EquipmentIndex)i);
			if (equipmentFromSlot.Item?.PrimaryWeapon == null || (equipmentFromSlot.Item?.PrimaryWeapon != null && !equipmentFromSlot.Item.PrimaryWeapon.IsShield && !equipmentFromSlot.Item.ItemFlags.HasAllFlags(ItemFlags.DropOnWeaponChange)))
			{
				_weaponEquipment.AddEquipmentToSlotWithoutAgent((EquipmentIndex)i, equipmentFromSlot);
			}
		}
		Monster baseMonsterFromRace = TaleWorlds.Core.FaceGen.GetBaseMonsterFromRace(_character.Race);
		_agentVisuals[0] = AgentVisuals.Create(new AgentVisualsData().Equipment(_weaponEquipment).BodyProperties(_character.GetBodyProperties(_weaponEquipment)).Frame(_characterFrame)
			.ActionSet(MBGlobals.GetActionSetWithSuffix(baseMonsterFromRace, _character.IsFemale, "_facegen"))
			.ActionCode(in action)
			.Scene(_scene)
			.Monster(baseMonsterFromRace)
			.SkeletonType(_character.IsFemale ? SkeletonType.Female : SkeletonType.Male)
			.Race(_character.Race)
			.PrepareImmediately(prepareImmediately: true)
			.UseMorphAnims(useMorphAnims: true), "BannerEditorChar", isRandomProgress: false, needBatchedVersionForWeaponMeshes: false, forceUseFaceCache: true);
		_agentVisuals[0].SetAgentLodZeroOrMaxExternal(makeZero: true);
		_agentVisuals[0].GetEntity().CheckResources(addToQueue: true, checkFaceResources: true);
		_agentVisuals[1] = AgentVisuals.Create(new AgentVisualsData().Equipment(_weaponEquipment).BodyProperties(_character.GetBodyProperties(_weaponEquipment)).Frame(_characterFrame)
			.ActionSet(MBGlobals.GetActionSetWithSuffix(baseMonsterFromRace, _character.IsFemale, "_facegen"))
			.ActionCode(in action)
			.Scene(_scene)
			.Race(_character.Race)
			.Monster(baseMonsterFromRace)
			.SkeletonType(_character.IsFemale ? SkeletonType.Female : SkeletonType.Male)
			.PrepareImmediately(prepareImmediately: true)
			.UseMorphAnims(useMorphAnims: true), "BannerEditorChar", isRandomProgress: false, needBatchedVersionForWeaponMeshes: false, forceUseFaceCache: true);
		_agentVisuals[1].SetAgentLodZeroOrMaxExternal(makeZero: true);
		_agentVisuals[1].GetEntity().CheckResources(addToQueue: true, checkFaceResources: true);
		UpdateBanners();
	}

	private void UpdateBanners()
	{
		if (_latestBannerTextureCreationData != null)
		{
			ThumbnailCacheManager.Current.DestroyTexture(_latestBannerTextureCreationData);
			_latestBannerTextureCreationData = null;
		}
		Banner.GetTableauTextureLargeForBannerEditor(BannerDebugInfo.CreateManual(GetType().Name), delegate(TaleWorlds.Engine.Texture tex)
		{
			OnNewBannerReadyForBanners(Banner, tex);
		}, out _latestBannerTextureCreationData);
	}

	private void OnNewBannerReadyForBanners(Banner bannerOfTexture, TaleWorlds.Engine.Texture newTexture)
	{
		if (_isFinalized || !(_scene != null) || !(_currentBanner.BannerCode == bannerOfTexture.BannerCode))
		{
			return;
		}
		GameEntity gameEntity = _scene.FindEntityWithTag("banner");
		if (gameEntity != null)
		{
			Mesh firstMesh = gameEntity.GetFirstMesh();
			if (firstMesh != null && Banner != null)
			{
				firstMesh.GetMaterial().SetTexture(TaleWorlds.Engine.Material.MBTextureType.DiffuseMap2, newTexture);
			}
		}
		else
		{
			gameEntity = _scene.FindEntityWithTag("banner_2");
			Mesh firstMesh2 = gameEntity.GetFirstMesh();
			if (firstMesh2 != null && Banner != null)
			{
				firstMesh2.GetMaterial().SetTexture(TaleWorlds.Engine.Material.MBTextureType.DiffuseMap2, newTexture);
			}
		}
		_refreshCharacterAndShieldNextFrame = true;
	}

	private void RefreshShieldAndCharacter()
	{
		_currentBanner = Banner;
		_refreshBannersNextFrame = true;
	}

	private void RefreshShieldAndCharacterAux()
	{
		if (_latestShieldTextureCreationData != null)
		{
			ThumbnailCacheManager.Current.DestroyTexture(_latestShieldTextureCreationData);
			_latestShieldTextureCreationData = null;
		}
		Banner.GetTableauTextureLargeForBannerEditor(BannerDebugInfo.CreateManual(GetType().Name), delegate(TaleWorlds.Engine.Texture tex)
		{
			OnNewBannerReadyForShield(tex);
		}, out _latestShieldTextureCreationData);
		_ = _agentVisualToShowIndex;
		_agentVisualToShowIndex = (_agentVisualToShowIndex + 1) % 2;
		AgentVisualsData copyAgentVisualsData = _agentVisuals[_agentVisualToShowIndex].GetCopyAgentVisualsData();
		copyAgentVisualsData.Equipment(_weaponEquipment).RightWieldedItemIndex(-1).LeftWieldedItemIndex(ShieldSlotIndex)
			.Banner(Banner)
			.Frame(_characterFrame)
			.BodyProperties(_character.GetBodyProperties(_weaponEquipment))
			.ClothColor1(Banner.GetPrimaryColor())
			.ClothColor2(Banner.GetFirstIconColor());
		_agentVisuals[_agentVisualToShowIndex].Refresh(needBatchedVersionForWeaponMeshes: false, copyAgentVisualsData, forceUseFaceCache: true);
		_agentVisuals[_agentVisualToShowIndex].GetEntity().CheckResources(addToQueue: true, checkFaceResources: true);
		_agentVisuals[_agentVisualToShowIndex].GetVisuals().GetSkeleton().TickAnimationsAndForceUpdate(0.001f, _characterFrame, tickAnimsForChildren: true);
		_agentVisuals[_agentVisualToShowIndex].SetVisible(value: false);
		_agentVisuals[_agentVisualToShowIndex].SetVisible(value: true);
		_checkWhetherAgentVisualIsReady = true;
	}

	private void OnNewBannerReadyForShield(TaleWorlds.Engine.Texture newTexture)
	{
		_shieldWeapon.GetWeaponData(needBatchedVersionForMeshes: false).TableauMaterial.SetTexture(TaleWorlds.Engine.Material.MBTextureType.DiffuseMap2, newTexture);
	}

	private bool IsHotKeyReleasedOnAnyLayer(string hotkeyName)
	{
		if (!GauntletLayer.Input.IsHotKeyReleased(hotkeyName))
		{
			return SceneLayer.Input.IsHotKeyReleased(hotkeyName);
		}
		return true;
	}

	private void HandleUserInput(float dt)
	{
		DataSource.CharacterGamepadControlsEnabled = Input.IsGamepadActive && SceneLayer.IsHitThisFrame;
		if (SceneLayer.IsHitThisFrame && ScreenManager.FocusedLayer == GauntletLayer)
		{
			GauntletLayer.IsFocusLayer = false;
			ScreenManager.TryLoseFocus(GauntletLayer);
			SceneLayer.IsFocusLayer = true;
			ScreenManager.TrySetFocus(SceneLayer);
		}
		else if (!SceneLayer.IsHitThisFrame && ScreenManager.FocusedLayer == SceneLayer)
		{
			SceneLayer.IsFocusLayer = false;
			ScreenManager.TryLoseFocus(SceneLayer);
			GauntletLayer.IsFocusLayer = true;
			ScreenManager.TrySetFocus(GauntletLayer);
		}
		if (IsHotKeyReleasedOnAnyLayer("Confirm"))
		{
			DataSource.ExecuteDone();
			UISoundsHelper.PlayUISound("event:/ui/panels/next");
			return;
		}
		if (IsHotKeyReleasedOnAnyLayer("Exit"))
		{
			DataSource.ExecuteCancel();
			UISoundsHelper.PlayUISound("event:/ui/panels/next");
			return;
		}
		Vec2 vec = new Vec2(SceneLayer.Input.GetNormalizedMouseMoveX() * 1920f, SceneLayer.Input.GetNormalizedMouseMoveY() * 1080f);
		bool flag = SceneLayer.Input.IsHotKeyDown("Zoom");
		bool flag2 = SceneLayer.Input.IsHotKeyDown("Rotate");
		bool flag3 = SceneLayer.Input.IsHotKeyDown("Ascend");
		if (flag || flag2 || flag3)
		{
			MBWindowManager.DontChangeCursorPos();
			GauntletLayer.InputRestrictions.SetMouseVisibility(isVisible: false);
		}
		else
		{
			GauntletLayer.InputRestrictions.SetMouseVisibility(isVisible: true);
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
		characterFrame.rotation.RotateAboutForward(System.MathF.PI * -3f / 50f);
		_camera.Frame = characterFrame;
		SceneLayer.SetCamera(_camera);
		SoundManager.SetListenerFrame(characterFrame);
	}

	public void OnDeactivate()
	{
		_agentVisuals[0].Reset();
		_agentVisuals[1].Reset();
		MBAgentRendererSceneController.DestructAgentRendererSceneController(_scene, _agentRendererSceneController, deleteThisFrame: false);
		_agentRendererSceneController = null;
		_scene.ClearAll();
		_scene.ManualInvalidate();
		_scene = null;
	}

	public void GoToIndex(int index)
	{
		_goToIndexAction(index);
	}
}
