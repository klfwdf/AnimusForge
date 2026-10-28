using TaleWorlds.CampaignSystem.Settlements;

namespace TaleWorlds.CampaignSystem.Actions;

public static class IncreaseSettlementHealthAction
{
	private static void ApplyInternal(Settlement settlement, float percentage)
	{
		settlement.SettlementHitPoints += percentage;
		settlement.SettlementHitPoints = ((settlement.SettlementHitPoints > 1f) ? 1f : settlement.SettlementHitPoints);
		if (settlement.SettlementHitPoints >= 1f && settlement.IsVillage && settlement.Village.VillageState != Village.VillageStates.Normal)
		{
			ChangeVillageStateAction.ApplyBySettingToNormal(settlement);
			settlement.Militia += 20f;
		}
	}

	public static void Apply(Settlement settlement, float percentage)
	{
		ApplyInternal(settlement, percentage);
	}
}
