using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces;

public abstract class SettlementPatrolModel : MBGameModel<SettlementPatrolModel>
{
	public abstract CampaignTime GetPatrolPartySpawnDuration(Settlement settlement, bool naval);

	public abstract bool CanSettlementHavePatrolParties(Settlement settlement, bool naval);

	public abstract PartyTemplateObject GetPartyTemplateForPatrolParty(Settlement settlement, bool naval);
}
