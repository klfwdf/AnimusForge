using System;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.View.Tableaus.Thumbnails;

public class ItemThumbnailCache : ThumbnailCache<ItemThumbnailCreationData>
{
	private struct CustomPoseParameters
	{
		public enum Alignment
		{
			Center,
			Top,
			Bottom
		}

		public string CameraTag;

		public string FrameTag;

		public float DistanceModifier;

		public Alignment FocusAlignment;
	}

	private int _itemTableauGPUAllocationIndex;

	public ItemThumbnailCache(int capacity)
		: base(capacity)
	{
	}

	protected override void OnInitialize()
	{
		base.OnInitialize();
		_itemTableauGPUAllocationIndex = Utilities.RegisterGPUAllocationGroup("ItemTableauCache");
	}

	protected override void OnFinalize()
	{
		base.OnFinalize();
	}

	protected override TextureCreationInfo OnCreateTexture(ItemThumbnailCreationData thumbnailCreationData)
	{
		ItemObject itemObject = thumbnailCreationData.ItemObject;
		_ = thumbnailCreationData.AdditionalArgs;
		Action<Texture> setAction = thumbnailCreationData.SetAction;
		Action cancelAction = thumbnailCreationData.CancelAction;
		string renderIdToUse = GetRenderIdToUse(thumbnailCreationData);
		if (((IThumbnailCache)this).GetValue(renderIdToUse, out Texture texture))
		{
			if (_renderCallbacks.ContainsKey(renderIdToUse))
			{
				_renderCallbacks[renderIdToUse].SetActions.Add(setAction);
				_renderCallbacks[renderIdToUse].CancelActions.Add(cancelAction);
			}
			else
			{
				setAction?.Invoke(texture);
			}
			((IThumbnailCache)this).AddReference(renderIdToUse);
			return TextureCreationInfo.WithExistingTexture(texture);
		}
		Camera camera = null;
		int num = 2;
		int width = 256;
		int height = 120;
		GameEntity gameEntity = CreateItemBaseEntity(itemObject, BannerlordTableauManager.TableauCharacterScenes[num], ref camera);
		string debugName = ThumbnailCache<ItemThumbnailCreationData>.CreateDebugIdFrom(renderIdToUse, "itm");
		ThumbnailRenderRequest request = ThumbnailRenderRequest.CreateWithoutTexture(BannerlordTableauManager.TableauCharacterScenes[num], camera, gameEntity, renderIdToUse, width, height, debugName, _itemTableauGPUAllocationIndex);
		_thumbnailCreatorView.RegisterRenderRequest(ref request);
		gameEntity.ManualInvalidate();
		((IThumbnailCache)this).Add(renderIdToUse, (Texture)null);
		((IThumbnailCache)this).AddReference(renderIdToUse);
		if (!_renderCallbacks.ContainsKey(renderIdToUse))
		{
			_renderCallbacks.Add(renderIdToUse, RenderCallbackCollection.CreateEmpty());
		}
		_renderCallbacks[renderIdToUse].SetActions.Add(setAction);
		_renderCallbacks[renderIdToUse].CancelActions.Add(cancelAction);
		return TextureCreationInfo.WithNewTexture();
	}

	protected override bool OnReleaseTexture(ItemThumbnailCreationData thumbnailCreationData)
	{
		string renderIdToUse = GetRenderIdToUse(thumbnailCreationData);
		return ((IThumbnailCache)this).RemoveReference(renderIdToUse);
	}

	private string GetRenderIdToUse(ItemThumbnailCreationData thumbnailCreationData)
	{
		ItemObject itemObject = thumbnailCreationData.ItemObject;
		string additionalArgs = thumbnailCreationData.AdditionalArgs;
		_ = thumbnailCreationData.SetAction;
		string text = itemObject.StringId;
		if (itemObject.Type == ItemObject.ItemTypeEnum.Shield)
		{
			text = text + "_" + additionalArgs;
		}
		return text;
	}

	private GameEntity CreateItemBaseEntity(ItemObject item, Scene scene, ref Camera camera)
	{
		MatrixFrame itemFrame = MatrixFrame.Identity;
		MatrixFrame itemFrame2 = MatrixFrame.Identity;
		MatrixFrame itemFrame3 = MatrixFrame.Identity;
		GetItemPoseAndCamera(item, scene, ref camera, ref itemFrame, ref itemFrame2, ref itemFrame3);
		return AddItem(scene, item, itemFrame, itemFrame2, itemFrame3);
	}

	private void GetItemPoseAndCamera(ItemObject item, Scene scene, ref Camera camera, ref MatrixFrame itemFrame, ref MatrixFrame itemFrame1, ref MatrixFrame itemFrame2)
	{
		if (item.IsCraftedWeapon)
		{
			GetItemPoseAndCameraForCraftedItem(item, scene, ref camera, ref itemFrame, ref itemFrame1, ref itemFrame2);
			return;
		}
		string text = "";
		CustomPoseParameters customPoseParameters = new CustomPoseParameters
		{
			CameraTag = "goods_cam",
			DistanceModifier = 6f,
			FrameTag = "goods_frame"
		};
		if (item.WeaponComponent != null)
		{
			WeaponClass weaponClass = item.WeaponComponent.PrimaryWeapon.WeaponClass;
			if ((uint)(weaponClass - 2) <= 1u)
			{
				text = "sword";
			}
		}
		else
		{
			switch (item.Type)
			{
			case ItemObject.ItemTypeEnum.HeadArmor:
				text = "helmet";
				break;
			case ItemObject.ItemTypeEnum.BodyArmor:
				text = "armor";
				break;
			}
		}
		if (item.Type == ItemObject.ItemTypeEnum.Shield)
		{
			text = "shield";
		}
		if (item.Type == ItemObject.ItemTypeEnum.Sling)
		{
			text = "sling";
		}
		if (item.Type == ItemObject.ItemTypeEnum.SlingStones)
		{
			text = "slingstones";
		}
		if (item.Type == ItemObject.ItemTypeEnum.Crossbow)
		{
			text = "crossbow";
		}
		if (item.Type == ItemObject.ItemTypeEnum.Bow)
		{
			text = "bow";
		}
		if (item.Type == ItemObject.ItemTypeEnum.LegArmor)
		{
			text = "boot";
		}
		if (item.Type == ItemObject.ItemTypeEnum.Horse)
		{
			text = ((HorseComponent)item.ItemComponent).Monster.MonsterUsage;
		}
		if (item.Type == ItemObject.ItemTypeEnum.HorseHarness)
		{
			text = "horse";
		}
		if (item.Type == ItemObject.ItemTypeEnum.Cape)
		{
			text = "cape";
		}
		if (item.Type == ItemObject.ItemTypeEnum.HandArmor)
		{
			text = "glove";
		}
		if (item.Type == ItemObject.ItemTypeEnum.Arrows)
		{
			text = "arrow";
		}
		if (item.Type == ItemObject.ItemTypeEnum.Bolts)
		{
			text = "bolt";
		}
		if (item.Type == ItemObject.ItemTypeEnum.Banner)
		{
			customPoseParameters = new CustomPoseParameters
			{
				CameraTag = "banner_cam",
				DistanceModifier = 1.5f,
				FrameTag = "banner_frame",
				FocusAlignment = CustomPoseParameters.Alignment.Top
			};
		}
		if (item.Type == ItemObject.ItemTypeEnum.Animal)
		{
			customPoseParameters = new CustomPoseParameters
			{
				CameraTag = customPoseParameters.CameraTag,
				DistanceModifier = 3f,
				FrameTag = customPoseParameters.FrameTag
			};
		}
		if (item.StringId == "iron" || item.StringId == "hardwood" || item.StringId == "charcoal" || item.StringId == "ironIngot1" || item.StringId == "ironIngot2" || item.StringId == "ironIngot3" || item.StringId == "ironIngot4" || item.StringId == "ironIngot5" || item.StringId == "ironIngot6" || item.ItemCategory == DefaultItemCategories.Silver)
		{
			text = "craftmat";
		}
		if (!string.IsNullOrEmpty(text))
		{
			string tag = text + "_cam";
			string tag2 = text + "_frame";
			GameEntity gameEntity = scene.FindEntityWithTag(tag);
			if (gameEntity != null)
			{
				camera = Camera.CreateCamera();
				Vec3 dofParams = default(Vec3);
				gameEntity.GetCameraParamsFromCameraScript(camera, ref dofParams);
			}
			GameEntity gameEntity2 = scene.FindEntityWithTag(tag2);
			if (gameEntity2 != null)
			{
				itemFrame = gameEntity2.GetGlobalFrame();
				gameEntity2.SetVisibilityExcludeParents(visible: false);
			}
		}
		else
		{
			GameEntity gameEntity3 = scene.FindEntityWithTag(customPoseParameters.CameraTag);
			if (gameEntity3 != null)
			{
				camera = Camera.CreateCamera();
				Vec3 dofParams2 = default(Vec3);
				gameEntity3.GetCameraParamsFromCameraScript(camera, ref dofParams2);
			}
			GameEntity gameEntity4 = scene.FindEntityWithTag(customPoseParameters.FrameTag);
			if (gameEntity4 != null)
			{
				itemFrame = gameEntity4.GetGlobalFrame();
				gameEntity4.SetVisibilityExcludeParents(visible: false);
				gameEntity4.UpdateGlobalBounds();
				MatrixFrame globalFrame = gameEntity4.GetGlobalFrame();
				MetaMesh itemMeshForInventory = new ItemRosterElement(item).GetItemMeshForInventory();
				Vec3 vec = new Vec3(1000000f, 1000000f, 1000000f);
				Vec3 vec2 = new Vec3(-1000000f, -1000000f, -1000000f);
				if (itemMeshForInventory != null)
				{
					_ = MatrixFrame.Identity;
					for (int i = 0; i != itemMeshForInventory.MeshCount; i++)
					{
						Vec3 boundingBoxMin = itemMeshForInventory.GetMeshAtIndex(i).GetBoundingBoxMin();
						Vec3 boundingBoxMax = itemMeshForInventory.GetMeshAtIndex(i).GetBoundingBoxMax();
						Vec3[] array = new Vec3[8]
						{
							globalFrame.TransformToParent(new Vec3(boundingBoxMin.x, boundingBoxMin.y, boundingBoxMin.z)),
							globalFrame.TransformToParent(new Vec3(boundingBoxMin.x, boundingBoxMin.y, boundingBoxMax.z)),
							globalFrame.TransformToParent(new Vec3(boundingBoxMin.x, boundingBoxMax.y, boundingBoxMin.z)),
							globalFrame.TransformToParent(new Vec3(boundingBoxMin.x, boundingBoxMax.y, boundingBoxMax.z)),
							globalFrame.TransformToParent(new Vec3(boundingBoxMax.x, boundingBoxMin.y, boundingBoxMin.z)),
							globalFrame.TransformToParent(new Vec3(boundingBoxMax.x, boundingBoxMin.y, boundingBoxMax.z)),
							globalFrame.TransformToParent(new Vec3(boundingBoxMax.x, boundingBoxMax.y, boundingBoxMin.z)),
							globalFrame.TransformToParent(new Vec3(boundingBoxMax.x, boundingBoxMax.y, boundingBoxMax.z))
						};
						for (int j = 0; j < 8; j++)
						{
							vec = Vec3.Vec3Min(vec, array[j]);
							vec2 = Vec3.Vec3Max(vec2, array[j]);
						}
					}
				}
				Vec3 v = (vec + vec2) * 0.5f;
				Vec3 vec3 = gameEntity4.GetGlobalFrame().TransformToLocal(in v);
				MatrixFrame globalFrame2 = gameEntity4.GetGlobalFrame();
				globalFrame2.origin -= vec3;
				itemFrame = globalFrame2;
				MatrixFrame frame = camera.Frame;
				float num = (vec2 - vec).Length * customPoseParameters.DistanceModifier;
				frame.origin += frame.rotation.u * num;
				if (customPoseParameters.FocusAlignment == CustomPoseParameters.Alignment.Top)
				{
					frame.origin += new Vec3(0f, 0f, (vec2 - vec).Z * 0.3f);
				}
				else if (customPoseParameters.FocusAlignment == CustomPoseParameters.Alignment.Bottom)
				{
					frame.origin -= new Vec3(0f, 0f, (vec2 - vec).Z * 0.3f);
				}
				camera.Frame = frame;
			}
		}
		if (camera == null)
		{
			camera = Camera.CreateCamera();
			camera.SetViewVolume(perspective: false, -1f, 1f, -0.5f, 0.5f, 0.01f, 100f);
			MatrixFrame identity = MatrixFrame.Identity;
			identity.origin -= identity.rotation.u * 7f;
			identity.rotation.u *= -1f;
			camera.Frame = identity;
		}
		if (item.Type == ItemObject.ItemTypeEnum.Shield)
		{
			GameEntity gameEntity5 = scene.FindEntityWithTag("shield_cam");
			itemFrame.rotation = MBItem.GetHolsterFrameByIndex(MBItem.GetItemHolsterIndex(item.ItemHolsters[0])).rotation;
			MatrixFrame frame2 = itemFrame.TransformToParent(gameEntity5.GetFrame());
			camera.Frame = frame2;
		}
		if (item.Type == ItemObject.ItemTypeEnum.Banner && item.StringId == "dragon_banner_center")
		{
			itemFrame.rotation.RotateAboutUp(System.MathF.PI);
		}
	}

	private GameEntity AddItem(Scene scene, ItemObject item, MatrixFrame itemFrame, MatrixFrame itemFrame1, MatrixFrame itemFrame2)
	{
		ItemRosterElement rosterElement = new ItemRosterElement(item);
		MetaMesh itemMeshForInventory = rosterElement.GetItemMeshForInventory();
		if (item.IsCraftedWeapon)
		{
			MatrixFrame frame = itemMeshForInventory.Frame;
			frame.Elevate((0f - item.WeaponDesign.CraftedWeaponLength) / 2f);
			itemMeshForInventory.Frame = frame;
		}
		GameEntity gameEntity = null;
		if (itemMeshForInventory != null && rosterElement.EquipmentElement.Item.ItemType == ItemObject.ItemTypeEnum.HandArmor)
		{
			gameEntity = GameEntity.CreateEmpty(scene);
			AnimationSystemData animationSystemData = Game.Current.DefaultMonster.FillAnimationSystemData(MBActionSet.GetActionSet(Game.Current.DefaultMonster.ActionSetCode), 1f, hasClippingPlane: false);
			gameEntity.CreateSkeletonWithActionSet(ref animationSystemData);
			gameEntity.SetFrame(ref itemFrame);
			gameEntity.Skeleton.SetAgentActionChannel(0, in ActionIndexCache.act_tableau_hand_armor_pose);
			gameEntity.AddMultiMeshToSkeleton(itemMeshForInventory);
			gameEntity.Skeleton.TickAnimationsAndForceUpdate(0.01f, itemFrame, tickAnimsForChildren: true);
		}
		else if (itemMeshForInventory != null)
		{
			if (item.WeaponComponent != null)
			{
				WeaponClass weaponClass = item.WeaponComponent.PrimaryWeapon.WeaponClass;
				if (weaponClass == WeaponClass.ThrowingAxe || weaponClass == WeaponClass.ThrowingKnife || weaponClass == WeaponClass.Javelin || weaponClass == WeaponClass.Bolt)
				{
					gameEntity = GameEntity.CreateEmpty(scene);
					MetaMesh metaMesh = itemMeshForInventory.CreateCopy();
					metaMesh.Frame = itemFrame;
					gameEntity.AddMultiMesh(metaMesh);
					MetaMesh metaMesh2 = itemMeshForInventory.CreateCopy();
					metaMesh2.Frame = itemFrame1;
					gameEntity.AddMultiMesh(metaMesh2);
					MetaMesh metaMesh3 = itemMeshForInventory.CreateCopy();
					metaMesh3.Frame = itemFrame2;
					gameEntity.AddMultiMesh(metaMesh3);
				}
				else
				{
					gameEntity = scene.AddItemEntity(ref itemFrame, itemMeshForInventory);
				}
			}
			else
			{
				gameEntity = scene.AddItemEntity(ref itemFrame, itemMeshForInventory);
				if (item.Type == ItemObject.ItemTypeEnum.HorseHarness && item.ArmorComponent != null)
				{
					MetaMesh copy = MetaMesh.GetCopy(item.ArmorComponent.ReinsMesh, showErrors: true, mayReturnNull: true);
					if (copy != null)
					{
						gameEntity.AddMultiMesh(copy);
					}
				}
			}
		}
		else
		{
			MBDebug.ShowWarning("[DEBUG]Item with " + rosterElement.EquipmentElement.Item.StringId + "[DEBUG] string id cannot be found");
		}
		gameEntity.SetVisibilityExcludeParents(visible: false);
		return gameEntity;
	}

	private void GetItemPoseAndCameraForCraftedItem(ItemObject item, Scene scene, ref Camera camera, ref MatrixFrame itemFrame, ref MatrixFrame itemFrame1, ref MatrixFrame itemFrame2)
	{
		if (camera == null)
		{
			camera = Camera.CreateCamera();
		}
		itemFrame = MatrixFrame.Identity;
		WeaponClass weaponClass = item.WeaponDesign.Template.WeaponDescriptions[0].WeaponClass;
		Vec3 u = itemFrame.rotation.u;
		Vec3 vec = itemFrame.origin - u * (item.WeaponDesign.CraftedWeaponLength * 0.5f);
		Vec3 v = vec + u * item.WeaponDesign.CraftedWeaponLength;
		Vec3 v2 = vec - u * item.WeaponDesign.BottomPivotOffset;
		int num = 0;
		Vec3 v3 = default(Vec3);
		foreach (float topPivotOffset in item.WeaponDesign.TopPivotOffsets)
		{
			if (!(topPivotOffset <= TaleWorlds.Library.MathF.Abs(1E-05f)))
			{
				Vec3 vec2 = vec + u * topPivotOffset;
				if (num == 1)
				{
					v3 = vec2;
				}
				_ = 2;
				num++;
			}
		}
		if (weaponClass == WeaponClass.OneHandedSword || weaponClass == WeaponClass.TwoHandedSword)
		{
			GameEntity gameEntity = scene.FindEntityWithTag("sword_camera");
			Vec3 dofParams = default(Vec3);
			gameEntity.GetCameraParamsFromCameraScript(camera, ref dofParams);
			gameEntity.SetVisibilityExcludeParents(visible: false);
			Vec3 vec3 = itemFrame.TransformToLocal(in v2);
			MatrixFrame m = MatrixFrame.Identity;
			m.origin = -vec3;
			GameEntity gameEntity2 = scene.FindEntityWithTag("sword");
			gameEntity2.SetVisibilityExcludeParents(visible: false);
			itemFrame = gameEntity2.GetGlobalFrame();
			itemFrame = itemFrame.TransformToParent(in m);
		}
		if (weaponClass == WeaponClass.OneHandedAxe || weaponClass == WeaponClass.TwoHandedAxe)
		{
			GameEntity gameEntity3 = scene.FindEntityWithTag("axe_camera");
			Vec3 dofParams2 = default(Vec3);
			gameEntity3.GetCameraParamsFromCameraScript(camera, ref dofParams2);
			gameEntity3.SetVisibilityExcludeParents(visible: false);
			Vec3 vec4 = itemFrame.TransformToLocal(in v3);
			MatrixFrame m2 = MatrixFrame.Identity;
			m2.origin = -vec4;
			GameEntity gameEntity4 = scene.FindEntityWithTag("axe");
			gameEntity4.SetVisibilityExcludeParents(visible: false);
			itemFrame = gameEntity4.GetGlobalFrame();
			itemFrame = itemFrame.TransformToParent(in m2);
		}
		if (weaponClass == WeaponClass.Dagger)
		{
			GameEntity gameEntity5 = scene.FindEntityWithTag("sword_camera");
			Vec3 dofParams3 = default(Vec3);
			gameEntity5.GetCameraParamsFromCameraScript(camera, ref dofParams3);
			gameEntity5.SetVisibilityExcludeParents(visible: false);
			Vec3 vec5 = itemFrame.TransformToLocal(in v2);
			MatrixFrame m3 = MatrixFrame.Identity;
			m3.origin = -vec5;
			GameEntity gameEntity6 = scene.FindEntityWithTag("sword");
			gameEntity6.SetVisibilityExcludeParents(visible: false);
			itemFrame = gameEntity6.GetGlobalFrame();
			itemFrame = itemFrame.TransformToParent(in m3);
		}
		if (weaponClass == WeaponClass.ThrowingAxe)
		{
			GameEntity gameEntity7 = scene.FindEntityWithTag("throwing_axe_camera");
			Vec3 dofParams4 = default(Vec3);
			gameEntity7.GetCameraParamsFromCameraScript(camera, ref dofParams4);
			gameEntity7.SetVisibilityExcludeParents(visible: false);
			Vec3 vec6 = itemFrame.TransformToLocal(vec + u * item.PrimaryWeapon.CenterOfMass);
			MatrixFrame m4 = MatrixFrame.Identity;
			m4.origin = -vec6 * 2.5f;
			GameEntity gameEntity8 = scene.FindEntityWithTag("throwing_axe");
			gameEntity8.SetVisibilityExcludeParents(visible: false);
			itemFrame = gameEntity8.GetGlobalFrame();
			itemFrame = itemFrame.TransformToParent(in m4);
			gameEntity8 = scene.FindEntityWithTag("throwing_axe_1");
			gameEntity8.SetVisibilityExcludeParents(visible: false);
			itemFrame1 = gameEntity8.GetGlobalFrame();
			itemFrame1 = itemFrame1.TransformToParent(in m4);
			gameEntity8 = scene.FindEntityWithTag("throwing_axe_2");
			gameEntity8.SetVisibilityExcludeParents(visible: false);
			itemFrame2 = gameEntity8.GetGlobalFrame();
			itemFrame2 = itemFrame2.TransformToParent(in m4);
		}
		if (weaponClass == WeaponClass.Javelin)
		{
			GameEntity gameEntity9 = scene.FindEntityWithTag("javelin_camera");
			Vec3 dofParams5 = default(Vec3);
			gameEntity9.GetCameraParamsFromCameraScript(camera, ref dofParams5);
			gameEntity9.SetVisibilityExcludeParents(visible: false);
			Vec3 vec7 = itemFrame.TransformToLocal(in v3);
			MatrixFrame m5 = MatrixFrame.Identity;
			m5.origin = -vec7 * 2.2f;
			GameEntity gameEntity10 = scene.FindEntityWithTag("javelin");
			gameEntity10.SetVisibilityExcludeParents(visible: false);
			itemFrame = gameEntity10.GetGlobalFrame();
			itemFrame = itemFrame.TransformToParent(in m5);
			gameEntity10 = scene.FindEntityWithTag("javelin_1");
			gameEntity10.SetVisibilityExcludeParents(visible: false);
			itemFrame1 = gameEntity10.GetGlobalFrame();
			itemFrame1 = itemFrame1.TransformToParent(in m5);
			gameEntity10 = scene.FindEntityWithTag("javelin_2");
			gameEntity10.SetVisibilityExcludeParents(visible: false);
			itemFrame2 = gameEntity10.GetGlobalFrame();
			itemFrame2 = itemFrame2.TransformToParent(in m5);
		}
		if (weaponClass == WeaponClass.ThrowingKnife)
		{
			GameEntity gameEntity11 = scene.FindEntityWithTag("javelin_camera");
			Vec3 dofParams6 = default(Vec3);
			gameEntity11.GetCameraParamsFromCameraScript(camera, ref dofParams6);
			gameEntity11.SetVisibilityExcludeParents(visible: false);
			Vec3 vec8 = itemFrame.TransformToLocal(in v);
			MatrixFrame m6 = MatrixFrame.Identity;
			m6.origin = -vec8 * 1.4f;
			GameEntity gameEntity12 = scene.FindEntityWithTag("javelin");
			gameEntity12.SetVisibilityExcludeParents(visible: false);
			itemFrame = gameEntity12.GetGlobalFrame();
			itemFrame = itemFrame.TransformToParent(in m6);
			gameEntity12 = scene.FindEntityWithTag("javelin_1");
			gameEntity12.SetVisibilityExcludeParents(visible: false);
			itemFrame1 = gameEntity12.GetGlobalFrame();
			itemFrame1 = itemFrame1.TransformToParent(in m6);
			gameEntity12 = scene.FindEntityWithTag("javelin_2");
			gameEntity12.SetVisibilityExcludeParents(visible: false);
			itemFrame2 = gameEntity12.GetGlobalFrame();
			itemFrame2 = itemFrame2.TransformToParent(in m6);
		}
		if (weaponClass == WeaponClass.TwoHandedPolearm || weaponClass == WeaponClass.OneHandedPolearm || weaponClass == WeaponClass.LowGripPolearm || weaponClass == WeaponClass.Mace || weaponClass == WeaponClass.TwoHandedMace)
		{
			GameEntity gameEntity13 = scene.FindEntityWithTag("spear_camera");
			Vec3 dofParams7 = default(Vec3);
			gameEntity13.GetCameraParamsFromCameraScript(camera, ref dofParams7);
			gameEntity13.SetVisibilityExcludeParents(visible: false);
			Vec3 vec9 = itemFrame.TransformToLocal(in v3);
			MatrixFrame m7 = MatrixFrame.Identity;
			m7.origin = -vec9;
			GameEntity gameEntity14 = scene.FindEntityWithTag("spear");
			gameEntity14.SetVisibilityExcludeParents(visible: false);
			itemFrame = gameEntity14.GetGlobalFrame();
			itemFrame = itemFrame.TransformToParent(in m7);
		}
	}
}
