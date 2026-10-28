using TaleWorlds.Library.EventSystem;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.WeaponCrafting.WeaponDesign;

public class CraftingOrderTabOpenedEvent : EventBase
{
	public bool IsOpen { get; private set; }

	public CraftingOrderTabOpenedEvent(bool isOpen)
	{
		IsOpen = isOpen;
	}
}
