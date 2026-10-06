using System.Collections.Generic;
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
using TaleWorlds.MountAndBlade.GauntletUI.BodyGenerator;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.ViewModelCollection.EscapeMenu;
using TaleWorlds.ScreenSystem;

namespace SandBox.GauntletUI.CharacterCreation;

[CharacterCreationStageView(typeof(CharacterCreationNarrativeStage))]
public class CharacterCreationNarrativeStageView : CharacterCreationStageViewBase
{
	protected readonly TextObject _affirmativeActionText;

	protected readonly TextObject _negativeActionText;

	private GauntletMovieIdentifier _movie;

	private GauntletLayer GauntletLayer;

	private CharacterCreationNarrativeStageVM _dataSource;

	private readonly CharacterCreationManager _characterCreationManager;

	private Scene _characterScene;

	private Camera _camera;

	private List<AgentVisuals> _currentMenuAgentVisuals;

	private List<GameEntity> _currentMenuMountEntities;

	private bool _isAgentVisualsDirty;

	private bool _isAgentVisualVisibilitiesDirty;

	private EscapeMenuVM _escapeMenuDatasource;

	private GauntletMovieIdentifier _escapeMenuMovie;

	public SceneLayer CharacterLayer { get; private set; }

	public CharacterCreationNarrativeStageView(CharacterCreationManager characterCreationManager, ControlCharacterCreationStage affirmativeAction, TextObject affirmativeActionText, ControlCharacterCreationStage negativeAction, TextObject negativeActionText, ControlCharacterCreationStage onRefresh, ControlCharacterCreationStageReturnInt getCurrentStageIndexAction, ControlCharacterCreationStageReturnInt getTotalStageCountAction, ControlCharacterCreationStageReturnInt getFurthestIndexAction, ControlCharacterCreationStageWithInt goToIndexAction)
		: base(affirmativeAction, negativeAction, onRefresh, getCurrentStageIndexAction, getTotalStageCountAction, getFurthestIndexAction, goToIndexAction)
	{
		_characterCreationManager = characterCreationManager;
		_affirmativeActionText = affirmativeActionText;
		_negativeActionText = negativeActionText;
		_currentMenuAgentVisuals = new List<AgentVisuals>();
		_currentMenuMountEntities = new List<GameEntity>();
		GauntletLayer = new GauntletLayer("CharacterCreationNarrative", 1);
		GauntletLayer.InputRestrictions.SetInputRestrictions();
		GauntletLayer.IsFocusLayer = true;
		GauntletLayer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("GenericPanelGameKeyCategory"));
		ScreenManager.TrySetFocus(GauntletLayer);
		_characterCreationManager.StartNarrativeStage();
		_dataSource = new CharacterCreationNarrativeStageVM(_characterCreationManager, NextStage, _affirmativeActionText, PreviousStage, _negativeActionText, OnMenuChanged)
		{
			OnOptionSelection = OnSelectionChanged
		};
		_dataSource.RefreshMenu();
		CreateHotKeyVisuals();
		_movie = GauntletLayer.LoadMovie("CharacterCreationNarrativeStage", _dataSource);
	}

	public override void SetGenericScene(Scene scene)
	{
		OpenScene(scene);
		RefreshAgentVisuals();
	}

	private void CreateHotKeyVisuals()
	{
		_dataSource?.SetCancelInputKey(HotKeyManager.GetCategory("GenericPanelGameKeyCategory").GetHotKey("Exit"));
		_dataSource?.SetDoneInputKey(HotKeyManager.GetCategory("GenericPanelGameKeyCategory").GetHotKey("Confirm"));
	}

	private void OpenScene(Scene cachedScene)
	{
		_characterScene = cachedScene;
		_characterScene.SetShadow(shadowEnabled: true);
		_characterScene.SetDynamicShadowmapCascadesRadiusMultiplier(0.1f);
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
		_characterScene.FindEntityWithName("cradle")?.SetVisibilityExcludeParents(visible: false);
	}

	private void RefreshAgentVisuals()
	{
		if (_characterScene == null)
		{
			_isAgentVisualsDirty = true;
			return;
		}
		ClearCharacterVisuals();
		ClearMountEntities();
		NarrativeMenu currentMenu = _characterCreationManager.CurrentMenu;
		for (int i = 0; i < currentMenu.Characters.Count; i++)
		{
			NarrativeMenuCharacter narrativeMenuCharacter = currentMenu.Characters[i];
			if (narrativeMenuCharacter.IsHuman)
			{
				SpawnHumanNarrativeMenuCharacter(narrativeMenuCharacter);
			}
			else
			{
				SpawnNonHumanNarrativeMenuCharacter(narrativeMenuCharacter);
			}
		}
		_isAgentVisualsDirty = false;
		_isAgentVisualVisibilitiesDirty = true;
	}

	private void SpawnHumanNarrativeMenuCharacter(NarrativeMenuCharacter character)
	{
		MatrixFrame matrixFrame = _characterScene.FindEntityWithTag(character.SpawnPointEntityId)?.GetGlobalFrame() ?? MatrixFrame.Identity;
		matrixFrame.origin.z = 0f;
		AgentVisuals agentVisuals = AgentVisuals.Create(CreateAgentVisual(character, matrixFrame), "facegenvisual" + character.StringId, isRandomProgress: false, needBatchedVersionForWeaponMeshes: false, forceUseFaceCache: false);
		agentVisuals.SetVisible(value: false);
		_currentMenuAgentVisuals.Add(agentVisuals);
		agentVisuals.GetVisuals().GetSkeleton().TickAnimationsAndForceUpdate(MBRandom.RandomFloat, matrixFrame, tickAnimsForChildren: true);
		Monster baseMonsterFromRace = TaleWorlds.Core.FaceGen.GetBaseMonsterFromRace(character.Race);
		if (character.IsHuman)
		{
			if (!string.IsNullOrEmpty(character.Item1Id) && GameEntity.Instantiate(_characterScene, character.Item1Id, callScriptCallbacks: true) != null)
			{
				agentVisuals.AddPrefabToAgentVisualBoneByRealBoneIndex(character.Item1Id, baseMonsterFromRace.MainHandItemBoneIndex);
			}
			if (!string.IsNullOrEmpty(character.Item2Id) && GameEntity.Instantiate(_characterScene, character.Item2Id, callScriptCallbacks: true) != null)
			{
				agentVisuals.AddPrefabToAgentVisualBoneByRealBoneIndex(character.Item2Id, baseMonsterFromRace.OffHandItemBoneIndex);
			}
		}
		agentVisuals.SetAgentLodZeroOrMax(value: true);
		agentVisuals.GetEntity().SetEnforcedMaximumLodLevel(0);
		agentVisuals.GetEntity().CheckResources(addToQueue: true, checkFaceResources: true);
		CharacterLayer.SetFocusedShadowmap(enable: true, ref matrixFrame.origin, 0.59999996f);
	}

	private void SpawnNonHumanNarrativeMenuCharacter(NarrativeMenuCharacter character)
	{
		ClearMountEntities();
		GameEntity gameEntity = _characterScene.FindEntityWithTag(character.SpawnPointEntityId);
		ItemObject itemObject = Game.Current.ObjectManager.GetObject<ItemObject>(character.Item1Id);
		HorseComponent horseComponent = itemObject.HorseComponent;
		Monster monster = horseComponent.Monster;
		GameEntity gameEntity2 = GameEntity.CreateEmpty(_characterScene);
		AnimationSystemData animationSystemData = monster.FillAnimationSystemData(MBGlobals.GetActionSet(horseComponent.Monster.ActionSetCode), 1f, hasClippingPlane: false);
		gameEntity2.CreateSkeletonWithActionSet(ref animationSystemData);
		ActionIndexCache actionIndex = ActionIndexCache.Create(character.AnimationId);
		gameEntity2.Skeleton.SetAgentActionChannel(0, in actionIndex);
		ItemObject harnessItem = Game.Current.ObjectManager.GetObject<ItemObject>(character.Item2Id);
		MountVisualCreator.AddMountMeshToEntity(gameEntity2, itemObject, harnessItem, character.MountCreationKey.ToString());
		MatrixFrame frame = gameEntity.GetGlobalFrame();
		gameEntity2.SetFrame(ref frame);
		gameEntity2.SetVisibilityExcludeParents(visible: false);
		gameEntity2.SetEnforcedMaximumLodLevel(0);
		gameEntity2.CheckResources(addToQueue: true, checkFaceResources: false);
		_currentMenuMountEntities.Add(gameEntity2);
	}

	private void ClearCharacterVisuals()
	{
		for (int i = 0; i < _currentMenuAgentVisuals.Count; i++)
		{
			_currentMenuAgentVisuals[i].Reset();
		}
		_currentMenuAgentVisuals.Clear();
	}

	private void ClearMountEntities()
	{
		for (int i = 0; i < _currentMenuMountEntities.Count; i++)
		{
			_currentMenuMountEntities[i].Remove(116);
		}
		_currentMenuMountEntities.Clear();
	}

	private AgentVisualsData CreateAgentVisual(NarrativeMenuCharacter character, MatrixFrame characterFrame)
	{
		EquipmentIndex equipmentIndex = character.RightHandEquipmentIndex;
		EquipmentIndex equipmentIndex2 = character.LeftHandEquipmentIndex;
		Equipment equipment = character.Equipment?.DefaultEquipment.Clone();
		if (character.IsHuman)
		{
			if (!string.IsNullOrEmpty(character.Item1Id))
			{
				ItemObject itemObject = Game.Current.ObjectManager.GetObject<ItemObject>(character.Item1Id);
				if (itemObject != null)
				{
					equipmentIndex = EquipmentIndex.WeaponItemBeginSlot;
					equipment.AddEquipmentToSlotWithoutAgent(equipmentIndex, new EquipmentElement(itemObject));
				}
			}
			if (!string.IsNullOrEmpty(character.Item2Id))
			{
				ItemObject itemObject2 = Game.Current.ObjectManager.GetObject<ItemObject>(character.Item2Id);
				if (itemObject2 != null)
				{
					equipmentIndex2 = EquipmentIndex.Weapon1;
					equipment.AddEquipmentToSlotWithoutAgent(equipmentIndex2, new EquipmentElement(itemObject2));
				}
			}
		}
		ActionIndexCache actionCode = ActionIndexCache.Create(character.AnimationId);
		Monster baseMonsterFromRace = TaleWorlds.Core.FaceGen.GetBaseMonsterFromRace(character.Race);
		AgentVisualsData agentVisualsData = new AgentVisualsData().UseMorphAnims(useMorphAnims: true).Equipment(equipment).BodyProperties(character.BodyProperties)
			.Frame(characterFrame)
			.ActionSet(MBGlobals.GetActionSetWithSuffix(baseMonsterFromRace, character.IsFemale, "_facegen"))
			.ActionCode(in actionCode)
			.Scene(_characterScene)
			.Monster(baseMonsterFromRace)
			.UseTranslucency(useTranslucency: true)
			.UseTesselation(useTesselation: true)
			.RightWieldedItemIndex((int)equipmentIndex)
			.LeftWieldedItemIndex((int)equipmentIndex2)
			.Race(CharacterObject.PlayerCharacter.Race)
			.SkeletonType(character.IsFemale ? SkeletonType.Female : SkeletonType.Male);
		CharacterCreationContent characterCreationContent = ((CharacterCreationState)GameStateManager.Current.ActiveState).CharacterCreationManager.CharacterCreationContent;
		if (characterCreationContent.SelectedCulture != null)
		{
			agentVisualsData.ClothColor1(characterCreationContent.SelectedCulture.Color);
			agentVisualsData.ClothColor2(characterCreationContent.SelectedCulture.Color2);
		}
		return agentVisualsData;
	}

	private void OnMenuChanged()
	{
		RefreshAgentVisuals();
	}

	private void OnSelectionChanged()
	{
		RefreshAgentVisuals();
	}

	public override void Tick(float dt)
	{
		base.Tick(dt);
		HandleEscapeMenu(this, CharacterLayer);
		if (_characterScene != null)
		{
			if (_isAgentVisualsDirty)
			{
				RefreshAgentVisuals();
			}
			_characterScene.Tick(dt);
		}
		bool flag = _currentMenuAgentVisuals.Count > 0 || _currentMenuMountEntities.Count > 0;
		for (int i = 0; i < _currentMenuAgentVisuals.Count; i++)
		{
			AgentVisuals agentVisuals = _currentMenuAgentVisuals[i];
			agentVisuals.TickVisuals();
			if (!agentVisuals.GetEntity().CheckResources(addToQueue: false, checkFaceResources: true))
			{
				flag = false;
			}
		}
		for (int j = 0; j < _currentMenuMountEntities.Count; j++)
		{
			GameEntity gameEntity = _currentMenuMountEntities[j];
			if (gameEntity != null && !gameEntity.CheckResources(addToQueue: false, checkFaceResources: true))
			{
				flag = false;
			}
		}
		if (_isAgentVisualVisibilitiesDirty && flag)
		{
			for (int k = 0; k < _currentMenuAgentVisuals.Count; k++)
			{
				_currentMenuAgentVisuals[k].SetVisible(value: true);
			}
			for (int l = 0; l < _currentMenuMountEntities.Count; l++)
			{
				_currentMenuMountEntities[l].SetVisibilityExcludeParents(visible: true);
			}
			_isAgentVisualVisibilitiesDirty = false;
		}
		HandleLayerInput();
	}

	private void HandleLayerInput()
	{
		if (GauntletLayer.Input.IsHotKeyReleased("Exit"))
		{
			UISoundsHelper.PlayUISound("event:/ui/panels/next");
			_dataSource.OnPreviousStage();
		}
		else if (GauntletLayer.Input.IsHotKeyReleased("Confirm") && _dataSource.CanAdvance)
		{
			UISoundsHelper.PlayUISound("event:/ui/panels/next");
			_dataSource.OnNextStage();
		}
	}

	public override void NextStage()
	{
		ClearMountEntities();
		_affirmativeAction();
	}

	public override void PreviousStage()
	{
		ClearMountEntities();
		_negativeAction();
	}

	protected override void OnFinalize()
	{
		base.OnFinalize();
		ClearCharacterVisuals();
		ClearMountEntities();
		CharacterLayer.SceneView.SetEnable(value: false);
		CharacterLayer.SceneView.ClearAll(clearScene: false, removeTerrain: false);
		_currentMenuAgentVisuals = null;
		GauntletLayer = null;
		_dataSource?.OnFinalize();
		_dataSource = null;
		CharacterLayer = null;
		_characterScene = null;
	}

	public override int GetVirtualStageCount()
	{
		return _characterCreationManager.CharacterCreationMenuCount;
	}

	public override IEnumerable<ScreenLayer> GetLayers()
	{
		return new List<ScreenLayer> { CharacterLayer, GauntletLayer };
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
