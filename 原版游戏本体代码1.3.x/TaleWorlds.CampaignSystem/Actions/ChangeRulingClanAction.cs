using System;

namespace TaleWorlds.CampaignSystem.Actions
{
	// Token: 0x020004A0 RID: 1184
	public class ChangeRulingClanAction
	{
		// Token: 0x0600497A RID: 18810 RVA: 0x00171FBD File Offset: 0x001701BD
		private static void ApplyInternal(Kingdom kingdom, Clan clan)
		{
			kingdom.RulingClan = clan;
			CampaignEventDispatcher.Instance.OnRulingClanChanged(kingdom, clan);
		}

		// Token: 0x0600497B RID: 18811 RVA: 0x00171FD2 File Offset: 0x001701D2
		public static void Apply(Kingdom kingdom, Clan clan)
		{
			ChangeRulingClanAction.ApplyInternal(kingdom, clan);
		}
	}
}
