using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Library.EventSystem;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia;

public class PlayerToggleTrackSettlementFromEncyclopediaEvent : EventBase
{
	public bool IsCurrentlyTracked { get; private set; }

	public Settlement ToggledTrackedSettlement { get; private set; }

	public PlayerToggleTrackSettlementFromEncyclopediaEvent(Settlement toggleTrackedSettlement, bool isCurrentlyTracked)
	{
		ToggledTrackedSettlement = toggleTrackedSettlement;
		IsCurrentlyTracked = isCurrentlyTracked;
	}
}
