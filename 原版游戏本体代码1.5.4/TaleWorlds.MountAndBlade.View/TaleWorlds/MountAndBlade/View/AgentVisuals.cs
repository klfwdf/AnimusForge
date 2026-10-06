using System;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.View.Tableaus.Thumbnails;
using TaleWorlds.ObjectSystem;

namespace TaleWorlds.MountAndBlade.View;

public class AgentVisuals : IAgentVisual
{
	public const float RandomGlossinessRange = 0.05f;

	public const float RandomClothingColor1HueRange = 4f;

	public const float RandomClothingColor1SaturationRange = 0.2f;

	public const float RandomClothingColor1BrightnessRange = 0.2f;

	public const float RandomClothingColor2HueRange = 8f;

	public const float RandomClothingColor2SaturationRange = 0.5f;

	public const float RandomClothingColor2BrightnessRange = 0.3f;

	private AgentVisualsData _data;

	private float _scale;

	public bool IsFemale
	{
		get
		{
			if (_data.SkeletonTypeData != SkeletonType.Female && _data.SkeletonTypeData != SkeletonType.KidFemale1 && _data.SkeletonTypeData != SkeletonType.KidFemale2)
			{
				return _data.SkeletonTypeData == SkeletonType.KidFemale3;
			}
			return true;
		}
	}

	public MBAgentVisuals GetVisuals()
	{
		return _data.AgentVisuals;
	}

	public void Reset()
	{
		_data.AgentVisuals.Reset();
	}

	public void ResetNextFrame()
	{
		_data.AgentVisuals.ResetNextFrame();
	}

	public MatrixFrame GetFrame()
	{
		return _data.FrameData;
	}

	public BodyProperties GetBodyProperties()
	{
		return _data.BodyPropertiesData;
	}

	public void SetBodyProperties(BodyProperties bodyProperties)
	{
		_data.BodyProperties(bodyProperties);
	}

	public bool GetIsFemale()
	{
		return IsFemale;
	}

	public string GetCharacterObjectID()
	{
		return _data.CharacterObjectStringIdData;
	}

	public void SetCharacterObjectID(string id)
	{
		_data.CharacterObjectStringId(id);
	}

	public Equipment GetEquipment()
	{
		return _data.EquipmentData;
	}

	private AgentVisuals(AgentVisualsData data, string name, bool isRandomProgress, bool needBatchedVersionForWeaponMeshes, bool forceUseFaceCache)
	{
		_data = data;
		_data.AgentVisuals = MBAgentVisuals.CreateAgentVisuals(_data.SceneData, name, data.MonsterData.EyeOffsetWrtHead);
		if (data.EntityData != null)
		{
			_data.AgentVisuals.SetEntity(data.EntityData);
		}
		_scale = ((_data.ScaleData <= 1E-05f) ? 1f : _data.ScaleData);
		Refresh(needBatchedVersionForWeaponMeshes, removeSkeleton: false, null, isRandomProgress, forceUseFaceCache);
	}

	public AgentVisualsData GetCopyAgentVisualsData()
	{
		return new AgentVisualsData(_data);
	}

	public GameEntity GetEntity()
	{
		return _data.AgentVisuals.GetEntity();
	}

	public WeakGameEntity GetWeakEntity()
	{
		return _data.AgentVisuals.GetWeakEntity();
	}

	public void SetVisible(bool value)
	{
		_data.AgentVisuals.SetVisible(value);
	}

	public Vec3 GetGlobalStableEyePoint(bool isHumanoid)
	{
		return _data.AgentVisuals.GetGlobalStableEyePoint(isHumanoid);
	}

	public Vec3 GetGlobalStableNeckPoint(bool isHumanoid)
	{
		return _data.AgentVisuals.GetGlobalStableNeckPoint(isHumanoid);
	}

	public CompositeComponent AddPrefabToAgentVisualBoneByBoneType(string prefabName, HumanBone boneType)
	{
		return _data.AgentVisuals.AddPrefabToAgentVisualBoneByBoneType(prefabName, boneType);
	}

	public CompositeComponent AddPrefabToAgentVisualBoneByRealBoneIndex(string prefabName, sbyte realBoneIndex)
	{
		return _data.AgentVisuals.AddPrefabToAgentVisualBoneByRealBoneIndex(prefabName, realBoneIndex);
	}

	public void SetAgentLodZeroOrMax(bool value)
	{
		_data.AgentVisuals.SetAgentLodZeroOrMax(value);
	}

	public float GetScale()
	{
		return _scale;
	}

	public void SetAction(in ActionIndexCache actionIndex, float startProgress = 0f, bool forceFaceMorphRestart = true)
	{
		if (_data.AgentVisuals != null)
		{
			Skeleton skeleton = _data.AgentVisuals.GetSkeleton();
			if (skeleton != null)
			{
				skeleton.SetAgentActionChannel(0, in actionIndex, startProgress, -0.2f, forceFaceMorphRestart);
				skeleton.ManualInvalidate();
			}
		}
	}

	public bool DoesActionContinueWithCurrentAction(in ActionIndexCache actionIndex)
	{
		bool result = false;
		if (_data.AgentVisuals != null)
		{
			Skeleton skeleton = _data.AgentVisuals.GetSkeleton();
			if (skeleton != null)
			{
				result = skeleton.DoesActionContinueWithCurrentActionAtChannel(0, in actionIndex);
			}
		}
		return result;
	}

	public float GetAnimationParameterAtChannel(int channelIndex)
	{
		float result = 0f;
		if (_data.AgentVisuals != null && _data.AgentVisuals.GetSkeleton() != null)
		{
			result = _data.AgentVisuals.GetSkeleton().GetAnimationParameterAtChannel(channelIndex);
		}
		return result;
	}

	public void Refresh(bool needBatchedVersionForWeaponMeshes, AgentVisualsData data, bool forceUseFaceCache = false)
	{
		AgentVisualsData data2 = _data;
		_data = data;
		bool removeSkeleton = data2.SkeletonTypeData != _data.SkeletonTypeData;
		Equipment equipmentData = _data.EquipmentData;
		Refresh(needBatchedVersionForWeaponMeshes, removeSkeleton, equipmentData, isRandomProgress: false, forceUseFaceCache);
	}

	public void SetClothWindToWeaponAtIndex(Vec3 localWindVector, bool isLocal, EquipmentIndex weaponIndex)
	{
		_data.AgentVisuals.SetClothWindToWeaponAtIndex(localWindVector, isLocal, weaponIndex);
	}

	private void Refresh(bool needBatchedVersionForWeaponMeshes, bool removeSkeleton = false, Equipment oldEquipment = null, bool isRandomProgress = false, bool forceUseFaceCache = false)
	{
		float channelParameter = 0f;
		float num = 0f;
		string text = "";
		bool flag = _data.MonsterData.Flags.HasAnyFlag(AgentFlag.IsHumanoid);
		Skeleton skeleton = _data.AgentVisuals.GetSkeleton();
		float blendPeriodOverride = -0.2f;
		ActionIndexCache actionIndex;
		if (skeleton != null && _data.ActionSetData.IsValid)
		{
			channelParameter = skeleton.GetAnimationParameterAtChannel(0);
			actionIndex = skeleton.GetActionAtChannel(0);
			blendPeriodOverride = 0f;
			if (flag)
			{
				num = MBSkeletonExtensions.GetSkeletonFaceAnimationTime(skeleton);
				text = MBSkeletonExtensions.GetSkeletonFaceAnimationName(skeleton);
			}
		}
		else
		{
			actionIndex = _data.ActionCodeData;
		}
		if (skeleton != null)
		{
			skeleton.ManualInvalidate();
		}
		_data.AgentVisuals.SetSetupMorphNode(_data.UseMorphAnimsData);
		_data.AgentVisuals.UseScaledWeapons(_data.UseScaledWeaponsData);
		MatrixFrame frame = _data.FrameData;
		_scale = ((_data.ScaleData == 0f) ? MBBodyProperties.GetScaleFromKey(_data.RaceData, IsFemale ? 1 : 0, _data.BodyPropertiesData) : _data.ScaleData);
		frame.rotation.ApplyScaleLocal(_scale);
		_data.AgentVisuals.SetFrame(ref frame);
		bool num2 = !removeSkeleton && skeleton != null && oldEquipment != null;
		bool flag2 = false;
		if (num2)
		{
			flag2 = ClearAndAddChangedVisualComponentsOfWeapons(oldEquipment, needBatchedVersionForWeaponMeshes);
		}
		if (!num2 || !flag2)
		{
			_data.AgentVisuals.ClearVisualComponents(removeSkeleton: false, removeLabel: false);
			if (_data.ActionSetData.IsValid && text != "facegen_teeth")
			{
				AnimationSystemData animationSystemData = _data.MonsterData.FillAnimationSystemData(_data.ActionSetData, 1f, _data.HasClippingPlaneData);
				Skeleton skeleton2 = MBSkeletonExtensions.CreateWithActionSet(ref animationSystemData);
				_data.AgentVisuals.SetSkeleton(skeleton2);
				skeleton2.ManualInvalidate();
			}
			if (_data.EquipmentData == null)
			{
				int mask = 481;
				AddSkinMeshesToEntity(mask, !needBatchedVersionForWeaponMeshes, forceUseFaceCache);
			}
			else if (!string.IsNullOrEmpty(_data.MountCreationKeyData) || !flag)
			{
				MountVisualCreator.AddMountMeshToEntity(GetEntity(), _data.EquipmentData[EquipmentIndex.ArmorItemEndSlot].Item, _data.EquipmentData[EquipmentIndex.HorseHarness].Item, _data.MountCreationKeyData, out var mountVisualCreationOutput);
				ItemObject item = _data.EquipmentData[EquipmentIndex.HorseHarness].Item;
				if (item != null && item.IsUsingTeamColor && mountVisualCreationOutput.MountHarnessMesh != null)
				{
					AddTeamColorToMesh(mountVisualCreationOutput.MountHarnessMesh, _data.ClothColor1Data, _data.ClothColor2Data);
				}
			}
			else
			{
				AddSkinArmorWeaponMultiMeshesToEntity(_data.ClothColor1Data, _data.ClothColor2Data, needBatchedVersionForWeaponMeshes, forceUseFaceCache);
			}
		}
		if (!_data.ActionSetData.IsValid || !(actionIndex != ActionIndexCache.act_none))
		{
			return;
		}
		if (isRandomProgress)
		{
			channelParameter = MBRandom.RandomFloat;
		}
		skeleton = _data.AgentVisuals.GetSkeleton();
		if (skeleton != null)
		{
			skeleton.SetAgentActionChannel(0, in actionIndex, channelParameter, blendPeriodOverride);
			if (num > 0f)
			{
				MBSkeletonExtensions.SetSkeletonFaceAnimationTime(skeleton, num);
			}
			skeleton.ManualInvalidate();
		}
	}

	public void TickVisuals()
	{
		if (_data.ActionSetData.IsValid)
		{
			_data.AgentVisuals.GetSkeleton().TickActionChannels();
		}
	}

	public void Tick(AgentVisuals parentAgentVisuals, float dt, bool isEntityMoving = false, float speed = 0f)
	{
		_data.AgentVisuals.Tick(parentAgentVisuals?._data.AgentVisuals, dt, isEntityMoving, speed);
	}

	public static AgentVisuals Create(AgentVisualsData data, string name, bool isRandomProgress, bool needBatchedVersionForWeaponMeshes, bool forceUseFaceCache)
	{
		return new AgentVisuals(data, name, isRandomProgress, needBatchedVersionForWeaponMeshes, forceUseFaceCache);
	}

	public static float GetRandomGlossFactor(Random randomGenerator)
	{
		return 1f + (randomGenerator.NextFloat() * 2f - 1f) * 0.05f;
	}

	public static void GetRandomClothingColors(int seed, Color inputColor1, Color inputColor2, out Color color1, out Color color2)
	{
		MBFastRandom mBFastRandom = new MBFastRandom((uint)seed);
		color1 = inputColor1.AddFactorInHSB((2f * mBFastRandom.NextFloat() - 1f) * 4f, (2f * mBFastRandom.NextFloat() - 1f) * 0.2f, (2f * mBFastRandom.NextFloat() - 1f) * 0.2f);
		color2 = inputColor2.AddFactorInHSB((2f * mBFastRandom.NextFloat() - 1f) * 8f, (2f * mBFastRandom.NextFloat() - 1f) * 0.5f, (2f * mBFastRandom.NextFloat() - 1f) * 0.3f);
	}

	private void AddSkinArmorWeaponMultiMeshesToEntity(uint teamColor1, uint teamColor2, bool needBatchedVersion, bool forceUseFaceCache = false)
	{
		AddSkinMeshesToEntity((int)_data.EquipmentData.GetSkinMeshesMask(), !needBatchedVersion, forceUseFaceCache);
		AddArmorMultiMeshesToAgentEntity(teamColor1, teamColor2);
		int hashCode = _data.BodyPropertiesData.GetHashCode();
		for (int i = 0; i < 5; i++)
		{
			if (!_data.EquipmentData[i].IsEmpty)
			{
				MissionWeapon missionWeapon = new MissionWeapon(_data.EquipmentData[i].Item, _data.EquipmentData[i].ItemModifier, _data.BannerData);
				if (_data.AddColorRandomnessData)
				{
					missionWeapon.SetRandomGlossMultiplier(hashCode);
				}
				WeaponData weaponData = missionWeapon.GetWeaponData(needBatchedVersion);
				WeaponData ammoWeaponData = missionWeapon.GetAmmoWeaponData(needBatchedVersion);
				_data.AgentVisuals.AddWeaponToAgentEntity(i, in weaponData, missionWeapon.GetWeaponStatsData(), in ammoWeaponData, missionWeapon.GetAmmoWeaponStatsData(), _data.GetCachedWeaponEntity((EquipmentIndex)i));
				weaponData.DeinitializeManagedPointers();
				ammoWeaponData.DeinitializeManagedPointers();
			}
		}
		_data.AgentVisuals.SetWieldedWeaponIndices(_data.RightWieldedItemIndexData, _data.LeftWieldedItemIndexData);
		for (int j = 0; j < 5; j++)
		{
			if (!_data.EquipmentData[j].IsEmpty && _data.EquipmentData[j].Item.PrimaryWeapon.IsConsumable)
			{
				short num = _data.EquipmentData[j].Item.PrimaryWeapon.MaxDataValue;
				if (j == _data.RightWieldedItemIndexData)
				{
					num--;
				}
				_data.AgentVisuals.UpdateQuiverMeshesWithoutAgent(j, num);
			}
		}
	}

	private void AddSkinMeshesToEntity(int mask, bool useGPUMorph, bool forceUseFaceCache = false)
	{
		SkinGenerationParams skinParams;
		if (_data.EquipmentData != null)
		{
			bool isFemale = _data.BodyPropertiesData.Age >= 14f && _data.SkeletonTypeData == SkeletonType.Female;
			skinParams = new SkinGenerationParams(mask, _data.EquipmentData.GetUnderwearType(isFemale), (int)_data.EquipmentData.BodyMeshType, (int)_data.EquipmentData.HairCoverType, (int)_data.EquipmentData.BeardCoverType, (int)_data.EquipmentData.BodyDeformType, _data.PrepareImmediatelyData, 0f, (int)_data.SkeletonTypeData, _data.RaceData, _data.UseTranslucencyData, _data.UseTesselationData, 0);
		}
		else
		{
			skinParams = new SkinGenerationParams(mask, Equipment.UnderwearTypes.FullUnderwear, 0, 4, 0, 0, _data.PrepareImmediatelyData, 0f, (int)_data.SkeletonTypeData, _data.RaceData, _data.UseTranslucencyData, _data.UseTesselationData, 0);
		}
		if (_data.CharacterObjectStringIdData != null)
		{
			MBObjectManager.Instance.GetObject<BasicCharacterObject>(_data.CharacterObjectStringIdData);
		}
		_data.AgentVisuals.AddSkinMeshes(skinParams, _data.BodyPropertiesData, useGPUMorph, useFaceCache: false);
	}

	public void SetFaceGenerationParams(FaceGenerationParams faceGenerationParams)
	{
		_data.AgentVisuals.SetFaceGenerationParams(faceGenerationParams);
	}

	public void SetVoiceDefinitionIndex(int voiceDefinitionIndex, float voicePitch)
	{
		_data.AgentVisuals.SetVoiceDefinitionIndex(voiceDefinitionIndex, voicePitch);
	}

	public void StartRhubarbRecord(string path, int soundId)
	{
		_data.AgentVisuals.StartRhubarbRecord(path, soundId);
	}

	public void SetAgentLodZeroOrMaxExternal(bool makeZero)
	{
		_data.AgentVisuals.SetAgentLodZeroOrMax(makeZero);
	}

	public void SetAgentLocalSpeed(Vec2 speed)
	{
		_data.AgentVisuals.SetAgentLocalSpeed(speed);
	}

	public void SetLookDirection(Vec3 direction)
	{
		_data.AgentVisuals.SetLookDirection(direction);
	}

	public void AddArmorMultiMeshesToAgentEntity(uint teamColor1, uint teamColor2)
	{
		Random randomGenerator = null;
		uint color3;
		uint color4;
		if (_data.AddColorRandomnessData)
		{
			int hashCode = _data.BodyPropertiesData.GetHashCode();
			randomGenerator = new Random(hashCode);
			GetRandomClothingColors(hashCode, Color.FromUint(teamColor1), Color.FromUint(teamColor2), out var color, out var color2);
			color3 = color.ToUnsignedInteger();
			color4 = color2.ToUnsignedInteger();
		}
		else
		{
			color3 = teamColor1;
			color4 = teamColor2;
		}
		for (EquipmentIndex equipmentIndex = EquipmentIndex.HorseHarness; equipmentIndex >= EquipmentIndex.WeaponItemBeginSlot; equipmentIndex--)
		{
			if (equipmentIndex == EquipmentIndex.NumAllWeaponSlots || equipmentIndex == EquipmentIndex.Body || equipmentIndex == EquipmentIndex.Leg || equipmentIndex == EquipmentIndex.Gloves || equipmentIndex == EquipmentIndex.Cape)
			{
				ItemObject item = _data.EquipmentData[(int)equipmentIndex].Item;
				ItemObject itemObject = _data.EquipmentData[(int)equipmentIndex].CosmeticItem ?? item;
				if (itemObject != null)
				{
					bool isFemale = _data.BodyPropertiesData.Age >= 14f && _data.SkeletonTypeData == SkeletonType.Female;
					bool useSlimVersion = equipmentIndex == EquipmentIndex.Body && _data.EquipmentData[EquipmentIndex.Gloves].Item != null && !_data.EquipmentData[EquipmentIndex.Gloves].Item.ArmorComponent.IsNoSlim;
					MetaMesh metaMesh = null;
					metaMesh = _data.EquipmentData[(int)equipmentIndex].GetMultiMesh(isFemale, useSlimVersion, needBatchedVersion: true);
					if (metaMesh != null)
					{
						if (_data.AddColorRandomnessData)
						{
							metaMesh.SetGlossMultiplier(GetRandomGlossFactor(randomGenerator));
						}
						if (itemObject.IsUsingTableau && _data.BannerData != null)
						{
							for (int i = 0; i < metaMesh.MeshCount; i++)
							{
								Mesh currentMesh = metaMesh.GetMeshAtIndex(i);
								Mesh mesh = currentMesh;
								if ((object)mesh != null && !mesh.HasTag("dont_use_tableau"))
								{
									Mesh mesh2 = currentMesh;
									if ((object)mesh2 != null && mesh2.HasTag("banner_replacement_mesh"))
									{
										((BannerVisual)_data.BannerData.BannerVisual).GetTableauTextureLarge(BannerDebugInfo.CreateManual(GetType().Name), delegate(Texture t)
										{
											ApplyBannerTextureToMesh(currentMesh, t);
										});
										currentMesh.ManualInvalidate();
										break;
									}
								}
								currentMesh.ManualInvalidate();
							}
						}
						else if (itemObject.IsUsingTeamColor)
						{
							AddTeamColorToMesh(metaMesh, color3, color4);
						}
						if (itemObject.UsingFacegenScaling)
						{
							Skeleton skeleton = _data.AgentVisuals.GetSkeleton();
							metaMesh.UseHeadBoneFaceGenScaling(skeleton, _data.MonsterData.HeadLookDirectionBoneIndex, _data.AgentVisuals.GetFacegenScalingMatrix());
							skeleton.ManualInvalidate();
						}
						_data.AgentVisuals.AddMultiMesh(metaMesh, MBAgentVisuals.GetBodyMeshIndex(equipmentIndex));
						metaMesh.ManualInvalidate();
					}
				}
			}
		}
	}

	private void ApplyBannerTextureToMesh(Mesh armorMesh, Texture bannerTexture)
	{
		if (armorMesh != null)
		{
			Material material = armorMesh.GetMaterial().CreateCopy();
			material.SetTexture(Material.MBTextureType.DiffuseMap2, bannerTexture);
			uint num = (uint)material.GetShader().GetMaterialShaderFlagMask("use_tableau_blending");
			ulong shaderFlags = material.GetShaderFlags();
			material.SetShaderFlags(shaderFlags | num);
			armorMesh.SetMaterial(material);
		}
	}

	public void MakeRandomVoiceForFacegen()
	{
		GameEntity entity = _data.AgentVisuals.GetEntity();
		Vec3 v = entity.Skeleton.GetBoneEntitialFrame(_data.MonsterData.HeadLookDirectionBoneIndex).origin;
		Vec3 position = entity.GetFrame().TransformToParent(in v);
		entity.Skeleton.SetAgentActionChannel(1, in ActionIndexCache.act_command_leftstance);
		SkinVoiceManager.SkinVoiceType[] obj = new SkinVoiceManager.SkinVoiceType[12]
		{
			SkinVoiceManager.VoiceType.Yell,
			SkinVoiceManager.VoiceType.Victory,
			SkinVoiceManager.VoiceType.Charge,
			SkinVoiceManager.VoiceType.Advance,
			SkinVoiceManager.VoiceType.Stop,
			SkinVoiceManager.VoiceType.FallBack,
			SkinVoiceManager.VoiceType.UseLadders,
			SkinVoiceManager.VoiceType.Infantry,
			SkinVoiceManager.VoiceType.FireAtWill,
			SkinVoiceManager.VoiceType.FormLine,
			SkinVoiceManager.VoiceType.FormShieldWall,
			SkinVoiceManager.VoiceType.FormCircle
		};
		int index = obj[MBRandom.RandomInt(obj.Length)].Index;
		_data.AgentVisuals.MakeVoice(index, position);
	}

	private bool ClearAndAddChangedVisualComponentsOfWeapons(Equipment oldEquipment, bool needBatchedVersionForMeshes)
	{
		int num = 0;
		for (int i = 0; i <= 3; i++)
		{
			if (!oldEquipment[i].IsEqualTo(_data.EquipmentData[i]))
			{
				num++;
			}
		}
		if (num > 1)
		{
			return false;
		}
		bool flag = false;
		for (int j = 0; j <= 3; j++)
		{
			if (!oldEquipment[j].IsEqualTo(_data.EquipmentData[j]))
			{
				flag = true;
				break;
			}
		}
		if (flag)
		{
			_data.AgentVisuals.ClearAllWeaponMeshes();
			int num2 = 0;
			int num3 = 0;
			while (num2 < 5)
			{
				if (!_data.EquipmentData[num2].IsEmpty)
				{
					MissionWeapon missionWeapon = new MissionWeapon(_data.EquipmentData[num2].Item, _data.EquipmentData[num2].ItemModifier, _data.BannerData);
					if (_data.AddColorRandomnessData)
					{
						missionWeapon.SetRandomGlossMultiplier(_data.BodyPropertiesData.GetHashCode());
					}
					ItemObject.ItemTypeEnum ammoTypeForItemType = ItemObject.GetAmmoTypeForItemType(WeaponComponentData.GetItemTypeFromWeaponClass(_data.EquipmentData[num2].Item.PrimaryWeapon.WeaponClass));
					bool flag2 = false;
					MissionWeapon missionWeapon2 = default(MissionWeapon);
					for (int k = 0; k < 5; k++)
					{
						if (!_data.EquipmentData[k].IsEmpty && WeaponComponentData.GetItemTypeFromWeaponClass(_data.EquipmentData[k].Item.PrimaryWeapon.WeaponClass) == ammoTypeForItemType)
						{
							flag2 = true;
							missionWeapon2 = new MissionWeapon(_data.EquipmentData[k].Item, _data.EquipmentData[k].ItemModifier, _data.BannerData);
							if (_data.AddColorRandomnessData)
							{
								missionWeapon2.SetRandomGlossMultiplier(_data.BodyPropertiesData.GetHashCode());
							}
						}
					}
					WeaponData weaponData = missionWeapon.GetWeaponData(needBatchedVersionForMeshes);
					WeaponData ammoWeaponData = (flag2 ? missionWeapon2.GetWeaponData(needBatchedVersionForMeshes) : WeaponData.InvalidWeaponData);
					WeaponStatsData[] ammoWeaponStatsData = (flag2 ? missionWeapon2.GetWeaponStatsData() : null);
					_data.AgentVisuals.AddWeaponToAgentEntity(num2, in weaponData, missionWeapon.GetWeaponStatsData(), in ammoWeaponData, ammoWeaponStatsData, null);
				}
				num2++;
				num3++;
			}
			_data.AgentVisuals.SetWieldedWeaponIndices(_data.RightWieldedItemIndexData, _data.LeftWieldedItemIndexData);
		}
		return flag;
	}

	public static void AddTeamColorToMesh(MetaMesh metaMesh, uint color1, uint color2)
	{
		for (int i = 0; i < metaMesh.MeshCount; i++)
		{
			Mesh meshAtIndex = metaMesh.GetMeshAtIndex(i);
			if (!meshAtIndex.HasTag("no_team_color"))
			{
				meshAtIndex.Color = color1;
				meshAtIndex.Color2 = color2;
				Material material = meshAtIndex.GetMaterial().CreateCopy();
				material.AddMaterialShaderFlag("use_double_colormap_with_mask_texture", showErrors: false);
				meshAtIndex.SetMaterial(material);
			}
			meshAtIndex.ManualInvalidate();
		}
	}

	public void SetClothingColors(uint color1, uint color2)
	{
		_data.ClothColor1(color1);
		_data.ClothColor2(color2);
	}

	public void GetClothingColors(out uint color1, out uint color2)
	{
		color1 = _data.ClothColor1Data;
		color2 = _data.ClothColor2Data;
	}

	public void SetEntity(GameEntity entity)
	{
		_data.AgentVisuals.SetEntity(entity);
	}

	void IAgentVisual.SetAction(in ActionIndexCache actionName, float startProgress, bool forceFaceMorphRestart)
	{
		SetAction(in actionName, startProgress, forceFaceMorphRestart);
	}
}
