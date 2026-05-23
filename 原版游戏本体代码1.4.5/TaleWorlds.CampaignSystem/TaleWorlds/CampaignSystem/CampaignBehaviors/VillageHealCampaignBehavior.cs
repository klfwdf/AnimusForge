using Helpers;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors;

public class VillageHealCampaignBehavior : CampaignBehaviorBase
{
	public override void RegisterEvents()
	{
		CampaignEvents.DailyTickSettlementEvent.AddNonSerializedListener(this, DailyTickSettlement);
	}

	private void DailyTickSettlement(Settlement settlement)
	{
		if ((settlement.IsVillage || settlement.IsTown) && settlement.SettlementHitPoints < 1f && settlement.Party.MapEvent == null && settlement.Party.SiegeEvent == null)
		{
			float num = (7000f - MathF.Min(7000f, MathF.Max(1000f, settlement.MapFaction.CurrentTotalStrength))) / 100000f;
			ExplainedNumber explainedNumber = new ExplainedNumber(0.06f + num);
			if (settlement.IsVillage && settlement.Village.TradeBound != null && PerkHelper.GetPerkValueForTown(DefaultPerks.Medicine.CleanInfrastructure, settlement.Village.TradeBound.Town))
			{
				explainedNumber.AddFactor(DefaultPerks.Medicine.CleanInfrastructure.SecondaryBonus, DefaultPerks.Medicine.CleanInfrastructure.Name);
			}
			if (settlement.OwnerClan.Leader.GetPerkValue(DefaultPerks.Roguery.InBestLight))
			{
				explainedNumber.AddFactor(DefaultPerks.Roguery.InBestLight.SecondaryBonus, DefaultPerks.Roguery.InBestLight.Name);
			}
			IncreaseSettlementHealthAction.Apply(settlement, explainedNumber.ResultNumber);
		}
	}

	public override void SyncData(IDataStore dataStore)
	{
	}
}
