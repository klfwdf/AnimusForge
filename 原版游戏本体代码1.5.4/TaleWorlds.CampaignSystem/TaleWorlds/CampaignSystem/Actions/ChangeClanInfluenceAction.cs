namespace TaleWorlds.CampaignSystem.Actions;

public static class ChangeClanInfluenceAction
{
	private static void ApplyInternal(Clan clan, float amount)
	{
		clan.Influence += amount;
		CampaignEventDispatcher.Instance.OnClanInfluenceChanged(clan, amount);
	}

	public static void Apply(Clan clan, float amount)
	{
		ApplyInternal(clan, amount);
	}
}
