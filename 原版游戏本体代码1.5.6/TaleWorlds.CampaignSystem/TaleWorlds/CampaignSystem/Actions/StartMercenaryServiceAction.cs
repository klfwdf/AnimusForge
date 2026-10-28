namespace TaleWorlds.CampaignSystem.Actions;

public static class StartMercenaryServiceAction
{
	public enum StartMercenaryServiceActionDetails
	{
		ApplyByDefault
	}

	private static void ApplyStart(Clan clan, Kingdom kingdom, int awardMultiplier, StartMercenaryServiceActionDetails details)
	{
		if (clan.IsUnderMercenaryService)
		{
			EndMercenaryServiceAction.EndByLeavingKingdom(clan);
		}
		clan.MercenaryAwardMultiplier = awardMultiplier;
		clan.Kingdom = kingdom;
		clan.StartMercenaryService();
		if (clan == Clan.PlayerClan)
		{
			Campaign.Current.KingdomManager.PlayerMercenaryServiceNextRenewalDay = Campaign.CurrentTime + 30f * (float)CampaignTime.HoursInDay;
		}
		CampaignEventDispatcher.Instance.OnMercenaryServiceStarted(clan, details);
	}

	public static void ApplyByDefault(Clan clan, Kingdom kingdom, int awardMultiplier)
	{
		ApplyStart(clan, kingdom, awardMultiplier, StartMercenaryServiceActionDetails.ApplyByDefault);
	}
}
