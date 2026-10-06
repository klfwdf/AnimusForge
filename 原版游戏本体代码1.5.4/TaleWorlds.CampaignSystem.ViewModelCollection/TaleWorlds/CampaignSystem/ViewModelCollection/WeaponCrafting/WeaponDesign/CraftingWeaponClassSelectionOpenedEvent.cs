using TaleWorlds.Library.EventSystem;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.WeaponCrafting.WeaponDesign;

public class CraftingWeaponClassSelectionOpenedEvent : EventBase
{
	public bool IsOpen { get; private set; }

	public CraftingWeaponClassSelectionOpenedEvent(bool isOpen)
	{
		IsOpen = isOpen;
	}
}
