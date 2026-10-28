using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Localization;

namespace NavalDLC.CampaignBehaviors;

public interface INavalPatrolPartiesCampaignBehavior
{
	TextObject GetSettlementPatrolStatus(Settlement settlement);

	MobileParty GetNavalPatrolParty(Settlement settlement);
}
