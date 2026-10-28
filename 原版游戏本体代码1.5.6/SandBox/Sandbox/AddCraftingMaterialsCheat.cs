using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace SandBox;

public class AddCraftingMaterialsCheat : GameplayCheatItem
{
	public override void ExecuteCheat()
	{
		for (CraftingMaterials craftingMaterials = CraftingMaterials.IronOre; craftingMaterials < CraftingMaterials.NumCraftingMats; craftingMaterials++)
		{
			ItemObject craftingMaterialItem = Campaign.Current.Models.SmithingModel.GetCraftingMaterialItem(craftingMaterials);
			PartyBase.MainParty.ItemRoster.AddToCounts(craftingMaterialItem, 10);
		}
	}

	public override TextObject GetName()
	{
		return new TextObject("{=63jJ3GGY}Add 10 Crafting Materials Each");
	}
}
