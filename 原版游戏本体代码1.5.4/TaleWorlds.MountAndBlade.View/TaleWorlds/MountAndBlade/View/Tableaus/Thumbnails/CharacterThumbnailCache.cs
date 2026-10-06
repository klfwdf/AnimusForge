using System;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.View.Scripts;

namespace TaleWorlds.MountAndBlade.View.Tableaus.Thumbnails;

public class CharacterThumbnailCache : ThumbnailCache<CharacterThumbnailCreationData>
{
	private int _characterCount;

	private int _characterTableauGPUAllocationIndex;

	public CharacterThumbnailCache(int capacity)
		: base(capacity)
	{
	}

	protected override void OnInitialize()
	{
		base.OnInitialize();
		_characterTableauGPUAllocationIndex = Utilities.RegisterGPUAllocationGroup("CharacterTableauCache");
	}

	protected override void OnFinalize()
	{
		base.OnFinalize();
	}

	protected override TextureCreationInfo OnCreateTexture(CharacterThumbnailCreationData thumbnailCreationData)
	{
		string renderId = thumbnailCreationData.RenderId;
		CharacterCode characterCode = thumbnailCreationData.CharacterCode;
		bool isBig = thumbnailCreationData.IsBig;
		Action<Texture> setAction = thumbnailCreationData.SetAction;
		Action cancelAction = thumbnailCreationData.CancelAction;
		int customSizeX = thumbnailCreationData.CustomSizeX;
		int customSizeY = thumbnailCreationData.CustomSizeY;
		if (((IThumbnailCache)this).GetValue(renderId, out Texture texture))
		{
			if (_renderCallbacks.ContainsKey(renderId))
			{
				_renderCallbacks[renderId].SetActions.Add(setAction);
				_renderCallbacks[renderId].CancelActions.Add(cancelAction);
			}
			else
			{
				setAction?.Invoke(texture);
			}
			((IThumbnailCache)this).AddReference(renderId);
			return TextureCreationInfo.WithExistingTexture(texture);
		}
		Camera camera = null;
		int num = ((!isBig) ? 4 : 0);
		GameEntity poseEntity = CreateCharacterBaseEntity(characterCode, BannerlordTableauManager.TableauCharacterScenes[num], ref camera, isBig);
		poseEntity = FillEntityWithPose(characterCode, poseEntity, BannerlordTableauManager.TableauCharacterScenes[num]);
		int width = 256;
		int height = (isBig ? 120 : 174);
		if (customSizeX > 0)
		{
			width = customSizeX;
		}
		if (customSizeY > 0)
		{
			height = customSizeY;
		}
		string debugName = ThumbnailCache<CharacterThumbnailCreationData>.CreateDebugIdFrom(renderId, "cha");
		ThumbnailRenderRequest request = ThumbnailRenderRequest.CreateWithoutTexture(BannerlordTableauManager.TableauCharacterScenes[num], camera, poseEntity, renderId, width, height, debugName, _characterTableauGPUAllocationIndex);
		_thumbnailCreatorView.RegisterRenderRequest(ref request);
		poseEntity.ManualInvalidate();
		_characterCount++;
		((IThumbnailCache)this).Add(renderId, (Texture)null);
		((IThumbnailCache)this).AddReference(renderId);
		if (!_renderCallbacks.ContainsKey(renderId))
		{
			_renderCallbacks.Add(renderId, RenderCallbackCollection.CreateEmpty());
		}
		_renderCallbacks[renderId].SetActions.Add(setAction);
		_renderCallbacks[renderId].CancelActions.Add(cancelAction);
		return TextureCreationInfo.WithNewTexture();
	}

	protected override bool OnReleaseTexture(CharacterThumbnailCreationData thumbnailCreationData)
	{
		string renderId = thumbnailCreationData.RenderId;
		return ((IThumbnailCache)this).RemoveReference(renderId);
	}

	private GameEntity CreateCharacterBaseEntity(CharacterCode characterCode, Scene scene, ref Camera camera, bool isBig)
	{
		GetPoseParamsFromCharacterCode(characterCode, out var poseName, out var _);
		string tag = poseName + "_pose";
		string tag2 = (isBig ? (poseName + "_cam") : (poseName + "_cam_small"));
		WeakGameEntity weakGameEntity = scene.FindWeakEntityWithTag(tag);
		if (weakGameEntity == null)
		{
			return null;
		}
		weakGameEntity.SetVisibilityExcludeParents(visible: true);
		GameEntity gameEntity = GameEntity.CopyFromPrefab(weakGameEntity);
		gameEntity.Name = weakGameEntity.Name + "Instance";
		gameEntity.RemoveTag(tag);
		scene.AttachEntity(gameEntity);
		gameEntity.SetVisibilityExcludeParents(visible: true);
		weakGameEntity.SetVisibilityExcludeParents(visible: false);
		WeakGameEntity weakGameEntity2 = scene.FindWeakEntityWithTag(tag2);
		Vec3 dofParams = default(Vec3);
		camera = Camera.CreateCamera();
		if (weakGameEntity2 != null)
		{
			weakGameEntity2.GetCameraParamsFromCameraScript(camera, ref dofParams);
			camera.Frame = weakGameEntity2.GetGlobalFrame();
		}
		return gameEntity;
	}

	private void GetPoseParamsFromCharacterCode(CharacterCode characterCode, out string poseName, out bool hasHorse)
	{
		hasHorse = false;
		if (characterCode.IsHero)
		{
			int num = MBRandom.NondeterministicRandomInt % 8;
			poseName = "lord_" + num;
			return;
		}
		poseName = "troop_villager";
		int num2 = -1;
		int num3 = -1;
		Equipment equipment = characterCode.CalculateEquipment();
		switch (characterCode.FormationClass)
		{
		case FormationClass.Infantry:
		case FormationClass.Cavalry:
		case FormationClass.NumberOfDefaultFormations:
		case FormationClass.HeavyInfantry:
		case FormationClass.LightCavalry:
		case FormationClass.HeavyCavalry:
		case FormationClass.NumberOfRegularFormations:
		case FormationClass.Bodyguard:
		{
			for (int j = 0; j < 4; j++)
			{
				if (equipment[j].Item?.PrimaryWeapon != null)
				{
					if (num3 == -1 && equipment[j].Item.ItemFlags.HasAnyFlag(ItemFlags.HeldInOffHand))
					{
						num3 = j;
					}
					if (num2 == -1 && equipment[j].Item.PrimaryWeapon.WeaponFlags.HasAnyFlag(WeaponFlags.MeleeWeapon))
					{
						num2 = j;
					}
				}
			}
			break;
		}
		case FormationClass.Ranged:
		case FormationClass.HorseArcher:
		{
			for (int i = 0; i < 4; i++)
			{
				if (equipment[i].Item?.PrimaryWeapon != null)
				{
					if (num3 == -1 && equipment[i].Item.ItemFlags.HasAnyFlag(ItemFlags.HeldInOffHand))
					{
						num3 = i;
					}
					if (num2 == -1 && equipment[i].Item.PrimaryWeapon.WeaponFlags.HasAnyFlag(WeaponFlags.RangedWeapon))
					{
						num2 = i;
					}
				}
			}
			break;
		}
		}
		if (num2 != -1)
		{
			switch (equipment[num2].Item.PrimaryWeapon.WeaponClass)
			{
			case WeaponClass.OneHandedSword:
			case WeaponClass.OneHandedAxe:
				if (num3 == -1)
				{
					poseName = "troop_infantry_sword1h";
				}
				else if (equipment[num3].Item.PrimaryWeapon.IsShield)
				{
					poseName = "troop_infantry_sword1h";
				}
				break;
			case WeaponClass.TwoHandedSword:
			case WeaponClass.TwoHandedAxe:
			case WeaponClass.TwoHandedMace:
				poseName = "troop_infantry_sword2h";
				break;
			case WeaponClass.Crossbow:
				poseName = "troop_crossbow";
				break;
			case WeaponClass.Bow:
				poseName = "troop_bow";
				break;
			case WeaponClass.LowGripPolearm:
			case WeaponClass.Javelin:
				poseName = "troop_spear";
				break;
			case WeaponClass.OneHandedPolearm:
			case WeaponClass.TwoHandedPolearm:
				poseName = "troop_spear";
				break;
			}
		}
		if (equipment[EquipmentIndex.ArmorItemEndSlot].IsEmpty)
		{
			return;
		}
		if (num2 != -1)
		{
			HorseComponent horseComponent = equipment[EquipmentIndex.ArmorItemEndSlot].Item.HorseComponent;
			bool flag = horseComponent != null && horseComponent.Monster?.FamilyType == 2;
			switch (equipment[num2].Item.Type)
			{
			case ItemObject.ItemTypeEnum.Bow:
				poseName = "troop_cavalry_archer";
				break;
			case ItemObject.ItemTypeEnum.OneHandedWeapon:
				if (num3 == -1)
				{
					poseName = "troop_cavalry_sword";
				}
				else if (equipment[num3].Item.PrimaryWeapon.IsShield)
				{
					poseName = "troop_cavalry_sword";
				}
				break;
			default:
				poseName = "troop_cavalry_lance";
				break;
			}
			if (flag)
			{
				poseName = "camel_" + poseName;
			}
		}
		hasHorse = true;
	}

	private GameEntity FillEntityWithPose(CharacterCode characterCode, GameEntity poseEntity, Scene scene)
	{
		if (characterCode.IsEmpty)
		{
			Debug.FailedAssert("Trying to fill entity with empty character code", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.View\\Tableaus\\Thumbnails\\CharacterThumbnailCache.cs", "FillEntityWithPose", 306);
			return poseEntity;
		}
		if (string.IsNullOrEmpty(characterCode.EquipmentCode))
		{
			Debug.FailedAssert("Trying to fill entity with invalid equipment code", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.View\\Tableaus\\Thumbnails\\CharacterThumbnailCache.cs", "FillEntityWithPose", 312);
			return poseEntity;
		}
		if (TaleWorlds.Core.FaceGen.GetBaseMonsterFromRace(characterCode.Race) == null)
		{
			Debug.FailedAssert("There are no monster data for the race: " + characterCode.Race, "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.View\\Tableaus\\Thumbnails\\CharacterThumbnailCache.cs", "FillEntityWithPose", 319);
			return poseEntity;
		}
		if (poseEntity != null)
		{
			GetPoseParamsFromCharacterCode(characterCode, out var _, out var _);
			CharacterSpawner characterSpawner = poseEntity.GetScriptComponents<CharacterSpawner>().First();
			characterSpawner.SetCreateFaceImmediately(value: false);
			characterSpawner.InitWithCharacter(characterCode);
		}
		return poseEntity;
	}
}
