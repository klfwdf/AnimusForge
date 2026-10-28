using TaleWorlds.Library.EventSystem;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Party;

public class PlayerToggledUpgradePopupEvent : EventBase
{
	public bool IsOpened { get; private set; }

	public PlayerToggledUpgradePopupEvent(bool isOpened)
	{
		IsOpened = isOpened;
	}
}
