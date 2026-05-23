using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.ViewModelCollection.WeaponCrafting;
using TaleWorlds.CampaignSystem.ViewModelCollection.WeaponCrafting.WeaponDesign;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.Engine.Screens;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.ModuleManager;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.Screens;
using TaleWorlds.ObjectSystem;
using TaleWorlds.ScreenSystem;
using TaleWorlds.TwoDimension;

namespace SandBox.GauntletUI
{
	// Token: 0x02000008 RID: 8
	[GameStateScreen(typeof(CraftingState))]
	public class GauntletCraftingScreen : ScreenBase, ICraftingStateHandler, IGameStateListener
	{
		// Token: 0x17000003 RID: 3
		// (get) Token: 0x06000037 RID: 55 RVA: 0x000030FF File Offset: 0x000012FF
		private SceneView SceneView
		{
			get
			{
				return this._sceneLayer.SceneView;
			}
		}

		// Token: 0x06000038 RID: 56 RVA: 0x0000310C File Offset: 0x0000130C
		public GauntletCraftingScreen(CraftingState craftingState)
		{
			this._craftingState = craftingState;
			this._craftingState.Handler = this;
		}

		// Token: 0x06000039 RID: 57 RVA: 0x00003128 File Offset: 0x00001328
		private void ReloadPieces()
		{
			string key = GauntletCraftingScreen._reloadXmlPath.Key;
			string text = GauntletCraftingScreen._reloadXmlPath.Value;
			if (!text.EndsWith(".xml"))
			{
				text += ".xml";
			}
			GauntletCraftingScreen._reloadXmlPath = new KeyValuePair<string, string>(null, null);
			XmlDocument xmlDocument = Game.Current.ObjectManager.LoadXMLFromFileSkipValidation(ModuleHelper.GetModuleFullPath(key) + "ModuleData/" + text, "");
			if (xmlDocument != null)
			{
				foreach (object obj in xmlDocument.ChildNodes[1].ChildNodes)
				{
					XmlNode xmlNode = (XmlNode)obj;
					XmlAttributeCollection attributes = xmlNode.Attributes;
					if (attributes != null)
					{
						string innerText = attributes["id"].InnerText;
						CraftingPiece @object = Game.Current.ObjectManager.GetObject<CraftingPiece>(innerText);
						if (@object != null)
						{
							@object.Deserialize(Game.Current.ObjectManager, xmlNode);
						}
					}
				}
				this._craftingState.CraftingLogic.ReIndex(true);
				this.RefreshItemEntity(this._dataSource.IsInCraftingMode);
				this._dataSource.WeaponDesign.RefreshItem();
			}
		}

		// Token: 0x0600003A RID: 58 RVA: 0x0000326C File Offset: 0x0000146C
		public void Initialize()
		{
			this._craftingCategory = UIResourceManager.LoadSpriteCategory("ui_crafting");
			this._gauntletLayer = new GauntletLayer("CraftingScreen", 1, false);
			this._gauntletMovie = this._gauntletLayer.LoadMovie("Crafting", this._dataSource);
			this._gauntletLayer.InputRestrictions.SetInputRestrictions(true, InputUsageMask.All);
			this._gauntletLayer.IsFocusLayer = true;
			base.AddLayer(this._gauntletLayer);
			this.OpenScene();
			this.RefreshItemEntity(true);
			this._isInitialized = true;
			Game game = Game.Current;
			if (game != null)
			{
				game.EventManager.TriggerEvent<TutorialContextChangedEvent>(new TutorialContextChangedEvent(TutorialContexts.CraftingScreen));
			}
			UISoundsHelper.PlayUISound("event:/ui/panels/panel_settlement_enter_smithy");
		}

		// Token: 0x0600003B RID: 59 RVA: 0x0000331C File Offset: 0x0000151C
		protected override void OnInitialize()
		{
			base.OnInitialize();
			this.Initialize();
			this._sceneLayer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("Generic"));
			this._gauntletLayer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("Generic"));
			this._sceneLayer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("CraftingHotkeyCategory"));
			this._gauntletLayer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("CraftingHotkeyCategory"));
			InformationManager.HideAllMessages();
		}

		// Token: 0x0600003C RID: 60 RVA: 0x000033A4 File Offset: 0x000015A4
		protected override void OnFinalize()
		{
			base.OnFinalize();
			Game game = Game.Current;
			if (game != null)
			{
				game.EventManager.TriggerEvent<TutorialContextChangedEvent>(new TutorialContextChangedEvent(TutorialContexts.None));
			}
			Scene craftingScene = this._craftingScene;
			if (craftingScene != null)
			{
				craftingScene.ManualInvalidate();
			}
			this._craftingScene = null;
			this.SceneView.ClearAll(true, true);
			this._craftingCategory.Unload();
			CraftingVM dataSource = this._dataSource;
			if (dataSource == null)
			{
				return;
			}
			dataSource.OnFinalize();
		}

		// Token: 0x0600003D RID: 61 RVA: 0x00003414 File Offset: 0x00001614
		protected override void OnFrameTick(float dt)
		{
			LoadingWindow.DisableGlobalLoadingWindow();
			base.OnFrameTick(dt);
			if (!this._gauntletLayer.IsFocusedOnInput() && (this._sceneLayer.Input.IsControlDown() || this._gauntletLayer.Input.IsControlDown()))
			{
				if (this._sceneLayer.Input.IsHotKeyPressed("Copy") || this._gauntletLayer.Input.IsHotKeyPressed("Copy"))
				{
					this.CopyXmlCode();
				}
				else if (this._sceneLayer.Input.IsHotKeyPressed("Paste") || this._gauntletLayer.Input.IsHotKeyPressed("Paste"))
				{
					this.PasteXmlCode();
				}
			}
			if (this._craftingState.CraftingLogic.CurrentCraftingTemplate == null)
			{
				return;
			}
			Scene craftingScene = this._craftingScene;
			if (craftingScene != null)
			{
				craftingScene.Tick(dt);
			}
			bool flag = false;
			if (GauntletCraftingScreen._reloadXmlPath.Key != null && GauntletCraftingScreen._reloadXmlPath.Value != null)
			{
				this.ReloadPieces();
				flag = true;
			}
			if (!flag)
			{
				if (base.DebugInput.IsHotKeyPressed("Reset"))
				{
					this.ResetEntityAndCamera();
				}
				this._dataSource.CanSwitchTabs = !Input.IsGamepadActive || !InformationManager.GetIsAnyTooltipActiveAndExtended();
				this._dataSource.AreGamepadControlHintsEnabled = Input.IsGamepadActive && this._sceneLayer.IsHitThisFrame && this._dataSource.IsInCraftingMode;
				if (this._dataSource.IsInCraftingMode)
				{
					this.TickCameraInput(dt);
				}
				Scene craftingScene2 = this._craftingScene;
				if (craftingScene2 != null)
				{
					craftingScene2.SetDepthOfFieldParameters(this._dofParams.x, this._dofParams.z, false);
				}
				Scene craftingScene3 = this._craftingScene;
				if (craftingScene3 != null)
				{
					craftingScene3.SetDepthOfFieldFocus(this._initialEntityFrame.origin.Distance(this._camera.Frame.origin));
				}
				this.SceneView.SetCamera(this._camera);
				if (Input.IsGamepadActive || (!this._gauntletLayer.IsFocusedOnInput() && !this._sceneLayer.IsFocusedOnInput()))
				{
					if (this.IsHotKeyReleasedInAnyLayer("Exit"))
					{
						UISoundsHelper.PlayUISound("event:/ui/default");
						this._dataSource.ExecuteCancel();
						return;
					}
					if (this.IsHotKeyReleasedInAnyLayer("Confirm"))
					{
						bool isInCraftingMode = this._dataSource.IsInCraftingMode;
						bool isInRefinementMode = this._dataSource.IsInRefinementMode;
						bool isInSmeltingMode = this._dataSource.IsInSmeltingMode;
						ValueTuple<bool, bool> valueTuple = this._dataSource.ExecuteConfirm();
						bool item = valueTuple.Item1;
						bool item2 = valueTuple.Item2;
						if (item)
						{
							if (!item2)
							{
								UISoundsHelper.PlayUISound("event:/ui/default");
								return;
							}
							if (isInCraftingMode)
							{
								UISoundsHelper.PlayUISound("event:/ui/crafting/craft_success");
								return;
							}
							if (isInRefinementMode)
							{
								UISoundsHelper.PlayUISound("event:/ui/crafting/refine_success");
								return;
							}
							if (isInSmeltingMode)
							{
								UISoundsHelper.PlayUISound("event:/ui/crafting/smelt_success");
								return;
							}
						}
					}
					else if (this._dataSource.CanSwitchTabs)
					{
						if (this.IsHotKeyReleasedInAnyLayer("SwitchToPreviousTab"))
						{
							if (this._dataSource.IsInSmeltingMode)
							{
								UISoundsHelper.PlayUISound("event:/ui/crafting/refine_tab");
								this._dataSource.ExecuteSwitchToRefinement();
								return;
							}
							if (this._dataSource.IsInCraftingMode)
							{
								UISoundsHelper.PlayUISound("event:/ui/crafting/smelt_tab");
								this._dataSource.ExecuteSwitchToSmelting();
								return;
							}
							if (this._dataSource.IsInRefinementMode)
							{
								UISoundsHelper.PlayUISound("event:/ui/crafting/craft_tab");
								this._dataSource.ExecuteSwitchToCrafting();
								return;
							}
						}
						else if (this.IsHotKeyReleasedInAnyLayer("SwitchToNextTab"))
						{
							if (this._dataSource.IsInSmeltingMode)
							{
								UISoundsHelper.PlayUISound("event:/ui/crafting/craft_tab");
								this._dataSource.ExecuteSwitchToCrafting();
								return;
							}
							if (this._dataSource.IsInCraftingMode)
							{
								UISoundsHelper.PlayUISound("event:/ui/crafting/refine_tab");
								this._dataSource.ExecuteSwitchToRefinement();
								return;
							}
							if (this._dataSource.IsInRefinementMode)
							{
								UISoundsHelper.PlayUISound("event:/ui/crafting/smelt_tab");
								this._dataSource.ExecuteSwitchToSmelting();
							}
						}
					}
				}
			}
		}

		// Token: 0x0600003E RID: 62 RVA: 0x000037D1 File Offset: 0x000019D1
		private void OnClose()
		{
			ICampaignMission campaignMission = CampaignMission.Current;
			if (campaignMission != null)
			{
				campaignMission.EndMission();
			}
			Game.Current.GameStateManager.PopState(0);
		}

		// Token: 0x0600003F RID: 63 RVA: 0x000037F3 File Offset: 0x000019F3
		private void OnResetCamera()
		{
			this.ResetEntityAndCamera();
		}

		// Token: 0x06000040 RID: 64 RVA: 0x000037FB File Offset: 0x000019FB
		private void OnWeaponCrafted()
		{
			WeaponDesignResultPopupVM craftingResultPopup = this._dataSource.WeaponDesign.CraftingResultPopup;
			if (craftingResultPopup == null)
			{
				return;
			}
			craftingResultPopup.SetDoneInputKey(HotKeyManager.GetCategory("CraftingHotkeyCategory").GetHotKey("Confirm"));
		}

		// Token: 0x06000041 RID: 65 RVA: 0x0000382C File Offset: 0x00001A2C
		public void OnCraftingLogicInitialized()
		{
			CraftingVM dataSource = this._dataSource;
			if (dataSource != null)
			{
				dataSource.OnFinalize();
			}
			this._dataSource = new CraftingVM(this._craftingState.CraftingLogic, new Action(this.OnClose), new Action(this.OnResetCamera), new Action(this.OnWeaponCrafted), new Func<WeaponComponentData, ItemObject.ItemUsageSetFlags>(this.GetItemUsageSetFlag))
			{
				OnItemRefreshed = new CraftingVM.OnItemRefreshedDelegate(this.RefreshItemEntity)
			};
			this._dataSource.WeaponDesign.CraftingHistory.SetDoneKey(HotKeyManager.GetCategory("CraftingHotkeyCategory").GetHotKey("Confirm"));
			this._dataSource.WeaponDesign.CraftingHistory.SetCancelKey(HotKeyManager.GetCategory("CraftingHotkeyCategory").GetHotKey("Exit"));
			this._dataSource.CraftingHeroPopup.SetExitInputKey(HotKeyManager.GetCategory("CraftingHotkeyCategory").GetHotKey("Exit"));
			this._dataSource.SetConfirmInputKey(HotKeyManager.GetCategory("CraftingHotkeyCategory").GetHotKey("Confirm"));
			this._dataSource.SetExitInputKey(HotKeyManager.GetCategory("CraftingHotkeyCategory").GetHotKey("Exit"));
			this._dataSource.SetPreviousTabInputKey(HotKeyManager.GetCategory("CraftingHotkeyCategory").GetHotKey("SwitchToPreviousTab"));
			this._dataSource.SetNextTabInputKey(HotKeyManager.GetCategory("CraftingHotkeyCategory").GetHotKey("SwitchToNextTab"));
			this._dataSource.AddCameraControlInputKey(HotKeyManager.GetCategory("CraftingHotkeyCategory").GetGameKey(56));
			this._dataSource.AddCameraControlInputKey(HotKeyManager.GetCategory("CraftingHotkeyCategory").GetGameKey(57));
			this._dataSource.AddCameraControlInputKey(HotKeyManager.GetCategory("CraftingHotkeyCategory").RegisteredGameAxisKeys.FirstOrDefault((GameAxisKey x) => x.Id == "CameraAxisX"));
		}

		// Token: 0x06000042 RID: 66 RVA: 0x00003A0A File Offset: 0x00001C0A
		public void OnCraftingLogicRefreshed()
		{
			this._dataSource.OnCraftingLogicRefreshed(this._craftingState.CraftingLogic);
			if (this._isInitialized)
			{
				this.RefreshItemEntity(true);
			}
		}

		// Token: 0x06000043 RID: 67 RVA: 0x00003A34 File Offset: 0x00001C34
		private void OpenScene()
		{
			this._craftingScene = Scene.CreateNewScene(true, false, DecalAtlasGroup.All, "mono_renderscene");
			this._craftingScene.SetName("GauntletCraftingScreen");
			SceneInitializationData sceneInitializationData = default(SceneInitializationData);
			sceneInitializationData.InitPhysicsWorld = false;
			this._craftingScene.Read("crafting_menu_outdoor", ref sceneInitializationData, "");
			this._craftingScene.DisableStaticShadows(true);
			this._craftingScene.SetShadow(true);
			this._craftingScene.SetClothSimulationState(true);
			this.InitializeEntityAndCamera();
			this._sceneLayer = new SceneLayer(true, true);
			this._sceneLayer.IsFocusLayer = true;
			base.AddLayer(this._sceneLayer);
			this.SceneView.SetScene(this._craftingScene);
			this.SceneView.SetCamera(this._camera);
			this.SceneView.SetSceneUsesShadows(true);
			this.SceneView.SetAcceptGlobalDebugRenderObjects(true);
			this.SceneView.SetRenderWithPostfx(true);
			this.SceneView.SetResolutionScaling(true);
		}

		// Token: 0x06000044 RID: 68 RVA: 0x00003B2C File Offset: 0x00001D2C
		private void InitializeEntityAndCamera()
		{
			GameEntity gameEntity = this._craftingScene.FindEntityWithTag("weapon_point");
			MatrixFrame globalFrame = gameEntity.GetGlobalFrame();
			this._craftingScene.RemoveEntity(gameEntity, 114);
			globalFrame.Elevate(1.6f);
			this._initialEntityFrame = globalFrame;
			this._craftingEntity = GameEntity.CreateEmpty(this._craftingScene, true, true, true);
			this._craftingEntity.SetFrame(ref globalFrame, true);
			this._camera = Camera.CreateCamera();
			this._dofParams = default(Vec3);
			GameEntity gameEntity2 = this._craftingScene.FindEntityWithTag("camera_point");
			gameEntity2.GetCameraParamsFromCameraScript(this._camera, ref this._dofParams);
			float fovVertical = this._camera.GetFovVertical();
			float aspectRatio = Screen.AspectRatio;
			float near = this._camera.Near;
			float far = this._camera.Far;
			this._camera.SetFovVertical(fovVertical, aspectRatio, near, far);
			this._craftingScene.SetDepthOfFieldParameters(this._dofParams.x, this._dofParams.z, false);
			this._craftingScene.SetDepthOfFieldFocus(this._dofParams.y);
			this._initialCameraFrame = gameEntity2.GetFrame();
			this._cameraZoomDirection = this._initialEntityFrame.origin - this._initialCameraFrame.origin;
		}

		// Token: 0x06000045 RID: 69 RVA: 0x00003C74 File Offset: 0x00001E74
		private void RefreshItemEntity(bool isItemVisible)
		{
			this._dataSource.WeaponDesign.CurrentWeaponHasScabbard = false;
			MatrixFrame matrixFrame = this._initialEntityFrame;
			if (this._craftingEntity != null)
			{
				matrixFrame = this._craftingEntity.GetFrame();
				this._craftingEntity.Remove(115);
				this._craftingEntity = null;
			}
			if (isItemVisible)
			{
				this._craftingEntity = GameEntity.CreateEmpty(this._craftingScene, true, true, true);
				this._craftingEntity.SetFrame(ref matrixFrame, true);
				this._craftedData = this._craftingState.CraftingLogic.CurrentWeaponDesign;
				if (this._craftedData != null)
				{
					matrixFrame = this._craftingEntity.GetFrame();
					float num = this._craftedData.CraftedWeaponLength / 2f;
					BladeData bladeData = this._craftedData.UsedPieces[0].CraftingPiece.BladeData;
					this._dataSource.WeaponDesign.CurrentWeaponHasScabbard = !string.IsNullOrEmpty(bladeData.HolsterMeshName);
					MetaMesh metaMesh;
					if (!this._dataSource.WeaponDesign.IsScabbardVisible)
					{
						metaMesh = CraftedDataView.BuildWeaponMesh(this._craftedData, -num, false, false);
					}
					else
					{
						metaMesh = CraftedDataView.BuildHolsterMeshWithWeapon(this._craftedData, -num, false);
						if (metaMesh == null)
						{
							metaMesh = CraftedDataView.BuildWeaponMesh(this._craftedData, -num, false, false);
						}
					}
					this._craftingEntity = this._craftingScene.AddItemEntity(ref matrixFrame, metaMesh);
				}
			}
		}

		// Token: 0x06000046 RID: 70 RVA: 0x00003DCC File Offset: 0x00001FCC
		private void TickCameraInput(float dt)
		{
			if (this._sceneLayer.IsHitThisFrame && ScreenManager.FocusedLayer == this._gauntletLayer)
			{
				this._gauntletLayer.IsFocusLayer = false;
				ScreenManager.TryLoseFocus(this._gauntletLayer);
				this._sceneLayer.IsFocusLayer = true;
				ScreenManager.TrySetFocus(this._sceneLayer);
			}
			else if (!this._sceneLayer.IsHitThisFrame && ScreenManager.FocusedLayer == this._sceneLayer)
			{
				this._sceneLayer.IsFocusLayer = false;
				ScreenManager.TryLoseFocus(this._sceneLayer);
				this._gauntletLayer.IsFocusLayer = true;
				ScreenManager.TrySetFocus(this._gauntletLayer);
			}
			Vec2 vec = new Vec2(this._sceneLayer.Input.GetNormalizedMouseMoveX() * 1920f, this._sceneLayer.Input.GetNormalizedMouseMoveY() * 1080f);
			bool flag = this._sceneLayer.Input.IsHotKeyDown("Rotate");
			bool flag2 = this._sceneLayer.Input.IsHotKeyDown("Zoom");
			bool flag3 = false;
			if (flag || flag2 || flag3)
			{
				MBWindowManager.DontChangeCursorPos();
				this._gauntletLayer.InputRestrictions.SetMouseVisibility(false);
			}
			else
			{
				this._gauntletLayer.InputRestrictions.SetMouseVisibility(true);
			}
			if (!base.DebugInput.IsControlDown() && !base.DebugInput.IsAltDown())
			{
				if (this._sceneLayer.Input.IsHotKeyDown("Rotate") && this._sceneLayer.Input.IsHotKeyDown("Zoom"))
				{
					this.ResetEntityAndCamera();
					return;
				}
				float num2;
				if (Input.IsGamepadActive)
				{
					float gameKeyState = this._sceneLayer.Input.GetGameKeyState(56);
					float gameKeyState2 = this._sceneLayer.Input.GetGameKeyState(57);
					float num = gameKeyState - gameKeyState2;
					this.NormalizeControllerInputForDeadZone(ref num, 0.1f);
					num2 = num * 4f * dt;
				}
				else
				{
					float deltaMouseScroll = this._sceneLayer.Input.GetDeltaMouseScroll();
					float num3 = (flag2 ? vec.y : 0f);
					num2 = deltaMouseScroll * 0.001f + num3 * 0.002f;
				}
				this._targetCameraValues.Zoom = MBMath.ClampFloat(this._targetCameraValues.Zoom + num2, -0.5f, 0.5f);
				float num4;
				if (Input.IsGamepadActive)
				{
					float gameKeyAxis = this._sceneLayer.Input.GetGameKeyAxis("CameraAxisX");
					this.NormalizeControllerInputForDeadZone(ref gameKeyAxis, 0.1f);
					num4 = gameKeyAxis * 400f * dt;
				}
				else
				{
					num4 = (flag ? vec.x : 0f) * 0.2f;
				}
				this._targetCameraValues.HorizontalRotation = MBMath.WrapAngle(this._targetCameraValues.HorizontalRotation + num4 * 0.017453292f);
				float num5;
				if (Input.IsGamepadActive)
				{
					float gameKeyAxis2 = this._sceneLayer.Input.GetGameKeyAxis("CameraAxisY");
					this.NormalizeControllerInputForDeadZone(ref gameKeyAxis2, 0.1f);
					num5 = gameKeyAxis2 * 400f * dt;
				}
				else
				{
					num5 = (flag ? (vec.y * -1f) : 0f) * 0.2f;
				}
				this._targetCameraValues.VerticalRotation = MBMath.WrapAngle(this._targetCameraValues.VerticalRotation + num5 * 0.017453292f);
			}
			this.UpdateCamera(dt);
		}

		// Token: 0x06000047 RID: 71 RVA: 0x000040F0 File Offset: 0x000022F0
		private void NormalizeControllerInputForDeadZone(ref float inputValue, float controllerDeadZone)
		{
			if (MathF.Abs(inputValue) < controllerDeadZone)
			{
				inputValue = 0f;
				return;
			}
			inputValue = (inputValue - (float)MathF.Sign(inputValue) * controllerDeadZone) / (1f - controllerDeadZone);
		}

		// Token: 0x06000048 RID: 72 RVA: 0x0000411C File Offset: 0x0000231C
		private void UpdateCamera(float dt)
		{
			float amount = MathF.Min(1f, 10f * dt);
			GauntletCraftingScreen.CameraParameters cameraParameters = new GauntletCraftingScreen.CameraParameters(MathF.AngleLerp(this._currentCameraValues.HorizontalRotation, this._targetCameraValues.HorizontalRotation, amount, 1E-05f), MathF.AngleLerp(this._currentCameraValues.VerticalRotation, this._targetCameraValues.VerticalRotation, amount, 1E-05f), MathF.Lerp(this._currentCameraValues.Zoom, this._targetCameraValues.Zoom, amount, 1E-05f));
			GauntletCraftingScreen.CameraParameters cameraParameters2 = new GauntletCraftingScreen.CameraParameters(cameraParameters.HorizontalRotation - this._currentCameraValues.HorizontalRotation, cameraParameters.VerticalRotation - this._currentCameraValues.VerticalRotation, cameraParameters.Zoom - this._currentCameraValues.Zoom);
			this._currentCameraValues = cameraParameters;
			MatrixFrame frame = this._craftingEntity.GetFrame();
			frame.rotation.RotateAboutUp(cameraParameters2.HorizontalRotation);
			frame.rotation.RotateAboutSide(cameraParameters2.VerticalRotation);
			this._craftingEntity.SetFrame(ref frame, true);
			MatrixFrame frame2 = this._camera.Frame;
			frame2.origin += this._cameraZoomDirection * cameraParameters2.Zoom;
			this._camera.Frame = frame2;
		}

		// Token: 0x06000049 RID: 73 RVA: 0x0000426C File Offset: 0x0000246C
		private void ResetEntityAndCamera()
		{
			this._currentCameraValues = new GauntletCraftingScreen.CameraParameters(0f, 0f, 0f);
			this._targetCameraValues = new GauntletCraftingScreen.CameraParameters(0f, 0f, 0f);
			this._craftingEntity.SetFrame(ref this._initialEntityFrame, true);
			this._camera.Frame = this._initialCameraFrame;
		}

		// Token: 0x0600004A RID: 74 RVA: 0x000042D0 File Offset: 0x000024D0
		private void CopyXmlCode()
		{
			Input.SetClipboardText(this._craftingState.CraftingLogic.GetXmlCodeForCurrentItem(this._craftingState.CraftingLogic.GetCurrentCraftedItemObject(false, null)));
		}

		// Token: 0x0600004B RID: 75 RVA: 0x000042FC File Offset: 0x000024FC
		private void PasteXmlCode()
		{
			string clipboardText = Input.GetClipboardText();
			if (!string.IsNullOrEmpty(clipboardText))
			{
				ItemObject @object = MBObjectManager.Instance.GetObject<ItemObject>(clipboardText);
				if (@object != null)
				{
					this.SwithToCraftedItem(@object);
					return;
				}
				CraftingTemplate craftingTemplate;
				ValueTuple<CraftingPiece, int>[] pieces;
				if (this._craftingState.CraftingLogic.TryGetWeaponPropertiesFromXmlCode(clipboardText, out craftingTemplate, out pieces))
				{
					this._dataSource.SetCurrentDesignManually(craftingTemplate, pieces);
				}
			}
		}

		// Token: 0x0600004C RID: 76 RVA: 0x00004354 File Offset: 0x00002554
		private void SwithToCraftedItem(ItemObject itemObject)
		{
			if (itemObject != null && itemObject.IsCraftedWeapon)
			{
				if (!this._dataSource.IsInCraftingMode)
				{
					this._dataSource.ExecuteSwitchToCrafting();
				}
				WeaponDesign weaponDesign = itemObject.WeaponDesign;
				if (this._craftingState.CraftingLogic.CurrentCraftingTemplate != weaponDesign.Template)
				{
					this._dataSource.WeaponDesign.SelectPrimaryWeaponClass(weaponDesign.Template);
				}
				foreach (WeaponDesignElement weaponDesignElement in weaponDesign.UsedPieces)
				{
					if (weaponDesignElement.IsValid)
					{
						this._dataSource.WeaponDesign.SwitchToPiece(weaponDesignElement);
					}
				}
			}
		}

		// Token: 0x0600004D RID: 77 RVA: 0x000043EE File Offset: 0x000025EE
		private ItemObject.ItemUsageSetFlags GetItemUsageSetFlag(WeaponComponentData item)
		{
			if (!string.IsNullOrEmpty(item.ItemUsage))
			{
				return MBItem.GetItemUsageSetFlags(item.ItemUsage);
			}
			return (ItemObject.ItemUsageSetFlags)0;
		}

		// Token: 0x0600004E RID: 78 RVA: 0x0000440A File Offset: 0x0000260A
		private bool IsHotKeyReleasedInAnyLayer(string hotKeyId)
		{
			return this._sceneLayer.Input.IsHotKeyReleased(hotKeyId) || this._gauntletLayer.Input.IsHotKeyReleased(hotKeyId);
		}

		// Token: 0x0600004F RID: 79 RVA: 0x00004432 File Offset: 0x00002632
		void IGameStateListener.OnInitialize()
		{
		}

		// Token: 0x06000050 RID: 80 RVA: 0x00004434 File Offset: 0x00002634
		void IGameStateListener.OnFinalize()
		{
		}

		// Token: 0x06000051 RID: 81 RVA: 0x00004436 File Offset: 0x00002636
		void IGameStateListener.OnActivate()
		{
		}

		// Token: 0x06000052 RID: 82 RVA: 0x00004438 File Offset: 0x00002638
		void IGameStateListener.OnDeactivate()
		{
		}

		// Token: 0x04000013 RID: 19
		private const float _controllerRotationSensitivity = 2f;

		// Token: 0x04000014 RID: 20
		private Scene _craftingScene;

		// Token: 0x04000015 RID: 21
		private SceneLayer _sceneLayer;

		// Token: 0x04000016 RID: 22
		private readonly CraftingState _craftingState;

		// Token: 0x04000017 RID: 23
		private CraftingVM _dataSource;

		// Token: 0x04000018 RID: 24
		private GauntletLayer _gauntletLayer;

		// Token: 0x04000019 RID: 25
		private GauntletMovieIdentifier _gauntletMovie;

		// Token: 0x0400001A RID: 26
		private SpriteCategory _craftingCategory;

		// Token: 0x0400001B RID: 27
		private Camera _camera;

		// Token: 0x0400001C RID: 28
		private MatrixFrame _initialCameraFrame;

		// Token: 0x0400001D RID: 29
		private Vec3 _dofParams;

		// Token: 0x0400001E RID: 30
		private Vec3 _cameraZoomDirection;

		// Token: 0x0400001F RID: 31
		private GauntletCraftingScreen.CameraParameters _currentCameraValues;

		// Token: 0x04000020 RID: 32
		private GauntletCraftingScreen.CameraParameters _targetCameraValues;

		// Token: 0x04000021 RID: 33
		private GameEntity _craftingEntity;

		// Token: 0x04000022 RID: 34
		private MatrixFrame _initialEntityFrame;

		// Token: 0x04000023 RID: 35
		private WeaponDesign _craftedData;

		// Token: 0x04000024 RID: 36
		private bool _isInitialized;

		// Token: 0x04000025 RID: 37
		private static KeyValuePair<string, string> _reloadXmlPath;

		// Token: 0x02000051 RID: 81
		private struct CameraParameters
		{
			// Token: 0x060003DB RID: 987 RVA: 0x00017C8C File Offset: 0x00015E8C
			public CameraParameters(float horizontalRotation, float verticalRotation, float zoom)
			{
				this.HorizontalRotation = horizontalRotation;
				this.VerticalRotation = verticalRotation;
				this.Zoom = zoom;
			}

			// Token: 0x040001C2 RID: 450
			public float HorizontalRotation;

			// Token: 0x040001C3 RID: 451
			public float VerticalRotation;

			// Token: 0x040001C4 RID: 452
			public float Zoom;
		}
	}
}
