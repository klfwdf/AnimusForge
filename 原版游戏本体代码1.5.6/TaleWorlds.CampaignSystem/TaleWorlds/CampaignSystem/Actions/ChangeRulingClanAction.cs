namespace TaleWorlds.CampaignSystem.Actions;

public class ChangeRulingClanAction
{
	private static void ApplyInternal(Kingdom kingdom, Clan newRulerClan)
	{
		Clan rulingClan = kingdom.RulingClan;
		kingdom.RulingClan = newRulerClan;
		CampaignEventDispatcher.Instance.OnRulingClanChanged(kingdom, rulingClan);
	}

	public static void Apply(Kingdom kingdom, Clan clan)
	{
		ApplyInternal(kingdom, clan);
	}
}
