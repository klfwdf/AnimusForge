using System;

namespace TaleWorlds.CampaignSystem.Actions
{
	// Token: 0x02000495 RID: 1173
	public static class ChangeClanInfluenceAction
	{
		// Token: 0x0600494B RID: 18763 RVA: 0x00170DDB File Offset: 0x0016EFDB
		private static void ApplyInternal(Clan clan, float amount)
		{
			clan.Influence += amount;
			CampaignEventDispatcher.Instance.OnClanInfluenceChanged(clan, amount);
		}

		// Token: 0x0600494C RID: 18764 RVA: 0x00170DF7 File Offset: 0x0016EFF7
		public static void Apply(Clan clan, float amount)
		{
			ChangeClanInfluenceAction.ApplyInternal(clan, amount);
		}
	}
}
