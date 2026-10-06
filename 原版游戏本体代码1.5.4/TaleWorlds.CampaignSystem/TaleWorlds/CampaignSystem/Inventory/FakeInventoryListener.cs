using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.Inventory;

public class FakeInventoryListener : InventoryListener
{
	public override int GetGold()
	{
		return 0;
	}

	public override TextObject GetTraderName()
	{
		return TextObject.GetEmpty();
	}

	public override void SetGold(int gold)
	{
	}

	public override void OnTransaction()
	{
	}

	public override PartyBase GetOppositeParty()
	{
		return null;
	}
}
