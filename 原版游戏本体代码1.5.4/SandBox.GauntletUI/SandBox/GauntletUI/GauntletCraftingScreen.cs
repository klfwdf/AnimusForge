using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.ViewModelCollection.WeaponCrafting;
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

namespace SandBox.GauntletUI;

[GameStateScreen(typeof(CraftingState))]
public class GauntletCraftingScreen : ScreenBase, ICraftingStateHandler, IGameStateListener
{
	private struct CameraParameters
	{
		public float HorizontalRotation;

		public float VerticalRotation;

		public float SelfRotation;

		public float Zoom;

		public CameraParameters(float horizontalRotation, float verticalRotation, float selfRotation, float zoom)
		{
			HorizontalRotation = horizontalRotation;
			VerticalRotation = verticalRotation;
			SelfRotation = selfRotation;
			Zoom = zoom;
		}
	}

	private static bool _enableLegacyCraftingCameraControls;

	private Scene _craftingScene;

	private SceneLayer _sceneLayer;

	private readonly CraftingState _craftingState;

	private CraftingVM _dataSource;

	private GauntletLayer _gauntletLayer;

	private GauntletMovieIdentifier _gauntletMovie;

	private SpriteCategory _craftingCategory;

	private Camera _camera;

	private MatrixFrame _initialCameraFrame;

	private Vec3 _dofParams;

	private Vec3 _cameraZoomDirection;

	private CameraParameters _currentCameraValues;

	private CameraParameters _targetCameraValues;

	private GameEntity _craftingEntity;

	private MatrixFrame _initialEntityFrame;

	private WeaponDesign _craftedData;

	private bool _isInitialized;

	private static KeyValuePair<string, string> _reloadXmlPath;

	private SceneView SceneView => _sceneLayer.SceneView;

	public GauntletCraftingScreen(CraftingState craftingState)
	{
		_craftingState = craftingState;
		_craftingState.Handler = this;
	}

	private void ReloadPieces()
	{
		string key = _reloadXmlPath.Key;
		string text = _reloadXmlPath.Value;
		if (!text.EndsWith(".xml"))
		{
			text += ".xml";
		}
		_reloadXmlPath = new KeyValuePair<string, string>(null, null);
		XmlDocument xmlDocument = Game.Current.ObjectManager.LoadXMLFromFileSkipValidation(ModuleHelper.GetModuleFullPath(key) + "ModuleData/" + text, "");
		if (xmlDocument == null)
		{
			return;
		}
		foreach (XmlNode childNode in xmlDocument.ChildNodes[1].ChildNodes)
		{
			XmlAttributeCollection attributes = childNode.Attributes;
			if (attributes != null)
			{
				string innerText = attributes["id"].InnerText;
				Game.Current.ObjectManager.GetObject<CraftingPiece>(innerText)?.Deserialize(Game.Current.ObjectManager, childNode);
			}
		}
		_craftingState.CraftingLogic.ReIndex(enforceReCreation: true);
		RefreshItemEntity(_dataSource.IsInCraftingMode);
		_dataSource.WeaponDesign.RefreshItem();
	}

	public void Initialize()
	{
		_craftingCategory = UIResourceManager.LoadSpriteCategory("ui_crafting");
		_gauntletLayer = new GauntletLayer("CraftingScreen", 1);
		_gauntletMovie = _gauntletLayer.LoadMovie("Crafting", _dataSource);
		_gauntletLayer.InputRestrictions.SetInputRestrictions();
		_gauntletLayer.IsFocusLayer = true;
		AddLayer(_gauntletLayer);
		OpenScene();
		RefreshItemEntity(isItemVisible: true);
		_isInitialized = true;
		Game.Current?.EventManager.TriggerEvent(new TutorialContextChangedEvent(TutorialContexts.CraftingScreen));
		UISoundsHelper.PlayUISound("event:/ui/panels/panel_settlement_enter_smithy");
	}

	protected override void OnInitialize()
	{
		base.OnInitialize();
		Initialize();
		_sceneLayer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("Generic"));
		_gauntletLayer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("Generic"));
		_sceneLayer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("GenericPanelGameKeyCategory"));
		_gauntletLayer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("GenericPanelGameKeyCategory"));
		_sceneLayer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("CraftingHotkeyCategory"));
		_gauntletLayer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("CraftingHotkeyCategory"));
		InformationManager.HideAllMessages();
	}

	protected override void OnFinalize()
	{
		base.OnFinalize();
		Game.Current?.EventManager.TriggerEvent(new TutorialContextChangedEvent(TutorialContexts.None));
		_craftingScene?.ManualInvalidate();
		_craftingScene = null;
		SceneView.ClearAll(clearScene: true, removeTerrain: true);
		_craftingCategory.Unload();
		_dataSource?.OnFinalize();
	}

	protected override void OnFrameTick(float dt)
	{
		LoadingWindow.DisableGlobalLoadingWindow();
		base.OnFrameTick(dt);
		if (!_gauntletLayer.IsFocusedOnInput() && (_sceneLayer.Input.IsControlDown() || _gauntletLayer.Input.IsControlDown()))
		{
			if (_sceneLayer.Input.IsHotKeyPressed("Copy") || _gauntletLayer.Input.IsHotKeyPressed("Copy"))
			{
				CopyXmlCode();
			}
			else if (_sceneLayer.Input.IsHotKeyPressed("Paste") || _gauntletLayer.Input.IsHotKeyPressed("Paste"))
			{
				PasteXmlCode();
			}
		}
		if (_craftingState.CraftingLogic.CurrentCraftingTemplate == null)
		{
			return;
		}
		_craftingScene?.Tick(dt);
		bool flag = false;
		if (_reloadXmlPath.Key != null && _reloadXmlPath.Value != null)
		{
			ReloadPieces();
			flag = true;
		}
		if (flag)
		{
			return;
		}
		if (base.DebugInput.IsHotKeyPressed("Reset"))
		{
			ResetEntityAndCamera();
		}
		_dataSource.CanSwitchTabs = !Input.IsGamepadActive || !InformationManager.GetIsAnyTooltipActiveAndExtended();
		_dataSource.AreGamepadControlHintsEnabled = Input.IsGamepadActive && _sceneLayer.IsHitThisFrame && _dataSource.IsInCraftingMode;
		if (_dataSource.IsInCraftingMode)
		{
			TickCameraInput(dt);
		}
		_craftingScene?.SetDepthOfFieldParameters(_dofParams.x, _dofParams.z, isVignetteOn: false);
		_craftingScene?.SetDepthOfFieldFocus(_initialEntityFrame.origin.Distance(_camera.Frame.origin));
		SceneView.SetCamera(_camera);
		if (!Input.IsGamepadActive && (_gauntletLayer.IsFocusedOnInput() || _sceneLayer.IsFocusedOnInput()))
		{
			return;
		}
		if (IsHotKeyReleasedInAnyLayer("Exit"))
		{
			UISoundsHelper.PlayUISound("event:/ui/default");
			_dataSource.ExecuteCancel();
		}
		else if (IsHotKeyReleasedInAnyLayer("Confirm"))
		{
			bool isInCraftingMode = _dataSource.IsInCraftingMode;
			bool isInRefinementMode = _dataSource.IsInRefinementMode;
			bool isInSmeltingMode = _dataSource.IsInSmeltingMode;
			var (flag2, flag3) = _dataSource.ExecuteConfirm();
			if (!flag2)
			{
				return;
			}
			if (flag3)
			{
				if (isInCraftingMode)
				{
					UISoundsHelper.PlayUISound("event:/ui/crafting/craft_success");
				}
				else if (isInRefinementMode)
				{
					UISoundsHelper.PlayUISound("event:/ui/crafting/refine_success");
				}
				else if (isInSmeltingMode)
				{
					UISoundsHelper.PlayUISound("event:/ui/crafting/smelt_success");
				}
			}
			else
			{
				UISoundsHelper.PlayUISound("event:/ui/default");
			}
		}
		else
		{
			if (!_dataSource.CanSwitchTabs)
			{
				return;
			}
			if (IsHotKeyReleasedInAnyLayer("SwitchToPreviousTab"))
			{
				if (_dataSource.IsInSmeltingMode)
				{
					UISoundsHelper.PlayUISound("event:/ui/crafting/refine_tab");
					_dataSource.ExecuteSwitchToRefinement();
				}
				else if (_dataSource.IsInCraftingMode)
				{
					UISoundsHelper.PlayUISound("event:/ui/crafting/smelt_tab");
					_dataSource.ExecuteSwitchToSmelting();
				}
				else if (_dataSource.IsInRefinementMode)
				{
					UISoundsHelper.PlayUISound("event:/ui/crafting/craft_tab");
					_dataSource.ExecuteSwitchToCrafting();
				}
			}
			else if (IsHotKeyReleasedInAnyLayer("SwitchToNextTab"))
			{
				if (_dataSource.IsInSmeltingMode)
				{
					UISoundsHelper.PlayUISound("event:/ui/crafting/craft_tab");
					_dataSource.ExecuteSwitchToCrafting();
				}
				else if (_dataSource.IsInCraftingMode)
				{
					UISoundsHelper.PlayUISound("event:/ui/crafting/refine_tab");
					_dataSource.ExecuteSwitchToRefinement();
				}
				else if (_dataSource.IsInRefinementMode)
				{
					UISoundsHelper.PlayUISound("event:/ui/crafting/smelt_tab");
					_dataSource.ExecuteSwitchToSmelting();
				}
			}
		}
	}

	private void OnClose()
	{
		CampaignMission.Current?.EndMission();
		Game.Current.GameStateManager.PopState();
	}

	private void OnResetCamera()
	{
		ResetEntityAndCamera();
	}

	private void OnWeaponCrafted()
	{
		_dataSource.WeaponDesign.CraftingResultPopup?.SetDoneInputKey(HotKeyManager.GetCategory("GenericPanelGameKeyCategory").GetHotKey("Confirm"));
	}

	public void OnCraftingLogicInitialized()
	{
		_dataSource?.OnFinalize();
		_dataSource = new CraftingVM(_craftingState.CraftingLogic, OnClose, OnResetCamera, OnWeaponCrafted, GetItemUsageSetFlag)
		{
			OnItemRefreshed = RefreshItemEntity
		};
		_dataSource.WeaponDesign.CraftingHistory.SetDoneKey(HotKeyManager.GetCategory("GenericPanelGameKeyCategory").GetHotKey("Confirm"));
		_dataSource.WeaponDesign.CraftingHistory.SetCancelKey(HotKeyManager.GetCategory("GenericPanelGameKeyCategory").GetHotKey("Exit"));
		_dataSource.CraftingHeroPopup.SetExitInputKey(HotKeyManager.GetCategory("GenericPanelGameKeyCategory").GetHotKey("Exit"));
		_dataSource.SetConfirmInputKey(HotKeyManager.GetCategory("GenericPanelGameKeyCategory").GetHotKey("Confirm"));
		_dataSource.SetExitInputKey(HotKeyManager.GetCategory("GenericPanelGameKeyCategory").GetHotKey("Exit"));
		_dataSource.SetPreviousTabInputKey(HotKeyManager.GetCategory("GenericPanelGameKeyCategory").GetHotKey("SwitchToPreviousTab"));
		_dataSource.SetNextTabInputKey(HotKeyManager.GetCategory("GenericPanelGameKeyCategory").GetHotKey("SwitchToNextTab"));
		_dataSource.AddCameraControlInputKey(HotKeyManager.GetCategory("CraftingHotkeyCategory").GetGameKey(56));
		_dataSource.AddCameraControlInputKey(HotKeyManager.GetCategory("CraftingHotkeyCategory").GetGameKey(57));
		_dataSource.AddCameraControlInputKey(HotKeyManager.GetCategory("CraftingHotkeyCategory").RegisteredGameAxisKeys.FirstOrDefault((GameAxisKey x) => x.Id == "CameraAxisX"));
	}

	public void OnCraftingLogicRefreshed()
	{
		_dataSource.OnCraftingLogicRefreshed(_craftingState.CraftingLogic);
		if (_isInitialized)
		{
			RefreshItemEntity(isItemVisible: true);
		}
	}

	private void OpenScene()
	{
		_craftingScene = Scene.CreateNewScene(initialize_physics: true, enable_decals: false);
		_craftingScene.SetName("GauntletCraftingScreen");
		SceneInitializationData initData = new SceneInitializationData
		{
			InitPhysicsWorld = false
		};
		_craftingScene.Read("crafting_menu_outdoor", ref initData);
		_craftingScene.DisableStaticShadows(value: true);
		_craftingScene.SetShadow(shadowEnabled: true);
		_craftingScene.SetClothSimulationState(state: true);
		InitializeEntityAndCamera();
		_sceneLayer = new SceneLayer();
		_sceneLayer.IsFocusLayer = true;
		AddLayer(_sceneLayer);
		SceneView.SetScene(_craftingScene);
		SceneView.SetCamera(_camera);
		SceneView.SetSceneUsesShadows(value: true);
		SceneView.SetAcceptGlobalDebugRenderObjects(value: true);
		SceneView.SetRenderWithPostfx(value: true);
		SceneView.SetResolutionScaling(value: true);
	}

	private void InitializeEntityAndCamera()
	{
		GameEntity gameEntity = _craftingScene.FindEntityWithTag("weapon_point");
		MatrixFrame frame = gameEntity.GetGlobalFrame();
		_craftingScene.RemoveEntity(gameEntity, 114);
		frame.Elevate(1.6f);
		_initialEntityFrame = frame;
		_craftingEntity = GameEntity.CreateEmpty(_craftingScene);
		_craftingEntity.SetFrame(ref frame);
		_camera = Camera.CreateCamera();
		_dofParams = default(Vec3);
		GameEntity gameEntity2 = _craftingScene.FindEntityWithTag("camera_point");
		gameEntity2.GetCameraParamsFromCameraScript(_camera, ref _dofParams);
		float fovVertical = _camera.GetFovVertical();
		float aspectRatio = Screen.AspectRatio;
		float near = _camera.Near;
		float far = _camera.Far;
		_camera.SetFovVertical(fovVertical, aspectRatio, near, far);
		_craftingScene.SetDepthOfFieldParameters(_dofParams.x, _dofParams.z, isVignetteOn: false);
		_craftingScene.SetDepthOfFieldFocus(_dofParams.y);
		_initialCameraFrame = gameEntity2.GetFrame();
		_cameraZoomDirection = _initialEntityFrame.origin - _initialCameraFrame.origin;
	}

	private void RefreshItemEntity(bool isItemVisible)
	{
		_dataSource.WeaponDesign.CurrentWeaponHasScabbard = false;
		MatrixFrame frame = _initialEntityFrame;
		if (_craftingEntity != null)
		{
			frame = _craftingEntity.GetFrame();
			_craftingEntity.Remove(115);
			_craftingEntity = null;
		}
		if (!isItemVisible)
		{
			return;
		}
		_craftingEntity = GameEntity.CreateEmpty(_craftingScene);
		_craftingEntity.SetFrame(ref frame);
		_craftedData = _craftingState.CraftingLogic.CurrentWeaponDesign;
		if (!(_craftedData != null))
		{
			return;
		}
		frame = _craftingEntity.GetFrame();
		float num = _craftedData.CraftedWeaponLength / 2f;
		BladeData bladeData = _craftedData.UsedPieces[0].CraftingPiece.BladeData;
		_dataSource.WeaponDesign.CurrentWeaponHasScabbard = !string.IsNullOrEmpty(bladeData.HolsterMeshName);
		MetaMesh metaMesh;
		if (!_dataSource.WeaponDesign.IsScabbardVisible)
		{
			metaMesh = CraftedDataView.BuildWeaponMesh(_craftedData, 0f - num, pieceTypeHidingEnabledForHolster: false, batchAllMeshes: false);
		}
		else
		{
			metaMesh = CraftedDataView.BuildHolsterMeshWithWeapon(_craftedData, 0f - num, batchAllMeshes: false);
			if (metaMesh == null)
			{
				metaMesh = CraftedDataView.BuildWeaponMesh(_craftedData, 0f - num, pieceTypeHidingEnabledForHolster: false, batchAllMeshes: false);
			}
		}
		_craftingEntity = _craftingScene.AddItemEntity(ref frame, metaMesh);
	}

	private void TickCameraInput(float dt)
	{
		if (_sceneLayer.IsHitThisFrame && ScreenManager.FocusedLayer == _gauntletLayer)
		{
			_gauntletLayer.IsFocusLayer = false;
			ScreenManager.TryLoseFocus(_gauntletLayer);
			_sceneLayer.IsFocusLayer = true;
			ScreenManager.TrySetFocus(_sceneLayer);
		}
		else if (!_sceneLayer.IsHitThisFrame && ScreenManager.FocusedLayer == _sceneLayer)
		{
			_sceneLayer.IsFocusLayer = false;
			ScreenManager.TryLoseFocus(_sceneLayer);
			_gauntletLayer.IsFocusLayer = true;
			ScreenManager.TrySetFocus(_gauntletLayer);
		}
		Vec2 vec = new Vec2(_sceneLayer.Input.GetNormalizedMouseMoveX() * 1920f, _sceneLayer.Input.GetNormalizedMouseMoveY() * 1080f);
		bool num = _enableLegacyCraftingCameraControls && !Input.IsGamepadActive;
		bool flag = _sceneLayer.Input.IsHotKeyDown("Rotate");
		bool flag2 = !num && _sceneLayer.Input.IsHotKeyDown("Zoom");
		bool flag3 = num && _sceneLayer.Input.IsHotKeyDown("Zoom");
		bool flag4 = false;
		if (flag || flag2 || flag3 || flag4)
		{
			MBWindowManager.DontChangeCursorPos();
			_gauntletLayer.InputRestrictions.SetMouseVisibility(isVisible: false);
		}
		else
		{
			_gauntletLayer.InputRestrictions.SetMouseVisibility(isVisible: true);
		}
		if (!base.DebugInput.IsControlDown() && !base.DebugInput.IsAltDown())
		{
			if (_sceneLayer.Input.IsHotKeyDown("Rotate") && _sceneLayer.Input.IsHotKeyDown("Zoom"))
			{
				ResetEntityAndCamera();
				return;
			}
			float num2;
			if (Input.IsGamepadActive)
			{
				float gameKeyState = _sceneLayer.Input.GetGameKeyState(56);
				float gameKeyState2 = _sceneLayer.Input.GetGameKeyState(57);
				float inputValue = gameKeyState - gameKeyState2;
				NormalizeControllerInputForDeadZone(ref inputValue, 0.1f);
				num2 = inputValue * 4f * dt;
			}
			else
			{
				float deltaMouseScroll = _sceneLayer.Input.GetDeltaMouseScroll();
				float num3 = (flag2 ? vec.y : 0f);
				num2 = deltaMouseScroll * 0.001f + num3 * 0.002f;
			}
			_targetCameraValues.Zoom = MBMath.ClampFloat(_targetCameraValues.Zoom + num2, -0.5f, 0.5f);
			float num4;
			if (Input.IsGamepadActive)
			{
				float inputValue2 = _sceneLayer.Input.GetGameKeyAxis("CameraAxisX");
				NormalizeControllerInputForDeadZone(ref inputValue2, 0.1f);
				num4 = inputValue2 * 400f * dt;
			}
			else
			{
				num4 = (flag ? vec.x : 0f) * 0.2f;
			}
			_targetCameraValues.HorizontalRotation = MBMath.WrapAngle(_targetCameraValues.HorizontalRotation + num4 * (System.MathF.PI / 180f));
			float num5;
			if (Input.IsGamepadActive)
			{
				float inputValue3 = _sceneLayer.Input.GetGameKeyAxis("CameraAxisY");
				NormalizeControllerInputForDeadZone(ref inputValue3, 0.1f);
				num5 = inputValue3 * 400f * dt;
			}
			else
			{
				num5 = (flag ? (vec.y * -1f) : 0f) * 0.2f;
			}
			_targetCameraValues.VerticalRotation = MBMath.WrapAngle(_targetCameraValues.VerticalRotation + num5 * (System.MathF.PI / 180f));
			float num6;
			if (Input.IsGamepadActive)
			{
				num6 = 0f;
			}
			else
			{
				float num7 = ((TaleWorlds.Library.MathF.Abs(vec.x) > TaleWorlds.Library.MathF.Abs(vec.y)) ? vec.x : vec.y);
				num6 = (flag3 ? num7 : 0f) * 0.2f;
			}
			_targetCameraValues.SelfRotation = MBMath.WrapAngle(_targetCameraValues.SelfRotation + num6 * (System.MathF.PI / 180f));
		}
		UpdateCamera(dt);
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
		CameraParameters currentCameraValues = new CameraParameters(TaleWorlds.Library.MathF.AngleLerp(_currentCameraValues.HorizontalRotation, _targetCameraValues.HorizontalRotation, amount), TaleWorlds.Library.MathF.AngleLerp(_currentCameraValues.VerticalRotation, _targetCameraValues.VerticalRotation, amount), TaleWorlds.Library.MathF.AngleLerp(_currentCameraValues.SelfRotation, _targetCameraValues.SelfRotation, amount), TaleWorlds.Library.MathF.Lerp(_currentCameraValues.Zoom, _targetCameraValues.Zoom, amount));
		CameraParameters cameraParameters = new CameraParameters(currentCameraValues.HorizontalRotation - _currentCameraValues.HorizontalRotation, currentCameraValues.VerticalRotation - _currentCameraValues.VerticalRotation, currentCameraValues.SelfRotation - _currentCameraValues.SelfRotation, currentCameraValues.Zoom - _currentCameraValues.Zoom);
		_currentCameraValues = currentCameraValues;
		MatrixFrame frame = _craftingEntity.GetFrame();
		if (_enableLegacyCraftingCameraControls && !Input.IsGamepadActive)
		{
			frame.rotation.RotateAboutSide(cameraParameters.VerticalRotation);
			frame.rotation.RotateAboutAnArbitraryVector(in Vec3.Up, cameraParameters.HorizontalRotation);
			frame.rotation.RotateAboutUp(cameraParameters.SelfRotation);
		}
		else
		{
			frame.rotation.RotateAboutUp(cameraParameters.HorizontalRotation);
			frame.rotation.RotateAboutSide(cameraParameters.VerticalRotation);
		}
		_craftingEntity.SetFrame(ref frame);
		MatrixFrame frame2 = _camera.Frame;
		frame2.origin += _cameraZoomDirection * cameraParameters.Zoom;
		_camera.Frame = frame2;
	}

	private void ResetEntityAndCamera()
	{
		_currentCameraValues = new CameraParameters(0f, 0f, 0f, 0f);
		_targetCameraValues = new CameraParameters(0f, 0f, 0f, 0f);
		_craftingEntity.SetFrame(ref _initialEntityFrame);
		_camera.Frame = _initialCameraFrame;
	}

	private void CopyXmlCode()
	{
		Input.SetClipboardText(_craftingState.CraftingLogic.GetXmlCodeForCurrentItem(_craftingState.CraftingLogic.GetCurrentCraftedItemObject()));
	}

	private void PasteXmlCode()
	{
		string clipboardText = Input.GetClipboardText();
		if (!string.IsNullOrEmpty(clipboardText))
		{
			ItemObject itemObject = MBObjectManager.Instance.GetObject<ItemObject>(clipboardText);
			CraftingTemplate craftingTemplate;
			(CraftingPiece, int)[] pieces;
			if (itemObject != null)
			{
				SwithToCraftedItem(itemObject);
			}
			else if (_craftingState.CraftingLogic.TryGetWeaponPropertiesFromXmlCode(clipboardText, out craftingTemplate, out pieces))
			{
				_dataSource.SetCurrentDesignManually(craftingTemplate, pieces);
			}
		}
	}

	private void SwithToCraftedItem(ItemObject itemObject)
	{
		if (itemObject == null || !itemObject.IsCraftedWeapon)
		{
			return;
		}
		if (!_dataSource.IsInCraftingMode)
		{
			_dataSource.ExecuteSwitchToCrafting();
		}
		WeaponDesign weaponDesign = itemObject.WeaponDesign;
		if (_craftingState.CraftingLogic.CurrentCraftingTemplate != weaponDesign.Template)
		{
			_dataSource.WeaponDesign.SelectPrimaryWeaponClass(weaponDesign.Template);
		}
		WeaponDesignElement[] usedPieces = weaponDesign.UsedPieces;
		foreach (WeaponDesignElement weaponDesignElement in usedPieces)
		{
			if (weaponDesignElement.IsValid)
			{
				_dataSource.WeaponDesign.SwitchToPiece(weaponDesignElement);
			}
		}
	}

	private ItemObject.ItemUsageSetFlags GetItemUsageSetFlag(WeaponComponentData item)
	{
		if (!string.IsNullOrEmpty(item.ItemUsage))
		{
			return MBItem.GetItemUsageSetFlags(item.ItemUsage);
		}
		return (ItemObject.ItemUsageSetFlags)0;
	}

	private bool IsHotKeyReleasedInAnyLayer(string hotKeyId)
	{
		if (!_sceneLayer.Input.IsHotKeyReleased(hotKeyId))
		{
			return _gauntletLayer.Input.IsHotKeyReleased(hotKeyId);
		}
		return true;
	}

	void IGameStateListener.OnInitialize()
	{
	}

	void IGameStateListener.OnFinalize()
	{
	}

	void IGameStateListener.OnActivate()
	{
	}

	void IGameStateListener.OnDeactivate()
	{
	}
}
