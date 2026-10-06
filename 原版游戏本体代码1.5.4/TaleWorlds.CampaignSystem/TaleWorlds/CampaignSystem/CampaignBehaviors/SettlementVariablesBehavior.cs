using TaleWorlds.CampaignSystem.Settlements;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors;

public class SettlementVariablesBehavior : CampaignBehaviorBase
{
	private float _resetLastAttackerPartyAsDays = 1f;

	public override void RegisterEvents()
	{
		CampaignEvents.HourlyTickSettlementEvent.AddNonSerializedListener(this, HourlyTickSettlement);
	}

	public override void SyncData(IDataStore dataStore)
	{
	}

	private void HourlyTickSettlement(Settlement settlement)
	{
		if (settlement.LastAttackerParty != null && settlement.Party.MapEvent == null && settlement.Party.SiegeEvent == null && settlement.LastThreatTime.ElapsedDaysUntilNow > _resetLastAttackerPartyAsDays)
		{
			settlement.LastAttackerParty = null;
		}
	}
}
