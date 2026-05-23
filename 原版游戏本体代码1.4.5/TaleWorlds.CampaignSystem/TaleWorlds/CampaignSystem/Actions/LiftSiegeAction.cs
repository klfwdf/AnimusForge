using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;

namespace TaleWorlds.CampaignSystem.Actions;

public static class LiftSiegeAction
{
	private static void ApplyInternal(MobileParty side1Party, Settlement settlement)
	{
		settlement.SiegeEvent.BesiegerCamp.RemoveAllSiegeParties();
	}

	public static void GetGameAction(MobileParty side1Party)
	{
		ApplyInternal(side1Party, side1Party.BesiegedSettlement);
	}
}
