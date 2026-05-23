using System;
using System.Collections.Generic;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces
{
	// Token: 0x0200019D RID: 413
	public abstract class SmithingModel : MBGameModel<SmithingModel>
	{
		// Token: 0x06001C51 RID: 7249
		public abstract int GetCraftingPartDifficulty(CraftingPiece craftingPiece);

		// Token: 0x06001C52 RID: 7250
		public abstract int CalculateWeaponDesignDifficulty(WeaponDesign weaponDesign);

		// Token: 0x06001C53 RID: 7251
		public abstract ItemModifier GetCraftedWeaponModifier(WeaponDesign weaponDesign, Hero weaponsmith);

		// Token: 0x06001C54 RID: 7252
		public abstract IEnumerable<Crafting.RefiningFormula> GetRefiningFormulas(Hero weaponsmith);

		// Token: 0x06001C55 RID: 7253
		public abstract ItemObject GetCraftingMaterialItem(CraftingMaterials craftingMaterial);

		// Token: 0x06001C56 RID: 7254
		public abstract int[] GetSmeltingOutputForItem(ItemObject item);

		// Token: 0x06001C57 RID: 7255
		public abstract int GetSkillXpForRefining(ref Crafting.RefiningFormula refineFormula);

		// Token: 0x06001C58 RID: 7256
		public abstract int GetSkillXpForSmelting(ItemObject item);

		// Token: 0x06001C59 RID: 7257
		public abstract int GetSkillXpForSmithingInFreeBuildMode(ItemObject item);

		// Token: 0x06001C5A RID: 7258
		public abstract int GetSkillXpForSmithingInCraftingOrderMode(ItemObject item);

		// Token: 0x06001C5B RID: 7259
		public abstract int[] GetSmithingCostsForWeaponDesign(WeaponDesign weaponDesign);

		// Token: 0x06001C5C RID: 7260
		public abstract int GetEnergyCostForRefining(ref Crafting.RefiningFormula refineFormula, Hero hero);

		// Token: 0x06001C5D RID: 7261
		public abstract int GetEnergyCostForSmithing(ItemObject item, Hero hero);

		// Token: 0x06001C5E RID: 7262
		public abstract int GetEnergyCostForSmelting(ItemObject item, Hero hero);

		// Token: 0x06001C5F RID: 7263
		public abstract float ResearchPointsNeedForNewPart(int totalPartCount, int openedPartCount);

		// Token: 0x06001C60 RID: 7264
		public abstract int GetPartResearchGainForSmeltingItem(ItemObject item, Hero hero);

		// Token: 0x06001C61 RID: 7265
		public abstract int GetPartResearchGainForSmithingItem(ItemObject item, Hero hero, bool isFreeBuildMode);
	}
}
