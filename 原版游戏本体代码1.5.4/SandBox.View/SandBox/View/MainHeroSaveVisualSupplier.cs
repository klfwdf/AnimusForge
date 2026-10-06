using System.Collections.Generic;
using System.Globalization;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace SandBox.View;

public class MainHeroSaveVisualSupplier : IMainHeroVisualSupplier
{
	string IMainHeroVisualSupplier.GetMainHeroVisualCode()
	{
		Hero mainHero = Hero.MainHero;
		CharacterObject characterObject = mainHero.CharacterObject;
		Monster baseMonsterFromRace = TaleWorlds.Core.FaceGen.GetBaseMonsterFromRace(characterObject.Race);
		MBStringBuilder mBStringBuilder = default(MBStringBuilder);
		mBStringBuilder.Initialize(1024, "GetMainHeroVisualCode");
		mBStringBuilder.Append("4|");
		mBStringBuilder.Append(MBActionSet.GetActionSet(baseMonsterFromRace.ActionSetCode).GetSkeletonName());
		mBStringBuilder.Append("|");
		Equipment battleEquipment = mainHero.BattleEquipment;
		mBStringBuilder.Append(battleEquipment.GetSkinMeshesMask().ToString());
		mBStringBuilder.Append("|");
		mBStringBuilder.Append(mainHero.IsFemale.ToString());
		mBStringBuilder.Append("|");
		mBStringBuilder.Append(mainHero.CharacterObject.Race.ToString());
		mBStringBuilder.Append("|");
		mBStringBuilder.Append(battleEquipment.GetUnderwearType(mainHero.IsFemale).ToString());
		mBStringBuilder.Append("|");
		mBStringBuilder.Append(battleEquipment.BodyMeshType.ToString());
		mBStringBuilder.Append("|");
		mBStringBuilder.Append(battleEquipment.HairCoverType.ToString());
		mBStringBuilder.Append("|");
		mBStringBuilder.Append(battleEquipment.BeardCoverType.ToString());
		mBStringBuilder.Append("|");
		mBStringBuilder.Append(battleEquipment.BodyDeformType.ToString());
		mBStringBuilder.Append("|");
		mBStringBuilder.Append(characterObject.FaceDirtAmount.ToString(CultureInfo.InvariantCulture));
		mBStringBuilder.Append("|");
		mBStringBuilder.Append(mainHero.BodyProperties.ToString());
		mBStringBuilder.Append("|");
		mBStringBuilder.Append((mainHero.MapFaction != null) ? mainHero.MapFaction.Color.ToString() : "0xFFFFFFFF");
		mBStringBuilder.Append("|");
		mBStringBuilder.Append((mainHero.MapFaction != null) ? mainHero.MapFaction.Color2.ToString() : "0xFFFFFFFF");
		mBStringBuilder.Append("|");
		for (EquipmentIndex equipmentIndex = EquipmentIndex.NumAllWeaponSlots; equipmentIndex < EquipmentIndex.ArmorItemEndSlot; equipmentIndex++)
		{
			ItemObject item = battleEquipment[equipmentIndex].Item;
			string text = ((item != null) ? item.MultiMeshName : "");
			bool flag = item?.IsUsingTeamColor ?? false;
			bool flag2 = item?.IsUsingTableau ?? false;
			bool flag3 = item != null && item.HasArmorComponent && item.ArmorComponent.MultiMeshHasGenderVariations;
			mBStringBuilder.Append(text + "|");
			mBStringBuilder.Append(flag + "|");
			mBStringBuilder.Append(flag3 + "|");
			mBStringBuilder.Append(flag2 + "|");
		}
		if (!mainHero.BattleEquipment[EquipmentIndex.ArmorItemEndSlot].IsEmpty && ActionIndexCache.act_inventory_idle.Index != -1)
		{
			ItemObject item2 = mainHero.BattleEquipment[EquipmentIndex.ArmorItemEndSlot].Item;
			ItemObject item3 = mainHero.BattleEquipment[EquipmentIndex.HorseHarness].Item;
			HorseComponent horseComponent = item2.HorseComponent;
			MBActionSet actionSet = MBActionSet.GetActionSet(item2.HorseComponent.Monster.ActionSetCode);
			mBStringBuilder.Append(actionSet.GetSkeletonName());
			mBStringBuilder.Append("|");
			mBStringBuilder.Append(item2.MultiMeshName);
			mBStringBuilder.Append("|");
			MountCreationKey randomMountKey = MountCreationKey.GetRandomMountKey(item2, characterObject.GetMountKeySeed());
			mBStringBuilder.Append(randomMountKey);
			mBStringBuilder.Append("|");
			if (horseComponent.HorseMaterialNames.Count > 0)
			{
				int index = MathF.Min(randomMountKey.MaterialIndex, horseComponent.HorseMaterialNames.Count - 1);
				HorseComponent.MaterialProperty materialProperty = horseComponent.HorseMaterialNames[index];
				mBStringBuilder.Append(materialProperty.Name);
				mBStringBuilder.Append("|");
				uint value = uint.MaxValue;
				int num = MathF.Min(randomMountKey.MeshMultiplierIndex, materialProperty.MeshMultiplier.Count - 1);
				if (num != -1)
				{
					value = materialProperty.MeshMultiplier[num].Item1;
				}
				mBStringBuilder.Append(value);
			}
			else
			{
				mBStringBuilder.Append("|");
			}
			mBStringBuilder.Append("|");
			mBStringBuilder.Append(actionSet.GetAnimationName(in ActionIndexCache.act_inventory_idle));
			mBStringBuilder.Append("|");
			if (item3 != null)
			{
				mBStringBuilder.Append(item3.MultiMeshName);
				mBStringBuilder.Append("|");
				mBStringBuilder.Append(item3.IsUsingTeamColor);
				mBStringBuilder.Append("|");
				mBStringBuilder.Append(item3.ArmorComponent.ReinsMesh);
				mBStringBuilder.Append("|");
			}
			else
			{
				mBStringBuilder.Append("|||");
			}
			List<string> list = new List<string>();
			foreach (KeyValuePair<string, bool> additionalMeshesName in horseComponent.AdditionalMeshesNameList)
			{
				if (additionalMeshesName.Key.Length <= 0)
				{
					continue;
				}
				string text2 = additionalMeshesName.Key;
				if (item3 == null || !additionalMeshesName.Value)
				{
					list.Add(text2);
					continue;
				}
				ArmorComponent armorComponent = item3.ArmorComponent;
				if (armorComponent == null || armorComponent.ManeCoverType != ArmorComponent.HorseHarnessCoverTypes.All)
				{
					ArmorComponent armorComponent2 = item3.ArmorComponent;
					if (armorComponent2 != null && armorComponent2.ManeCoverType > ArmorComponent.HorseHarnessCoverTypes.None)
					{
						text2 = text2 + "_" + item3?.ArmorComponent?.ManeCoverType;
					}
					list.Add(text2);
				}
			}
			mBStringBuilder.Append(list.Count);
			foreach (string item4 in list)
			{
				mBStringBuilder.Append("|");
				mBStringBuilder.Append(item4);
			}
		}
		else
		{
			mBStringBuilder.Append("|||||||||0");
		}
		return mBStringBuilder.ToStringAndRelease();
	}
}
