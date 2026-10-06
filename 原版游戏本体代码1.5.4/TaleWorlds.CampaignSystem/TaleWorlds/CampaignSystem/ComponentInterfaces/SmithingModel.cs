using System.Collections.Generic;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces;

public abstract class SmithingModel : MBGameModel<SmithingModel>
{
	public abstract int GetCraftingPartDifficulty(CraftingPiece craftingPiece);

	public abstract int CalculateWeaponDesignDifficulty(WeaponDesign weaponDesign);

	public abstract ItemModifier GetCraftedWeaponModifier(WeaponDesign weaponDesign, Hero weaponsmith);

	public abstract IEnumerable<Crafting.RefiningFormula> GetRefiningFormulas(Hero weaponsmith);

	public abstract ItemObject GetCraftingMaterialItem(CraftingMaterials craftingMaterial);

	public abstract int[] GetSmeltingOutputForItem(ItemObject item);

	public abstract int GetSkillXpForRefining(ref Crafting.RefiningFormula refineFormula);

	public abstract int GetSkillXpForSmelting(ItemObject item);

	public abstract int GetSkillXpForSmithingInFreeBuildMode(ItemObject item);

	public abstract int GetSkillXpForSmithingInCraftingOrderMode(ItemObject item);

	public abstract int[] GetSmithingCostsForWeaponDesign(WeaponDesign weaponDesign);

	public abstract int GetEnergyCostForRefining(ref Crafting.RefiningFormula refineFormula, Hero hero);

	public abstract int GetEnergyCostForSmithing(ItemObject item, Hero hero);

	public abstract int GetEnergyCostForSmelting(ItemObject item, Hero hero);

	public abstract float ResearchPointsNeedForNewPart(int totalPartCount, int openedPartCount);

	public abstract int GetPartResearchGainForSmeltingItem(ItemObject item, Hero hero);

	public abstract int GetPartResearchGainForSmithingItem(ItemObject item, Hero hero, bool isFreeBuildMode);
}
