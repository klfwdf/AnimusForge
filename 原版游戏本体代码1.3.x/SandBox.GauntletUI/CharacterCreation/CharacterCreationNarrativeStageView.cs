using System;
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

namespace SandBox.GauntletUI.CharacterCreation
{
	// Token: 0x0200004C RID: 76
	[CharacterCreationStageView(typeof(CharacterCreationNarrativeStage))]
	public class CharacterCreationNarrativeStageView : CharacterCreationStageViewBase
	{
		// Token: 0x17000046 RID: 70
		// (get) Token: 0x06000370 RID: 880 RVA: 0x000145C5 File Offset: 0x000127C5
		// (set) Token: 0x06000371 RID: 881 RVA: 0x000145CD File Offset: 0x000127CD
		public SceneLayer CharacterLayer { get; private set; }

		// Token: 0x06000372 RID: 882 RVA: 0x000145D8 File Offset: 0x000127D8
		public CharacterCreationNarrativeStageView(CharacterCreationManager characterCreationManager, ControlCharacterCreationStage affirmativeAction, TextObject affirmativeActionText, ControlCharacterCreationStage negativeAction, TextObject negativeActionText, ControlCharacterCreationStage onRefresh, ControlCharacterCreationStageReturnInt getCurrentStageIndexAction, ControlCharacterCreationStageReturnInt getTotalStageCountAction, ControlCharacterCreationStageReturnInt getFurthestIndexAction, ControlCharacterCreationStageWithInt goToIndexAction)
			: base(affirmativeAction, negativeAction, onRefresh, getCurrentStageIndexAction, getTotalStageCountAction, getFurthestIndexAction, goToIndexAction)
		{
			this._characterCreationManager = characterCreationManager;
			this._affirmativeActionText = affirmativeActionText;
			this._negativeActionText = negativeActionText;
			this._currentMenuAgentVisuals = new List<AgentVisuals>();
			this._currentMenuMountEntities = new List<GameEntity>();
			this.GauntletLayer = new GauntletLayer("CharacterCreationNarrative", 1, false);
			this.GauntletLayer.InputRestrictions.SetInputRestrictions(true, InputUsageMask.All);
			this.GauntletLayer.IsFocusLayer = true;
			this.GauntletLayer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("GenericPanelGameKeyCategory"));
			ScreenManager.TrySetFocus(this.GauntletLayer);
			this._characterCreationManager.StartNarrativeStage();
			CharacterCreationNarrativeStageVM dataSource;
			(dataSource = new CharacterCreationNarrativeStageVM(this._characterCreationManager, new Action(this.NextStage), this._affirmativeActionText, new Action(this.PreviousStage), this._negativeActionText, new Action(this.OnMenuChanged))).OnOptionSelection = new Action(this.OnSelectionChanged);
			this._dataSource = dataSource;
			this._dataSource.RefreshMenu();
			this.CreateHotKeyVisuals();
			this._movie = this.GauntletLayer.LoadMovie("CharacterCreationNarrativeStage", this._dataSource);
		}

		// Token: 0x06000373 RID: 883 RVA: 0x00014706 File Offset: 0x00012906
		public override void SetGenericScene(Scene scene)
		{
			this.OpenScene(scene);
			this.RefreshAgentVisuals();
		}

		// Token: 0x06000374 RID: 884 RVA: 0x00014718 File Offset: 0x00012918
		private void CreateHotKeyVisuals()
		{
			CharacterCreationNarrativeStageVM dataSource = this._dataSource;
			if (dataSource != null)
			{
				dataSource.SetCancelInputKey(HotKeyManager.GetCategory("GenericPanelGameKeyCategory").GetHotKey("Exit"));
			}
			CharacterCreationNarrativeStageVM dataSource2 = this._dataSource;
			if (dataSource2 == null)
			{
				return;
			}
			dataSource2.SetDoneInputKey(HotKeyManager.GetCategory("GenericPanelGameKeyCategory").GetHotKey("Confirm"));
		}

		// Token: 0x06000375 RID: 885 RVA: 0x00014770 File Offset: 0x00012970
		private void OpenScene(Scene cachedScene)
		{
			this._characterScene = cachedScene;
			this._characterScene.SetShadow(true);
			this._characterScene.SetDynamicShadowmapCascadesRadiusMultiplier(0.1f);
			this._characterScene.SetDoNotWaitForLoadingStatesToRender(true);
			this._characterScene.DisableStaticShadows(true);
			this._camera = Camera.CreateCamera();
			BodyGeneratorView.InitCamera(this._camera, this._cameraPosition);
			this.CharacterLayer = new SceneLayer(false, true);
			this.CharacterLayer.SetScene(this._characterScene);
			this.CharacterLayer.SetCamera(this._camera);
			this.CharacterLayer.SetSceneUsesShadows(true);
			this.CharacterLayer.SetRenderWithPostfx(true);
			this.CharacterLayer.SetPostfxFromConfig();
			this.CharacterLayer.SceneView.SetResolutionScaling(true);
			int num = -1;
			num &= -5;
			this.CharacterLayer.SetPostfxConfigParams(num);
			this.CharacterLayer.SetPostfxFromConfig();
			GameEntity gameEntity = this._characterScene.FindEntityWithName("cradle");
			if (gameEntity == null)
			{
				return;
			}
			gameEntity.SetVisibilityExcludeParents(false);
		}

		// Token: 0x06000376 RID: 886 RVA: 0x00014874 File Offset: 0x00012A74
		private void RefreshAgentVisuals()
		{
			if (this._characterScene == null)
			{
				this._isAgentVisualsDirty = true;
				return;
			}
			this.ClearCharacterVisuals();
			this.ClearMountEntities();
			NarrativeMenu currentMenu = this._characterCreationManager.CurrentMenu;
			for (int i = 0; i < currentMenu.Characters.Count; i++)
			{
				NarrativeMenuCharacter narrativeMenuCharacter = currentMenu.Characters[i];
				if (narrativeMenuCharacter.IsHuman)
				{
					this.SpawnHumanNarrativeMenuCharacter(narrativeMenuCharacter);
				}
				else
				{
					this.SpawnNonHumanNarrativeMenuCharacter(narrativeMenuCharacter);
				}
			}
			this._isAgentVisualsDirty = false;
			this._isAgentVisualVisibilitiesDirty = true;
		}

		// Token: 0x06000377 RID: 887 RVA: 0x000148F8 File Offset: 0x00012AF8
		private void SpawnHumanNarrativeMenuCharacter(NarrativeMenuCharacter character)
		{
			GameEntity gameEntity = this._characterScene.FindEntityWithTag(character.SpawnPointEntityId);
			MatrixFrame matrixFrame = ((gameEntity != null) ? gameEntity.GetGlobalFrame() : MatrixFrame.Identity);
			matrixFrame.origin.z = 0f;
			AgentVisuals agentVisuals = AgentVisuals.Create(this.CreateAgentVisual(character, matrixFrame), "facegenvisual" + character.StringId, false, false, false);
			agentVisuals.SetVisible(false);
			this._currentMenuAgentVisuals.Add(agentVisuals);
			agentVisuals.GetVisuals().GetSkeleton().TickAnimationsAndForceUpdate(MBRandom.RandomFloat, matrixFrame, true);
			Monster baseMonsterFromRace = TaleWorlds.Core.FaceGen.GetBaseMonsterFromRace(character.Race);
			if (character.IsHuman)
			{
				if (!string.IsNullOrEmpty(character.Item1Id) && GameEntity.Instantiate(this._characterScene, character.Item1Id, true, true, "") != null)
				{
					agentVisuals.AddPrefabToAgentVisualBoneByRealBoneIndex(character.Item1Id, baseMonsterFromRace.MainHandItemBoneIndex);
				}
				if (!string.IsNullOrEmpty(character.Item2Id) && GameEntity.Instantiate(this._characterScene, character.Item2Id, true, true, "") != null)
				{
					agentVisuals.AddPrefabToAgentVisualBoneByRealBoneIndex(character.Item2Id, baseMonsterFromRace.OffHandItemBoneIndex);
				}
			}
			agentVisuals.SetAgentLodZeroOrMax(true);
			agentVisuals.GetEntity().SetEnforcedMaximumLodLevel(0);
			agentVisuals.GetEntity().CheckResources(true, true);
			this.CharacterLayer.SetFocusedShadowmap(true, ref matrixFrame.origin, 0.59999996f);
		}

		// Token: 0x06000378 RID: 888 RVA: 0x00014A54 File Offset: 0x00012C54
		private void SpawnNonHumanNarrativeMenuCharacter(NarrativeMenuCharacter character)
		{
			this.ClearMountEntities();
			GameEntity gameEntity = this._characterScene.FindEntityWithTag(character.SpawnPointEntityId);
			ItemObject @object = Game.Current.ObjectManager.GetObject<ItemObject>(character.Item1Id);
			HorseComponent horseComponent = @object.HorseComponent;
			Monster monster = horseComponent.Monster;
			GameEntity gameEntity2 = GameEntity.CreateEmpty(this._characterScene, true, true, true);
			AnimationSystemData animationSystemData = monster.FillAnimationSystemData(MBGlobals.GetActionSet(horseComponent.Monster.ActionSetCode), 1f, false);
			gameEntity2.CreateSkeletonWithActionSet(ref animationSystemData);
			ActionIndexCache actionIndexCache = ActionIndexCache.Create(character.AnimationId);
			gameEntity2.Skeleton.SetAgentActionChannel(0, actionIndexCache, 0f, -0.2f, true, 0f);
			ItemObject object2 = Game.Current.ObjectManager.GetObject<ItemObject>(character.Item2Id);
			MountVisualCreator.AddMountMeshToEntity(gameEntity2, @object, object2, character.MountCreationKey.ToString(), null);
			MatrixFrame globalFrame = gameEntity.GetGlobalFrame();
			gameEntity2.SetFrame(ref globalFrame, true);
			gameEntity2.SetVisibilityExcludeParents(false);
			gameEntity2.SetEnforcedMaximumLodLevel(0);
			gameEntity2.CheckResources(true, false);
			this._currentMenuMountEntities.Add(gameEntity2);
		}

		// Token: 0x06000379 RID: 889 RVA: 0x00014B58 File Offset: 0x00012D58
		private void ClearCharacterVisuals()
		{
			for (int i = 0; i < this._currentMenuAgentVisuals.Count; i++)
			{
				this._currentMenuAgentVisuals[i].Reset();
			}
			this._currentMenuAgentVisuals.Clear();
		}

		// Token: 0x0600037A RID: 890 RVA: 0x00014B98 File Offset: 0x00012D98
		private void ClearMountEntities()
		{
			for (int i = 0; i < this._currentMenuMountEntities.Count; i++)
			{
				this._currentMenuMountEntities[i].Remove(116);
			}
			this._currentMenuMountEntities.Clear();
		}

		// Token: 0x0600037B RID: 891 RVA: 0x00014BDC File Offset: 0x00012DDC
		private AgentVisualsData CreateAgentVisual(NarrativeMenuCharacter character, MatrixFrame characterFrame)
		{
			EquipmentIndex equipmentIndex = character.RightHandEquipmentIndex;
			EquipmentIndex equipmentIndex2 = character.LeftHandEquipmentIndex;
			MBEquipmentRoster equipment = character.Equipment;
			Equipment equipment2 = ((equipment != null) ? equipment.DefaultEquipment.Clone(false) : null);
			if (character.IsHuman)
			{
				if (!string.IsNullOrEmpty(character.Item1Id))
				{
					ItemObject @object = Game.Current.ObjectManager.GetObject<ItemObject>(character.Item1Id);
					if (@object != null)
					{
						equipmentIndex = EquipmentIndex.WeaponItemBeginSlot;
						equipment2.AddEquipmentToSlotWithoutAgent(equipmentIndex, new EquipmentElement(@object, null, null, false));
					}
				}
				if (!string.IsNullOrEmpty(character.Item2Id))
				{
					ItemObject object2 = Game.Current.ObjectManager.GetObject<ItemObject>(character.Item2Id);
					if (object2 != null)
					{
						equipmentIndex2 = EquipmentIndex.Weapon1;
						equipment2.AddEquipmentToSlotWithoutAgent(equipmentIndex2, new EquipmentElement(object2, null, null, false));
					}
				}
			}
			ActionIndexCache actionIndexCache = ActionIndexCache.Create(character.AnimationId);
			Monster baseMonsterFromRace = TaleWorlds.Core.FaceGen.GetBaseMonsterFromRace(character.Race);
			AgentVisualsData agentVisualsData = new AgentVisualsData().UseMorphAnims(true).Equipment(equipment2).BodyProperties(character.BodyProperties)
				.Frame(characterFrame)
				.ActionSet(MBGlobals.GetActionSetWithSuffix(baseMonsterFromRace, character.IsFemale, "_facegen"))
				.ActionCode(actionIndexCache)
				.Scene(this._characterScene)
				.Monster(baseMonsterFromRace)
				.UseTranslucency(true)
				.UseTesselation(true)
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

		// Token: 0x0600037C RID: 892 RVA: 0x00014D81 File Offset: 0x00012F81
		private void OnMenuChanged()
		{
			this.RefreshAgentVisuals();
		}

		// Token: 0x0600037D RID: 893 RVA: 0x00014D89 File Offset: 0x00012F89
		private void OnSelectionChanged()
		{
			this.RefreshAgentVisuals();
		}

		// Token: 0x0600037E RID: 894 RVA: 0x00014D94 File Offset: 0x00012F94
		public override void Tick(float dt)
		{
			base.Tick(dt);
			base.HandleEscapeMenu(this, this.CharacterLayer);
			if (this._characterScene != null)
			{
				if (this._isAgentVisualsDirty)
				{
					this.RefreshAgentVisuals();
				}
				this._characterScene.Tick(dt);
			}
			bool flag = this._currentMenuAgentVisuals.Count > 0 || this._currentMenuMountEntities.Count > 0;
			for (int i = 0; i < this._currentMenuAgentVisuals.Count; i++)
			{
				AgentVisuals agentVisuals = this._currentMenuAgentVisuals[i];
				agentVisuals.TickVisuals();
				if (!agentVisuals.GetEntity().CheckResources(false, true))
				{
					flag = false;
				}
			}
			for (int j = 0; j < this._currentMenuMountEntities.Count; j++)
			{
				GameEntity gameEntity = this._currentMenuMountEntities[j];
				if (gameEntity != null && !gameEntity.CheckResources(false, true))
				{
					flag = false;
				}
			}
			if (this._isAgentVisualVisibilitiesDirty && flag)
			{
				for (int k = 0; k < this._currentMenuAgentVisuals.Count; k++)
				{
					this._currentMenuAgentVisuals[k].SetVisible(true);
				}
				for (int l = 0; l < this._currentMenuMountEntities.Count; l++)
				{
					this._currentMenuMountEntities[l].SetVisibilityExcludeParents(true);
				}
				this._isAgentVisualVisibilitiesDirty = false;
			}
			this.HandleLayerInput();
		}

		// Token: 0x0600037F RID: 895 RVA: 0x00014EE0 File Offset: 0x000130E0
		private void HandleLayerInput()
		{
			if (this.GauntletLayer.Input.IsHotKeyReleased("Exit"))
			{
				UISoundsHelper.PlayUISound("event:/ui/panels/next");
				this._dataSource.OnPreviousStage();
				return;
			}
			if (this.GauntletLayer.Input.IsHotKeyReleased("Confirm") && this._dataSource.CanAdvance)
			{
				UISoundsHelper.PlayUISound("event:/ui/panels/next");
				this._dataSource.OnNextStage();
			}
		}

		// Token: 0x06000380 RID: 896 RVA: 0x00014F53 File Offset: 0x00013153
		public override void NextStage()
		{
			this.ClearMountEntities();
			this._affirmativeAction();
		}

		// Token: 0x06000381 RID: 897 RVA: 0x00014F66 File Offset: 0x00013166
		public override void PreviousStage()
		{
			this.ClearMountEntities();
			this._negativeAction();
		}

		// Token: 0x06000382 RID: 898 RVA: 0x00014F7C File Offset: 0x0001317C
		protected override void OnFinalize()
		{
			base.OnFinalize();
			this.ClearCharacterVisuals();
			this.ClearMountEntities();
			this.CharacterLayer.SceneView.SetEnable(false);
			this.CharacterLayer.SceneView.ClearAll(false, false);
			this._currentMenuAgentVisuals = null;
			this.GauntletLayer = null;
			CharacterCreationNarrativeStageVM dataSource = this._dataSource;
			if (dataSource != null)
			{
				dataSource.OnFinalize();
			}
			this._dataSource = null;
			this.CharacterLayer = null;
			this._characterScene = null;
		}

		// Token: 0x06000383 RID: 899 RVA: 0x00014FF2 File Offset: 0x000131F2
		public override int GetVirtualStageCount()
		{
			return this._characterCreationManager.CharacterCreationMenuCount;
		}

		// Token: 0x06000384 RID: 900 RVA: 0x00014FFF File Offset: 0x000131FF
		public override IEnumerable<ScreenLayer> GetLayers()
		{
			return new List<ScreenLayer> { this.CharacterLayer, this.GauntletLayer };
		}

		// Token: 0x06000385 RID: 901 RVA: 0x0001501E File Offset: 0x0001321E
		public override void LoadEscapeMenuMovie()
		{
			this._escapeMenuDatasource = new EscapeMenuVM(base.GetEscapeMenuItems(this), null);
			this._escapeMenuMovie = this.GauntletLayer.LoadMovie("EscapeMenu", this._escapeMenuDatasource);
		}

		// Token: 0x06000386 RID: 902 RVA: 0x0001504F File Offset: 0x0001324F
		public override void ReleaseEscapeMenuMovie()
		{
			this.GauntletLayer.ReleaseMovie(this._escapeMenuMovie);
			this._escapeMenuDatasource = null;
			this._escapeMenuMovie = null;
		}

		// Token: 0x04000171 RID: 369
		protected readonly TextObject _affirmativeActionText;

		// Token: 0x04000172 RID: 370
		protected readonly TextObject _negativeActionText;

		// Token: 0x04000173 RID: 371
		private GauntletMovieIdentifier _movie;

		// Token: 0x04000174 RID: 372
		private GauntletLayer GauntletLayer;

		// Token: 0x04000175 RID: 373
		private CharacterCreationNarrativeStageVM _dataSource;

		// Token: 0x04000176 RID: 374
		private readonly CharacterCreationManager _characterCreationManager;

		// Token: 0x04000177 RID: 375
		private Scene _characterScene;

		// Token: 0x04000178 RID: 376
		private Camera _camera;

		// Token: 0x04000179 RID: 377
		private List<AgentVisuals> _currentMenuAgentVisuals;

		// Token: 0x0400017A RID: 378
		private List<GameEntity> _currentMenuMountEntities;

		// Token: 0x0400017B RID: 379
		private bool _isAgentVisualsDirty;

		// Token: 0x0400017C RID: 380
		private bool _isAgentVisualVisibilitiesDirty;

		// Token: 0x0400017E RID: 382
		private EscapeMenuVM _escapeMenuDatasource;

		// Token: 0x0400017F RID: 383
		private GauntletMovieIdentifier _escapeMenuMovie;
	}
}
