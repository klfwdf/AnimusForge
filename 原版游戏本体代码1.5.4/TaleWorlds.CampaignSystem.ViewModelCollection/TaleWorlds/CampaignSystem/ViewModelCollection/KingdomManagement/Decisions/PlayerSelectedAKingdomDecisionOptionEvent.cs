using TaleWorlds.CampaignSystem.Election;
using TaleWorlds.Library.EventSystem;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.KingdomManagement.Decisions;

public class PlayerSelectedAKingdomDecisionOptionEvent : EventBase
{
	public DecisionOutcome Option { get; private set; }

	public PlayerSelectedAKingdomDecisionOptionEvent(DecisionOutcome option)
	{
		Option = option;
	}
}
