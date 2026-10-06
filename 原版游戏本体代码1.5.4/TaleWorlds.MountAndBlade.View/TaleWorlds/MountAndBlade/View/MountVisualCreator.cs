using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.View;

public static class MountVisualCreator
{
	public static void SetMaterialProperties(ItemObject mountItem, MetaMesh mountMesh, MountCreationKey key, ref uint maneMeshMultiplier)
	{
		HorseComponent horseComponent = mountItem.HorseComponent;
		int index = MathF.Min(key.MaterialIndex, horseComponent.HorseMaterialNames.Count - 1);
		HorseComponent.MaterialProperty materialProperty = horseComponent.HorseMaterialNames[index];
		Material fromResource = Material.GetFromResource(materialProperty.Name);
		if (mountItem.ItemType == ItemObject.ItemTypeEnum.Horse)
		{
			int num = MathF.Min(key.MeshMultiplierIndex, materialProperty.MeshMultiplier.Count - 1);
			if (num != -1)
			{
				maneMeshMultiplier = materialProperty.MeshMultiplier[num].Item1;
			}
			mountMesh.SetMaterialToSubMeshesWithTag(fromResource, "horse_body");
			mountMesh.SetFactorColorToSubMeshesWithTag(maneMeshMultiplier, "horse_tail");
		}
		else
		{
			mountMesh.SetMaterial(fromResource);
		}
	}

	public static MountVisualCreationOutput AddMountMesh(MBAgentVisuals agentVisual, ItemObject mountItem, ItemObject harnessItem, string mountCreationKeyStr, Agent agent = null)
	{
		MetaMesh metaMesh = null;
		MetaMesh metaMesh2 = null;
		MetaMesh metaMesh3 = null;
		MetaMesh metaMesh4 = null;
		HorseComponent horseComponent = mountItem.HorseComponent;
		uint maneMeshMultiplier = uint.MaxValue;
		metaMesh2 = mountItem.GetMultiMesh(isFemale: false, useSlimVersion: false, needBatchedVersion: true);
		MountCreationKey mountCreationKey = null;
		if (string.IsNullOrEmpty(mountCreationKeyStr))
		{
			mountCreationKeyStr = MountCreationKey.GetRandomMountKeyString(mountItem, MBRandom.RandomInt());
		}
		mountCreationKey = MountCreationKey.FromString(mountCreationKeyStr);
		if (mountItem.ItemType == ItemObject.ItemTypeEnum.Horse)
		{
			SetHorseColors(metaMesh2, mountCreationKey);
		}
		if (horseComponent.HorseMaterialNames != null && horseComponent.HorseMaterialNames.Count > 0)
		{
			SetMaterialProperties(mountItem, metaMesh2, mountCreationKey, ref maneMeshMultiplier);
		}
		int nondeterministicRandomInt = MBRandom.NondeterministicRandomInt;
		SetVoiceDefinition(agent, nondeterministicRandomInt);
		if (harnessItem != null)
		{
			metaMesh4 = harnessItem.GetMultiMesh(isFemale: false, useSlimVersion: false, needBatchedVersion: true);
		}
		foreach (KeyValuePair<string, bool> additionalMeshesName in horseComponent.AdditionalMeshesNameList)
		{
			if (additionalMeshesName.Key.Length <= 0)
			{
				continue;
			}
			string text = additionalMeshesName.Key;
			if (harnessItem == null || !additionalMeshesName.Value)
			{
				metaMesh = MetaMesh.GetCopy(text);
				if (maneMeshMultiplier != uint.MaxValue)
				{
					metaMesh.SetFactor1Linear(maneMeshMultiplier);
				}
				continue;
			}
			ArmorComponent armorComponent = harnessItem.ArmorComponent;
			if (armorComponent == null || armorComponent.ManeCoverType != ArmorComponent.HorseHarnessCoverTypes.All)
			{
				ArmorComponent armorComponent2 = harnessItem.ArmorComponent;
				if (armorComponent2 != null && armorComponent2.ManeCoverType > ArmorComponent.HorseHarnessCoverTypes.None)
				{
					text = text + "_" + harnessItem?.ArmorComponent?.ManeCoverType;
				}
				metaMesh = MetaMesh.GetCopy(text);
				if (maneMeshMultiplier != uint.MaxValue)
				{
					metaMesh.SetFactor1Linear(maneMeshMultiplier);
				}
			}
		}
		if (metaMesh2 != null && harnessItem != null && harnessItem.ArmorComponent?.TailCoverType == ArmorComponent.HorseTailCoverTypes.All)
		{
			metaMesh2.RemoveMeshesWithTag("horse_tail");
		}
		if (metaMesh4 != null)
		{
			if (agentVisual != null)
			{
				MetaMesh metaMesh5 = null;
				if (NativeConfig.CharacterDetail > 2 && harnessItem.ArmorComponent != null)
				{
					metaMesh5 = MetaMesh.GetCopy(harnessItem.ArmorComponent.ReinsRopeMesh, showErrors: false, mayReturnNull: true);
				}
				metaMesh3 = MetaMesh.GetCopy(harnessItem.ArmorComponent?.ReinsMesh, showErrors: false, mayReturnNull: true);
				if (metaMesh5 != null && metaMesh3 != null)
				{
					agentVisual.AddHorseReinsClothMesh(metaMesh3, metaMesh5);
					metaMesh5.ManualInvalidate();
				}
			}
			else if (harnessItem.ArmorComponent != null)
			{
				metaMesh3 = MetaMesh.GetCopy(harnessItem.ArmorComponent.ReinsMesh, showErrors: true, mayReturnNull: true);
			}
		}
		return new MountVisualCreationOutput(metaMesh, metaMesh2, metaMesh3, metaMesh4);
	}

	public static void SetHorseColors(MetaMesh horseMesh, MountCreationKey mountCreationKey)
	{
		horseMesh.SetVectorArgument((int)mountCreationKey._leftFrontLegColorIndex, (int)mountCreationKey._rightFrontLegColorIndex, (int)mountCreationKey._leftBackLegColorIndex, (int)mountCreationKey._rightBackLegColorIndex);
	}

	public static void ClearMountMesh(GameEntity gameEntity)
	{
		gameEntity.RemoveAllChildren();
		gameEntity.Remove(106);
	}

	private static void SetVoiceDefinition(Agent agent, int seedForRandomVoiceTypeAndPitch)
	{
		MBAgentVisuals mBAgentVisuals = agent?.AgentVisuals;
		if (mBAgentVisuals != null)
		{
			string soundAndCollisionInfoClassName = agent.GetSoundAndCollisionInfoClassName();
			int num = ((!string.IsNullOrEmpty(soundAndCollisionInfoClassName)) ? SkinVoiceManager.GetVoiceDefinitionCountWithMonsterSoundAndCollisionInfoClassName(soundAndCollisionInfoClassName) : 0);
			if (num == 0)
			{
				mBAgentVisuals.SetVoiceDefinitionIndex(-1, 0f);
				return;
			}
			int num2 = MathF.Abs(seedForRandomVoiceTypeAndPitch);
			float voicePitch = (float)num2 * 4.656613E-10f;
			int[] array = new int[num];
			SkinVoiceManager.GetVoiceDefinitionListWithMonsterSoundAndCollisionInfoClassName(soundAndCollisionInfoClassName, array);
			int voiceDefinitionIndex = array[num2 % num];
			mBAgentVisuals.SetVoiceDefinitionIndex(voiceDefinitionIndex, voicePitch);
		}
	}

	public static void AddMountMeshToEntity(GameEntity gameEntity, ItemObject mountItem, ItemObject harnessItem, string mountCreationKeyStr, out MountVisualCreationOutput mountVisualCreationOutput, Agent agent = null)
	{
		mountVisualCreationOutput = AddMountMesh(null, mountItem, harnessItem, mountCreationKeyStr, agent);
		AddMultiMeshToSkeleton(mountVisualCreationOutput.HorseManeMesh, gameEntity);
		AddMultiMeshToSkeleton(mountVisualCreationOutput.MountMesh, gameEntity);
		AddMultiMeshToSkeleton(mountVisualCreationOutput.ReinMesh, gameEntity);
		AddMultiMeshToSkeleton(mountVisualCreationOutput.MountHarnessMesh, gameEntity);
	}

	public static void AddMountMeshToEntity(GameEntity gameEntity, ItemObject mountItem, ItemObject harnessItem, string mountCreationKeyStr, Agent agent = null)
	{
		AddMountMeshToEntity(gameEntity, mountItem, harnessItem, mountCreationKeyStr, out var _, agent);
	}

	public static void AddMountMeshToAgentVisual(MBAgentVisuals agentVisual, ItemObject mountItem, ItemObject harnessItem, string mountCreationKeyStr, Agent agent = null)
	{
		MountVisualCreationOutput mountVisualCreationOutput = AddMountMesh(agentVisual, mountItem, harnessItem, mountCreationKeyStr, agent);
		AddMultiMeshToAgentVisual(mountVisualCreationOutput.HorseManeMesh, agentVisual);
		AddMultiMeshToAgentVisual(mountVisualCreationOutput.MountMesh, agentVisual);
		AddMultiMeshToAgentVisual(mountVisualCreationOutput.ReinMesh, agentVisual);
		AddMultiMeshToAgentVisual(mountVisualCreationOutput.MountHarnessMesh, agentVisual);
		if (agent != null && harnessItem != null && harnessItem.IsUsingTeamColor && mountVisualCreationOutput.MountHarnessMesh != null)
		{
			AgentVisuals.AddTeamColorToMesh(mountVisualCreationOutput.MountHarnessMesh, agent.ClothingColor1, agent.ClothingColor2);
		}
		if (mountItem.HorseComponent?.SkeletonScale != null)
		{
			agentVisual.ApplySkeletonScale(mountItem.HorseComponent.SkeletonScale.MountSitBoneScale, mountItem.HorseComponent.SkeletonScale.MountRadiusAdder, mountItem.HorseComponent.SkeletonScale.BoneIndices, mountItem.HorseComponent.SkeletonScale.Scales);
		}
	}

	private static void AddMultiMeshToAgentVisual(MetaMesh metaMesh, MBAgentVisuals agentVisual)
	{
		if (metaMesh != null)
		{
			agentVisual.AddMultiMesh(metaMesh, BodyMeshTypes.Invalid);
			metaMesh.ManualInvalidate();
		}
	}

	private static void AddMultiMeshToSkeleton(MetaMesh metaMesh, GameEntity gameEntity)
	{
		if (metaMesh != null)
		{
			gameEntity.AddMultiMeshToSkeleton(metaMesh);
			metaMesh.ManualInvalidate();
		}
	}
}
