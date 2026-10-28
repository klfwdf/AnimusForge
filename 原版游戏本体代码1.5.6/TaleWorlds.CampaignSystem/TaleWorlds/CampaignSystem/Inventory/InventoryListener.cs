using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.Inventory;

public abstract class InventoryListener
{
	public abstract int GetGold();

	public abstract TextObject GetTraderName();

	public abstract void SetGold(int gold);

	public abstract PartyBase GetOppositeParty();

	public abstract void OnTransaction();
}
