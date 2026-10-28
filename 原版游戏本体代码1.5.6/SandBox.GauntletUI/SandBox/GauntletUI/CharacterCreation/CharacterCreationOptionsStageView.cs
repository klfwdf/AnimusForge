using System;
using System.Collections.Generic;
using System.Linq;
using SandBox.View.CharacterCreation;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterCreationContent;
using TaleWorlds.CampaignSystem.ViewModelCollection.CharacterCreation.OptionsStage;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection;
using TaleWorlds.Engine;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.Engine.Screens;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.GauntletUI.BodyGenerator;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.ViewModelCollection.EscapeMenu;
using TaleWorlds.ScreenSystem;
using TaleWorlds.TwoDimension;

namespace SandBox.GauntletUI.CharacterCreation;

[CharacterCreationStageView(typeof(CharacterCreationOptionsStage))]
public class CharacterCreationOptionsStageView : CharacterCreationStageViewBase
{
	protected readonly TextObject _affirmativeActionText;

	protected readonly TextObject _negativeActionText;

	private readonly GauntletMovieIdentifier _movie;

	private GauntletLayer GauntletLayer;

	private CharacterCreationOptionsStageVM _dataSource;

	private readonly CharacterCreationManager _characterCreationManager;

	private Scene _characterScene;

	private Camera _camera;

	private MatrixFrame _initialCharacterFrame;

	private AgentVisuals _agentVisuals;

	private GameEntity _mountEntity;

	private float _charRotationAmount;

	private EscapeMenuVM _escapeMenuDatasource;

	private GauntletMovieIdentifier _escapeMenuMovie;

	public SceneLayer CharacterLayer { get; private set; }

	public CharacterCreationOptionsStageView(CharacterCreationManager characterCreationManager, ControlCharacterCreationStage affirmativeAction, TextObject affirmativeActionText, ControlCharacterCreationStage negativeAction, TextObject negativeActionText, ControlCharacterCreationStage refreshAction, ControlCharacterCreationStageReturnInt getCurrentStageIndexAction, ControlCharacterCreationStageReturnInt getTotalStageCountAction, ControlCharacterCreationStageReturnInt getFurthestIndexAction, ControlCharacterCreationStageWithInt goToIndexAction)
		: base(affirmativeAction, negativeAction, refreshAction, getCurrentStageIndexAction, getTotalStageCountAction, getFurthestIndexAction, goToIndexAction)
	{
		_characterCreationManager = characterCreationManager;
		_affirmativeActionText = new TextObject("{=lBQXP6Wj}Start Game");
		_negativeActionText = negativeActionText;
		GauntletLayer = new GauntletLayer("CharacterCreationOptions", 1);
		GauntletLayer.InputRestrictions.SetInputRestrictions();
		GauntletLayer.IsFocusLayer = true;
		GauntletLayer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("GenericPanelGameKeyCategory"));
		ScreenManager.TrySetFocus(GauntletLayer);
		_dataSource = new CharacterCreationOptionsStageVM(_characterCreationManager, NextStage, _affirmativeActionText, PreviousStage, _negativeActionText);
		_dataSource.SetCancelInputKey(HotKeyManager.GetCategory("GenericPanelGameKeyCategory").GetHotKey("Exit"));
		_dataSource.SetDoneInputKey(HotKeyManager.GetCategory("GenericPanelGameKeyCategory").GetHotKey("Confirm"));
		GameAxisKey gameAxisKey = HotKeyManager.GetCategory("FaceGenHotkeyCategory").RegisteredGameAxisKeys.FirstOrDefault((GameAxisKey x) => x.Id == "CameraAxisX");
		_dataSource.AddCameraControlInputKey(gameAxisKey, Module.CurrentModule.GlobalTextManager.FindText("str_key_name", typeof(FaceGenHotkeyCategory).Name + "_" + gameAxisKey.Id));
		_movie = GauntletLayer.LoadMovie("CharacterCreationOptionsStage", _dataSource);
	}

	public override void SetGenericScene(Scene scene)
	{
		OpenScene(scene);
		AddCharacterEntity();
		RefreshMountEntity();
	}

	private void OpenScene(Scene cachedScene)
	{
		_characterScene = cachedScene;
		_characterScene.SetShadow(shadowEnabled: true);
		_characterScene.SetDynamicShadowmapCascadesRadiusMultiplier(0.1f);
		_characterScene.FindEntityWithName("cradle")?.SetVisibilityExcludeParents(visible: false);
		_characterScene.SetDoNotWaitForLoadingStatesToRender(value: true);
		_characterScene.DisableStaticShadows(value: true);
		_camera = Camera.CreateCamera();
		BodyGeneratorView.InitCamera(_camera, _cameraPosition);
		CharacterLayer = new SceneLayer(clearSceneOnFinalize: false);
		CharacterLayer.SetScene(_characterScene);
		CharacterLayer.SetCamera(_camera);
		CharacterLayer.SetSceneUsesShadows(value: true);
		CharacterLayer.SetRenderWithPostfx(value: true);
		CharacterLayer.SetPostfxFromConfig();
		CharacterLayer.SceneView.SetResolutionScaling(value: true);
		int num = -1;
		num &= -5;
		CharacterLayer.SetPostfxConfigParams(num);
		CharacterLayer.SetPostfxFromConfig();
		CharacterLayer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("FaceGenHotkeyCategory"));
		CharacterLayer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("GenericPanelGameKeyCategory"));
	}

	private void AddCharacterEntity()
	{
		GameEntity gameEntity = _characterScene.FindEntityWithTag("spawnpoint_player_1");
		_initialCharacterFrame = gameEntity.GetFrame();
		_initialCharacterFrame.origin.z = 0f;
		CharacterObject characterObject = Hero.MainHero.CharacterObject;
		Monster baseMonsterFromRace = TaleWorlds.Core.FaceGen.GetBaseMonsterFromRace(characterObject.Race);
		AgentVisualsData agentVisualsData = new AgentVisualsData().UseMorphAnims(useMorphAnims: true).Equipment(characterObject.Equipment).BodyProperties(characterObject.GetBodyProperties(characterObject.Equipment))
			.SkeletonType(characterObject.IsFemale ? SkeletonType.Female : SkeletonType.Male)
			.Frame(_initialCharacterFrame)
			.ActionSet(MBGlobals.GetActionSetWithSuffix(baseMonsterFromRace, characterObject.IsFemale, "_facegen"))
			.ActionCode(in ActionIndexCache.act_inventory_idle_start)
			.Scene(_characterScene)
			.Race(characterObject.Race)
			.Monster(baseMonsterFromRace)
			.PrepareImmediately(prepareImmediately: true)
			.UseTranslucency(useTranslucency: true)
			.UseTesselation(useTesselation: true);
		CharacterCreationContent characterCreationContent = (GameStateManager.Current.ActiveState as CharacterCreationState).CharacterCreationManager.CharacterCreationContent;
		Banner selectedBanner = characterCreationContent.SelectedBanner;
		CultureObject selectedCulture = characterCreationContent.SelectedCulture;
		if (selectedBanner != null)
		{
			agentVisualsData.ClothColor1(selectedBanner.GetPrimaryColor());
			agentVisualsData.ClothColor2(selectedBanner.GetFirstIconColor());
		}
		else if (characterCreationContent.SelectedCulture != null)
		{
			agentVisualsData.ClothColor1(selectedCulture.Color);
			agentVisualsData.ClothColor2(selectedCulture.Color2);
		}
		_agentVisuals = AgentVisuals.Create(agentVisualsData, "facegenvisual", isRandomProgress: false, needBatchedVersionForWeaponMeshes: false, forceUseFaceCache: false);
		CharacterLayer.SetFocusedShadowmap(enable: true, ref _initialCharacterFrame.origin, 0.59999996f);
	}

	private void RefreshCharacterEntityFrame()
	{
		MatrixFrame frame = _initialCharacterFrame;
		frame.rotation.RotateAboutUp(_charRotationAmount);
		frame.rotation.ApplyScaleLocal(_agentVisuals.GetScale());
		_agentVisuals.GetEntity().SetFrame(ref frame);
	}

	private void RefreshMountEntity()
	{
		RemoveMount();
		if (CharacterObject.PlayerCharacter.HasMount())
		{
			ItemObject item = CharacterObject.PlayerCharacter.Equipment[EquipmentIndex.ArmorItemEndSlot].Item;
			GameEntity gameEntity = _characterScene.FindEntityWithTag("spawnpoint_mount_1");
			MountCreationKey randomMountKey = MountCreationKey.GetRandomMountKey(item, CharacterObject.PlayerCharacter.GetMountKeySeed());
			HorseComponent horseComponent = item.HorseComponent;
			Monster monster = horseComponent.Monster;
			_mountEntity = GameEntity.CreateEmpty(_characterScene);
			AnimationSystemData animationSystemData = monster.FillAnimationSystemData(MBGlobals.GetActionSet(horseComponent.Monster.ActionSetCode), 1f, hasClippingPlane: false);
			_mountEntity.CreateSkeletonWithActionSet(ref animationSystemData);
			_mountEntity.Skeleton.SetAgentActionChannel(0, in ActionIndexCache.act_inventory_idle_start, MBRandom.RandomFloat);
			MountVisualCreator.AddMountMeshToEntity(harnessItem: CharacterObject.PlayerCharacter.Equipment[EquipmentIndex.HorseHarness].Item, gameEntity: _mountEntity, mountItem: item, mountCreationKeyStr: randomMountKey.ToString());
			MatrixFrame frame = gameEntity.GetGlobalFrame();
			_mountEntity.SetFrame(ref frame);
			_agentVisuals.GetVisuals().GetSkeleton().TickAnimationsAndForceUpdate(0.001f, _initialCharacterFrame, tickAnimsForChildren: true);
		}
	}

	private void RemoveMount()
	{
		if (_mountEntity != null)
		{
			_mountEntity.Remove(117);
		}
		_mountEntity = null;
	}

	public override void Tick(float dt)
	{
		base.Tick(dt);
		HandleEscapeMenu(this, CharacterLayer);
		_characterScene?.Tick(dt);
		_agentVisuals?.TickVisuals();
		TickInput(dt);
		HandleLayerInput();
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

	private void TickInput(float dt)
	{
		_dataSource.CharacterGamepadControlsEnabled = Input.IsGamepadActive && CharacterLayer.IsHitThisFrame;
		if (CharacterLayer.IsHitThisFrame && ScreenManager.FocusedLayer == GauntletLayer)
		{
			GauntletLayer.IsFocusLayer = false;
			ScreenManager.TryLoseFocus(GauntletLayer);
			CharacterLayer.IsFocusLayer = true;
			ScreenManager.TrySetFocus(CharacterLayer);
		}
		else if (!CharacterLayer.IsHitThisFrame && ScreenManager.FocusedLayer == CharacterLayer)
		{
			CharacterLayer.IsFocusLayer = false;
			ScreenManager.TryLoseFocus(CharacterLayer);
			GauntletLayer.IsFocusLayer = true;
			ScreenManager.TrySetFocus(GauntletLayer);
		}
		Vec2 vec = new Vec2(CharacterLayer.Input.GetNormalizedMouseMoveX() * 1920f, CharacterLayer.Input.GetNormalizedMouseMoveY() * 1080f);
		bool flag = CharacterLayer.Input.IsHotKeyDown("Rotate");
		if (flag)
		{
			MBWindowManager.DontChangeCursorPos();
			GauntletLayer.InputRestrictions.SetMouseVisibility(isVisible: false);
		}
		else
		{
			GauntletLayer.InputRestrictions.SetMouseVisibility(isVisible: true);
		}
		float num;
		if (Input.IsGamepadActive)
		{
			float inputValue = CharacterLayer.Input.GetGameKeyAxis("CameraAxisX");
			NormalizeControllerInputForDeadZone(ref inputValue, 0.1f);
			num = inputValue * 400f * dt;
		}
		else
		{
			num = (flag ? vec.x : 0f) * 0.2f;
		}
		_charRotationAmount = MBMath.WrapAngle(_charRotationAmount + num * (System.MathF.PI / 180f));
		RefreshCharacterEntityFrame();
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

	private bool IsHotKeyReleasedOnAnyLayer(string hotkeyName)
	{
		if (!GauntletLayer.Input.IsHotKeyReleased(hotkeyName))
		{
			return CharacterLayer.Input.IsHotKeyReleased(hotkeyName);
		}
		return true;
	}

	protected override void OnFinalize()
	{
		base.OnFinalize();
		SpriteCategory spriteCategory = UIResourceManager.GetSpriteCategory("ui_bannericons");
		if (spriteCategory.IsLoaded)
		{
			spriteCategory.Unload();
		}
		CharacterLayer.SceneView.SetEnable(value: false);
		CharacterLayer.SceneView.ClearAll(clearScene: false, removeTerrain: false);
		_agentVisuals.Reset();
		_agentVisuals = null;
		GauntletLayer = null;
		_dataSource?.OnFinalize();
		_dataSource = null;
		CharacterLayer = null;
		_characterScene = null;
	}

	public override IEnumerable<ScreenLayer> GetLayers()
	{
		return new List<ScreenLayer> { CharacterLayer, GauntletLayer };
	}

	public override int GetVirtualStageCount()
	{
		return 1;
	}

	public override void NextStage()
	{
		RemoveMount();
		_affirmativeAction();
	}

	public override void PreviousStage()
	{
		RemoveMount();
		_negativeAction();
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
}
