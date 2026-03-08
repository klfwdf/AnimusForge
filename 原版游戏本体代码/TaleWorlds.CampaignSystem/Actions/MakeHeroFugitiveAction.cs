using System;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.Actions
{
	// Token: 0x020004BA RID: 1210
	public static class MakeHeroFugitiveAction
	{
		// Token: 0x060049F7 RID: 18935 RVA: 0x00174478 File Offset: 0x00172678
		private static void ApplyInternal(Hero fugitive, bool showNotification)
		{
			if (fugitive.IsAlive)
			{
				if (fugitive.PartyBelongedTo != null)
				{
					if (fugitive.PartyBelongedTo.LeaderHero == fugitive)
					{
						DestroyPartyAction.Apply(null, fugitive.PartyBelongedTo);
					}
					else
					{
						fugitive.PartyBelongedTo.MemberRoster.RemoveTroop(fugitive.CharacterObject, 1, default(UniqueTroopDescriptor), 0);
					}
				}
				if (fugitive.CurrentSettlement != null)
				{
					LeaveSettlementAction.ApplyForCharacterOnly(fugitive);
				}
				fugitive.ChangeState(Hero.CharacterStates.Fugitive);
				CampaignEventDispatcher.Instance.OnCharacterBecameFugitive(fugitive, showNotification);
			}
		}

		// Token: 0x060049F8 RID: 18936 RVA: 0x001744F3 File Offset: 0x001726F3
		public static void Apply(Hero fugitive, bool showNotification = false)
		{
			MakeHeroFugitiveAction.ApplyInternal(fugitive, showNotification);
		}
	}
}
