using System;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Engine.Options;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.ObjectSystem;

namespace TaleWorlds.MountAndBlade.View.Tableaus;

public class ItemTableau
{
	private static int _tableauIndex;

	private Scene _tableauScene;

	private GameEntity _itemTableauEntity;

	private MatrixFrame _itemTableauFrame = MatrixFrame.Identity;

	private bool _isRotating;

	private bool _isTranslating;

	private bool _isRotatingByDefault;

	private bool _initialized;

	private int _tableauSizeX;

	private int _tableauSizeY;

	private float _cameraRatio;

	private Camera _camera;

	private Vec3 _midPoint;

	private const float InitialCamFov = 1f;

	private float _curZoomSpeed;

	private Vec3 _curCamDisplacement = Vec3.Zero;

	private bool _isEnabled;

	private float _panRotation;

	private float _tiltRotation;

	private bool _hasInitialTiltRotation;

	private float _initialTiltRotation;

	private bool _hasInitialPanRotation;

	private float _initialPanRotation;

	private float RenderScale = 1f;

	private string _stringId = "";

	private int _ammo;

	private int _averageUnitCost;

	private string _itemModifierId = "";

	private string _bannerCode = "";

	private ItemRosterElement _itemRosterElement;

	private MatrixFrame _initialFrame;

	private bool _lockMouse;

	public Texture Texture { get; private set; }

	private TableauView View
	{
		get
		{
			if (Texture != null)
			{
				return Texture.TableauView;
			}
			return null;
		}
	}

	private bool _isSizeValid
	{
		get
		{
			if (_tableauSizeX > 0)
			{
				return _tableauSizeY > 0;
			}
			return false;
		}
	}

	public ItemTableau()
	{
		SetEnabled(enabled: true);
	}

	public void SetTargetSize(int width, int height)
	{
		bool isSizeValid = _isSizeValid;
		_isRotating = false;
		if (width <= 0 || height <= 0)
		{
			_tableauSizeX = 10;
			_tableauSizeY = 10;
		}
		else
		{
			RenderScale = NativeOptions.GetConfig(NativeOptions.NativeOptionsType.ResolutionScale) / 100f;
			_tableauSizeX = (int)((float)width * RenderScale);
			_tableauSizeY = (int)((float)height * RenderScale);
		}
		_cameraRatio = (float)_tableauSizeX / (float)_tableauSizeY;
		View?.SetEnable(value: false);
		View?.AddClearTask(clearOnlySceneview: true);
		Texture?.Release();
		if (!isSizeValid && _isSizeValid)
		{
			Recalculate();
		}
		Texture = TableauView.AddTableau($"ItemTableau_{_tableauIndex++}", TableauMaterialTabInventoryItemTooltipOnRender, _tableauScene, _tableauSizeX, _tableauSizeY);
	}

	public void OnFinalize()
	{
		View?.SetEnable(value: false);
		_camera?.ReleaseCameraEntity();
		_camera = null;
		View?.AddClearTask();
		_tableauScene?.ManualInvalidate();
		_tableauScene = null;
		Texture = null;
		_initialized = false;
		if (_lockMouse)
		{
			UpdateMouseLock(forceUnlock: true);
		}
	}

	protected void SetEnabled(bool enabled)
	{
		_isRotatingByDefault = true;
		_isRotating = false;
		ResetCamera();
		_isEnabled = enabled;
		TableauView view = View;
		if (view != null)
		{
			view.SetEnable(_isEnabled);
		}
	}

	public void SetStringId(string stringId)
	{
		_stringId = stringId;
		Recalculate();
	}

	public void SetAmmo(int ammo)
	{
		_ammo = ammo;
		Recalculate();
	}

	public void SetAverageUnitCost(int averageUnitCost)
	{
		_averageUnitCost = averageUnitCost;
		Recalculate();
	}

	public void SetItemModifierId(string itemModifierId)
	{
		_itemModifierId = itemModifierId;
		Recalculate();
	}

	public void SetBannerCode(string bannerCode)
	{
		_bannerCode = bannerCode;
		Recalculate();
	}

	public void Recalculate()
	{
		if (UiStringHelper.IsStringNoneOrEmptyForUi(_stringId) || !_isSizeValid)
		{
			return;
		}
		ItemModifier itemModifier = null;
		ItemObject itemObject = MBObjectManager.Instance.GetObject<ItemObject>(_stringId);
		if (itemObject == null)
		{
			itemObject = Game.Current.ObjectManager.GetObjectTypeList<ItemObject>().FirstOrDefault((ItemObject item) => item.IsCraftedWeapon && item.WeaponDesign.HashedCode == _stringId);
		}
		if (!string.IsNullOrEmpty(_itemModifierId))
		{
			itemModifier = MBObjectManager.Instance.GetObject<ItemModifier>(_itemModifierId);
		}
		if (itemObject == null)
		{
			return;
		}
		_itemRosterElement = new ItemRosterElement(itemObject, _ammo, itemModifier);
		RefreshItemTableau();
		if (_itemTableauEntity != null)
		{
			float num = Screen.RealScreenResolutionWidth / (float)_tableauSizeX;
			float num2 = Screen.RealScreenResolutionHeight / (float)_tableauSizeY;
			float num3 = ((num > num2) ? num : num2);
			if (num3 < 1f)
			{
				Vec3 globalBoxMax = _itemTableauEntity.GlobalBoxMax;
				Vec3 globalBoxMin = _itemTableauEntity.GlobalBoxMin;
				_itemTableauFrame = _itemTableauEntity.GetFrame();
				float length = _itemTableauFrame.rotation.f.Length;
				_itemTableauFrame.rotation.Orthonormalize();
				_itemTableauFrame.rotation.ApplyScaleLocal(length * num3);
				_itemTableauEntity.SetFrame(ref _itemTableauFrame);
				if (globalBoxMax.NearlyEquals(_itemTableauEntity.GlobalBoxMax) && globalBoxMin.NearlyEquals(_itemTableauEntity.GlobalBoxMin))
				{
					_itemTableauEntity.SetBoundingboxDirty();
					_itemTableauEntity.RecomputeBoundingBox();
				}
				_itemTableauFrame.origin += (globalBoxMax + globalBoxMin - _itemTableauEntity.GlobalBoxMax - _itemTableauEntity.GlobalBoxMin) * 0.5f;
				_itemTableauEntity.SetFrame(ref _itemTableauFrame);
				_midPoint = (_itemTableauEntity.GlobalBoxMax + _itemTableauEntity.GlobalBoxMin) * 0.5f + (globalBoxMax + globalBoxMin - _itemTableauEntity.GlobalBoxMax - _itemTableauEntity.GlobalBoxMin) * 0.5f;
			}
			else
			{
				_midPoint = (_itemTableauEntity.GlobalBoxMax + _itemTableauEntity.GlobalBoxMin) * 0.5f;
			}
			if (_itemRosterElement.EquipmentElement.Item.ItemType == ItemObject.ItemTypeEnum.HandArmor || _itemRosterElement.EquipmentElement.Item.ItemType == ItemObject.ItemTypeEnum.Shield)
			{
				_midPoint *= 1.2f;
			}
			ResetCamera();
		}
		_isRotatingByDefault = true;
		_isRotating = false;
	}

	public void Initialize()
	{
		_isRotatingByDefault = true;
		_isRotating = false;
		_isTranslating = false;
		_tableauScene = Scene.CreateNewScene();
		_tableauScene.SetName("ItemTableau");
		_tableauScene.DisableStaticShadows(value: true);
		_tableauScene.SetAtmosphereWithName("character_menu_a");
		Vec3 color = new Vec3(2.15f, 2.2f, 2.25f) * 2.25f;
		Vec3 direction = new Vec3(1f, -1f, -1f);
		_tableauScene.SetSunLight(ref color, ref direction);
		_tableauScene.SetClothSimulationState(state: false);
		ResetCamera();
		_initialized = true;
	}

	private void TranslateCamera(bool value)
	{
		TranslateCameraAux(value);
	}

	private void TranslateCameraAux(bool value)
	{
		_isRotatingByDefault = !value && _isRotatingByDefault;
		_isTranslating = value;
		UpdateMouseLock();
	}

	private void ResetCamera()
	{
		_curCamDisplacement = Vec3.Zero;
		_curZoomSpeed = 0f;
		if (_camera != null)
		{
			_camera.Frame = MatrixFrame.Identity;
			SetCamFovHorizontal(1f);
			MakeCameraLookMidPoint();
		}
	}

	public void RotateItem(bool value)
	{
		_isRotatingByDefault = !value && _isRotatingByDefault;
		_isRotating = value;
		UpdateMouseLock();
	}

	public void RotateItemVerticalWithAmount(float value)
	{
		UpdateRotation(0f, value / -2f);
	}

	public void RotateItemHorizontalWithAmount(float value)
	{
		UpdateRotation(value / 2f, 0f);
	}

	public void OnTick(float dt)
	{
		float num = Input.MouseMoveX + Input.GetKeyState(InputKey.ControllerLStick).X * 1000f * dt;
		float num2 = Input.MouseMoveY + Input.GetKeyState(InputKey.ControllerLStick).Y * -1000f * dt;
		if (_isEnabled && (_isRotating || _isTranslating) && (!num.ApproximatelyEqualsTo(0f) || !num2.ApproximatelyEqualsTo(0f)))
		{
			if (_isRotating)
			{
				UpdateRotation(num, num2);
			}
			if (_isTranslating)
			{
				UpdatePosition(num, num2);
			}
		}
		TickCameraZoom(dt);
	}

	private void UpdatePosition(float mouseMoveX, float mouseMoveY)
	{
		if (_initialized)
		{
			Vec3 vec = new Vec3(mouseMoveX / (float)(-_tableauSizeX), mouseMoveY / (float)_tableauSizeY);
			vec *= 2.2f * _camera.HorizontalFov;
			_curCamDisplacement += vec;
			MakeCameraLookMidPoint();
		}
	}

	private void UpdateRotation(float mouseMoveX, float mouseMoveY)
	{
		if (_initialized)
		{
			_panRotation += mouseMoveX * 0.004363323f;
			_tiltRotation += mouseMoveY * 0.004363323f;
			_tiltRotation = TaleWorlds.Library.MathF.Clamp(_tiltRotation, System.MathF.PI * -19f / 20f, -System.MathF.PI / 20f);
			MatrixFrame m = _itemTableauEntity.GetFrame();
			Vec3 vec = (_itemTableauEntity.GetBoundingBoxMax() + _itemTableauEntity.GetBoundingBoxMin()) * 0.5f;
			MatrixFrame m2 = MatrixFrame.Identity;
			m2.origin = vec;
			MatrixFrame m3 = MatrixFrame.Identity;
			m3.origin = -vec;
			m *= m2;
			m.rotation = Mat3.Identity;
			m.rotation.ApplyScaleLocal(_initialFrame.rotation.GetScaleVector());
			m.rotation.RotateAboutSide(_tiltRotation);
			m.rotation.RotateAboutUp(_panRotation);
			m *= m3;
			_itemTableauEntity.SetFrame(ref m);
		}
	}

	public void SetInitialTiltRotation(float amount)
	{
		_hasInitialTiltRotation = true;
		_initialTiltRotation = amount;
	}

	public void SetInitialPanRotation(float amount)
	{
		_hasInitialPanRotation = true;
		_initialPanRotation = amount;
	}

	public void Zoom(double value)
	{
		_curZoomSpeed -= (float)(value / 1000.0);
	}

	public void SetItem(ItemRosterElement itemRosterElement)
	{
		_itemRosterElement = itemRosterElement;
		RefreshItemTableau();
	}

	private void RefreshItemTableau()
	{
		if (!_initialized)
		{
			Initialize();
		}
		if (_itemTableauEntity != null)
		{
			_itemTableauEntity.Remove(102);
			_itemTableauEntity = null;
		}
		if (_itemRosterElement.EquipmentElement.Item == null)
		{
			return;
		}
		ItemObject.ItemTypeEnum itemType = _itemRosterElement.EquipmentElement.Item.ItemType;
		if (_itemTableauEntity == null)
		{
			MatrixFrame placementFrame = _itemRosterElement.GetItemFrameForItemTooltip();
			placementFrame.origin.z += 2.5f;
			MetaMesh itemMeshForInventory = _itemRosterElement.GetItemMeshForInventory();
			uint color = 0u;
			uint color2 = 0u;
			if (!string.IsNullOrEmpty(_bannerCode))
			{
				Banner banner = new Banner(_bannerCode);
				color = banner.GetPrimaryColor();
				if (banner.BannerDataList.Count > 0 && BannerManager.Instance.ReadOnlyColorPalette.TryGetValue(banner.BannerDataList[1].ColorId, out var value))
				{
					color2 = value.Color;
				}
			}
			if (itemMeshForInventory != null)
			{
				if (itemType == ItemObject.ItemTypeEnum.HandArmor)
				{
					_itemTableauEntity = GameEntity.CreateEmpty(_tableauScene);
					AnimationSystemData animationSystemData = Game.Current.DefaultMonster.FillAnimationSystemData(MBActionSet.GetActionSet(Game.Current.DefaultMonster.ActionSetCode), 1f, hasClippingPlane: false);
					_itemTableauEntity.CreateSkeletonWithActionSet(ref animationSystemData);
					_itemTableauEntity.SetFrame(ref placementFrame);
					_itemTableauEntity.Skeleton.SetAgentActionChannel(0, in ActionIndexCache.act_tableau_hand_armor_pose);
					_itemTableauEntity.AddMultiMeshToSkeleton(itemMeshForInventory);
					_itemTableauEntity.Skeleton.TickActionChannels();
					_itemTableauEntity.Skeleton.TickAnimationsAndForceUpdate(0.01f, placementFrame, tickAnimsForChildren: true);
				}
				else if (itemType == ItemObject.ItemTypeEnum.Horse || itemType == ItemObject.ItemTypeEnum.Animal)
				{
					HorseComponent horseComponent = _itemRosterElement.EquipmentElement.Item.HorseComponent;
					Monster monster = horseComponent.Monster;
					_itemTableauEntity = GameEntity.CreateEmpty(_tableauScene);
					AnimationSystemData animationSystemData2 = monster.FillAnimationSystemData(MBGlobals.GetActionSet(horseComponent.Monster.ActionSetCode), 1f, hasClippingPlane: false);
					_itemTableauEntity.CreateSkeletonWithActionSet(ref animationSystemData2);
					_itemTableauEntity.Skeleton.SetAgentActionChannel(0, in ActionIndexCache.act_inventory_idle_start);
					_itemTableauEntity.SetFrame(ref placementFrame);
					_itemTableauEntity.AddMultiMeshToSkeleton(itemMeshForInventory);
				}
				else if (itemType == ItemObject.ItemTypeEnum.HorseHarness && _itemRosterElement.EquipmentElement.Item.ArmorComponent != null)
				{
					_itemTableauEntity = _tableauScene.AddItemEntity(ref placementFrame, itemMeshForInventory);
					MetaMesh copy = MetaMesh.GetCopy(_itemRosterElement.EquipmentElement.Item.ArmorComponent.ReinsMesh, showErrors: true, mayReturnNull: true);
					if (copy != null)
					{
						_itemTableauEntity.AddMultiMesh(copy);
					}
				}
				else
				{
					switch (itemType)
					{
					case ItemObject.ItemTypeEnum.Shield:
						if (!string.IsNullOrEmpty(_bannerCode))
						{
							Banner banner3 = new Banner(_bannerCode);
							if (_itemRosterElement.EquipmentElement.Item.IsUsingTableau && !banner3.IsBannerDataListEmpty())
							{
								itemMeshForInventory.SetMaterial(_itemRosterElement.EquipmentElement.Item.GetTableauMaterial(banner3));
							}
						}
						_itemTableauEntity = _tableauScene.AddItemEntity(ref placementFrame, itemMeshForInventory);
						break;
					case ItemObject.ItemTypeEnum.Banner:
						if (!string.IsNullOrEmpty(_bannerCode))
						{
							Banner banner2 = new Banner(_bannerCode);
							if (_itemRosterElement.EquipmentElement.Item.IsUsingTableau && !banner2.IsBannerDataListEmpty())
							{
								itemMeshForInventory.SetMaterial(_itemRosterElement.EquipmentElement.Item.GetTableauMaterial(banner2));
							}
							for (int i = 0; i < itemMeshForInventory.MeshCount; i++)
							{
								itemMeshForInventory.GetMeshAtIndex(i).Color = color;
								itemMeshForInventory.GetMeshAtIndex(i).Color2 = color2;
							}
						}
						_itemTableauEntity = _tableauScene.AddItemEntity(ref placementFrame, itemMeshForInventory);
						break;
					default:
						_itemTableauEntity = _tableauScene.AddItemEntity(ref placementFrame, itemMeshForInventory);
						break;
					}
				}
			}
			else
			{
				MBDebug.ShowWarning("[DEBUG]Item with " + _itemRosterElement.EquipmentElement.Item.StringId + "[DEBUG] string id cannot be found");
			}
		}
		MetaMesh metaMesh = null;
		SkinMask p = SkinMask.AllVisible;
		if (_itemRosterElement.EquipmentElement.Item.HasArmorComponent)
		{
			p = _itemRosterElement.EquipmentElement.Item.ArmorComponent.MeshesMask;
		}
		string text = "";
		bool flag = _itemRosterElement.EquipmentElement.Item.ItemFlags.HasAnyFlag(ItemFlags.NotUsableByMale);
		bool flag2 = false;
		if (ItemObject.ItemTypeEnum.HeadArmor == itemType || ItemObject.ItemTypeEnum.Cape == itemType)
		{
			text = "base_head";
			flag2 = true;
		}
		else if (ItemObject.ItemTypeEnum.BodyArmor == itemType)
		{
			if (p.HasAnyFlag(SkinMask.BodyVisible))
			{
				text = "base_body";
				flag2 = true;
			}
		}
		else if (ItemObject.ItemTypeEnum.LegArmor == itemType)
		{
			if (p.HasAnyFlag(SkinMask.LegsVisible))
			{
				text = "base_foot";
				flag2 = true;
			}
		}
		else if (ItemObject.ItemTypeEnum.HandArmor == itemType)
		{
			if (p.HasAnyFlag(SkinMask.HandsVisible))
			{
				MetaMesh copy2 = MetaMesh.GetCopy(flag ? "base_hand_female" : "base_hand", showErrors: false);
				_itemTableauEntity.AddMultiMeshToSkeleton(copy2);
			}
		}
		else if (ItemObject.ItemTypeEnum.HorseHarness == itemType)
		{
			text = "horse_base_mesh";
			flag2 = false;
		}
		if (text.Length > 0)
		{
			if (flag2 && flag)
			{
				text += "_female";
			}
			metaMesh = MetaMesh.GetCopy(text, showErrors: false);
			_itemTableauEntity.AddMultiMesh(metaMesh);
		}
		TableauView view = View;
		if (view != null)
		{
			float radius = (_itemTableauEntity.GetBoundingBoxMax() - _itemTableauEntity.GetBoundingBoxMin()).Length * 2f;
			Vec3 center = _itemTableauEntity.GetGlobalFrame().origin;
			view.SetFocusedShadowmap(enable: true, ref center, radius);
		}
		if (_itemTableauEntity != null)
		{
			_initialFrame = _itemTableauEntity.GetFrame();
			Vec3 eulerAngles = _initialFrame.rotation.GetEulerAngles();
			_panRotation = eulerAngles.x;
			_tiltRotation = eulerAngles.z;
			if (_hasInitialPanRotation)
			{
				_panRotation = _initialPanRotation;
			}
			else if (itemType == ItemObject.ItemTypeEnum.Shield)
			{
				_panRotation = -System.MathF.PI;
			}
			if (_hasInitialTiltRotation)
			{
				_tiltRotation = _initialTiltRotation;
			}
			else if (itemType == ItemObject.ItemTypeEnum.Shield)
			{
				_tiltRotation = 0f;
			}
			else
			{
				_tiltRotation = -System.MathF.PI / 2f;
			}
		}
	}

	private void TableauMaterialTabInventoryItemTooltipOnRender(Texture sender, EventArgs e)
	{
		if (!_initialized)
		{
			return;
		}
		TableauView tableauView = View;
		if (tableauView == null)
		{
			tableauView = sender.TableauView;
			tableauView.SetEnable(_isEnabled);
		}
		if (_itemRosterElement.EquipmentElement.Item == null)
		{
			tableauView.SetContinuousRendering(value: false);
			tableauView.SetDeleteAfterRendering(value: true);
			return;
		}
		tableauView.SetRenderWithPostfx(value: true);
		tableauView.SetClearColor(0u);
		tableauView.SetScene(_tableauScene);
		if (_camera == null)
		{
			_camera = Camera.CreateCamera();
			_camera.SetViewVolume(perspective: true, -0.5f, 0.5f, -0.5f, 0.5f, 0.01f, 100f);
			ResetCamera();
			tableauView.SetSceneUsesSkybox(value: false);
		}
		tableauView.SetCamera(_camera);
		if (_isRotatingByDefault)
		{
			UpdateRotation(1f, 0f);
		}
		tableauView.SetDeleteAfterRendering(value: false);
		tableauView.SetContinuousRendering(value: true);
	}

	private void MakeCameraLookMidPoint()
	{
		Vec3 vec = _camera.Frame.rotation.TransformToParent(in _curCamDisplacement);
		Vec3 vec2 = _midPoint + vec;
		float num = _midPoint.Length * 0.5263158f;
		Vec3 position = vec2 - _camera.Direction * num;
		_camera.Position = position;
	}

	private void SetCamFovHorizontal(float camFov)
	{
		_camera.SetFovHorizontal(camFov, 1f, 0.1f, 50f);
	}

	private void UpdateMouseLock(bool forceUnlock = false)
	{
		_lockMouse = (_isRotating || _isTranslating) && !forceUnlock;
		MouseManager.LockCursorAtCurrentPosition(_lockMouse);
		MouseManager.ShowCursor(!_lockMouse);
	}

	private void TickCameraZoom(float dt)
	{
		if (_camera != null)
		{
			float horizontalFov = _camera.HorizontalFov;
			horizontalFov += _curZoomSpeed;
			float minValue = 0.61086524f;
			float maxValue = System.MathF.PI * 13f / 36f;
			horizontalFov = MBMath.ClampFloat(horizontalFov, minValue, maxValue);
			SetCamFovHorizontal(horizontalFov);
			if (dt > 0f)
			{
				_curZoomSpeed = MBMath.Lerp(_curZoomSpeed, 0f, MBMath.ClampFloat(dt * 25.9f, 0f, 1f));
			}
		}
	}
}
