using System;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.View.Tableaus.Thumbnails;

public class CraftingPieceThumbnailCache : ThumbnailCache<CraftingPieceCreationData>
{
	private int _itemTableauGPUAllocationIndex;

	public CraftingPieceThumbnailCache(int capacity)
		: base(capacity)
	{
	}

	protected override void OnInitialize()
	{
		base.OnInitialize();
		_itemTableauGPUAllocationIndex = Utilities.RegisterGPUAllocationGroup("CraftingPieceThumbnailCache");
	}

	protected override void OnFinalize()
	{
		base.OnFinalize();
	}

	protected override TextureCreationInfo OnCreateTexture(CraftingPieceCreationData thumbnailCreationData)
	{
		CraftingPiece craftingPiece = thumbnailCreationData.CraftingPiece;
		string type = thumbnailCreationData.Type;
		Action<Texture> setAction = thumbnailCreationData.SetAction;
		Action cancelAction = thumbnailCreationData.CancelAction;
		string text = craftingPiece.StringId + "$" + type;
		if (((IThumbnailCache)this).GetValue(text, out Texture texture))
		{
			if (_renderCallbacks.ContainsKey(text))
			{
				_renderCallbacks[text].SetActions.Add(setAction);
				_renderCallbacks[text].CancelActions.Add(cancelAction);
			}
			else
			{
				setAction?.Invoke(texture);
			}
			((IThumbnailCache)this).AddReference(text);
			return TextureCreationInfo.WithExistingTexture(texture);
		}
		Camera camera = null;
		int num = 2;
		int width = 256;
		int height = 180;
		GameEntity gameEntity = CreateCraftingPieceBaseEntity(craftingPiece, type, BannerlordTableauManager.TableauCharacterScenes[num], ref camera);
		string debugName = ThumbnailCache<CraftingPieceCreationData>.CreateDebugIdFrom(text, "crf");
		ThumbnailRenderRequest request = ThumbnailRenderRequest.CreateWithoutTexture(BannerlordTableauManager.TableauCharacterScenes[num], camera, gameEntity, text, width, height, debugName, _itemTableauGPUAllocationIndex);
		_thumbnailCreatorView.RegisterRenderRequest(ref request);
		gameEntity.ManualInvalidate();
		((IThumbnailCache)this).Add(text, (Texture)null);
		((IThumbnailCache)this).AddReference(text);
		if (!_renderCallbacks.ContainsKey(text))
		{
			_renderCallbacks.Add(text, RenderCallbackCollection.CreateEmpty());
		}
		_renderCallbacks[text].SetActions.Add(setAction);
		_renderCallbacks[text].CancelActions.Add(cancelAction);
		return TextureCreationInfo.WithNewTexture();
	}

	protected override bool OnReleaseTexture(CraftingPieceCreationData thumbnailCreationData)
	{
		string renderId = thumbnailCreationData.RenderId;
		return ((IThumbnailCache)this).RemoveReference(renderId);
	}

	private GameEntity CreateCraftingPieceBaseEntity(CraftingPiece craftingPiece, string ItemType, Scene scene, ref Camera camera)
	{
		MatrixFrame placementFrame = MatrixFrame.Identity;
		bool flag = false;
		string tag = "craftingPiece_cam";
		string tag2 = "craftingPiece_frame";
		if (craftingPiece.PieceType == CraftingPiece.PieceTypes.Blade)
		{
			switch (ItemType)
			{
			case "OneHandedAxe":
			case "ThrowingAxe":
				tag = "craft_axe_camera";
				tag2 = "craft_axe";
				break;
			case "TwoHandedAxe":
				tag = "craft_big_axe_camera";
				tag2 = "craft_big_axe";
				break;
			case "Dagger":
			case "ThrowingKnife":
			case "TwoHandedPolearm":
			case "Pike":
			case "Javelin":
				tag = "craft_spear_blade_camera";
				tag2 = "craft_spear_blade";
				break;
			case "Mace":
			case "TwoHandedMace":
				tag = "craft_mace_camera";
				tag2 = "craft_mace";
				break;
			default:
				tag = "craft_blade_camera";
				tag2 = "craft_blade";
				break;
			}
			flag = true;
		}
		else if (craftingPiece.PieceType == CraftingPiece.PieceTypes.Pommel)
		{
			tag = "craft_pommel_camera";
			tag2 = "craft_pommel";
			flag = true;
		}
		else if (craftingPiece.PieceType == CraftingPiece.PieceTypes.Guard)
		{
			tag = "craft_guard_camera";
			tag2 = "craft_guard";
			flag = true;
		}
		else if (craftingPiece.PieceType == CraftingPiece.PieceTypes.Handle)
		{
			tag = "craft_handle_camera";
			tag2 = "craft_handle";
			flag = true;
		}
		bool flag2 = false;
		if (flag)
		{
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
				placementFrame = gameEntity2.GetGlobalFrame();
				gameEntity2.SetVisibilityExcludeParents(visible: false);
				flag2 = true;
			}
		}
		else
		{
			GameEntity gameEntity3 = scene.FindEntityWithTag("old_system_item_frame");
			if (gameEntity3 != null)
			{
				placementFrame = gameEntity3.GetGlobalFrame();
				gameEntity3.SetVisibilityExcludeParents(visible: false);
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
		if (!flag2)
		{
			placementFrame = craftingPiece.GetCraftingPieceFrameForInventory();
		}
		MetaMesh copy = MetaMesh.GetCopy(craftingPiece.MeshName);
		GameEntity gameEntity4 = null;
		if (copy != null)
		{
			gameEntity4 = scene.AddItemEntity(ref placementFrame, copy);
		}
		else
		{
			MBDebug.ShowWarning("[DEBUG]craftingPiece with " + craftingPiece.StringId + "[DEBUG] string id cannot be found");
		}
		gameEntity4.SetVisibilityExcludeParents(visible: false);
		return gameEntity4;
	}
}
