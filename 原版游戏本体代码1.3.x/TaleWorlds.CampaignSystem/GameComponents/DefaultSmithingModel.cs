using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Helpers;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.GameComponents
{
	// Token: 0x02000158 RID: 344
	public class DefaultSmithingModel : SmithingModel
	{
		// Token: 0x06001A6D RID: 6765 RVA: 0x00085B35 File Offset: 0x00083D35
		public override int GetCraftingPartDifficulty(CraftingPiece craftingPiece)
		{
			if (!craftingPiece.IsEmptyPiece)
			{
				return craftingPiece.PieceTier * 50;
			}
			return 0;
		}

		// Token: 0x06001A6E RID: 6766 RVA: 0x00085B4C File Offset: 0x00083D4C
		public override int CalculateWeaponDesignDifficulty(WeaponDesign weaponDesign)
		{
			float num = 0f;
			float num2 = 0f;
			foreach (WeaponDesignElement weaponDesignElement in weaponDesign.UsedPieces)
			{
				if (weaponDesignElement.IsValid && !weaponDesignElement.CraftingPiece.IsEmptyPiece)
				{
					if (weaponDesignElement.CraftingPiece.PieceType == CraftingPiece.PieceTypes.Blade)
					{
						num += 100f;
						num2 += (float)(this.GetCraftingPartDifficulty(weaponDesignElement.CraftingPiece) * 100);
					}
					else if (weaponDesignElement.CraftingPiece.PieceType == CraftingPiece.PieceTypes.Guard)
					{
						num += 20f;
						num2 += (float)(this.GetCraftingPartDifficulty(weaponDesignElement.CraftingPiece) * 20);
					}
					else if (weaponDesignElement.CraftingPiece.PieceType == CraftingPiece.PieceTypes.Handle)
					{
						num += 60f;
						num2 += (float)(this.GetCraftingPartDifficulty(weaponDesignElement.CraftingPiece) * 60);
					}
					else if (weaponDesignElement.CraftingPiece.PieceType == CraftingPiece.PieceTypes.Pommel)
					{
						num += 20f;
						num2 += (float)(this.GetCraftingPartDifficulty(weaponDesignElement.CraftingPiece) * 20);
					}
				}
			}
			return MathF.Round(num2 / num);
		}

		// Token: 0x06001A6F RID: 6767 RVA: 0x00085C60 File Offset: 0x00083E60
		public override ItemModifier GetCraftedWeaponModifier(WeaponDesign weaponDesign, Hero hero)
		{
			List<ValueTuple<ItemQuality, float>> modifierQualityProbabilities = this.GetModifierQualityProbabilities(weaponDesign, hero);
			ItemQuality itemQuality = modifierQualityProbabilities.Last<ValueTuple<ItemQuality, float>>().Item1;
			float num = MBRandom.RandomFloat;
			foreach (ValueTuple<ItemQuality, float> valueTuple in modifierQualityProbabilities)
			{
				if (num <= valueTuple.Item2)
				{
					itemQuality = valueTuple.Item1;
					break;
				}
				num -= valueTuple.Item2;
			}
			itemQuality = this.AdjustQualityRegardingDesignTier(itemQuality, weaponDesign);
			List<ItemModifier> modifiersBasedOnQuality = weaponDesign.Template.ItemModifierGroup.GetModifiersBasedOnQuality(itemQuality);
			if (modifiersBasedOnQuality.IsEmpty<ItemModifier>())
			{
				return null;
			}
			if (modifiersBasedOnQuality.Count == 1)
			{
				return modifiersBasedOnQuality[0];
			}
			int index = MBRandom.RandomInt(0, modifiersBasedOnQuality.Count);
			return modifiersBasedOnQuality[index];
		}

		// Token: 0x06001A70 RID: 6768 RVA: 0x00085D2C File Offset: 0x00083F2C
		public override IEnumerable<Crafting.RefiningFormula> GetRefiningFormulas(Hero weaponsmith)
		{
			if (weaponsmith.GetPerkValue(DefaultPerks.Crafting.CharcoalMaker))
			{
				yield return new Crafting.RefiningFormula(CraftingMaterials.Wood, 2, CraftingMaterials.Iron1, 0, CraftingMaterials.Charcoal, 3, CraftingMaterials.IronOre, 0);
			}
			else
			{
				yield return new Crafting.RefiningFormula(CraftingMaterials.Wood, 2, CraftingMaterials.Iron1, 0, CraftingMaterials.Charcoal, 1, CraftingMaterials.IronOre, 0);
			}
			yield return new Crafting.RefiningFormula(CraftingMaterials.IronOre, 1, CraftingMaterials.Charcoal, 1, CraftingMaterials.Iron1, weaponsmith.GetPerkValue(DefaultPerks.Crafting.IronMaker) ? 3 : 2, CraftingMaterials.IronOre, 0);
			yield return new Crafting.RefiningFormula(CraftingMaterials.Iron1, 1, CraftingMaterials.Charcoal, 1, CraftingMaterials.Iron2, 1, CraftingMaterials.IronOre, 0);
			yield return new Crafting.RefiningFormula(CraftingMaterials.Iron2, 2, CraftingMaterials.Charcoal, 1, CraftingMaterials.Iron3, 1, CraftingMaterials.Iron1, 1);
			if (weaponsmith.GetPerkValue(DefaultPerks.Crafting.SteelMaker))
			{
				yield return new Crafting.RefiningFormula(CraftingMaterials.Iron3, 2, CraftingMaterials.Charcoal, 1, CraftingMaterials.Iron4, 1, CraftingMaterials.Iron1, 1);
			}
			if (weaponsmith.GetPerkValue(DefaultPerks.Crafting.SteelMaker2))
			{
				yield return new Crafting.RefiningFormula(CraftingMaterials.Iron4, 2, CraftingMaterials.Charcoal, 1, CraftingMaterials.Iron5, 1, CraftingMaterials.Iron1, 1);
			}
			if (weaponsmith.GetPerkValue(DefaultPerks.Crafting.SteelMaker3))
			{
				yield return new Crafting.RefiningFormula(CraftingMaterials.Iron5, 2, CraftingMaterials.Charcoal, 1, CraftingMaterials.Iron6, 1, CraftingMaterials.Iron1, 1);
			}
			yield break;
		}

		// Token: 0x06001A71 RID: 6769 RVA: 0x00085D3C File Offset: 0x00083F3C
		public override int GetSkillXpForRefining(ref Crafting.RefiningFormula refineFormula)
		{
			return MathF.Round(0.3f * (float)(this.GetCraftingMaterialItem(refineFormula.Output).Value * refineFormula.OutputCount));
		}

		// Token: 0x06001A72 RID: 6770 RVA: 0x00085D64 File Offset: 0x00083F64
		public override int GetSkillXpForSmelting(ItemObject item)
		{
			return MathF.Round(0.02f * (float)item.Value);
		}

		// Token: 0x06001A73 RID: 6771 RVA: 0x00085D78 File Offset: 0x00083F78
		public override int GetSkillXpForSmithingInFreeBuildMode(ItemObject item)
		{
			return MathF.Round(0.02f * (float)item.Value);
		}

		// Token: 0x06001A74 RID: 6772 RVA: 0x00085D8C File Offset: 0x00083F8C
		public override int GetSkillXpForSmithingInCraftingOrderMode(ItemObject item)
		{
			return MathF.Round(0.1f * (float)item.Value);
		}

		// Token: 0x06001A75 RID: 6773 RVA: 0x00085DA0 File Offset: 0x00083FA0
		public override int GetEnergyCostForRefining(ref Crafting.RefiningFormula refineFormula, Hero hero)
		{
			ExplainedNumber explainedNumber = new ExplainedNumber(6f, false, null);
			if (hero.GetPerkValue(DefaultPerks.Crafting.PracticalRefiner))
			{
				explainedNumber.AddFactor(DefaultPerks.Crafting.PracticalRefiner.PrimaryBonus, null);
			}
			return (int)explainedNumber.ResultNumber;
		}

		// Token: 0x06001A76 RID: 6774 RVA: 0x00085DE4 File Offset: 0x00083FE4
		public override int GetEnergyCostForSmithing(ItemObject item, Hero hero)
		{
			ExplainedNumber explainedNumber = new ExplainedNumber((float)(10 + ItemObject.ItemTiers.Tier6 * item.Tier), false, null);
			if (hero.GetPerkValue(DefaultPerks.Crafting.PracticalSmith))
			{
				explainedNumber.AddFactor(DefaultPerks.Crafting.PracticalSmith.PrimaryBonus, null);
			}
			return (int)explainedNumber.ResultNumber;
		}

		// Token: 0x06001A77 RID: 6775 RVA: 0x00085E30 File Offset: 0x00084030
		public override int GetEnergyCostForSmelting(ItemObject item, Hero hero)
		{
			ExplainedNumber explainedNumber = new ExplainedNumber(10f, false, null);
			if (hero.GetPerkValue(DefaultPerks.Crafting.PracticalSmelter))
			{
				explainedNumber.AddFactor(DefaultPerks.Crafting.PracticalSmelter.PrimaryBonus, null);
			}
			return (int)explainedNumber.ResultNumber;
		}

		// Token: 0x06001A78 RID: 6776 RVA: 0x00085E74 File Offset: 0x00084074
		public override ItemObject GetCraftingMaterialItem(CraftingMaterials craftingMaterial)
		{
			switch (craftingMaterial)
			{
			case CraftingMaterials.IronOre:
				return DefaultItems.IronOre;
			case CraftingMaterials.Iron1:
				return DefaultItems.IronIngot1;
			case CraftingMaterials.Iron2:
				return DefaultItems.IronIngot2;
			case CraftingMaterials.Iron3:
				return DefaultItems.IronIngot3;
			case CraftingMaterials.Iron4:
				return DefaultItems.IronIngot4;
			case CraftingMaterials.Iron5:
				return DefaultItems.IronIngot5;
			case CraftingMaterials.Iron6:
				return DefaultItems.IronIngot6;
			case CraftingMaterials.Wood:
				return DefaultItems.HardWood;
			case CraftingMaterials.Charcoal:
				return DefaultItems.Charcoal;
			default:
				return DefaultItems.IronIngot1;
			}
		}

		// Token: 0x06001A79 RID: 6777 RVA: 0x00085EE8 File Offset: 0x000840E8
		public override int[] GetSmeltingOutputForItem(ItemObject item)
		{
			int[] array = new int[9];
			if (item.WeaponDesign != null)
			{
				foreach (WeaponDesignElement weaponDesignElement in item.WeaponDesign.UsedPieces)
				{
					if (weaponDesignElement != null && weaponDesignElement.IsValid)
					{
						foreach (ValueTuple<CraftingMaterials, int> valueTuple in weaponDesignElement.CraftingPiece.MaterialsUsed)
						{
							array[(int)valueTuple.Item1] += valueTuple.Item2;
						}
					}
				}
				this.AddSmeltingReductions(array);
			}
			return array;
		}

		// Token: 0x06001A7A RID: 6778 RVA: 0x00085F9C File Offset: 0x0008419C
		[return: TupleElementNames(new string[] { "quality", "probability" })]
		private List<ValueTuple<ItemQuality, float>> GetModifierQualityProbabilities(WeaponDesign weaponDesign, Hero hero)
		{
			int num = this.CalculateWeaponDesignDifficulty(weaponDesign);
			int skillValue = hero.CharacterObject.GetSkillValue(DefaultSkills.Crafting);
			List<ValueTuple<ItemQuality, float>> list = new List<ValueTuple<ItemQuality, float>>();
			ExplainedNumber explainedNumber = new ExplainedNumber((float)(-(float)num), false, null);
			SkillHelper.AddSkillBonusForCharacter(DefaultSkillEffects.SmithingLevel, hero.CharacterObject, ref explainedNumber);
			explainedNumber.LimitMin(-300f);
			explainedNumber.LimitMax(300f, null);
			list.Add(new ValueTuple<ItemQuality, float>(ItemQuality.Poor, 0.36f * (1f - this.CalculateSigmoidFunction(explainedNumber.ResultNumber, -70f, 0.018f))));
			list.Add(new ValueTuple<ItemQuality, float>(ItemQuality.Inferior, 0.45f * (1f - this.CalculateSigmoidFunction(explainedNumber.ResultNumber, -55f, 0.018f))));
			list.Add(new ValueTuple<ItemQuality, float>(ItemQuality.Common, this.CalculateSigmoidFunction(explainedNumber.ResultNumber, 25f, 0.018f)));
			list.Add(new ValueTuple<ItemQuality, float>(ItemQuality.Fine, 0.36f * this.CalculateSigmoidFunction(explainedNumber.ResultNumber, 40f, 0.018f)));
			list.Add(new ValueTuple<ItemQuality, float>(ItemQuality.Masterwork, 0.27f * this.CalculateSigmoidFunction(explainedNumber.ResultNumber, 70f, 0.018f)));
			list.Add(new ValueTuple<ItemQuality, float>(ItemQuality.Legendary, 0.18f * this.CalculateSigmoidFunction(explainedNumber.ResultNumber, 115f, 0.018f)));
			float num2 = list.Sum(([TupleElementNames(new string[] { "quality", "probability" })] ValueTuple<ItemQuality, float> tuple) => tuple.Item2);
			for (int i = 0; i < list.Count; i++)
			{
				ValueTuple<ItemQuality, float> valueTuple = list[i];
				list[i] = new ValueTuple<ItemQuality, float>(valueTuple.Item1, valueTuple.Item2 / num2);
			}
			List<ItemQuality> list2 = new List<ItemQuality>();
			bool perkValue = hero.CharacterObject.GetPerkValue(DefaultPerks.Crafting.ExperiencedSmith);
			if (perkValue)
			{
				list2.Add(ItemQuality.Masterwork);
				list2.Add(ItemQuality.Legendary);
				DefaultSmithingModel.AdjustModifierProbabilities(list, ItemQuality.Fine, DefaultPerks.Crafting.ExperiencedSmith.PrimaryBonus, list2);
			}
			bool perkValue2 = hero.CharacterObject.GetPerkValue(DefaultPerks.Crafting.MasterSmith);
			if (perkValue2)
			{
				list2.Clear();
				list2.Add(ItemQuality.Legendary);
				if (perkValue)
				{
					list2.Add(ItemQuality.Fine);
				}
				DefaultSmithingModel.AdjustModifierProbabilities(list, ItemQuality.Masterwork, DefaultPerks.Crafting.MasterSmith.PrimaryBonus, list2);
			}
			if (hero.CharacterObject.GetPerkValue(DefaultPerks.Crafting.LegendarySmith))
			{
				list2.Clear();
				if (perkValue)
				{
					list2.Add(ItemQuality.Fine);
				}
				if (perkValue2)
				{
					list2.Add(ItemQuality.Masterwork);
				}
				float amount = DefaultPerks.Crafting.LegendarySmith.PrimaryBonus + Math.Max((float)(skillValue - 275), 0f) / 5f * 0.01f;
				DefaultSmithingModel.AdjustModifierProbabilities(list, ItemQuality.Legendary, amount, list2);
			}
			return list;
		}

		// Token: 0x06001A7B RID: 6779 RVA: 0x0008624C File Offset: 0x0008444C
		private static void AdjustModifierProbabilities([TupleElementNames(new string[] { "quality", "probability" })] List<ValueTuple<ItemQuality, float>> modifierProbabilities, ItemQuality qualityToAdjust, float amount, List<ItemQuality> qualitiesToIgnore)
		{
			int num = modifierProbabilities.Count - (qualitiesToIgnore.Count + 1);
			float num2 = amount / (float)num;
			float num3 = 0f;
			for (int i = 0; i < modifierProbabilities.Count; i++)
			{
				ValueTuple<ItemQuality, float> valueTuple = modifierProbabilities[i];
				if (valueTuple.Item1 == qualityToAdjust)
				{
					modifierProbabilities[i] = new ValueTuple<ItemQuality, float>(valueTuple.Item1, valueTuple.Item2 + amount);
				}
				else if (!qualitiesToIgnore.Contains(valueTuple.Item1))
				{
					float num4 = valueTuple.Item2 - (num2 + num3);
					if (num4 < 0f)
					{
						num3 = -num4;
						num4 = 0f;
					}
					else
					{
						num3 = 0f;
					}
					modifierProbabilities[i] = new ValueTuple<ItemQuality, float>(valueTuple.Item1, num4);
				}
			}
			float num5 = modifierProbabilities.Sum(([TupleElementNames(new string[] { "quality", "probability" })] ValueTuple<ItemQuality, float> tuple) => tuple.Item2);
			for (int j = 0; j < modifierProbabilities.Count; j++)
			{
				ValueTuple<ItemQuality, float> valueTuple2 = modifierProbabilities[j];
				modifierProbabilities[j] = new ValueTuple<ItemQuality, float>(valueTuple2.Item1, valueTuple2.Item2 / num5);
			}
		}

		// Token: 0x06001A7C RID: 6780 RVA: 0x00086374 File Offset: 0x00084574
		private ItemQuality AdjustQualityRegardingDesignTier(ItemQuality weaponQuality, WeaponDesign weaponDesign)
		{
			int num = 0;
			float num2 = 0f;
			foreach (WeaponDesignElement weaponDesignElement in weaponDesign.UsedPieces)
			{
				if (weaponDesignElement.IsValid)
				{
					num2 += (float)weaponDesignElement.CraftingPiece.PieceTier;
					num++;
				}
			}
			num2 /= (float)num;
			if (num2 >= 4.5f)
			{
				return weaponQuality;
			}
			if (num2 >= 3.5f)
			{
				if (weaponQuality < ItemQuality.Legendary)
				{
					return weaponQuality;
				}
				return ItemQuality.Masterwork;
			}
			else
			{
				if (weaponQuality < ItemQuality.Masterwork)
				{
					return weaponQuality;
				}
				return ItemQuality.Fine;
			}
		}

		// Token: 0x06001A7D RID: 6781 RVA: 0x000863E8 File Offset: 0x000845E8
		private float CalculateSigmoidFunction(float x, float mean, float curvature)
		{
			double num = Math.Exp((double)(curvature * (x - mean)));
			return (float)(num / (1.0 + num));
		}

		// Token: 0x06001A7E RID: 6782 RVA: 0x0008640F File Offset: 0x0008460F
		private float GetDifficultyForElement(WeaponDesignElement weaponDesignElement)
		{
			return (float)weaponDesignElement.CraftingPiece.PieceTier * (1f + 0.5f * weaponDesignElement.ScaleFactor);
		}

		// Token: 0x06001A7F RID: 6783 RVA: 0x00086430 File Offset: 0x00084630
		private void AddSmeltingReductions(int[] quantities)
		{
			if (quantities[6] > 0)
			{
				quantities[6]--;
				quantities[5]++;
			}
			else if (quantities[5] > 0)
			{
				quantities[5]--;
				quantities[4]++;
			}
			else if (quantities[4] > 0)
			{
				quantities[4]--;
				quantities[3]++;
			}
			else if (quantities[3] > 0)
			{
				quantities[3]--;
				quantities[2]++;
			}
			else if (quantities[2] > 0)
			{
				quantities[2]--;
				quantities[1]++;
			}
			quantities[8]--;
		}

		// Token: 0x06001A80 RID: 6784 RVA: 0x000864E8 File Offset: 0x000846E8
		public override int[] GetSmithingCostsForWeaponDesign(WeaponDesign weaponDesign)
		{
			int[] array = new int[9];
			foreach (WeaponDesignElement weaponDesignElement in weaponDesign.UsedPieces)
			{
				if (weaponDesignElement != null && weaponDesignElement.IsValid)
				{
					foreach (ValueTuple<CraftingMaterials, int> valueTuple in weaponDesignElement.CraftingPiece.MaterialsUsed)
					{
						array[(int)valueTuple.Item1] -= valueTuple.Item2;
					}
				}
			}
			array[8]--;
			return array;
		}

		// Token: 0x06001A81 RID: 6785 RVA: 0x0008658C File Offset: 0x0008478C
		public override float ResearchPointsNeedForNewPart(int totalPartCount, int openedPartCount)
		{
			return MathF.Sqrt(100f / (float)totalPartCount) * ((float)openedPartCount * 9f + 10f);
		}

		// Token: 0x06001A82 RID: 6786 RVA: 0x000865AC File Offset: 0x000847AC
		public override int GetPartResearchGainForSmeltingItem(ItemObject item, Hero hero)
		{
			ExplainedNumber explainedNumber = new ExplainedNumber(1f + (float)MathF.Round(0.02f * (float)item.Value), false, null);
			if (hero.GetPerkValue(DefaultPerks.Crafting.CuriousSmelter))
			{
				explainedNumber.AddFactor(DefaultPerks.Crafting.CuriousSmelter.PrimaryBonus, null);
			}
			return (int)explainedNumber.ResultNumber;
		}

		// Token: 0x06001A83 RID: 6787 RVA: 0x00086604 File Offset: 0x00084804
		public override int GetPartResearchGainForSmithingItem(ItemObject item, Hero hero, bool isFreeBuild)
		{
			ExplainedNumber explainedNumber = new ExplainedNumber(1f, false, null);
			if (hero.GetPerkValue(DefaultPerks.Crafting.CuriousSmith))
			{
				explainedNumber.AddFactor(DefaultPerks.Crafting.CuriousSmith.PrimaryBonus, null);
			}
			if (isFreeBuild)
			{
				explainedNumber.AddFactor(0.1f, null);
			}
			return 1 + MathF.Floor(0.1f * (float)item.Value * explainedNumber.ResultNumber);
		}

		// Token: 0x040008D2 RID: 2258
		private const int BladeDifficultyCalculationWeight = 100;

		// Token: 0x040008D3 RID: 2259
		private const int GuardDifficultyCalculationWeight = 20;

		// Token: 0x040008D4 RID: 2260
		private const int HandleDifficultyCalculationWeight = 60;

		// Token: 0x040008D5 RID: 2261
		private const int PommelDifficultyCalculationWeight = 20;
	}
}
