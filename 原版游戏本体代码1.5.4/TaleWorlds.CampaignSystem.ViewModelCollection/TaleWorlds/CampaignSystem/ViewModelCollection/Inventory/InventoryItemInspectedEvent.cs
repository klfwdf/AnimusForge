using TaleWorlds.CampaignSystem.Inventory;
using TaleWorlds.Core;
using TaleWorlds.Library.EventSystem;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Inventory;

public class InventoryItemInspectedEvent : EventBase
{
	public ItemRosterElement Item { get; private set; }

	public InventoryLogic.InventorySide ItemSide { get; private set; }

	public InventoryItemInspectedEvent(ItemRosterElement item, InventoryLogic.InventorySide itemSide)
	{
		ItemSide = itemSide;
		Item = item;
	}
}
