using System.Linq;

namespace TaleWorlds.CampaignSystem.Actions;

public static class DestroyKingdomAction
{
	private static void ApplyInternal(Kingdom destroyedKingdom, bool isKingdomLeaderDeath = false)
	{
		destroyedKingdom.DeactivateKingdom();
		foreach (Clan item in destroyedKingdom.Clans.ToList())
		{
			if (!item.IsEliminated)
			{
				if (isKingdomLeaderDeath)
				{
					DestroyClanAction.ApplyByClanLeaderDeath(item);
				}
				else
				{
					DestroyClanAction.Apply(item);
				}
				destroyedKingdom.RemoveClanInternal(item);
			}
		}
		Campaign.Current.FactionManager.RemoveFactionsFromCampaignWars(destroyedKingdom);
		CampaignEventDispatcher.Instance.OnKingdomDestroyed(destroyedKingdom);
	}

	public static void Apply(Kingdom destroyedKingdom)
	{
		ApplyInternal(destroyedKingdom);
	}

	public static void ApplyByKingdomLeaderDeath(Kingdom destroyedKingdom)
	{
		ApplyInternal(destroyedKingdom, isKingdomLeaderDeath: true);
	}
}
