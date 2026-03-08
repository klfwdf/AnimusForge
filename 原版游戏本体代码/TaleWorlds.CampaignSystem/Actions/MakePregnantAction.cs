using System;

namespace TaleWorlds.CampaignSystem.Actions
{
	// Token: 0x020004BC RID: 1212
	public static class MakePregnantAction
	{
		// Token: 0x060049FC RID: 18940 RVA: 0x00174654 File Offset: 0x00172854
		private static void ApplyInternal(Hero mother)
		{
			mother.IsPregnant = true;
			CampaignEventDispatcher.Instance.OnChildConceived(mother);
		}

		// Token: 0x060049FD RID: 18941 RVA: 0x00174668 File Offset: 0x00172868
		public static void Apply(Hero mother)
		{
			MakePregnantAction.ApplyInternal(mother);
		}
	}
}
