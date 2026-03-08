using System;
using TaleWorlds.CampaignSystem.Party;

namespace TaleWorlds.CampaignSystem.Actions
{
	// Token: 0x020004C0 RID: 1216
	public static class RemoveCompanionAction
	{
		// Token: 0x06004A08 RID: 18952 RVA: 0x00174B2C File Offset: 0x00172D2C
		private static void ApplyInternal(Clan clan, Hero companion, RemoveCompanionAction.RemoveCompanionDetail detail)
		{
			MobileParty partyBelongedTo = companion.PartyBelongedTo;
			PartyBase partyBase = ((partyBelongedTo != null) ? partyBelongedTo.Party : null);
			companion.CompanionOf = null;
			if (partyBase != null && partyBase.IsMobile && detail != RemoveCompanionAction.RemoveCompanionDetail.ByTurningToLord)
			{
				bool flag = partyBase.LeaderHero == companion;
				partyBase.MemberRoster.AddToCounts(companion.CharacterObject, -1, false, 0, 0, true, -1);
				if (flag)
				{
					partyBase.MobileParty.SetMoveModeHold();
					partyBase.MobileParty.Ai.RethinkAtNextHourlyTick = true;
					if (partyBase.MemberRoster.Count == 0)
					{
						DestroyPartyAction.Apply(null, partyBase.MobileParty);
					}
					else
					{
						DisbandPartyAction.StartDisband(partyBase.MobileParty);
					}
				}
			}
			if (detail == RemoveCompanionAction.RemoveCompanionDetail.Fire)
			{
				if (companion.PartyBelongedToAsPrisoner != null)
				{
					EndCaptivityAction.ApplyByEscape(companion, null, true);
				}
				else
				{
					MakeHeroFugitiveAction.Apply(companion, false);
				}
				if (companion.IsWanderer)
				{
					companion.ResetEquipments();
				}
			}
			if (companion.GovernorOf != null)
			{
				ChangeGovernorAction.RemoveGovernorOf(companion);
			}
			CampaignEventDispatcher.Instance.OnCompanionRemoved(companion, detail);
		}

		// Token: 0x06004A09 RID: 18953 RVA: 0x00174C0B File Offset: 0x00172E0B
		public static void ApplyByFire(Clan clan, Hero companion)
		{
			RemoveCompanionAction.ApplyInternal(clan, companion, RemoveCompanionAction.RemoveCompanionDetail.Fire);
		}

		// Token: 0x06004A0A RID: 18954 RVA: 0x00174C15 File Offset: 0x00172E15
		public static void ApplyAfterQuest(Clan clan, Hero companion)
		{
			RemoveCompanionAction.ApplyInternal(clan, companion, RemoveCompanionAction.RemoveCompanionDetail.AfterQuest);
		}

		// Token: 0x06004A0B RID: 18955 RVA: 0x00174C1F File Offset: 0x00172E1F
		public static void ApplyByDeath(Clan clan, Hero companion)
		{
			RemoveCompanionAction.ApplyInternal(clan, companion, RemoveCompanionAction.RemoveCompanionDetail.Death);
		}

		// Token: 0x06004A0C RID: 18956 RVA: 0x00174C29 File Offset: 0x00172E29
		public static void ApplyByByTurningToLord(Clan clan, Hero companion)
		{
			RemoveCompanionAction.ApplyInternal(clan, companion, RemoveCompanionAction.RemoveCompanionDetail.ByTurningToLord);
		}

		// Token: 0x02000894 RID: 2196
		public enum RemoveCompanionDetail
		{
			// Token: 0x04002455 RID: 9301
			Fire,
			// Token: 0x04002456 RID: 9302
			Death,
			// Token: 0x04002457 RID: 9303
			AfterQuest,
			// Token: 0x04002458 RID: 9304
			ByTurningToLord
		}
	}
}
