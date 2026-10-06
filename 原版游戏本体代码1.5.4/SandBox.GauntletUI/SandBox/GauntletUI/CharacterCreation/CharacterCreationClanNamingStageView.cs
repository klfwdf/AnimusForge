using System;
using System.Collections.Generic;
using System.Linq;
using SandBox.View.CharacterCreation;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterCreationContent;
using TaleWorlds.CampaignSystem.ViewModelCollection.CharacterCreation;
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
using TaleWorlds.MountAndBlade.View.Tableaus.Thumbnails;
using TaleWorlds.MountAndBlade.ViewModelCollection.EscapeMenu;
using TaleWorlds.ScreenSystem;

namespace SandBox.GauntletUI.CharacterCreation;

[CharacterCreationStageView(typeof(CharacterCreationClanNamingStage))]
public class CharacterCreationClanNamingStageView : CharacterCreationStageViewBase
{
	private CharacterCreationManager _characterCreationManager;

	private GauntletLayer GauntletLayer;

	private CharacterCreationClanNamingStageVM _dataSource;

	private GauntletMovieIdentifier _clanNamingStageMovie;

	private TextObject _affirmativeActionText;

	private TextObject _negativeActionText;

	private Banner _banner;

	private float _cameraCurrentRotation;

	private float _cameraTargetRotation;

	private float _cameraCurrentDistanceAdder;

	private float _cameraTargetDistanceAdder;

	private float _cameraCurrentElevationAdder;

	private float _cameraTargetElevationAdder;

	private readonly BasicCharacterObject _character;

	private Scene _scene;

	private MBAgentRendererSceneController _agentRendererSceneController;

	private AgentVisuals _agentVisuals;

	private MatrixFrame _characterFrame;

	private Equipment _weaponEquipment;

	private Camera _camera;

	private EscapeMenuVM _escapeMenuDatasource;

	private GauntletMovieIdentifier _escapeMenuMovie;

	private ItemRosterElement ShieldRosterElement => _dataSource.ShieldRosterElement;

	private int ShieldSlotIndex => _dataSource.ShieldSlotIndex;

	public SceneLayer SceneLayer { get; private set; }

	public CharacterCreationClanNamingStageView(CharacterCreationManager characterCreationManager, ControlCharacterCreationStage affirmativeAction, TextObject affirmativeActionText, ControlCharacterCreationStage negativeAction, TextObject negativeActionText, ControlCharacterCreationStage refreshAction, ControlCharacterCreationStageReturnInt getCurrentStageIndexAction, ControlCharacterCreationStageReturnInt getTotalStageCountAction, ControlCharacterCreationStageReturnInt getFurthestIndexAction, ControlCharacterCreationStageWithInt goToIndexAction)
		: base(affirmativeAction, negativeAction, refreshAction, getCurrentStageIndexAction, getTotalStageCountAction, getFurthestIndexAction, goToIndexAction)
	{
		_characterCreationManager = characterCreationManager;
		_affirmativeActionText = affirmativeActionText;
		_negativeActionText = negativeActionText;
		GauntletLayer = new GauntletLayer("CharacterCreationClanNaming", 1)
		{
			IsFocusLayer = true
		};
		GauntletLayer.InputRestrictions.SetInputRestrictions();
		GauntletLayer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("GenericPanelGameKeyCategory"));
		ScreenManager.TrySetFocus(GauntletLayer);
		_character = CharacterObject.PlayerCharacter;
		_banner = Clan.PlayerClan.Banner;
		_dataSource = new CharacterCreationClanNamingStageVM(_character, _characterCreationManager, NextStage, _affirmativeActionText, PreviousStage, _negativeActionText);
		_dataSource.SetCancelInputKey(HotKeyManager.GetCategory("GenericPanelGameKeyCategory").GetHotKey("Exit"));
		_dataSource.SetDoneInputKey(HotKeyManager.GetCategory("GenericPanelGameKeyCategory").GetHotKey("Confirm"));
		_dataSource.AddCameraControlInputKey(HotKeyManager.GetCategory("FaceGenHotkeyCategory").GetGameKey(56));
		_dataSource.AddCameraControlInputKey(HotKeyManager.GetCategory("FaceGenHotkeyCategory").GetGameKey(57));
		GameAxisKey gameAxisKey = HotKeyManager.GetCategory("FaceGenHotkeyCategory").RegisteredGameAxisKeys.FirstOrDefault((GameAxisKey x) => x.Id == "CameraAxisX");
		GameAxisKey gameAxisKey2 = HotKeyManager.GetCategory("FaceGenHotkeyCategory").RegisteredGameAxisKeys.FirstOrDefault((GameAxisKey x) => x.Id == "CameraAxisY");
		_dataSource.AddCameraControlInputKey(gameAxisKey, Module.CurrentModule.GlobalTextManager.FindText("str_key_name", typeof(FaceGenHotkeyCategory).Name + "_" + gameAxisKey.Id));
		_dataSource.AddCameraControlInputKey(gameAxisKey2, Module.CurrentModule.GlobalTextManager.FindText("str_key_name", typeof(FaceGenHotkeyCategory).Name + "_" + gameAxisKey2.Id));
		_clanNamingStageMovie = GauntletLayer.LoadMovie("CharacterCreationClanNamingStage", _dataSource);
		CreateScene();
		RefreshCharacterEntity();
	}

	public override void Tick(float dt)
	{
		HandleUserInput(dt);
		UpdateCamera(dt);
		if (SceneLayer != null && SceneLayer.ReadyToRender())
		{
			LoadingWindow.DisableGlobalLoadingWindow();
		}
		if (_scene != null)
		{
			_scene.Tick(dt);
		}
		HandleEscapeMenu(this, GauntletLayer);
		HandleLayerInput();
	}

	private void CreateScene()
	{
		_scene = Scene.CreateNewScene(initialize_physics: true, enable_decals: false);
		_scene.SetName("MBBannerEditorScreen");
		SceneInitializationData initData = new SceneInitializationData(initializeWithDefaults: true);
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
			if (equipmentFromSlot.Item?.PrimaryWeapon == null || (equipmentFromSlot.Item?.PrimaryWeapon != null && !equipmentFromSlot.Item.PrimaryWeapon.IsShield))
			{
				_weaponEquipment.AddEquipmentToSlotWithoutAgent((EquipmentIndex)i, equipmentFromSlot);
			}
		}
		Monster baseMonsterFromRace = TaleWorlds.Core.FaceGen.GetBaseMonsterFromRace(_character.Race);
		_agentVisuals = AgentVisuals.Create(new AgentVisualsData().Equipment(_weaponEquipment).BodyProperties(_character.GetBodyProperties(_weaponEquipment)).Frame(_characterFrame)
			.ActionSet(MBGlobals.GetActionSetWithSuffix(baseMonsterFromRace, _character.IsFemale, "_facegen"))
			.ActionCode(in action)
			.Scene(_scene)
			.Race(_character.Race)
			.Monster(baseMonsterFromRace)
			.SkeletonType(_character.IsFemale ? SkeletonType.Female : SkeletonType.Male)
			.PrepareImmediately(prepareImmediately: true)
			.UseMorphAnims(useMorphAnims: true), "BannerEditorChar", isRandomProgress: false, needBatchedVersionForWeaponMeshes: false, forceUseFaceCache: true);
		_agentVisuals.SetAgentLodZeroOrMaxExternal(makeZero: true);
		UpdateBanners();
	}

	private void UpdateBanners()
	{
		_banner.GetTableauTextureLarge(BannerDebugInfo.CreateManual(GetType().Name), OnNewBannerReadyForBanners);
	}

	private void OnNewBannerReadyForBanners(Texture newTexture)
	{
		if (_scene == null)
		{
			return;
		}
		GameEntity gameEntity = _scene.FindEntityWithTag("banner");
		if (gameEntity == null)
		{
			return;
		}
		Mesh firstMesh = gameEntity.GetFirstMesh();
		if (firstMesh != null && _banner != null)
		{
			firstMesh.GetMaterial().SetTexture(Material.MBTextureType.DiffuseMap2, newTexture);
		}
		gameEntity = _scene.FindEntityWithTag("banner_2");
		if (!(gameEntity == null))
		{
			firstMesh = gameEntity.GetFirstMesh();
			if (firstMesh != null && _banner != null)
			{
				firstMesh.GetMaterial().SetTexture(Material.MBTextureType.DiffuseMap2, newTexture);
			}
		}
	}

	private void RefreshCharacterEntity()
	{
		_weaponEquipment.AddEquipmentToSlotWithoutAgent((EquipmentIndex)ShieldSlotIndex, ShieldRosterElement.EquipmentElement);
		AgentVisualsData copyAgentVisualsData = _agentVisuals.GetCopyAgentVisualsData();
		copyAgentVisualsData.Equipment(_weaponEquipment).RightWieldedItemIndex(-1).LeftWieldedItemIndex(ShieldSlotIndex)
			.Banner(_banner)
			.ClothColor1(_banner.GetPrimaryColor())
			.ClothColor2(_banner.GetFirstIconColor());
		_agentVisuals.Refresh(needBatchedVersionForWeaponMeshes: false, copyAgentVisualsData);
		MissionWeapon shieldWeapon = new MissionWeapon(ShieldRosterElement.EquipmentElement.Item, ShieldRosterElement.EquipmentElement.ItemModifier, _banner);
		Action<Texture> setAction = delegate(Texture tex)
		{
			shieldWeapon.GetWeaponData(needBatchedVersionForMeshes: false).TableauMaterial.SetTexture(Material.MBTextureType.DiffuseMap2, tex);
		};
		_banner.GetTableauTextureLarge(BannerDebugInfo.CreateManual(GetType().Name), setAction);
	}

	private void HandleLayerInput()
	{
		if (IsHotKeyReleasedOnAnyLayer("Exit"))
		{
			UISoundsHelper.PlayUISound("event:/ui/panels/next");
			_dataSource.OnPreviousStage();
		}
		else if (IsHotKeyReleasedOnAnyLayer("Confirm") && _dataSource.CanAdvance)
		{
			UISoundsHelper.PlayUISound("event:/ui/panels/next");
			_dataSource.OnNextStage();
		}
	}

	private void HandleUserInput(float dt)
	{
		_dataSource.CharacterGamepadControlsEnabled = Input.IsGamepadActive && SceneLayer.IsHitThisFrame;
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
		_cameraTargetElevationAdder = MBMath.ClampFloat(_cameraTargetElevationAdder + num5, 0.5f, 1.9f * _agentVisuals.GetScale());
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

	public override IEnumerable<ScreenLayer> GetLayers()
	{
		return new List<ScreenLayer> { SceneLayer, GauntletLayer };
	}

	public override int GetVirtualStageCount()
	{
		return 1;
	}

	public override void NextStage()
	{
		TextObject variable = new TextObject(_dataSource.ClanName);
		TextObject textObject = GameTexts.FindText("str_generic_clan_name");
		textObject.SetTextVariable("CLAN_NAME", variable);
		Clan.PlayerClan.ChangeClanName(textObject, textObject);
		_affirmativeAction?.Invoke();
	}

	public override void PreviousStage()
	{
		_negativeAction?.Invoke();
	}

	protected override void OnFinalize()
	{
		base.OnFinalize();
		SceneLayer.SceneView.SetEnable(value: false);
		SceneLayer.SceneView.ClearAll(clearScene: true, removeTerrain: true);
		GauntletLayer = null;
		SceneLayer = null;
		_dataSource?.OnFinalize();
		_dataSource = null;
		_clanNamingStageMovie = null;
		_agentVisuals.Reset();
		_agentVisuals = null;
		MBAgentRendererSceneController.DestructAgentRendererSceneController(_scene, _agentRendererSceneController, deleteThisFrame: false);
		_agentRendererSceneController = null;
		_scene?.ManualInvalidate();
		_scene = null;
	}

	public override void LoadEscapeMenuMovie()
	{
		_escapeMenuDatasource = new EscapeMenuVM(GetEscapeMenuItems(this));
		_escapeMenuMovie = GauntletLayer.LoadMovie("EscapeMenu", _escapeMenuDatasource);
	}

	public override void ReleaseEscapeMenuMovie()
	{
		GauntletLayer.ReleaseMovie(_escapeMenuMovie);
		_escapeMenuDatasource = null;
		_escapeMenuMovie = null;
	}

	private bool IsHotKeyReleasedOnAnyLayer(string hotkeyName)
	{
		if (!GauntletLayer.Input.IsHotKeyReleased(hotkeyName))
		{
			return SceneLayer.Input.IsHotKeyReleased(hotkeyName);
		}
		return true;
	}
}
