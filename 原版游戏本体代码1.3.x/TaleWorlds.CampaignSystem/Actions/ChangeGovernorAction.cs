using System;
using TaleWorlds.CampaignSystem.Settlements;

namespace TaleWorlds.CampaignSystem.Actions
{
	// Token: 0x02000498 RID: 1176
	public static class ChangeGovernorAction
	{
		// Token: 0x06004952 RID: 18770 RVA: 0x001710D0 File Offset: 0x0016F2D0
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

		// Token: 0x06004953 RID: 18771 RVA: 0x00171158 File Offset: 0x0016F358
		private static void ApplyGiveUpInternal(Hero governor)
		{
			Town governorOf = governor.GovernorOf;
			governorOf.Governor = null;
			governor.GovernorOf = null;
			CampaignEventDispatcher.Instance.OnGovernorChanged(governorOf, governor, null);
		}

		// Token: 0x06004954 RID: 18772 RVA: 0x00171187 File Offset: 0x0016F387
		public static void Apply(Town fortification, Hero governor)
		{
			ChangeGovernorAction.ApplyInternal(fortification, governor);
		}

		// Token: 0x06004955 RID: 18773 RVA: 0x00171190 File Offset: 0x0016F390
		public static void RemoveGovernorOf(Hero governor)
		{
			ChangeGovernorAction.ApplyGiveUpInternal(governor);
		}

		// Token: 0x06004956 RID: 18774 RVA: 0x00171198 File Offset: 0x0016F398
		public static void RemoveGovernorOfIfExists(Town town)
		{
			if (town.Governor != null)
			{
				ChangeGovernorAction.ApplyGiveUpInternal(town.Governor);
			}
		}
	}
}
