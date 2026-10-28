namespace TaleWorlds.CampaignSystem.Actions;

public static class EndMercenaryServiceAction
{
	public enum EndMercenaryServiceActionDetails
	{
		ApplyByDefault,
		ApplyByLeavingKingdom,
		ApplyByBecomingVassal
	}

	private static void Apply(Clan clan, EndMercenaryServiceActionDetails details)
	{
		clan.EndMercenaryService(details == EndMercenaryServiceActionDetails.ApplyByLeavingKingdom);
		CampaignEventDispatcher.Instance.OnMercenaryServiceEnded(clan, details);
	}

	public static void EndByDefault(Clan clan)
	{
		Apply(clan, EndMercenaryServiceActionDetails.ApplyByDefault);
	}

	public static void EndByLeavingKingdom(Clan clan)
	{
		Apply(clan, EndMercenaryServiceActionDetails.ApplyByLeavingKingdom);
	}

	public static void EndByBecomingVassal(Clan clan)
	{
		Apply(clan, EndMercenaryServiceActionDetails.ApplyByBecomingVassal);
	}
}
