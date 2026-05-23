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
using TaleWorlds.MountAndBlade.GauntletUI.BodyGenerator;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.ViewModelCollection.EscapeMenu;
using TaleWorlds.ScreenSystem;

namespace SandBox.GauntletUI.CharacterCreation
{
	// Token: 0x0200004E RID: 78
	[CharacterCreationStageView(typeof(CharacterCreationReviewStage))]
	public class CharacterCreationReviewStageView : CharacterCreationStageViewBase
	{
		// Token: 0x17000048 RID: 72
		// (get) Token: 0x0600039C RID: 924 RVA: 0x00015A78 File Offset: 0x00013C78
		// (set) Token: 0x0600039D RID: 925 RVA: 0x00015A80 File Offset: 0x00013C80
		public SceneLayer CharacterLayer { get; private set; }

		// Token: 0x0600039E RID: 926 RVA: 0x00015A8C File Offset: 0x00013C8C
		public CharacterCreationReviewStageView(CharacterCreationManager characterCreationManager, ControlCharacterCreationStage affirmativeAction, TextObject affirmativeActionText, ControlCharacterCreationStage negativeAction, TextObject negativeActionText, ControlCharacterCreationStage onRefresh, ControlCharacterCreationStageReturnInt getCurrentStageIndexAction, ControlCharacterCreationStageReturnInt getTotalStageCountAction, ControlCharacterCreationStageReturnInt getFurthestIndexAction, ControlCharacterCreationStageWithInt goToIndexAction)
			: base(affirmativeAction, negativeAction, onRefresh, getCurrentStageIndexAction, getTotalStageCountAction, getFurthestIndexAction, goToIndexAction)
		{
			this._characterCreationManager = characterCreationManager;
			this._affirmativeActionText = new TextObject("{=Rvr1bcu8}Next", null);
			this._negativeActionText = negativeActionText;
			this.GauntletLayer = new GauntletLayer("CharacterCreationReview", 1, false);
			this.GauntletLayer.InputRestrictions.SetInputRestrictions(true, InputUsageMask.All);
			this.GauntletLayer.IsFocusLayer = true;
			this.GauntletLayer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("GenericPanelGameKeyCategory"));
			ScreenManager.TrySetFocus(this.GauntletLayer);
			bool isBannerAndClanNameSet = this._characterCreationManager.GetStage<CharacterCreationBannerEditorStage>() != null && this._characterCreationManager.GetStage<CharacterCreationClanNamingStage>() != null;
			this._dataSource = new CharacterCreationReviewStageVM(this._characterCreationManager, new Action(this.NextStage), this._affirmativeActionText, new Action(this.PreviousStage), this._negativeActionText, isBannerAndClanNameSet);
			this._dataSource.SetCancelInputKey(HotKeyManager.GetCategory("GenericPanelGameKeyCategory").GetHotKey("Exit"));
			this._dataSource.SetDoneInputKey(HotKeyManager.GetCategory("GenericPanelGameKeyCategory").GetHotKey("Confirm"));
			GameAxisKey gameAxisKey = HotKeyManager.GetCategory("FaceGenHotkeyCategory").RegisteredGameAxisKeys.FirstOrDefault((GameAxisKey x) => x.Id == "CameraAxisX");
			this._dataSource.AddCameraControlInputKey(gameAxisKey, Module.CurrentModule.GlobalTextManager.FindText("str_key_name", typeof(FaceGenHotkeyCategory).Name + "_" + gameAxisKey.Id));
			this._movie = this.GauntletLayer.LoadMovie("CharacterCreationReviewStage", this._dataSource);
		}

		// Token: 0x0600039F RID: 927 RVA: 0x00015C45 File Offset: 0x00013E45
		public override void SetGenericScene(Scene scene)
		{
			this.OpenScene(scene);
			this.AddCharacterEntity();
			this.RefreshMountEntity();
		}

		// Token: 0x060003A0 RID: 928 RVA: 0x00015C5C File Offset: 0x00013E5C
		private void OpenScene(Scene cachedScene)
		{
			this._characterScene = cachedScene;
			this._characterScene.SetShadow(true);
			this._characterScene.SetDynamicShadowmapCascadesRadiusMultiplier(0.1f);
			GameEntity gameEntity = this._characterScene.FindEntityWithName("cradle");
			if (gameEntity != null)
			{
				gameEntity.SetVisibilityExcludeParents(false);
			}
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
			this.CharacterLayer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("FaceGenHotkeyCategory"));
			this.CharacterLayer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("GenericPanelGameKeyCategory"));
		}

		// Token: 0x060003A1 RID: 929 RVA: 0x00015D94 File Offset: 0x00013F94
		private void AddCharacterEntity()
		{
			GameEntity gameEntity = this._characterScene.FindEntityWithTag("spawnpoint_player_1");
			this._initialCharacterFrame = gameEntity.GetFrame();
			this._initialCharacterFrame.origin.z = 0f;
			CharacterObject characterObject = Hero.MainHero.CharacterObject;
			Monster baseMonsterFromRace = TaleWorlds.Core.FaceGen.GetBaseMonsterFromRace(characterObject.Race);
			AgentVisualsData agentVisualsData = new AgentVisualsData().UseMorphAnims(true).Equipment(characterObject.Equipment).BodyProperties(characterObject.GetBodyProperties(characterObject.Equipment, -1))
				.SkeletonType(characterObject.IsFemale ? SkeletonType.Female : SkeletonType.Male)
				.Frame(this._initialCharacterFrame)
				.ActionSet(MBGlobals.GetActionSetWithSuffix(baseMonsterFromRace, characterObject.IsFemale, "_facegen"))
				.ActionCode(ActionIndexCache.act_childhood_schooled)
				.Scene(this._characterScene)
				.Race(characterObject.Race)
				.Monster(baseMonsterFromRace)
				.UseTranslucency(true)
				.UseTesselation(true);
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
			this._agentVisuals = AgentVisuals.Create(agentVisualsData, "facegenvisual", false, false, true);
			this.CharacterLayer.SetFocusedShadowmap(true, ref this._initialCharacterFrame.origin, 0.59999996f);
		}

		// Token: 0x060003A2 RID: 930 RVA: 0x00015F1C File Offset: 0x0001411C
		private void RefreshCharacterEntityFrame()
		{
			if (this._agentVisuals != null)
			{
				MatrixFrame initialCharacterFrame = this._initialCharacterFrame;
				initialCharacterFrame.rotation.RotateAboutUp(this._charRotationAmount);
				initialCharacterFrame.rotation.ApplyScaleLocal(this._agentVisuals.GetScale());
				this._agentVisuals.GetEntity().SetFrame(ref initialCharacterFrame, true);
			}
		}

		// Token: 0x060003A3 RID: 931 RVA: 0x00015F74 File Offset: 0x00014174
		private void RefreshMountEntity()
		{
			this.RemoveMount();
			if (CharacterObject.PlayerCharacter.HasMount())
			{
				ItemObject item = CharacterObject.PlayerCharacter.Equipment[EquipmentIndex.ArmorItemEndSlot].Item;
				MountCreationKey randomMountKey = MountCreationKey.GetRandomMountKey(item, CharacterObject.PlayerCharacter.GetMountKeySeed());
				GameEntity gameEntity = this._characterScene.FindEntityWithTag("spawnpoint_mount_1");
				HorseComponent horseComponent = item.HorseComponent;
				Monster monster = horseComponent.Monster;
				this._mountEntity = GameEntity.CreateEmpty(this._characterScene, true, true, true);
				AnimationSystemData animationSystemData = monster.FillAnimationSystemData(MBGlobals.GetActionSet(horseComponent.Monster.ActionSetCode), 1f, false);
				this._mountEntity.CreateSkeletonWithActionSet(ref animationSystemData);
				this._mountEntity.Skeleton.SetAgentActionChannel(0, ActionIndexCache.act_inventory_idle_start, 0f, -0.2f, true, 0f);
				ItemObject item2 = CharacterObject.PlayerCharacter.Equipment[EquipmentIndex.HorseHarness].Item;
				MountVisualCreator.AddMountMeshToEntity(this._mountEntity, item, item2, randomMountKey.ToString(), null);
				MatrixFrame globalFrame = gameEntity.GetGlobalFrame();
				this._mountEntity.SetFrame(ref globalFrame, true);
				this._agentVisuals.GetVisuals().GetSkeleton().TickAnimationsAndForceUpdate(0.001f, this._initialCharacterFrame, true);
			}
		}

		// Token: 0x060003A4 RID: 932 RVA: 0x000160A9 File Offset: 0x000142A9
		private void RemoveMount()
		{
			if (this._mountEntity != null)
			{
				this._mountEntity.Remove(118);
			}
			this._mountEntity = null;
		}

		// Token: 0x060003A5 RID: 933 RVA: 0x000160D0 File Offset: 0x000142D0
		public override void Tick(float dt)
		{
			base.Tick(dt);
			base.HandleEscapeMenu(this, this.CharacterLayer);
			Scene characterScene = this._characterScene;
			if (characterScene != null)
			{
				characterScene.Tick(dt);
			}
			AgentVisuals agentVisuals = this._agentVisuals;
			if (agentVisuals != null)
			{
				agentVisuals.TickVisuals();
			}
			this.TickInput(dt);
			this.HandleLayerInput();
		}

		// Token: 0x060003A6 RID: 934 RVA: 0x00016124 File Offset: 0x00014324
		private void HandleLayerInput()
		{
			if (this.IsHotKeyReleasedOnAnyLayer("Exit"))
			{
				UISoundsHelper.PlayUISound("event:/ui/panels/next");
				this._dataSource.OnPreviousStage();
				return;
			}
			if (this.IsHotKeyReleasedOnAnyLayer("Confirm") && this._dataSource.CanAdvance)
			{
				UISoundsHelper.PlayUISound("event:/ui/panels/next");
				this._dataSource.OnNextStage();
			}
		}

		// Token: 0x060003A7 RID: 935 RVA: 0x00016184 File Offset: 0x00014384
		private void TickInput(float dt)
		{
			this._dataSource.CharacterGamepadControlsEnabled = Input.IsGamepadActive && this.CharacterLayer.IsHitThisFrame;
			if (this.CharacterLayer.IsHitThisFrame && ScreenManager.FocusedLayer == this.GauntletLayer)
			{
				this.GauntletLayer.IsFocusLayer = false;
				ScreenManager.TryLoseFocus(this.GauntletLayer);
				this.CharacterLayer.IsFocusLayer = true;
				ScreenManager.TrySetFocus(this.CharacterLayer);
			}
			else if (!this.CharacterLayer.IsHitThisFrame && ScreenManager.FocusedLayer == this.CharacterLayer)
			{
				this.CharacterLayer.IsFocusLayer = false;
				ScreenManager.TryLoseFocus(this.CharacterLayer);
				this.GauntletLayer.IsFocusLayer = true;
				ScreenManager.TrySetFocus(this.GauntletLayer);
			}
			Vec2 vec = new Vec2(this.CharacterLayer.Input.GetNormalizedMouseMoveX() * 1920f, this.CharacterLayer.Input.GetNormalizedMouseMoveY() * 1080f);
			bool flag = this.CharacterLayer.Input.IsHotKeyDown("Rotate");
			if (flag)
			{
				MBWindowManager.DontChangeCursorPos();
				this.GauntletLayer.InputRestrictions.SetMouseVisibility(false);
			}
			else
			{
				this.GauntletLayer.InputRestrictions.SetMouseVisibility(true);
			}
			float num;
			if (Input.IsGamepadActive)
			{
				float gameKeyAxis = this.CharacterLayer.Input.GetGameKeyAxis("CameraAxisX");
				this.NormalizeControllerInputForDeadZone(ref gameKeyAxis, 0.1f);
				num = gameKeyAxis * 400f * dt;
			}
			else
			{
				num = (flag ? vec.x : 0f) * 0.2f;
			}
			this._charRotationAmount = MBMath.WrapAngle(this._charRotationAmount + num * 0.017453292f);
			this.RefreshCharacterEntityFrame();
		}

		// Token: 0x060003A8 RID: 936 RVA: 0x00016324 File Offset: 0x00014524
		private void NormalizeControllerInputForDeadZone(ref float inputValue, float controllerDeadZone)
		{
			if (MathF.Abs(inputValue) < controllerDeadZone)
			{
				inputValue = 0f;
				return;
			}
			inputValue = (inputValue - (float)MathF.Sign(inputValue) * controllerDeadZone) / (1f - controllerDeadZone);
		}

		// Token: 0x060003A9 RID: 937 RVA: 0x0001634F File Offset: 0x0001454F
		private bool IsHotKeyReleasedOnAnyLayer(string hotkeyName)
		{
			return this.GauntletLayer.Input.IsHotKeyReleased(hotkeyName) || this.CharacterLayer.Input.IsHotKeyReleased(hotkeyName);
		}

		// Token: 0x060003AA RID: 938 RVA: 0x00016378 File Offset: 0x00014578
		public override void NextStage()
		{
			TextObject textObject = GameTexts.FindText("str_generic_character_firstname", null);
			textObject.SetTextVariable("CHARACTER_FIRSTNAME", new TextObject(this._dataSource.Name, null));
			TextObject textObject2 = GameTexts.FindText("str_generic_character_name", null);
			textObject2.SetTextVariable("CHARACTER_NAME", new TextObject(this._dataSource.Name, null));
			textObject2.SetTextVariable("CHARACTER_GENDER", Hero.MainHero.IsFemale ? 1 : 0);
			textObject.SetTextVariable("CHARACTER_GENDER", Hero.MainHero.IsFemale ? 1 : 0);
			Hero.MainHero.SetName(textObject2, textObject);
			this.RemoveMount();
			this._affirmativeAction();
		}

		// Token: 0x060003AB RID: 939 RVA: 0x0001642C File Offset: 0x0001462C
		protected override void OnFinalize()
		{
			base.OnFinalize();
			this.CharacterLayer.SceneView.SetEnable(false);
			this.CharacterLayer.SceneView.ClearAll(false, false);
			this._agentVisuals.Reset();
			this._agentVisuals = null;
			this.GauntletLayer = null;
			CharacterCreationReviewStageVM dataSource = this._dataSource;
			if (dataSource != null)
			{
				dataSource.OnFinalize();
			}
			this._dataSource = null;
			this.CharacterLayer = null;
			this._characterScene = null;
		}

		// Token: 0x060003AC RID: 940 RVA: 0x000164A1 File Offset: 0x000146A1
		public override int GetVirtualStageCount()
		{
			return 1;
		}

		// Token: 0x060003AD RID: 941 RVA: 0x000164A4 File Offset: 0x000146A4
		public override void PreviousStage()
		{
			this.RemoveMount();
			this._negativeAction();
		}

		// Token: 0x060003AE RID: 942 RVA: 0x000164B7 File Offset: 0x000146B7
		public override IEnumerable<ScreenLayer> GetLayers()
		{
			return new List<ScreenLayer> { this.CharacterLayer, this.GauntletLayer };
		}

		// Token: 0x060003AF RID: 943 RVA: 0x000164D6 File Offset: 0x000146D6
		public override void LoadEscapeMenuMovie()
		{
			this._escapeMenuDatasource = new EscapeMenuVM(base.GetEscapeMenuItems(this), null);
			this._escapeMenuMovie = this.GauntletLayer.LoadMovie("EscapeMenu", this._escapeMenuDatasource);
		}

		// Token: 0x060003B0 RID: 944 RVA: 0x00016507 File Offset: 0x00014707
		public override void ReleaseEscapeMenuMovie()
		{
			this.GauntletLayer.ReleaseMovie(this._escapeMenuMovie);
			this._escapeMenuDatasource = null;
			this._escapeMenuMovie = null;
		}

		// Token: 0x0400018F RID: 399
		protected readonly TextObject _affirmativeActionText;

		// Token: 0x04000190 RID: 400
		protected readonly TextObject _negativeActionText;

		// Token: 0x04000191 RID: 401
		private readonly GauntletMovieIdentifier _movie;

		// Token: 0x04000192 RID: 402
		private GauntletLayer GauntletLayer;

		// Token: 0x04000193 RID: 403
		private CharacterCreationReviewStageVM _dataSource;

		// Token: 0x04000194 RID: 404
		private readonly CharacterCreationManager _characterCreationManager;

		// Token: 0x04000195 RID: 405
		private Scene _characterScene;

		// Token: 0x04000196 RID: 406
		private Camera _camera;

		// Token: 0x04000197 RID: 407
		private MatrixFrame _initialCharacterFrame;

		// Token: 0x04000198 RID: 408
		private AgentVisuals _agentVisuals;

		// Token: 0x04000199 RID: 409
		private GameEntity _mountEntity;

		// Token: 0x0400019A RID: 410
		private float _charRotationAmount;

		// Token: 0x0400019C RID: 412
		private EscapeMenuVM _escapeMenuDatasource;

		// Token: 0x0400019D RID: 413
		private GauntletMovieIdentifier _escapeMenuMovie;
	}
}
