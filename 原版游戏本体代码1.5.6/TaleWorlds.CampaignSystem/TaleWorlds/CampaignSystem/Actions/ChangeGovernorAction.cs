using TaleWorlds.CampaignSystem.Settlements;

namespace TaleWorlds.CampaignSystem.Actions;

public static class ChangeGovernorAction
{
	private static void ApplyInternal(Town fortification, Hero governor)
	{
		Hero governor2 = fortification.Governor;
		if (governor == null)
		{
			fortification.Governor = null;
		}
		else if (governor.CurrentSettlement == fortification.Settlement && !governor.IsPrisoner)
		{
			fortification.Governor = governor;
			TeleportHeroAction.ApplyImmediateTeleportToSettlement(governor, fortification.Settlement);
		}
		else
		{
			fortification.Governor = null;
			TeleportHeroAction.ApplyDelayedTeleportToSettlementAsGovernor(governor, fortification.Settlement);
		}
		if (governor2 != null)
		{
			governor2.GovernorOf = null;
		}
		CampaignEventDispatcher.Instance.OnGovernorChanged(fortification, governor2, governor);
		if (governor != null)
		{
			CampaignEventDispatcher.Instance.OnHeroGetsBusy(governor, HeroGetsBusyReasons.BecomeGovernor);
		}
	}

	private static void ApplyGiveUpInternal(Hero governor)
	{
		Town governorOf = governor.GovernorOf;
		governorOf.Governor = null;
		governor.GovernorOf = null;
		CampaignEventDispatcher.Instance.OnGovernorChanged(governorOf, governor, null);
	}

	public static void Apply(Town fortification, Hero governor)
	{
		ApplyInternal(fortification, governor);
	}

	public static void RemoveGovernorOf(Hero governor)
	{
		ApplyGiveUpInternal(governor);
	}

	public static void RemoveGovernorOfIfExists(Town town)
	{
		if (town.Governor != null)
		{
			ApplyGiveUpInternal(town.Governor);
		}
	}
}
