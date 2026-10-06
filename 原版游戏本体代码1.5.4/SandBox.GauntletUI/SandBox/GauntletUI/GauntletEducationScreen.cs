using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.ViewModelCollection.Education;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.Engine.Screens;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.Screens;
using TaleWorlds.MountAndBlade.ViewModelCollection.EscapeMenu;
using TaleWorlds.ScreenSystem;

namespace SandBox.GauntletUI;

[GameStateScreen(typeof(EducationState))]
public class GauntletEducationScreen : ScreenBase, IGameStateListener
{
	private readonly EducationState _educationState;

	private readonly Hero _child;

	private readonly PreloadHelper _preloadHelper;

	private EducationVM _dataSource;

	private GauntletLayer _gauntletLayer;

	private bool _startedRendering;

	private Scene _characterScene;

	private MBAgentRendererSceneController _agentRendererSceneController;

	private Camera _camera;

	private List<AgentVisuals> _agentVisuals;

	private GameEntity _cradleEntity;

	private EscapeMenuVM _escapeMenuDatasource;

	private GauntletMovieIdentifier _escapeMenuMovie;

	private bool _isEscapeOpen;

	public SceneLayer CharacterLayer { get; private set; }

	public GauntletEducationScreen(EducationState educationState)
	{
		_educationState = educationState;
		_child = _educationState.Child;
		_agentVisuals = new List<AgentVisuals>();
		_preloadHelper = new PreloadHelper();
	}

	private void OnOptionSelect(EducationCampaignBehavior.EducationCharacterProperties[] characterProperties)
	{
		RefreshSceneCharacters(characterProperties);
	}

	protected override void OnFrameTick(float dt)
	{
		base.OnFrameTick(dt);
		if (CharacterLayer.SceneView.ReadyToRender() && !_startedRendering)
		{
			_preloadHelper.WaitForMeshesToBeLoaded();
			LoadingWindow.DisableGlobalLoadingWindow();
			_startedRendering = true;
		}
		_characterScene?.Tick(dt);
		_agentVisuals?.ForEach(delegate(AgentVisuals v)
		{
			v?.TickVisuals();
		});
		if (_startedRendering)
		{
			if (_gauntletLayer.Input.IsHotKeyReleased("ToggleEscapeMenu"))
			{
				ToggleEscapeMenu();
			}
			else if (_gauntletLayer.Input.IsHotKeyReleased("Exit"))
			{
				UISoundsHelper.PlayUISound("event:/ui/default");
				_dataSource.ExecutePreviousStage();
			}
			else if (_gauntletLayer.Input.IsHotKeyReleased("Confirm") && _dataSource.CanAdvance)
			{
				UISoundsHelper.PlayUISound("event:/ui/default");
				_dataSource.ExecuteNextStage();
			}
		}
	}

	private void ToggleEscapeMenu()
	{
		if (_isEscapeOpen)
		{
			RemoveEscapeMenu();
		}
		else
		{
			OpenEscapeMenu();
		}
	}

	private void CloseEducationScreen(bool isCancel)
	{
		Game.Current.GameStateManager.PopState();
	}

	private void OpenScene()
	{
		_characterScene = Scene.CreateNewScene(initialize_physics: true, enable_decals: false);
		SceneInitializationData initData = new SceneInitializationData
		{
			InitPhysicsWorld = false
		};
		_characterScene.Read("character_menu_new", ref initData);
		_characterScene.SetShadow(shadowEnabled: true);
		_characterScene.SetDynamicShadowmapCascadesRadiusMultiplier(0.1f);
		_agentRendererSceneController = MBAgentRendererSceneController.CreateNewAgentRendererSceneController(_characterScene);
		_camera = Camera.CreateCamera();
		_camera.SetFovVertical(System.MathF.PI / 4f, Screen.AspectRatio, 0.02f, 200f);
		_camera.Frame = Camera.ConstructCameraFromPositionElevationBearing(new Vec3(6.45f, 4.35f, 1.6f), -0.195f, 163.17f);
		CharacterLayer = new SceneLayer();
		CharacterLayer.SetScene(_characterScene);
		CharacterLayer.SetCamera(_camera);
		CharacterLayer.SetSceneUsesShadows(value: true);
		CharacterLayer.SetRenderWithPostfx(value: true);
		CharacterLayer.SetPostfxFromConfig();
		CharacterLayer.SceneView.SetResolutionScaling(value: true);
		if (!CharacterLayer.Input.IsCategoryRegistered(HotKeyManager.GetCategory("FaceGenHotkeyCategory")))
		{
			CharacterLayer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("FaceGenHotkeyCategory"));
		}
		int num = -1;
		num &= -5;
		CharacterLayer.SetPostfxConfigParams(num);
		CharacterLayer.SetPostfxFromConfig();
		_characterScene.FindEntityWithName("_to_carry_bd_basket_a")?.SetVisibilityExcludeParents(visible: false);
		_characterScene.FindEntityWithName("_to_carry_merchandise_hides_b")?.SetVisibilityExcludeParents(visible: false);
		_characterScene.FindEntityWithName("_to_carry_foods_basket_apple")?.SetVisibilityExcludeParents(visible: false);
		_characterScene.FindEntityWithName("_to_carry_bd_fabric_c")?.SetVisibilityExcludeParents(visible: false);
		_characterScene.FindEntityWithName("notebook")?.SetVisibilityExcludeParents(visible: false);
		_characterScene.FindEntityWithName("baby")?.SetVisibilityExcludeParents(visible: false);
		_characterScene.FindEntityWithName("blacksmith_hammer")?.SetVisibilityExcludeParents(visible: false);
		_cradleEntity = _characterScene.FindEntityWithName("cradle");
		_cradleEntity?.SetVisibilityExcludeParents(visible: false);
	}

	private void RefreshSceneCharacters(EducationCampaignBehavior.EducationCharacterProperties[] characterProperties)
	{
		List<float> list = new List<float>();
		_cradleEntity?.SetVisibilityExcludeParents(visible: false);
		if (_agentVisuals != null)
		{
			foreach (AgentVisuals agentVisual in _agentVisuals)
			{
				Skeleton skeleton = agentVisual.GetVisuals().GetSkeleton();
				list.Add(skeleton.GetAnimationParameterAtChannel(0));
				agentVisual.Reset();
			}
			_agentVisuals.Clear();
		}
		if (characterProperties == null || characterProperties.IsEmpty())
		{
			return;
		}
		bool flag = characterProperties.Length == 1;
		string tag = "";
		for (int i = 0; i < characterProperties.Length; i++)
		{
			if (flag)
			{
				tag = "spawnpoint_player_1";
			}
			else
			{
				switch (i)
				{
				case 0:
					tag = "spawnpoint_player_brother_stage";
					break;
				case 1:
					tag = "spawnpoint_brother_brother_stage";
					break;
				}
			}
			MatrixFrame frame = _characterScene.FindEntityWithTag(tag).GetFrame();
			frame.origin.z = 0f;
			string text = "act_inventory_idle_start";
			if (!string.IsNullOrWhiteSpace(characterProperties[i].ActionId))
			{
				text = characterProperties[i].ActionId;
			}
			string prefabId = characterProperties[i].PrefabId;
			bool useOffHand = characterProperties[i].UseOffHand;
			bool flag2 = false;
			Equipment equipment = characterProperties[i].Equipment.Clone();
			if (!string.IsNullOrEmpty(prefabId) && Game.Current.ObjectManager.GetObject<ItemObject>(prefabId) != null)
			{
				ItemObject item = Game.Current.ObjectManager.GetObject<ItemObject>(prefabId);
				equipment.AddEquipmentToSlotWithoutAgent((!useOffHand) ? EquipmentIndex.Weapon1 : EquipmentIndex.WeaponItemBeginSlot, new EquipmentElement(item));
				flag2 = true;
			}
			AgentVisuals agentVisuals = AgentVisuals.Create(CreateAgentVisual(characterProperties[i].Character, frame, equipment, text, _characterScene, _child.Culture), "facegenvisual0", isRandomProgress: false, needBatchedVersionForWeaponMeshes: false, forceUseFaceCache: false);
			agentVisuals.GetVisuals().GetSkeleton().TickAnimationsAndForceUpdate(0.001f, frame, tickAnimsForChildren: true);
			if (!string.IsNullOrWhiteSpace(text))
			{
				ActionIndexCache actionIndex = ActionIndexCache.Create(text);
				agentVisuals.GetVisuals().GetSkeleton().SetAgentActionChannel(0, in actionIndex);
			}
			if (!flag2 && !string.IsNullOrEmpty(prefabId) && GameEntity.Instantiate(_characterScene, prefabId, callScriptCallbacks: true) != null)
			{
				agentVisuals.AddPrefabToAgentVisualBoneByRealBoneIndex(prefabId, characterProperties[i].GetUsedHandBoneIndex());
			}
			CharacterLayer.SetFocusedShadowmap(enable: true, ref frame.origin, 0.59999996f);
			_agentVisuals.Add(agentVisuals);
			if (_child.Age < 5f && _cradleEntity != null)
			{
				MatrixFrame frame2 = _cradleEntity.GetFrame();
				MatrixFrame frame3 = new MatrixFrame(in frame2.rotation, in frame.origin);
				_cradleEntity.SetFrame(ref frame3);
				_cradleEntity.SetVisibilityExcludeParents(visible: true);
			}
		}
	}

	private void PreloadCharactersAndEquipment(List<BasicCharacterObject> characters, List<Equipment> equipments)
	{
		_preloadHelper.PreloadCharacters(characters);
		_preloadHelper.PreloadEquipments(equipments);
	}

	void IGameStateListener.OnActivate()
	{
		base.OnActivate();
		_gauntletLayer = new GauntletLayer("EducationScreen", 1);
		_gauntletLayer.InputRestrictions.SetInputRestrictions();
		_gauntletLayer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("GenericPanelGameKeyCategory"));
		_gauntletLayer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("GenericCampaignPanelsGameKeyCategory"));
		_gauntletLayer.IsFocusLayer = true;
		ScreenManager.TrySetFocus(_gauntletLayer);
		AddLayer(_gauntletLayer);
		OpenScene();
		AddLayer(CharacterLayer);
		_dataSource = new EducationVM(_educationState.Child, CloseEducationScreen, OnOptionSelect, PreloadCharactersAndEquipment);
		_dataSource.SetCancelInputKey(HotKeyManager.GetCategory("GenericPanelGameKeyCategory").GetHotKey("Exit"));
		_dataSource.SetDoneInputKey(HotKeyManager.GetCategory("GenericPanelGameKeyCategory").GetHotKey("Confirm"));
		_gauntletLayer.LoadMovie("EducationScreen", _dataSource);
		Game.Current.EventManager.TriggerEvent(new TutorialContextChangedEvent(TutorialContexts.EducationScreen));
		LoadingWindow.EnableGlobalLoadingWindow();
	}

	void IGameStateListener.OnDeactivate()
	{
		base.OnDeactivate();
		RemoveLayer(_gauntletLayer);
		_gauntletLayer.IsFocusLayer = false;
		ScreenManager.TryLoseFocus(_gauntletLayer);
		Game.Current.EventManager.TriggerEvent(new TutorialContextChangedEvent(TutorialContexts.None));
		LoadingWindow.EnableGlobalLoadingWindow();
	}

	void IGameStateListener.OnFinalize()
	{
		base.OnFinalize();
		CharacterLayer.SceneView.SetEnable(value: false);
		CharacterLayer.SceneView.ClearAll(clearScene: true, removeTerrain: true);
		_dataSource.OnFinalize();
		_dataSource = null;
		_gauntletLayer = null;
		_agentVisuals?.ForEach(delegate(AgentVisuals v)
		{
			v?.Reset();
		});
		_agentVisuals = null;
		CharacterLayer = null;
		MBAgentRendererSceneController.DestructAgentRendererSceneController(_characterScene, _agentRendererSceneController, deleteThisFrame: false);
		_agentRendererSceneController = null;
		_characterScene?.ManualInvalidate();
		_characterScene = null;
	}

	void IGameStateListener.OnInitialize()
	{
	}

	private static AgentVisualsData CreateAgentVisual(CharacterObject character, MatrixFrame characterFrame, Equipment equipment, string actionName, Scene scene, CultureObject childsCulture)
	{
		ActionIndexCache actionCode = ActionIndexCache.Create(actionName);
		BodyProperties bodyProperties2;
		if (character.Age < (float)Campaign.Current.Models.AgeModel.BecomeInfantAge)
		{
			BodyProperties bodyProperties = character.GetBodyProperties(equipment);
			bodyProperties2 = new BodyProperties(new DynamicBodyProperties(3f, bodyProperties.Weight, bodyProperties.Build), bodyProperties.StaticProperties);
		}
		else
		{
			bodyProperties2 = character.GetBodyProperties(equipment);
		}
		Monster baseMonsterFromRace = TaleWorlds.Core.FaceGen.GetBaseMonsterFromRace(character.Race);
		AgentVisualsData agentVisualsData = new AgentVisualsData().UseMorphAnims(useMorphAnims: true).Equipment(equipment).BodyProperties(bodyProperties2)
			.Frame(characterFrame)
			.ActionSet(MBGlobals.GetActionSetWithSuffix(baseMonsterFromRace, character.IsFemale, "_facegen"))
			.ActionCode(in actionCode)
			.Scene(scene)
			.Monster(baseMonsterFromRace)
			.PrepareImmediately(prepareImmediately: true)
			.UseTranslucency(useTranslucency: true)
			.UseTesselation(useTesselation: true)
			.RightWieldedItemIndex(0)
			.LeftWieldedItemIndex(1)
			.SkeletonType(character.IsFemale ? SkeletonType.Female : SkeletonType.Male);
		if (childsCulture != null)
		{
			agentVisualsData.ClothColor1(Clan.PlayerClan.Color);
			agentVisualsData.ClothColor2(Clan.PlayerClan.Color2);
		}
		return agentVisualsData;
	}

	private void OpenEscapeMenu()
	{
		_escapeMenuDatasource = new EscapeMenuVM(GetEscapeMenuItems());
		_escapeMenuMovie = _gauntletLayer.LoadMovie("EscapeMenu", _escapeMenuDatasource);
		_isEscapeOpen = true;
	}

	private void RemoveEscapeMenu()
	{
		_gauntletLayer.ReleaseMovie(_escapeMenuMovie);
		_escapeMenuDatasource = null;
		_escapeMenuMovie = null;
		_isEscapeOpen = false;
	}

	private List<EscapeMenuItemVM> GetEscapeMenuItems()
	{
		TextObject ironmanDisabledReason = GameTexts.FindText("str_pause_menu_disabled_hint", "IronmanMode");
		TextObject educationDisabledReason = GameTexts.FindText("str_pause_menu_disabled_hint", "Education");
		return new List<EscapeMenuItemVM>
		{
			new EscapeMenuItemVM(new TextObject("{=UAD5gWKK}Return to Education"), delegate
			{
				RemoveEscapeMenu();
			}, null, () => new Tuple<bool, TextObject>(item1: false, null), isPositiveBehaviored: true),
			new EscapeMenuItemVM(new TextObject("{=PXT6aA4J}Campaign Options"), delegate
			{
			}, null, () => new Tuple<bool, TextObject>(item1: true, educationDisabledReason)),
			new EscapeMenuItemVM(new TextObject("{=NqarFr4P}Options"), delegate
			{
			}, null, () => new Tuple<bool, TextObject>(item1: true, educationDisabledReason)),
			new EscapeMenuItemVM(new TextObject("{=bV75iwKa}Save"), delegate
			{
			}, null, () => new Tuple<bool, TextObject>(item1: true, educationDisabledReason)),
			new EscapeMenuItemVM(new TextObject("{=e0KdfaNe}Save As"), delegate
			{
			}, null, () => new Tuple<bool, TextObject>(item1: true, educationDisabledReason)),
			new EscapeMenuItemVM(new TextObject("{=9NuttOBC}Load"), delegate
			{
			}, null, () => new Tuple<bool, TextObject>(item1: true, educationDisabledReason)),
			new EscapeMenuItemVM(new TextObject("{=AbEh2y8o}Save And Exit"), delegate
			{
			}, null, () => new Tuple<bool, TextObject>(item1: true, educationDisabledReason)),
			new EscapeMenuItemVM(new TextObject("{=RamV6yLM}Exit to Main Menu"), delegate
			{
				RemoveEscapeMenu();
				MBGameManager.EndGame();
			}, null, () => new Tuple<bool, TextObject>(CampaignOptions.IsIronmanMode, ironmanDisabledReason))
		};
	}
}
