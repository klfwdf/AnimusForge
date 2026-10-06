using TaleWorlds.Library.EventSystem;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.WeaponCrafting.WeaponDesign;

public class CraftingWeaponResultPopupToggledEvent : EventBase
{
	public bool IsOpen { get; }

	public CraftingWeaponResultPopupToggledEvent(bool isOpen)
	{
		IsOpen = isOpen;
	}
}
