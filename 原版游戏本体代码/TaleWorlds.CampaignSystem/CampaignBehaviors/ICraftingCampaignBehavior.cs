using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem.CraftingSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors
{
	// Token: 0x020003FA RID: 1018
	public interface ICraftingCampaignBehavior : ICampaignBehavior
	{
		// Token: 0x17000E17 RID: 3607
		// (get) Token: 0x06003F96 RID: 16278
		IReadOnlyDictionary<Town, CraftingCampaignBehavior.CraftingOrderSlots> CraftingOrders { get; }

		// Token: 0x17000E18 RID: 3608
		// (get) Token: 0x06003F97 RID: 16279
		IReadOnlyCollection<WeaponDesign> CraftingHistory { get; }

		// Token: 0x06003F98 RID: 16280
		void CompleteOrder(Town town, CraftingOrder craftingOrder, ItemObject craftedItem, Hero completerHero);

		// Token: 0x06003F99 RID: 16281
		ItemModifier GetCurrentItemModifier();

		// Token: 0x06003F9A RID: 16282
		void SetCurrentItemModifier(ItemModifier modifier);

		// Token: 0x06003F9B RID: 16283
		void SetCraftedWeaponName(ItemObject craftedWeaponItem, TextObject name);

		// Token: 0x06003F9C RID: 16284
		void GetOrderResult(CraftingOrder craftingOrder, ItemObject craftedItem, out bool isSucceed, out TextObject orderRemark, out TextObject orderResult, out int finalPrice);

		// Token: 0x06003F9D RID: 16285
		int GetCraftingDifficulty(WeaponDesign weaponDesign);

		// Token: 0x06003F9E RID: 16286
		int GetHeroCraftingStamina(Hero hero);

		// Token: 0x06003F9F RID: 16287
		void SetHeroCraftingStamina(Hero hero, int value);

		// Token: 0x06003FA0 RID: 16288
		int GetMaxHeroCraftingStamina(Hero hero);

		// Token: 0x06003FA1 RID: 16289
		void DoRefinement(Hero hero, Crafting.RefiningFormula refineFormula);

		// Token: 0x06003FA2 RID: 16290
		void DoSmelting(Hero currentCraftingHero, EquipmentElement equipmentElement);

		// Token: 0x06003FA3 RID: 16291
		ItemObject CreateCraftedWeaponInFreeBuildMode(Hero hero, WeaponDesign weaponDesign, ItemModifier weaponModifier = null);

		// Token: 0x06003FA4 RID: 16292
		ItemObject CreateCraftedWeaponInCraftingOrderMode(Hero crafterHero, CraftingOrder craftingOrder, WeaponDesign weaponDesign);

		// Token: 0x06003FA5 RID: 16293
		bool IsOpened(CraftingPiece craftingPiece, CraftingTemplate craftingTemplate);

		// Token: 0x06003FA6 RID: 16294
		CraftingOrder CreateCustomOrderForHero(Hero orderOwner, float orderDifficulty = -1f, WeaponDesign weaponDesign = null, CraftingTemplate craftingTemplate = null);

		// Token: 0x06003FA7 RID: 16295
		void CancelCustomOrder(Town town, CraftingOrder craftingOrder);

		// Token: 0x06003FA8 RID: 16296
		Hero GetActiveCraftingHero();

		// Token: 0x06003FA9 RID: 16297
		void SetActiveCraftingHero(Hero hero);
	}
}
