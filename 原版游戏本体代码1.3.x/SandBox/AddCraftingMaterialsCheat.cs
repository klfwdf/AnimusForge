using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace SandBox
{
	// Token: 0x0200000F RID: 15
	public class AddCraftingMaterialsCheat : GameplayCheatItem
	{
		// Token: 0x0600002D RID: 45 RVA: 0x000038A8 File Offset: 0x00001AA8
		public override void ExecuteCheat()
		{
			for (CraftingMaterials craftingMaterials = CraftingMaterials.IronOre; craftingMaterials < CraftingMaterials.NumCraftingMats; craftingMaterials++)
			{
				ItemObject craftingMaterialItem = Campaign.Current.Models.SmithingModel.GetCraftingMaterialItem(craftingMaterials);
				PartyBase.MainParty.ItemRoster.AddToCounts(craftingMaterialItem, 10);
			}
		}

		// Token: 0x0600002E RID: 46 RVA: 0x000038EB File Offset: 0x00001AEB
		public override TextObject GetName()
		{
			return new TextObject("{=63jJ3GGY}Add 10 Crafting Materials Each", null);
		}
	}
}
