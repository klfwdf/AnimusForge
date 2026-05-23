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

namespace SandBox.GauntletUI.CharacterCreation
{
	// Token: 0x0200004D RID: 77
	[CharacterCreationStageView(typeof(CharacterCreationOptionsStage))]
	public class CharacterCreationOptionsStageView : CharacterCreationStageViewBase
	{
		// Token: 0x17000047 RID: 71
		// (get) Token: 0x06000387 RID: 903 RVA: 0x00015070 File Offset: 0x00013270
		// (set) Token: 0x06000388 RID: 904 RVA: 0x00015078 File Offset: 0x00013278
		public SceneLayer CharacterLayer { get; private set; }

		// Token: 0x06000389 RID: 905 RVA: 0x00015084 File Offset: 0x00013284
		public CharacterCreationOptionsStageView(CharacterCreationManager characterCreationManager, ControlCharacterCreationStage affirmativeAction, TextObject affirmativeActionText, ControlCharacterCreationStage negativeAction, TextObject negativeActionText, ControlCharacterCreationStage refreshAction, ControlCharacterCreationStageReturnInt getCurrentStageIndexAction, ControlCharacterCreationStageReturnInt getTotalStageCountAction, ControlCharacterCreationStageReturnInt getFurthestIndexAction, ControlCharacterCreationStageWithInt goToIndexAction)
			: base(affirmativeAction, negativeAction, refreshAction, getCurrentStageIndexAction, getTotalStageCountAction, getFurthestIndexAction, goToIndexAction)
		{
			this._characterCreationManager = characterCreationManager;
			this._affirmativeActionText = new TextObject("{=lBQXP6Wj}Start Game", null);
			this._negativeActionText = negativeActionText;
			this.GauntletLayer = new GauntletLayer("CharacterCreationOptions", 1, false);
			this.GauntletLayer.InputRestrictions.SetInputRestrictions(true, InputUsageMask.All);
			this.GauntletLayer.IsFocusLayer = true;
			this.GauntletLayer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("GenericPanelGameKeyCategory"));
			ScreenManager.TrySetFocus(this.GauntletLayer);
			this._dataSource = new CharacterCreationOptionsStageVM(this._characterCreationManager, new Action(this.NextStage), this._affirmativeActionText, new Action(this.PreviousStage), this._negativeActionText);
			this._dataSource.SetCancelInputKey(HotKeyManager.GetCategory("GenericPanelGameKeyCategory").GetHotKey("Exit"));
			this._dataSource.SetDoneInputKey(HotKeyManager.GetCategory("GenericPanelGameKeyCategory").GetHotKey("Confirm"));
			GameAxisKey gameAxisKey = HotKeyManager.GetCategory("FaceGenHotkeyCategory").RegisteredGameAxisKeys.FirstOrDefault((GameAxisKey x) => x.Id == "CameraAxisX");
			this._dataSource.AddCameraControlInputKey(gameAxisKey, Module.CurrentModule.GlobalTextManager.FindText("str_key_name", typeof(FaceGenHotkeyCategory).Name + "_" + gameAxisKey.Id));
			this._movie = this.GauntletLayer.LoadMovie("CharacterCreationOptionsStage", this._dataSource);
		}

		// Token: 0x0600038A RID: 906 RVA: 0x0001521D File Offset: 0x0001341D
		public override void SetGenericScene(Scene scene)
		{
			this.OpenScene(scene);
			this.AddCharacterEntity();
			this.RefreshMountEntity();
		}

		// Token: 0x0600038B RID: 907 RVA: 0x00015234 File Offset: 0x00013434
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

		// Token: 0x0600038C RID: 908 RVA: 0x0001536C File Offset: 0x0001356C
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
				.ActionCode(ActionIndexCache.act_inventory_idle_start)
				.Scene(this._characterScene)
				.Race(characterObject.Race)
				.Monster(baseMonsterFromRace)
				.PrepareImmediately(true)
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
			this._agentVisuals = AgentVisuals.Create(agentVisualsData, "facegenvisual", false, false, false);
			this.CharacterLayer.SetFocusedShadowmap(true, ref this._initialCharacterFrame.origin, 0.59999996f);
		}

		// Token: 0x0600038D RID: 909 RVA: 0x000154FC File Offset: 0x000136FC
		private void RefreshCharacterEntityFrame()
		{
			MatrixFrame initialCharacterFrame = this._initialCharacterFrame;
			initialCharacterFrame.rotation.RotateAboutUp(this._charRotationAmount);
			initialCharacterFrame.rotation.ApplyScaleLocal(this._agentVisuals.GetScale());
			this._agentVisuals.GetEntity().SetFrame(ref initialCharacterFrame, true);
		}

		// Token: 0x0600038E RID: 910 RVA: 0x0001554C File Offset: 0x0001374C
		private void RefreshMountEntity()
		{
			this.RemoveMount();
			if (CharacterObject.PlayerCharacter.HasMount())
			{
				ItemObject item = CharacterObject.PlayerCharacter.Equipment[EquipmentIndex.ArmorItemEndSlot].Item;
				GameEntity gameEntity = this._characterScene.FindEntityWithTag("spawnpoint_mount_1");
				MountCreationKey randomMountKey = MountCreationKey.GetRandomMountKey(item, CharacterObject.PlayerCharacter.GetMountKeySeed());
				HorseComponent horseComponent = item.HorseComponent;
				Monster monster = horseComponent.Monster;
				this._mountEntity = GameEntity.CreateEmpty(this._characterScene, true, true, true);
				AnimationSystemData animationSystemData = monster.FillAnimationSystemData(MBGlobals.GetActionSet(horseComponent.Monster.ActionSetCode), 1f, false);
				this._mountEntity.CreateSkeletonWithActionSet(ref animationSystemData);
				this._mountEntity.Skeleton.SetAgentActionChannel(0, ActionIndexCache.act_inventory_idle_start, MBRandom.RandomFloat, -0.2f, true, 0f);
				ItemObject item2 = CharacterObject.PlayerCharacter.Equipment[EquipmentIndex.HorseHarness].Item;
				MountVisualCreator.AddMountMeshToEntity(this._mountEntity, item, item2, randomMountKey.ToString(), null);
				MatrixFrame globalFrame = gameEntity.GetGlobalFrame();
				this._mountEntity.SetFrame(ref globalFrame, true);
				this._agentVisuals.GetVisuals().GetSkeleton().TickAnimationsAndForceUpdate(0.001f, this._initialCharacterFrame, true);
			}
		}

		// Token: 0x0600038F RID: 911 RVA: 0x00015681 File Offset: 0x00013881
		private void RemoveMount()
		{
			if (this._mountEntity != null)
			{
				this._mountEntity.Remove(117);
			}
			this._mountEntity = null;
		}

		// Token: 0x06000390 RID: 912 RVA: 0x000156A8 File Offset: 0x000138A8
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

		// Token: 0x06000391 RID: 913 RVA: 0x000156FC File Offset: 0x000138FC
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

		// Token: 0x06000392 RID: 914 RVA: 0x0001575C File Offset: 0x0001395C
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

		// Token: 0x06000393 RID: 915 RVA: 0x000158FC File Offset: 0x00013AFC
		private void NormalizeControllerInputForDeadZone(ref float inputValue, float controllerDeadZone)
		{
			if (MathF.Abs(inputValue) < controllerDeadZone)
			{
				inputValue = 0f;
				return;
			}
			inputValue = (inputValue - (float)MathF.Sign(inputValue) * controllerDeadZone) / (1f - controllerDeadZone);
		}

		// Token: 0x06000394 RID: 916 RVA: 0x00015927 File Offset: 0x00013B27
		private bool IsHotKeyReleasedOnAnyLayer(string hotkeyName)
		{
			return this.GauntletLayer.Input.IsHotKeyReleased(hotkeyName) || this.CharacterLayer.Input.IsHotKeyReleased(hotkeyName);
		}

		// Token: 0x06000395 RID: 917 RVA: 0x00015950 File Offset: 0x00013B50
		protected override void OnFinalize()
		{
			base.OnFinalize();
			SpriteCategory spriteCategory = UIResourceManager.GetSpriteCategory("ui_bannericons");
			if (spriteCategory.IsLoaded)
			{
				spriteCategory.Unload();
			}
			this.CharacterLayer.SceneView.SetEnable(false);
			this.CharacterLayer.SceneView.ClearAll(false, false);
			this._agentVisuals.Reset();
			this._agentVisuals = null;
			this.GauntletLayer = null;
			CharacterCreationOptionsStageVM dataSource = this._dataSource;
			if (dataSource != null)
			{
				dataSource.OnFinalize();
			}
			this._dataSource = null;
			this.CharacterLayer = null;
			this._characterScene = null;
		}

		// Token: 0x06000396 RID: 918 RVA: 0x000159DE File Offset: 0x00013BDE
		public override IEnumerable<ScreenLayer> GetLayers()
		{
			return new List<ScreenLayer> { this.CharacterLayer, this.GauntletLayer };
		}

		// Token: 0x06000397 RID: 919 RVA: 0x000159FD File Offset: 0x00013BFD
		public override int GetVirtualStageCount()
		{
			return 1;
		}

		// Token: 0x06000398 RID: 920 RVA: 0x00015A00 File Offset: 0x00013C00
		public override void NextStage()
		{
			this.RemoveMount();
			this._affirmativeAction();
		}

		// Token: 0x06000399 RID: 921 RVA: 0x00015A13 File Offset: 0x00013C13
		public override void PreviousStage()
		{
			this.RemoveMount();
			this._negativeAction();
		}

		// Token: 0x0600039A RID: 922 RVA: 0x00015A26 File Offset: 0x00013C26
		public override void LoadEscapeMenuMovie()
		{
			this._escapeMenuDatasource = new EscapeMenuVM(base.GetEscapeMenuItems(this), null);
			this._escapeMenuMovie = this.GauntletLayer.LoadMovie("EscapeMenu", this._escapeMenuDatasource);
		}

		// Token: 0x0600039B RID: 923 RVA: 0x00015A57 File Offset: 0x00013C57
		public override void ReleaseEscapeMenuMovie()
		{
			this.GauntletLayer.ReleaseMovie(this._escapeMenuMovie);
			this._escapeMenuDatasource = null;
			this._escapeMenuMovie = null;
		}

		// Token: 0x04000180 RID: 384
		protected readonly TextObject _affirmativeActionText;

		// Token: 0x04000181 RID: 385
		protected readonly TextObject _negativeActionText;

		// Token: 0x04000182 RID: 386
		private readonly GauntletMovieIdentifier _movie;

		// Token: 0x04000183 RID: 387
		private GauntletLayer GauntletLayer;

		// Token: 0x04000184 RID: 388
		private CharacterCreationOptionsStageVM _dataSource;

		// Token: 0x04000185 RID: 389
		private readonly CharacterCreationManager _characterCreationManager;

		// Token: 0x04000186 RID: 390
		private Scene _characterScene;

		// Token: 0x04000187 RID: 391
		private Camera _camera;

		// Token: 0x04000188 RID: 392
		private MatrixFrame _initialCharacterFrame;

		// Token: 0x04000189 RID: 393
		private AgentVisuals _agentVisuals;

		// Token: 0x0400018A RID: 394
		private GameEntity _mountEntity;

		// Token: 0x0400018C RID: 396
		private float _charRotationAmount;

		// Token: 0x0400018D RID: 397
		private EscapeMenuVM _escapeMenuDatasource;

		// Token: 0x0400018E RID: 398
		private GauntletMovieIdentifier _escapeMenuMovie;
	}
}
