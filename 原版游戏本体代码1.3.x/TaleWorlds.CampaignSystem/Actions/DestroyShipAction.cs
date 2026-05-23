using System;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.CampaignSystem.Party;

namespace TaleWorlds.CampaignSystem.Actions
{
	// Token: 0x020004A8 RID: 1192
	public static class DestroyShipAction
	{
		// Token: 0x0600499F RID: 18847 RVA: 0x00172918 File Offset: 0x00170B18
		private static void ApplyInternal(Ship ship, DestroyShipAction.ShipDestroyDetail detail)
		{
			PartyBase owner = ship.Owner;
			if (owner != null)
			{
				MobileParty mobileParty = owner.MobileParty;
				if (mobileParty != null)
				{
					mobileParty.SetNavalVisualAsDirty();
				}
			}
			ship.Owner = null;
			CampaignEventDispatcher.Instance.OnShipDestroyed(owner, ship, detail);
		}

		// Token: 0x060049A0 RID: 18848 RVA: 0x00172954 File Offset: 0x00170B54
		public static void Apply(Ship ship)
		{
			DestroyShipAction.ApplyInternal(ship, DestroyShipAction.ShipDestroyDetail.ApplyDefault);
		}

		// Token: 0x060049A1 RID: 18849 RVA: 0x0017295D File Offset: 0x00170B5D
		public static void ApplyByDiscard(Ship ship)
		{
			DestroyShipAction.ApplyInternal(ship, DestroyShipAction.ShipDestroyDetail.ApplyByDiscard);
		}

		// Token: 0x02000889 RID: 2185
		public enum ShipDestroyDetail
		{
			// Token: 0x04002425 RID: 9253
			ApplyDefault,
			// Token: 0x04002426 RID: 9254
			ApplyByDiscard
		}
	}
}
