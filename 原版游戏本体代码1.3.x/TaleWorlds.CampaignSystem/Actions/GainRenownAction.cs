using System;

namespace TaleWorlds.CampaignSystem.Actions
{
	// Token: 0x020004B1 RID: 1201
	public static class GainRenownAction
	{
		// Token: 0x060049D1 RID: 18897 RVA: 0x00173360 File Offset: 0x00171560
		private static void ApplyInternal(Hero hero, float gainedRenown, bool doNotNotify)
		{
			if (gainedRenown > 0f)
			{
				hero.Clan.AddRenown(gainedRenown, true);
				CampaignEventDispatcher.Instance.OnRenownGained(hero, (int)gainedRenown, doNotNotify);
			}
		}

		// Token: 0x060049D2 RID: 18898 RVA: 0x00173385 File Offset: 0x00171585
		public static void Apply(Hero hero, float renownValue, bool doNotNotify = false)
		{
			GainRenownAction.ApplyInternal(hero, renownValue, doNotNotify);
		}
	}
}
