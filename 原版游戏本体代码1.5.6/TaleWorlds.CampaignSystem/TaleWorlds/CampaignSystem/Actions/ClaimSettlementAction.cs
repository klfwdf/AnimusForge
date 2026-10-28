using TaleWorlds.CampaignSystem.Settlements;

namespace TaleWorlds.CampaignSystem.Actions;

public static class ClaimSettlementAction
{
	private static void ApplyInternal(Hero claimant, Settlement claimedSettlement)
	{
		ImpactRelations(claimant, claimedSettlement);
	}

	private static void ImpactRelations(Hero claimant, Settlement claimedSettlement)
	{
		if (claimedSettlement.OwnerClan.Leader != null)
		{
			ChangeRelationAction.ApplyRelationChangeBetweenHeroes(claimant, claimedSettlement.OwnerClan.Leader, -50, showQuickNotification: false);
			if (!claimedSettlement.OwnerClan.IsMapFaction)
			{
				ChangeRelationAction.ApplyRelationChangeBetweenHeroes(claimant, claimedSettlement.OwnerClan.Leader, -20, showQuickNotification: false);
			}
		}
	}

	public static void Apply(Hero claimant, Settlement claimedSettlement)
	{
		ApplyInternal(claimant, claimedSettlement);
	}
}
