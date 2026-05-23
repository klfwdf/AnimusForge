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

namespace SandBox.GauntletUI.CharacterCreation
{
	// Token: 0x02000049 RID: 73
	[CharacterCreationStageView(typeof(CharacterCreationClanNamingStage))]
	public class CharacterCreationClanNamingStageView : CharacterCreationStageViewBase
	{
		// Token: 0x17000043 RID: 67
		// (get) Token: 0x06000344 RID: 836 RVA: 0x0001326D File Offset: 0x0001146D
		private ItemRosterElement ShieldRosterElement
		{
			get
			{
				return this._dataSource.ShieldRosterElement;
			}
		}

		// Token: 0x17000044 RID: 68
		// (get) Token: 0x06000345 RID: 837 RVA: 0x0001327A File Offset: 0x0001147A
		private int ShieldSlotIndex
		{
			get
			{
				return this._dataSource.ShieldSlotIndex;
			}
		}

		// Token: 0x17000045 RID: 69
		// (get) Token: 0x06000347 RID: 839 RVA: 0x00013290 File Offset: 0x00011490
		// (set) Token: 0x06000346 RID: 838 RVA: 0x00013287 File Offset: 0x00011487
		public SceneLayer SceneLayer { get; private set; }

		// Token: 0x06000348 RID: 840 RVA: 0x00013298 File Offset: 0x00011498
		public CharacterCreationClanNamingStageView(CharacterCreationManager characterCreationManager, ControlCharacterCreationStage affirmativeAction, TextObject affirmativeActionText, ControlCharacterCreationStage negativeAction, TextObject negativeActionText, ControlCharacterCreationStage refreshAction, ControlCharacterCreationStageReturnInt getCurrentStageIndexAction, ControlCharacterCreationStageReturnInt getTotalStageCountAction, ControlCharacterCreationStageReturnInt getFurthestIndexAction, ControlCharacterCreationStageWithInt goToIndexAction)
			: base(affirmativeAction, negativeAction, refreshAction, getCurrentStageIndexAction, getTotalStageCountAction, getFurthestIndexAction, goToIndexAction)
		{
			this._characterCreationManager = characterCreationManager;
			this._affirmativeActionText = affirmativeActionText;
			this._negativeActionText = negativeActionText;
			this.GauntletLayer = new GauntletLayer("CharacterCreationClanNaming", 1, false)
			{
				IsFocusLayer = true
			};
			this.GauntletLayer.InputRestrictions.SetInputRestrictions(true, InputUsageMask.All);
			this.GauntletLayer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("GenericPanelGameKeyCategory"));
			ScreenManager.TrySetFocus(this.GauntletLayer);
			this._character = CharacterObject.PlayerCharacter;
			this._banner = Clan.PlayerClan.Banner;
			this._dataSource = new CharacterCreationClanNamingStageVM(this._character, this._characterCreationManager, new Action(this.NextStage), this._affirmativeActionText, new Action(this.PreviousStage), this._negativeActionText);
			this._dataSource.SetCancelInputKey(HotKeyManager.GetCategory("GenericPanelGameKeyCategory").GetHotKey("Exit"));
			this._dataSource.SetDoneInputKey(HotKeyManager.GetCategory("GenericPanelGameKeyCategory").GetHotKey("Confirm"));
			this._dataSource.AddCameraControlInputKey(HotKeyManager.GetCategory("FaceGenHotkeyCategory").GetGameKey(56));
			this._dataSource.AddCameraControlInputKey(HotKeyManager.GetCategory("FaceGenHotkeyCategory").GetGameKey(57));
			GameAxisKey gameAxisKey = HotKeyManager.GetCategory("FaceGenHotkeyCategory").RegisteredGameAxisKeys.FirstOrDefault((GameAxisKey x) => x.Id == "CameraAxisX");
			GameAxisKey gameAxisKey2 = HotKeyManager.GetCategory("FaceGenHotkeyCategory").RegisteredGameAxisKeys.FirstOrDefault((GameAxisKey x) => x.Id == "CameraAxisY");
			this._dataSource.AddCameraControlInputKey(gameAxisKey, Module.CurrentModule.GlobalTextManager.FindText("str_key_name", typeof(FaceGenHotkeyCategory).Name + "_" + gameAxisKey.Id));
			this._dataSource.AddCameraControlInputKey(gameAxisKey2, Module.CurrentModule.GlobalTextManager.FindText("str_key_name", typeof(FaceGenHotkeyCategory).Name + "_" + gameAxisKey2.Id));
			this._clanNamingStageMovie = this.GauntletLayer.LoadMovie("CharacterCreationClanNamingStage", this._dataSource);
			this.CreateScene();
			this.RefreshCharacterEntity();
		}

		// Token: 0x06000349 RID: 841 RVA: 0x000134FC File Offset: 0x000116FC
		public override void Tick(float dt)
		{
			this.HandleUserInput(dt);
			this.UpdateCamera(dt);
			if (this.SceneLayer != null && this.SceneLayer.ReadyToRender())
			{
				LoadingWindow.DisableGlobalLoadingWindow();
			}
			if (this._scene != null)
			{
				this._scene.Tick(dt);
			}
			base.HandleEscapeMenu(this, this.GauntletLayer);
			this.HandleLayerInput();
		}

		// Token: 0x0600034A RID: 842 RVA: 0x00013560 File Offset: 0x00011760
		private void CreateScene()
		{
			this._scene = Scene.CreateNewScene(true, false, DecalAtlasGroup.All, "mono_renderscene");
			this._scene.SetName("MBBannerEditorScreen");
			SceneInitializationData sceneInitializationData = new SceneInitializationData(true);
			this._scene.Read("banner_editor_scene", ref sceneInitializationData, "");
			this._scene.SetShadow(true);
			this._scene.DisableStaticShadows(true);
			this._scene.SetDynamicShadowmapCascadesRadiusMultiplier(0.1f);
			this._agentRendererSceneController = MBAgentRendererSceneController.CreateNewAgentRendererSceneController(this._scene);
			float aspectRatio = Screen.AspectRatio;
			GameEntity gameEntity = this._scene.FindEntityWithTag("spawnpoint_player");
			this._characterFrame = gameEntity.GetFrame();
			this._characterFrame.rotation.OrthonormalizeAccordingToForwardAndKeepUpAsZAxis();
			this._cameraTargetDistanceAdder = 3.5f;
			this._cameraCurrentDistanceAdder = this._cameraTargetDistanceAdder;
			this._cameraTargetElevationAdder = 1.15f;
			this._cameraCurrentElevationAdder = this._cameraTargetElevationAdder;
			this._camera = Camera.CreateCamera();
			this._camera.SetFovVertical(0.6981317f, aspectRatio, 0.2f, 200f);
			this.SceneLayer = new SceneLayer(true, true);
			this.SceneLayer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("GenericPanelGameKeyCategory"));
			this.SceneLayer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("FaceGenHotkeyCategory"));
			this.SceneLayer.SetScene(this._scene);
			this.UpdateCamera(0f);
			this.SceneLayer.SetSceneUsesShadows(true);
			this.SceneLayer.SceneView.SetResolutionScaling(true);
			int num = -1;
			num &= -5;
			this.SceneLayer.SetPostfxConfigParams(num);
			this.AddCharacterEntity(ActionIndexCache.act_walk_idle_1h_with_shield_left_stance);
		}

		// Token: 0x0600034B RID: 843 RVA: 0x00013708 File Offset: 0x00011908
		private void AddCharacterEntity(in ActionIndexCache action)
		{
			this._weaponEquipment = new Equipment();
			int i = 0;
			while (i < 12)
			{
				EquipmentElement equipmentFromSlot = this._character.Equipment.GetEquipmentFromSlot((EquipmentIndex)i);
				ItemObject item = equipmentFromSlot.Item;
				if (((item != null) ? item.PrimaryWeapon : null) == null)
				{
					goto IL_5E;
				}
				ItemObject item2 = equipmentFromSlot.Item;
				if (((item2 != null) ? item2.PrimaryWeapon : null) != null && !equipmentFromSlot.Item.PrimaryWeapon.IsShield)
				{
					goto IL_5E;
				}
				IL_6B:
				i++;
				continue;
				IL_5E:
				this._weaponEquipment.AddEquipmentToSlotWithoutAgent((EquipmentIndex)i, equipmentFromSlot);
				goto IL_6B;
			}
			Monster baseMonsterFromRace = TaleWorlds.Core.FaceGen.GetBaseMonsterFromRace(this._character.Race);
			this._agentVisuals = AgentVisuals.Create(new AgentVisualsData().Equipment(this._weaponEquipment).BodyProperties(this._character.GetBodyProperties(this._weaponEquipment, -1)).Frame(this._characterFrame)
				.ActionSet(MBGlobals.GetActionSetWithSuffix(baseMonsterFromRace, this._character.IsFemale, "_facegen"))
				.ActionCode(action)
				.Scene(this._scene)
				.Race(this._character.Race)
				.Monster(baseMonsterFromRace)
				.SkeletonType(this._character.IsFemale ? SkeletonType.Female : SkeletonType.Male)
				.PrepareImmediately(true)
				.UseMorphAnims(true), "BannerEditorChar", false, false, true);
			this._agentVisuals.SetAgentLodZeroOrMaxExternal(true);
			this.UpdateBanners();
		}

		// Token: 0x0600034C RID: 844 RVA: 0x00013858 File Offset: 0x00011A58
		private void UpdateBanners()
		{
			Banner banner = this._banner;
			BannerDebugInfo bannerDebugInfo = BannerDebugInfo.CreateManual(base.GetType().Name);
			banner.GetTableauTextureLarge(bannerDebugInfo, new Action<Texture>(this.OnNewBannerReadyForBanners));
		}

		// Token: 0x0600034D RID: 845 RVA: 0x00013890 File Offset: 0x00011A90
		private void OnNewBannerReadyForBanners(Texture newTexture)
		{
			if (this._scene == null)
			{
				return;
			}
			GameEntity gameEntity = this._scene.FindEntityWithTag("banner");
			if (gameEntity == null)
			{
				return;
			}
			Mesh firstMesh = gameEntity.GetFirstMesh();
			if (firstMesh != null && this._banner != null)
			{
				firstMesh.GetMaterial().SetTexture(Material.MBTextureType.DiffuseMap2, newTexture);
			}
			gameEntity = this._scene.FindEntityWithTag("banner_2");
			if (gameEntity == null)
			{
				return;
			}
			firstMesh = gameEntity.GetFirstMesh();
			if (firstMesh != null && this._banner != null)
			{
				firstMesh.GetMaterial().SetTexture(Material.MBTextureType.DiffuseMap2, newTexture);
			}
		}

		// Token: 0x0600034E RID: 846 RVA: 0x0001392C File Offset: 0x00011B2C
		private void RefreshCharacterEntity()
		{
			this._weaponEquipment.AddEquipmentToSlotWithoutAgent((EquipmentIndex)this.ShieldSlotIndex, this.ShieldRosterElement.EquipmentElement);
			AgentVisualsData copyAgentVisualsData = this._agentVisuals.GetCopyAgentVisualsData();
			copyAgentVisualsData.Equipment(this._weaponEquipment).RightWieldedItemIndex(-1).LeftWieldedItemIndex(this.ShieldSlotIndex)
				.Banner(this._banner)
				.ClothColor1(this._banner.GetPrimaryColor())
				.ClothColor2(this._banner.GetFirstIconColor());
			this._agentVisuals.Refresh(false, copyAgentVisualsData, false);
			MissionWeapon shieldWeapon = new MissionWeapon(this.ShieldRosterElement.EquipmentElement.Item, this.ShieldRosterElement.EquipmentElement.ItemModifier, this._banner);
			Action<Texture> setAction = delegate(Texture tex)
			{
				shieldWeapon.GetWeaponData(false).TableauMaterial.SetTexture(Material.MBTextureType.DiffuseMap2, tex);
			};
			Banner banner = this._banner;
			BannerDebugInfo bannerDebugInfo = BannerDebugInfo.CreateManual(base.GetType().Name);
			banner.GetTableauTextureLarge(bannerDebugInfo, setAction);
		}

		// Token: 0x0600034F RID: 847 RVA: 0x00013A2C File Offset: 0x00011C2C
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

		// Token: 0x06000350 RID: 848 RVA: 0x00013A8C File Offset: 0x00011C8C
		private void HandleUserInput(float dt)
		{
			this._dataSource.CharacterGamepadControlsEnabled = Input.IsGamepadActive && this.SceneLayer.IsHitThisFrame;
			if (this.SceneLayer.IsHitThisFrame && ScreenManager.FocusedLayer == this.GauntletLayer)
			{
				this.GauntletLayer.IsFocusLayer = false;
				ScreenManager.TryLoseFocus(this.GauntletLayer);
				this.SceneLayer.IsFocusLayer = true;
				ScreenManager.TrySetFocus(this.SceneLayer);
			}
			else if (!this.SceneLayer.IsHitThisFrame && ScreenManager.FocusedLayer == this.SceneLayer)
			{
				this.SceneLayer.IsFocusLayer = false;
				ScreenManager.TryLoseFocus(this.SceneLayer);
				this.GauntletLayer.IsFocusLayer = true;
				ScreenManager.TrySetFocus(this.GauntletLayer);
			}
			Vec2 vec = new Vec2(this.SceneLayer.Input.GetNormalizedMouseMoveX() * 1920f, this.SceneLayer.Input.GetNormalizedMouseMoveY() * 1080f);
			bool flag = this.SceneLayer.Input.IsHotKeyDown("Zoom");
			bool flag2 = this.SceneLayer.Input.IsHotKeyDown("Rotate");
			bool flag3 = this.SceneLayer.Input.IsHotKeyDown("Ascend");
			if (flag || flag2 || flag3)
			{
				MBWindowManager.DontChangeCursorPos();
				this.GauntletLayer.InputRestrictions.SetMouseVisibility(false);
			}
			else
			{
				this.GauntletLayer.InputRestrictions.SetMouseVisibility(true);
			}
			float gameKeyState = this.SceneLayer.Input.GetGameKeyState(56);
			float num = this.SceneLayer.Input.GetGameKeyState(57) - gameKeyState;
			float num2;
			if (Input.IsGamepadActive)
			{
				this.NormalizeControllerInputForDeadZone(ref num, 0.1f);
				num2 = num * 5f * dt;
			}
			else
			{
				float num3 = this.SceneLayer.Input.GetDeltaMouseScroll() * -1f;
				float num4 = (flag ? (vec.y * -1f) : 0f);
				num2 = num3 * 0.002f + num4 * 0.004f;
			}
			this._cameraTargetDistanceAdder = MBMath.ClampFloat(this._cameraTargetDistanceAdder + num2, 1.5f, 5f);
			float num6;
			if (Input.IsGamepadActive)
			{
				float num5 = this.SceneLayer.Input.GetGameKeyAxis("CameraAxisX") * -1f;
				this.NormalizeControllerInputForDeadZone(ref num5, 0.1f);
				num6 = num5 * 600f * this.SceneLayer.Input.GetMouseSensitivity() * dt;
			}
			else
			{
				num6 = (flag2 ? (vec.x * -1f) : 0f) * 0.3f * this.SceneLayer.Input.GetMouseSensitivity();
			}
			this._cameraTargetRotation = MBMath.WrapAngle(this._cameraTargetRotation + num6 * 0.017453292f);
			float num7;
			if (Input.IsGamepadActive)
			{
				float gameKeyAxis = this.SceneLayer.Input.GetGameKeyAxis("CameraAxisY");
				this.NormalizeControllerInputForDeadZone(ref gameKeyAxis, 0.1f);
				num7 = gameKeyAxis * 2f * dt;
			}
			else
			{
				num7 = (flag3 ? vec.y : 0f) * 0.002f;
			}
			this._cameraTargetElevationAdder = MBMath.ClampFloat(this._cameraTargetElevationAdder + num7, 0.5f, 1.9f * this._agentVisuals.GetScale());
		}

		// Token: 0x06000351 RID: 849 RVA: 0x00013DAE File Offset: 0x00011FAE
		private void NormalizeControllerInputForDeadZone(ref float inputValue, float controllerDeadZone)
		{
			if (MathF.Abs(inputValue) < controllerDeadZone)
			{
				inputValue = 0f;
				return;
			}
			inputValue = (inputValue - (float)MathF.Sign(inputValue) * controllerDeadZone) / (1f - controllerDeadZone);
		}

		// Token: 0x06000352 RID: 850 RVA: 0x00013DDC File Offset: 0x00011FDC
		private void UpdateCamera(float dt)
		{
			float amount = MathF.Min(1f, 10f * dt);
			this._cameraCurrentRotation = MathF.AngleLerp(this._cameraCurrentRotation, this._cameraTargetRotation, amount, 1E-05f);
			this._cameraCurrentElevationAdder = MathF.Lerp(this._cameraCurrentElevationAdder, this._cameraTargetElevationAdder, amount, 1E-05f);
			this._cameraCurrentDistanceAdder = MathF.Lerp(this._cameraCurrentDistanceAdder, this._cameraTargetDistanceAdder, amount, 1E-05f);
			MatrixFrame characterFrame = this._characterFrame;
			characterFrame.rotation.RotateAboutUp(this._cameraCurrentRotation);
			characterFrame.origin += this._cameraCurrentElevationAdder * characterFrame.rotation.u + this._cameraCurrentDistanceAdder * characterFrame.rotation.f;
			characterFrame.rotation.RotateAboutSide(-1.5707964f);
			characterFrame.rotation.RotateAboutUp(3.1415927f);
			characterFrame.rotation.RotateAboutForward(-0.18849556f);
			this._camera.Frame = characterFrame;
			this.SceneLayer.SetCamera(this._camera);
			SoundManager.SetListenerFrame(characterFrame);
		}

		// Token: 0x06000353 RID: 851 RVA: 0x00013F09 File Offset: 0x00012109
		public override IEnumerable<ScreenLayer> GetLayers()
		{
			return new List<ScreenLayer> { this.SceneLayer, this.GauntletLayer };
		}

		// Token: 0x06000354 RID: 852 RVA: 0x00013F28 File Offset: 0x00012128
		public override int GetVirtualStageCount()
		{
			return 1;
		}

		// Token: 0x06000355 RID: 853 RVA: 0x00013F2C File Offset: 0x0001212C
		public override void NextStage()
		{
			TextObject variable = new TextObject(this._dataSource.ClanName, null);
			TextObject textObject = GameTexts.FindText("str_generic_clan_name", null);
			textObject.SetTextVariable("CLAN_NAME", variable);
			Clan.PlayerClan.ChangeClanName(textObject, textObject);
			ControlCharacterCreationStage affirmativeAction = this._affirmativeAction;
			if (affirmativeAction == null)
			{
				return;
			}
			affirmativeAction();
		}

		// Token: 0x06000356 RID: 854 RVA: 0x00013F80 File Offset: 0x00012180
		public override void PreviousStage()
		{
			ControlCharacterCreationStage negativeAction = this._negativeAction;
			if (negativeAction == null)
			{
				return;
			}
			negativeAction();
		}

		// Token: 0x06000357 RID: 855 RVA: 0x00013F94 File Offset: 0x00012194
		protected override void OnFinalize()
		{
			base.OnFinalize();
			this.SceneLayer.SceneView.SetEnable(false);
			this.SceneLayer.SceneView.ClearAll(true, true);
			this.GauntletLayer = null;
			this.SceneLayer = null;
			CharacterCreationClanNamingStageVM dataSource = this._dataSource;
			if (dataSource != null)
			{
				dataSource.OnFinalize();
			}
			this._dataSource = null;
			this._clanNamingStageMovie = null;
			this._agentVisuals.Reset();
			this._agentVisuals = null;
			MBAgentRendererSceneController.DestructAgentRendererSceneController(this._scene, this._agentRendererSceneController, false);
			this._agentRendererSceneController = null;
			Scene scene = this._scene;
			if (scene != null)
			{
				scene.ManualInvalidate();
			}
			this._scene = null;
		}

		// Token: 0x06000358 RID: 856 RVA: 0x0001403A File Offset: 0x0001223A
		public override void LoadEscapeMenuMovie()
		{
			this._escapeMenuDatasource = new EscapeMenuVM(base.GetEscapeMenuItems(this), null);
			this._escapeMenuMovie = this.GauntletLayer.LoadMovie("EscapeMenu", this._escapeMenuDatasource);
		}

		// Token: 0x06000359 RID: 857 RVA: 0x0001406B File Offset: 0x0001226B
		public override void ReleaseEscapeMenuMovie()
		{
			this.GauntletLayer.ReleaseMovie(this._escapeMenuMovie);
			this._escapeMenuDatasource = null;
			this._escapeMenuMovie = null;
		}

		// Token: 0x0600035A RID: 858 RVA: 0x0001408C File Offset: 0x0001228C
		private bool IsHotKeyReleasedOnAnyLayer(string hotkeyName)
		{
			return this.GauntletLayer.Input.IsHotKeyReleased(hotkeyName) || this.SceneLayer.Input.IsHotKeyReleased(hotkeyName);
		}

		// Token: 0x0400014D RID: 333
		private CharacterCreationManager _characterCreationManager;

		// Token: 0x0400014E RID: 334
		private GauntletLayer GauntletLayer;

		// Token: 0x0400014F RID: 335
		private CharacterCreationClanNamingStageVM _dataSource;

		// Token: 0x04000150 RID: 336
		private GauntletMovieIdentifier _clanNamingStageMovie;

		// Token: 0x04000151 RID: 337
		private TextObject _affirmativeActionText;

		// Token: 0x04000152 RID: 338
		private TextObject _negativeActionText;

		// Token: 0x04000153 RID: 339
		private Banner _banner;

		// Token: 0x04000154 RID: 340
		private float _cameraCurrentRotation;

		// Token: 0x04000155 RID: 341
		private float _cameraTargetRotation;

		// Token: 0x04000156 RID: 342
		private float _cameraCurrentDistanceAdder;

		// Token: 0x04000157 RID: 343
		private float _cameraTargetDistanceAdder;

		// Token: 0x04000158 RID: 344
		private float _cameraCurrentElevationAdder;

		// Token: 0x04000159 RID: 345
		private float _cameraTargetElevationAdder;

		// Token: 0x0400015A RID: 346
		private readonly BasicCharacterObject _character;

		// Token: 0x0400015B RID: 347
		private Scene _scene;

		// Token: 0x0400015C RID: 348
		private MBAgentRendererSceneController _agentRendererSceneController;

		// Token: 0x0400015D RID: 349
		private AgentVisuals _agentVisuals;

		// Token: 0x0400015E RID: 350
		private MatrixFrame _characterFrame;

		// Token: 0x0400015F RID: 351
		private Equipment _weaponEquipment;

		// Token: 0x04000160 RID: 352
		private Camera _camera;

		// Token: 0x04000162 RID: 354
		private EscapeMenuVM _escapeMenuDatasource;

		// Token: 0x04000163 RID: 355
		private GauntletMovieIdentifier _escapeMenuMovie;
	}
}
