namespace TaleWorlds.CampaignSystem.Actions;

public static class AddCompanionAction
{
	private static void ApplyInternal(Clan clan, Hero companion)
	{
		if (companion.CompanionOf != null)
		{
			RemoveCompanionAction.ApplyByFire(companion.CompanionOf, companion);
		}
		companion.CompanionOf = clan;
		CampaignEventDispatcher.Instance.OnNewCompanionAdded(companion);
	}

	public static void Apply(Clan clan, Hero companion)
	{
		ApplyInternal(clan, companion);
	}
}
