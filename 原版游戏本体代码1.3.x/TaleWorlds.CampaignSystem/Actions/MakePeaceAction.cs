using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;

namespace TaleWorlds.CampaignSystem.Actions
{
	// Token: 0x020004BB RID: 1211
	public static class MakePeaceAction
	{
		// Token: 0x060049F9 RID: 18937 RVA: 0x001744FC File Offset: 0x001726FC
		private static void ApplyInternal(IFaction faction1, IFaction faction2, int dailyTributeFrom1To2, int dailyTributeDuration, MakePeaceAction.MakePeaceDetail detail = MakePeaceAction.MakePeaceDetail.Default)
		{
			StanceLink stanceWith = faction1.GetStanceWith(faction2);
			FactionManager.SetNeutral(faction1, faction2);
			stanceWith.SetDailyTributePaid(faction1, dailyTributeFrom1To2, dailyTributeDuration);
			if (faction1 == Hero.MainHero.MapFaction || faction2 == Hero.MainHero.MapFaction)
			{
				IFaction dirtySide = ((faction1 == Hero.MainHero.MapFaction) ? faction2 : faction1);
				IEnumerable<Settlement> all = Settlement.All;
				Func<Settlement, bool> predicate;
				Func<Settlement, bool> <>9__0;
				if ((predicate = <>9__0) == null)
				{
					predicate = (<>9__0 = (Settlement party) => party.IsVisible && party.MapFaction == dirtySide);
				}
				foreach (Settlement settlement in all.Where(predicate))
				{
					settlement.Party.SetVisualAsDirty();
				}
				IEnumerable<MobileParty> all2 = MobileParty.All;
				Func<MobileParty, bool> predicate2;
				Func<MobileParty, bool> <>9__1;
				if ((predicate2 = <>9__1) == null)
				{
					predicate2 = (<>9__1 = (MobileParty party) => party.IsVisible && party.MapFaction == dirtySide);
				}
				foreach (MobileParty mobileParty in all2.Where(predicate2))
				{
					mobileParty.Party.SetVisualAsDirty();
				}
			}
			CampaignEventDispatcher.Instance.OnMakePeace(faction1, faction2, detail);
		}

		// Token: 0x060049FA RID: 18938 RVA: 0x0017463C File Offset: 0x0017283C
		public static void Apply(IFaction faction1, IFaction faction2)
		{
			MakePeaceAction.ApplyInternal(faction1, faction2, 0, 0, MakePeaceAction.MakePeaceDetail.Default);
		}

		// Token: 0x060049FB RID: 18939 RVA: 0x00174648 File Offset: 0x00172848
		public static void ApplyByKingdomDecision(IFaction faction1, IFaction faction2, int dailyTributeFrom1To2, int dailyTributeDuration)
		{
			MakePeaceAction.ApplyInternal(faction1, faction2, dailyTributeFrom1To2, dailyTributeDuration, MakePeaceAction.MakePeaceDetail.ByKingdomDecision);
		}

		// Token: 0x04001443 RID: 5187
		private const float DefaultValueForBeingLimitedAfterPeace = 100000f;

		// Token: 0x02000892 RID: 2194
		public enum MakePeaceDetail
		{
			// Token: 0x0400244F RID: 9295
			Default,
			// Token: 0x04002450 RID: 9296
			ByKingdomDecision
		}
	}
}
