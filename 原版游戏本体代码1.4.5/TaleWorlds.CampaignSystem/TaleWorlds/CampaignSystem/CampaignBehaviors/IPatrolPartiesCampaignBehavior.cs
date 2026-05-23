using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors;

public interface IPatrolPartiesCampaignBehavior
{
	TextObject GetSettlementPatrolStatus(Settlement settlement);
}
