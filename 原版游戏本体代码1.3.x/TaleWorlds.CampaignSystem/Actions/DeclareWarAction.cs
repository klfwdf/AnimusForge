using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;

namespace TaleWorlds.CampaignSystem.Actions
{
	// Token: 0x020004A4 RID: 1188
	public static class DeclareWarAction
	{
		// Token: 0x0600498C RID: 18828 RVA: 0x001722F4 File Offset: 0x001704F4
		private static void ApplyInternal(IFaction faction1, IFaction faction2, DeclareWarAction.DeclareWarDetail declareWarDetail)
		{
			FactionManager.DeclareWar(faction1, faction2);
			if (faction1.IsKingdomFaction && (float)faction2.Fiefs.Count > 1f + (float)faction1.Fiefs.Count * 0.2f)
			{
				Kingdom kingdom = (Kingdom)faction1;
				kingdom.PoliticalStagnation = (int)((float)kingdom.PoliticalStagnation * 0.85f - 3f);
				if (kingdom.PoliticalStagnation < 0)
				{
					kingdom.PoliticalStagnation = 0;
				}
			}
			if (faction2.IsKingdomFaction && (float)faction1.Fiefs.Count > 1f + (float)faction2.Fiefs.Count * 0.2f)
			{
				Kingdom kingdom2 = (Kingdom)faction2;
				kingdom2.PoliticalStagnation = (int)((float)kingdom2.PoliticalStagnation * 0.85f - 3f);
				if (kingdom2.PoliticalStagnation < 0)
				{
					kingdom2.PoliticalStagnation = 0;
				}
			}
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
			CampaignEventDispatcher.Instance.OnWarDeclared(faction1, faction2, declareWarDetail);
		}

		// Token: 0x0600498D RID: 18829 RVA: 0x001724F0 File Offset: 0x001706F0
		public static void ApplyByKingdomDecision(IFaction faction1, IFaction faction2)
		{
			DeclareWarAction.ApplyInternal(faction1, faction2, DeclareWarAction.DeclareWarDetail.CausedByKingdomDecision);
		}

		// Token: 0x0600498E RID: 18830 RVA: 0x001724FA File Offset: 0x001706FA
		public static void ApplyByDefault(IFaction faction1, IFaction faction2)
		{
			DeclareWarAction.ApplyInternal(faction1, faction2, DeclareWarAction.DeclareWarDetail.Default);
		}

		// Token: 0x0600498F RID: 18831 RVA: 0x00172504 File Offset: 0x00170704
		public static void ApplyByPlayerHostility(IFaction faction1, IFaction faction2)
		{
			DeclareWarAction.ApplyInternal(faction1, faction2, DeclareWarAction.DeclareWarDetail.CausedByPlayerHostility);
		}

		// Token: 0x06004990 RID: 18832 RVA: 0x0017250E File Offset: 0x0017070E
		public static void ApplyByRebellion(IFaction faction1, IFaction faction2)
		{
			DeclareWarAction.ApplyInternal(faction1, faction2, DeclareWarAction.DeclareWarDetail.CausedByRebellion);
		}

		// Token: 0x06004991 RID: 18833 RVA: 0x00172518 File Offset: 0x00170718
		public static void ApplyByCrimeRatingChange(IFaction faction1, IFaction faction2)
		{
			DeclareWarAction.ApplyInternal(faction1, faction2, DeclareWarAction.DeclareWarDetail.CausedByCrimeRatingChange);
		}

		// Token: 0x06004992 RID: 18834 RVA: 0x00172522 File Offset: 0x00170722
		public static void ApplyByKingdomCreation(IFaction faction1, IFaction faction2)
		{
			DeclareWarAction.ApplyInternal(faction1, faction2, DeclareWarAction.DeclareWarDetail.CausedByKingdomCreation);
		}

		// Token: 0x06004993 RID: 18835 RVA: 0x0017252C File Offset: 0x0017072C
		public static void ApplyByClaimOnThrone(IFaction faction1, IFaction faction2)
		{
			DeclareWarAction.ApplyInternal(faction1, faction2, DeclareWarAction.DeclareWarDetail.CausedByClaimOnThrone);
		}

		// Token: 0x06004994 RID: 18836 RVA: 0x00172536 File Offset: 0x00170736
		public static void ApplyByCallToWarAgreement(IFaction faction1, IFaction faction2)
		{
			DeclareWarAction.ApplyInternal(faction1, faction2, DeclareWarAction.DeclareWarDetail.CausedByCallToWarAgreement);
		}

		// Token: 0x02000885 RID: 2181
		public enum DeclareWarDetail
		{
			// Token: 0x04002412 RID: 9234
			Default,
			// Token: 0x04002413 RID: 9235
			CausedByPlayerHostility,
			// Token: 0x04002414 RID: 9236
			CausedByKingdomDecision,
			// Token: 0x04002415 RID: 9237
			CausedByRebellion,
			// Token: 0x04002416 RID: 9238
			CausedByCrimeRatingChange,
			// Token: 0x04002417 RID: 9239
			CausedByKingdomCreation,
			// Token: 0x04002418 RID: 9240
			CausedByClaimOnThrone,
			// Token: 0x04002419 RID: 9241
			CausedByCallToWarAgreement
		}
	}
}
