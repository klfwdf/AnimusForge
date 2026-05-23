using System;
using System.Collections.Generic;
using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.GameComponents;

public class DefaultSmithingModel : SmithingModel
{
	private const int BladeDifficultyCalculationWeight = 100;

	private const int GuardDifficultyCalculationWeight = 20;

	private const int HandleDifficultyCalculationWeight = 60;

	private const int PommelDifficultyCalculationWeight = 20;

	public override int GetCraftingPartDifficulty(CraftingPiece craftingPiece)
	{
		if (!craftingPiece.IsEmptyPiece)
		{
			return craftingPiece.PieceTier * 50;
		}
		return 0;
	}

	public override int CalculateWeaponDesignDifficulty(WeaponDesign weaponDesign)
	{
		float num = 0f;
		float num2 = 0f;
		WeaponDesignElement[] usedPieces = weaponDesign.UsedPieces;
		foreach (WeaponDesignElement weaponDesignElement in usedPieces)
		{
			if (weaponDesignElement.IsValid && !weaponDesignElement.CraftingPiece.IsEmptyPiece)
			{
				if (weaponDesignElement.CraftingPiece.PieceType == CraftingPiece.PieceTypes.Blade)
				{
					num += 100f;
					num2 += (float)(GetCraftingPartDifficulty(weaponDesignElement.CraftingPiece) * 100);
				}
				else if (weaponDesignElement.CraftingPiece.PieceType == CraftingPiece.PieceTypes.Guard)
				{
					num += 20f;
					num2 += (float)(GetCraftingPartDifficulty(weaponDesignElement.CraftingPiece) * 20);
				}
				else if (weaponDesignElement.CraftingPiece.PieceType == CraftingPiece.PieceTypes.Handle)
				{
					num += 60f;
					num2 += (float)(GetCraftingPartDifficulty(weaponDesignElement.CraftingPiece) * 60);
				}
				else if (weaponDesignElement.CraftingPiece.PieceType == CraftingPiece.PieceTypes.Pommel)
				{
					num += 20f;
					num2 += (float)(GetCraftingPartDifficulty(weaponDesignElement.CraftingPiece) * 20);
				}
			}
		}
		return TaleWorlds.Library.MathF.Round(num2 / num);
	}

	public override ItemModifier GetCraftedWeaponModifier(WeaponDesign weaponDesign, Hero hero)
	{
		List<(ItemQuality quality, float probability)> modifierQualityProbabilities = GetModifierQualityProbabilities(weaponDesign, hero);
		ItemQuality weaponQuality = modifierQualityProbabilities.Last().quality;
		float num = MBRandom.RandomFloat;
		foreach (var item in modifierQualityProbabilities)
		{
			if (num <= item.probability)
			{
				(weaponQuality, _) = item;
				break;
			}
			num -= item.probability;
		}
		weaponQuality = AdjustQualityRegardingDesignTier(weaponQuality, weaponDesign);
		List<ItemModifier> modifiersBasedOnQuality = weaponDesign.Template.ItemModifierGroup.GetModifiersBasedOnQuality(weaponQuality);
		if (modifiersBasedOnQuality.IsEmpty())
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

	public override IEnumerable<Crafting.RefiningFormula> GetRefiningFormulas(Hero weaponsmith)
	{
		if (weaponsmith.GetPerkValue(DefaultPerks.Crafting.CharcoalMaker))
		{
			yield return new Crafting.RefiningFormula(CraftingMaterials.Wood, 2, CraftingMaterials.Iron1, 0, CraftingMaterials.Charcoal, 3);
		}
		else
		{
			yield return new Crafting.RefiningFormula(CraftingMaterials.Wood, 2, CraftingMaterials.Iron1, 0, CraftingMaterials.Charcoal);
		}
		yield return new Crafting.RefiningFormula(CraftingMaterials.IronOre, 1, CraftingMaterials.Charcoal, 1, CraftingMaterials.Iron1, weaponsmith.GetPerkValue(DefaultPerks.Crafting.IronMaker) ? 3 : 2);
		yield return new Crafting.RefiningFormula(CraftingMaterials.Iron1, 1, CraftingMaterials.Charcoal, 1, CraftingMaterials.Iron2);
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
	}

	public override int GetSkillXpForRefining(ref Crafting.RefiningFormula refineFormula)
	{
		return TaleWorlds.Library.MathF.Round(0.3f * (float)(GetCraftingMaterialItem(refineFormula.Output).Value * refineFormula.OutputCount));
	}

	public override int GetSkillXpForSmelting(ItemObject item)
	{
		return TaleWorlds.Library.MathF.Round(0.02f * (float)item.Value);
	}

	public override int GetSkillXpForSmithingInFreeBuildMode(ItemObject item)
	{
		return TaleWorlds.Library.MathF.Round(0.02f * (float)item.Value);
	}

	public override int GetSkillXpForSmithingInCraftingOrderMode(ItemObject item)
	{
		return TaleWorlds.Library.MathF.Round(0.1f * (float)item.Value);
	}

	public override int GetEnergyCostForRefining(ref Crafting.RefiningFormula refineFormula, Hero hero)
	{
		ExplainedNumber explainedNumber = new ExplainedNumber(6f);
		if (hero.GetPerkValue(DefaultPerks.Crafting.PracticalRefiner))
		{
			explainedNumber.AddFactor(DefaultPerks.Crafting.PracticalRefiner.PrimaryBonus);
		}
		return (int)explainedNumber.ResultNumber;
	}

	public override int GetEnergyCostForSmithing(ItemObject item, Hero hero)
	{
		ExplainedNumber explainedNumber = new ExplainedNumber(10 + 5 * (int)item.Tier);
		if (hero.GetPerkValue(DefaultPerks.Crafting.PracticalSmith))
		{
			explainedNumber.AddFactor(DefaultPerks.Crafting.PracticalSmith.PrimaryBonus);
		}
		return (int)explainedNumber.ResultNumber;
	}

	public override int GetEnergyCostForSmelting(ItemObject item, Hero hero)
	{
		ExplainedNumber explainedNumber = new ExplainedNumber(10f);
		if (hero.GetPerkValue(DefaultPerks.Crafting.PracticalSmelter))
		{
			explainedNumber.AddFactor(DefaultPerks.Crafting.PracticalSmelter.PrimaryBonus);
		}
		return (int)explainedNumber.ResultNumber;
	}

	public override ItemObject GetCraftingMaterialItem(CraftingMaterials craftingMaterial)
	{
		return craftingMaterial switch
		{
			CraftingMaterials.IronOre => DefaultItems.IronOre, 
			CraftingMaterials.Iron1 => DefaultItems.IronIngot1, 
			CraftingMaterials.Iron2 => DefaultItems.IronIngot2, 
			CraftingMaterials.Iron3 => DefaultItems.IronIngot3, 
			CraftingMaterials.Iron4 => DefaultItems.IronIngot4, 
			CraftingMaterials.Iron5 => DefaultItems.IronIngot5, 
			CraftingMaterials.Iron6 => DefaultItems.IronIngot6, 
			CraftingMaterials.Wood => DefaultItems.HardWood, 
			CraftingMaterials.Charcoal => DefaultItems.Charcoal, 
			_ => DefaultItems.IronIngot1, 
		};
	}

	public override int[] GetSmeltingOutputForItem(ItemObject item)
	{
		int[] array = new int[9];
		if (item.WeaponDesign != null)
		{
			WeaponDesignElement[] usedPieces = item.WeaponDesign.UsedPieces;
			foreach (WeaponDesignElement weaponDesignElement in usedPieces)
			{
				if (weaponDesignElement == null || !weaponDesignElement.IsValid)
				{
					continue;
				}
				foreach (var item2 in weaponDesignElement.CraftingPiece.MaterialsUsed)
				{
					array[(int)item2.Item1] += item2.Item2;
				}
			}
			AddSmeltingReductions(array);
		}
		return array;
	}

	private List<(ItemQuality quality, float probability)> GetModifierQualityProbabilities(WeaponDesign weaponDesign, Hero hero)
	{
		int num = CalculateWeaponDesignDifficulty(weaponDesign);
		int skillValue = hero.CharacterObject.GetSkillValue(DefaultSkills.Crafting);
		List<(ItemQuality, float)> list = new List<(ItemQuality, float)>();
		ExplainedNumber explainedNumber = new ExplainedNumber(-num);
		SkillHelper.AddSkillBonusForCharacter(DefaultSkillEffects.SmithingLevel, hero.CharacterObject, ref explainedNumber);
		explainedNumber.LimitMin(-300f);
		explainedNumber.LimitMax(300f);
		list.Add((ItemQuality.Poor, 0.36f * (1f - CalculateSigmoidFunction(explainedNumber.ResultNumber, -70f, 0.018f))));
		list.Add((ItemQuality.Inferior, 0.45f * (1f - CalculateSigmoidFunction(explainedNumber.ResultNumber, -55f, 0.018f))));
		list.Add((ItemQuality.Common, CalculateSigmoidFunction(explainedNumber.ResultNumber, 25f, 0.018f)));
		list.Add((ItemQuality.Fine, 0.36f * CalculateSigmoidFunction(explainedNumber.ResultNumber, 40f, 0.018f)));
		list.Add((ItemQuality.Masterwork, 0.27f * CalculateSigmoidFunction(explainedNumber.ResultNumber, 70f, 0.018f)));
		list.Add((ItemQuality.Legendary, 0.18f * CalculateSigmoidFunction(explainedNumber.ResultNumber, 115f, 0.018f)));
		float num2 = list.Sum<(ItemQuality, float)>(((ItemQuality quality, float probability) tuple2) => tuple2.probability);
		for (int num3 = 0; num3 < list.Count; num3++)
		{
			(ItemQuality, float) tuple = list[num3];
			list[num3] = (tuple.Item1, tuple.Item2 / num2);
		}
		List<ItemQuality> list2 = new List<ItemQuality>();
		bool perkValue = hero.CharacterObject.GetPerkValue(DefaultPerks.Crafting.ExperiencedSmith);
		if (perkValue)
		{
			list2.Add(ItemQuality.Masterwork);
			list2.Add(ItemQuality.Legendary);
			AdjustModifierProbabilities(list, ItemQuality.Fine, DefaultPerks.Crafting.ExperiencedSmith.PrimaryBonus, list2);
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
			AdjustModifierProbabilities(list, ItemQuality.Masterwork, DefaultPerks.Crafting.MasterSmith.PrimaryBonus, list2);
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
			float amount = DefaultPerks.Crafting.LegendarySmith.PrimaryBonus + Math.Max(skillValue - 275, 0f) / 5f * 0.01f;
			AdjustModifierProbabilities(list, ItemQuality.Legendary, amount, list2);
		}
		return list;
	}

	private static void AdjustModifierProbabilities(List<(ItemQuality quality, float probability)> modifierProbabilities, ItemQuality qualityToAdjust, float amount, List<ItemQuality> qualitiesToIgnore)
	{
		int num = modifierProbabilities.Count - (qualitiesToIgnore.Count + 1);
		float num2 = amount / (float)num;
		float num3 = 0f;
		for (int i = 0; i < modifierProbabilities.Count; i++)
		{
			(ItemQuality, float) tuple = modifierProbabilities[i];
			if (tuple.Item1 == qualityToAdjust)
			{
				modifierProbabilities[i] = (tuple.Item1, tuple.Item2 + amount);
			}
			else if (!qualitiesToIgnore.Contains(tuple.Item1))
			{
				float num4 = tuple.Item2 - (num2 + num3);
				if (num4 < 0f)
				{
					num3 = 0f - num4;
					num4 = 0f;
				}
				else
				{
					num3 = 0f;
				}
				modifierProbabilities[i] = (tuple.Item1, num4);
			}
		}
		float num5 = modifierProbabilities.Sum(((ItemQuality quality, float probability) tuple3) => tuple3.probability);
		for (int num6 = 0; num6 < modifierProbabilities.Count; num6++)
		{
			(ItemQuality, float) tuple2 = modifierProbabilities[num6];
			modifierProbabilities[num6] = (tuple2.Item1, tuple2.Item2 / num5);
		}
	}

	private ItemQuality AdjustQualityRegardingDesignTier(ItemQuality weaponQuality, WeaponDesign weaponDesign)
	{
		int num = 0;
		float num2 = 0f;
		WeaponDesignElement[] usedPieces = weaponDesign.UsedPieces;
		foreach (WeaponDesignElement weaponDesignElement in usedPieces)
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
		if (weaponQuality < ItemQuality.Masterwork)
		{
			return weaponQuality;
		}
		return ItemQuality.Fine;
	}

	private float CalculateSigmoidFunction(float x, float mean, float curvature)
	{
		double num = Math.Exp(curvature * (x - mean));
		return (float)(num / (1.0 + num));
	}

	private float GetDifficultyForElement(WeaponDesignElement weaponDesignElement)
	{
		return (float)weaponDesignElement.CraftingPiece.PieceTier * (1f + 0.5f * weaponDesignElement.ScaleFactor);
	}

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

	public override int[] GetSmithingCostsForWeaponDesign(WeaponDesign weaponDesign)
	{
		int[] array = new int[9];
		WeaponDesignElement[] usedPieces = weaponDesign.UsedPieces;
		foreach (WeaponDesignElement weaponDesignElement in usedPieces)
		{
			if (weaponDesignElement == null || !weaponDesignElement.IsValid)
			{
				continue;
			}
			foreach (var item in weaponDesignElement.CraftingPiece.MaterialsUsed)
			{
				array[(int)item.Item1] -= item.Item2;
			}
		}
		array[8]--;
		return array;
	}

	public override float ResearchPointsNeedForNewPart(int totalPartCount, int openedPartCount)
	{
		return TaleWorlds.Library.MathF.Sqrt(100f / (float)totalPartCount) * ((float)openedPartCount * 9f + 10f);
	}

	public override int GetPartResearchGainForSmeltingItem(ItemObject item, Hero hero)
	{
		ExplainedNumber explainedNumber = new ExplainedNumber(1f + (float)TaleWorlds.Library.MathF.Round(0.02f * (float)item.Value));
		if (hero.GetPerkValue(DefaultPerks.Crafting.CuriousSmelter))
		{
			explainedNumber.AddFactor(DefaultPerks.Crafting.CuriousSmelter.PrimaryBonus);
		}
		return (int)explainedNumber.ResultNumber;
	}

	public override int GetPartResearchGainForSmithingItem(ItemObject item, Hero hero, bool isFreeBuild)
	{
		ExplainedNumber explainedNumber = new ExplainedNumber(1f);
		if (hero.GetPerkValue(DefaultPerks.Crafting.CuriousSmith))
		{
			explainedNumber.AddFactor(DefaultPerks.Crafting.CuriousSmith.PrimaryBonus);
		}
		if (isFreeBuild)
		{
			explainedNumber.AddFactor(0.1f);
		}
		return 1 + TaleWorlds.Library.MathF.Floor(0.1f * (float)item.Value * explainedNumber.ResultNumber);
	}
}
