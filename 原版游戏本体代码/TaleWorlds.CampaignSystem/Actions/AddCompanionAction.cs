using System;

namespace TaleWorlds.CampaignSystem.Actions
{
	// Token: 0x0200048E RID: 1166
	public static class AddCompanionAction
	{
		// Token: 0x06004936 RID: 18742 RVA: 0x00170115 File Offset: 0x0016E315
		private static void ApplyInternal(Clan clan, Hero companion)
		{
			if (companion.CompanionOf != null)
			{
				RemoveCompanionAction.ApplyByFire(companion.CompanionOf, companion);
			}
			companion.CompanionOf = clan;
			CampaignEventDispatcher.Instance.OnNewCompanionAdded(companion);
		}

		// Token: 0x06004937 RID: 18743 RVA: 0x0017013D File Offset: 0x0016E33D
		public static void Apply(Clan clan, Hero companion)
		{
			AddCompanionAction.ApplyInternal(clan, companion);
		}
	}
}
