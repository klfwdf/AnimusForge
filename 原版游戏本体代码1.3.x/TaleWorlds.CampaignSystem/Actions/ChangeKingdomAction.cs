using System;
using System.Collections.Generic;
using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Settlements;

namespace TaleWorlds.CampaignSystem.Actions
{
	// Token: 0x02000499 RID: 1177
	public static class ChangeKingdomAction
	{
		// Token: 0x06004957 RID: 18775 RVA: 0x001711B0 File Offset: 0x0016F3B0
		private static void ApplyInternal(Clan clan, Kingdom newKingdom, ChangeKingdomAction.ChangeKingdomActionDetail detail, CampaignTime shouldStayInKingdomUntil, int awardMultiplier = 0, bool byRebellion = false, bool showNotification = true)
		{
			Kingdom kingdom = clan.Kingdom;
			clan.DebtToKingdom = 0;
			if (detail == ChangeKingdomAction.ChangeKingdomActionDetail.JoinKingdom || detail == ChangeKingdomAction.ChangeKingdomActionDetail.JoinAsMercenary || detail == ChangeKingdomAction.ChangeKingdomActionDetail.JoinKingdomByDefection)
			{
				clan.ShouldStayInKingdomUntil = shouldStayInKingdomUntil;
				FactionHelper.AdjustFactionStancesForClanJoiningKingdom(clan, newKingdom);
			}
			else
			{
				clan.ShouldStayInKingdomUntil = CampaignTime.Zero;
			}
			if (detail == ChangeKingdomAction.ChangeKingdomActionDetail.JoinKingdom || detail == ChangeKingdomAction.ChangeKingdomActionDetail.CreateKingdom || detail == ChangeKingdomAction.ChangeKingdomActionDetail.JoinKingdomByDefection)
			{
				if (clan.IsUnderMercenaryService)
				{
					EndMercenaryServiceAction.EndByDefault(clan);
				}
				if (kingdom != null)
				{
					clan.ClanLeaveKingdom(!byRebellion);
				}
				if (newKingdom != null && detail == ChangeKingdomAction.ChangeKingdomActionDetail.CreateKingdom)
				{
					ChangeRulingClanAction.Apply(newKingdom, clan);
				}
				clan.Kingdom = newKingdom;
			}
			else if (detail == ChangeKingdomAction.ChangeKingdomActionDetail.JoinAsMercenary)
			{
				StartMercenaryServiceAction.ApplyByDefault(clan, newKingdom, awardMultiplier);
			}
			else if (detail == ChangeKingdomAction.ChangeKingdomActionDetail.LeaveWithRebellion || detail == ChangeKingdomAction.ChangeKingdomActionDetail.LeaveKingdom || detail == ChangeKingdomAction.ChangeKingdomActionDetail.LeaveAsMercenary || detail == ChangeKingdomAction.ChangeKingdomActionDetail.LeaveByClanDestruction || detail == ChangeKingdomAction.ChangeKingdomActionDetail.LeaveByKingdomDestruction)
			{
				clan.Kingdom = null;
				bool flag = false;
				if (clan.IsUnderMercenaryService)
				{
					flag = true;
					EndMercenaryServiceAction.EndByLeavingKingdom(clan);
				}
				if (detail == ChangeKingdomAction.ChangeKingdomActionDetail.LeaveWithRebellion)
				{
					DeclareWarAction.ApplyByRebellion(kingdom, clan);
					using (List<IFaction>.Enumerator enumerator = kingdom.FactionsAtWarWith.GetEnumerator())
					{
						while (enumerator.MoveNext())
						{
							IFaction faction = enumerator.Current;
							if (faction != clan && !clan.IsAtWarWith(faction))
							{
								DeclareWarAction.ApplyByDefault(clan, faction);
							}
						}
						goto IL_298;
					}
				}
				if (detail == ChangeKingdomAction.ChangeKingdomActionDetail.LeaveKingdom)
				{
					using (List<Settlement>.Enumerator enumerator2 = new List<Settlement>(clan.Settlements).GetEnumerator())
					{
						while (enumerator2.MoveNext())
						{
							Settlement settlement = enumerator2.Current;
							ChangeOwnerOfSettlementAction.ApplyByLeaveFaction(kingdom.Leader, settlement);
							foreach (Hero hero in new List<Hero>(settlement.HeroesWithoutParty))
							{
								if (hero.CurrentSettlement != null && hero.Clan == clan)
								{
									if (hero.PartyBelongedTo != null)
									{
										LeaveSettlementAction.ApplyForParty(hero.PartyBelongedTo);
										EnterSettlementAction.ApplyForParty(hero.PartyBelongedTo, clan.Leader.HomeSettlement);
									}
									else
									{
										LeaveSettlementAction.ApplyForCharacterOnly(hero);
										EnterSettlementAction.ApplyForCharacterOnly(hero, clan.Leader.HomeSettlement);
									}
								}
							}
						}
						goto IL_298;
					}
				}
				if (detail == ChangeKingdomAction.ChangeKingdomActionDetail.LeaveByKingdomDestruction)
				{
					if (flag)
					{
						using (List<IFaction>.Enumerator enumerator = kingdom.FactionsAtWarWith.GetEnumerator())
						{
							while (enumerator.MoveNext())
							{
								IFaction faction2 = enumerator.Current;
								if (clan != faction2 && !Campaign.Current.Models.DiplomacyModel.IsAtConstantWar(clan, faction2))
								{
									MakePeaceAction.Apply(clan, faction2);
								}
							}
							goto IL_298;
						}
					}
					foreach (IFaction faction3 in kingdom.FactionsAtWarWith)
					{
						if (clan != faction3 && !clan.GetStanceWith(faction3).IsAtWar)
						{
							DeclareWarAction.ApplyByDefault(clan, faction3);
						}
					}
				}
			}
			IL_298:
			if (detail == ChangeKingdomAction.ChangeKingdomActionDetail.LeaveAsMercenary || detail == ChangeKingdomAction.ChangeKingdomActionDetail.LeaveKingdom)
			{
				foreach (IFaction faction4 in clan.FactionsAtWarWith.ToList<IFaction>())
				{
					if (clan != faction4 && !Campaign.Current.Models.DiplomacyModel.IsAtConstantWar(clan, faction4))
					{
						MakePeaceAction.Apply(clan, faction4);
						FactionHelper.FinishAllRelatedHostileActionsOfFactionToFaction(clan, faction4);
						FactionHelper.FinishAllRelatedHostileActionsOfFactionToFaction(faction4, clan);
					}
				}
				ChangeKingdomAction.CheckIfPartyIconIsDirty(clan, kingdom);
			}
			foreach (WarPartyComponent warPartyComponent in clan.WarPartyComponents)
			{
				if (warPartyComponent.MobileParty.MapEvent == null)
				{
					warPartyComponent.MobileParty.SetMoveModeHold();
				}
			}
			CampaignEventDispatcher.Instance.OnClanChangedKingdom(clan, kingdom, newKingdom, detail, showNotification);
		}

		// Token: 0x06004958 RID: 18776 RVA: 0x00171584 File Offset: 0x0016F784
		public static void ApplyByJoinToKingdom(Clan clan, Kingdom newKingdom, CampaignTime shouldStayInKingdomUntil = default(CampaignTime), bool showNotification = true)
		{
			ChangeKingdomAction.ApplyInternal(clan, newKingdom, ChangeKingdomAction.ChangeKingdomActionDetail.JoinKingdom, shouldStayInKingdomUntil, 0, false, showNotification);
		}

		// Token: 0x06004959 RID: 18777 RVA: 0x00171592 File Offset: 0x0016F792
		public static void ApplyByJoinToKingdomByDefection(Clan clan, Kingdom oldKingdom, Kingdom newKingdom, CampaignTime shouldStayInKingdomUntil = default(CampaignTime), bool showNotification = true)
		{
			ChangeKingdomAction.ApplyInternal(clan, newKingdom, ChangeKingdomAction.ChangeKingdomActionDetail.JoinKingdomByDefection, shouldStayInKingdomUntil, 0, false, showNotification);
			CampaignEventDispatcher.Instance.OnClanDefected(clan, oldKingdom, newKingdom);
		}

		// Token: 0x0600495A RID: 18778 RVA: 0x001715AE File Offset: 0x0016F7AE
		public static void ApplyByCreateKingdom(Clan clan, Kingdom newKingdom, bool showNotification = true)
		{
			ChangeKingdomAction.ApplyInternal(clan, newKingdom, ChangeKingdomAction.ChangeKingdomActionDetail.CreateKingdom, CampaignTime.Zero, 0, false, showNotification);
		}

		// Token: 0x0600495B RID: 18779 RVA: 0x001715C0 File Offset: 0x0016F7C0
		public static void ApplyByLeaveByKingdomDestruction(Clan clan, bool showNotification = true)
		{
			ChangeKingdomAction.ApplyInternal(clan, null, ChangeKingdomAction.ChangeKingdomActionDetail.LeaveByKingdomDestruction, CampaignTime.Zero, 0, false, showNotification);
		}

		// Token: 0x0600495C RID: 18780 RVA: 0x001715D2 File Offset: 0x0016F7D2
		public static void ApplyByLeaveKingdom(Clan clan, bool showNotification = true)
		{
			ChangeKingdomAction.ApplyInternal(clan, null, ChangeKingdomAction.ChangeKingdomActionDetail.LeaveKingdom, CampaignTime.Zero, 0, false, showNotification);
		}

		// Token: 0x0600495D RID: 18781 RVA: 0x001715E4 File Offset: 0x0016F7E4
		public static void ApplyByLeaveWithRebellionAgainstKingdom(Clan clan, bool showNotification = true)
		{
			ChangeKingdomAction.ApplyInternal(clan, null, ChangeKingdomAction.ChangeKingdomActionDetail.LeaveWithRebellion, CampaignTime.Zero, 0, false, showNotification);
		}

		// Token: 0x0600495E RID: 18782 RVA: 0x001715F6 File Offset: 0x0016F7F6
		public static void ApplyByJoinFactionAsMercenary(Clan clan, Kingdom newKingdom, CampaignTime shouldStayInKingdomUntil = default(CampaignTime), int awardMultiplier = 50, bool showNotification = true)
		{
			ChangeKingdomAction.ApplyInternal(clan, newKingdom, ChangeKingdomAction.ChangeKingdomActionDetail.JoinAsMercenary, shouldStayInKingdomUntil, awardMultiplier, false, showNotification);
		}

		// Token: 0x0600495F RID: 18783 RVA: 0x00171605 File Offset: 0x0016F805
		public static void ApplyByLeaveKingdomAsMercenary(Clan mercenaryClan, bool showNotification = true)
		{
			ChangeKingdomAction.ApplyInternal(mercenaryClan, null, ChangeKingdomAction.ChangeKingdomActionDetail.LeaveAsMercenary, CampaignTime.Zero, 0, false, showNotification);
		}

		// Token: 0x06004960 RID: 18784 RVA: 0x00171617 File Offset: 0x0016F817
		public static void ApplyByLeaveKingdomByClanDestruction(Clan clan, bool showNotification = true)
		{
			ChangeKingdomAction.ApplyInternal(clan, null, ChangeKingdomAction.ChangeKingdomActionDetail.LeaveByClanDestruction, CampaignTime.Zero, 0, false, showNotification);
		}

		// Token: 0x06004961 RID: 18785 RVA: 0x0017162C File Offset: 0x0016F82C
		private static void CheckIfPartyIconIsDirty(Clan clan, Kingdom oldKingdom)
		{
			IFaction faction;
			if (clan.Kingdom == null)
			{
				faction = clan;
			}
			else
			{
				IFaction kingdom = clan.Kingdom;
				faction = kingdom;
			}
			IFaction faction2 = faction;
			IFaction faction3 = oldKingdom ?? clan;
			foreach (MobileParty mobileParty in MobileParty.All)
			{
				if (mobileParty.IsVisible && ((mobileParty.Party.Owner != null && mobileParty.Party.Owner.Clan == clan) || (clan == Clan.PlayerClan && ((!FactionManager.IsAtWarAgainstFaction(mobileParty.MapFaction, faction2) && FactionManager.IsAtWarAgainstFaction(mobileParty.MapFaction, faction3)) || (FactionManager.IsAtWarAgainstFaction(mobileParty.MapFaction, faction2) && !FactionManager.IsAtWarAgainstFaction(mobileParty.MapFaction, faction3))))))
				{
					mobileParty.Party.SetVisualAsDirty();
				}
			}
			foreach (Settlement settlement in clan.Settlements)
			{
				settlement.Party.SetVisualAsDirty();
			}
		}

		// Token: 0x04001436 RID: 5174
		public const float PotentialSettlementsPerNobleEffect = 0.2f;

		// Token: 0x04001437 RID: 5175
		public const float NewGainedFiefsValueForKingdomConstant = 0.1f;

		// Token: 0x04001438 RID: 5176
		public const float LordsUnitStrengthValue = 20f;

		// Token: 0x04001439 RID: 5177
		public const float MercenaryUnitStrengthValue = 5f;

		// Token: 0x0400143A RID: 5178
		public const float MinimumNeededGoldForRecruitingMercenaries = 20000f;

		// Token: 0x02000880 RID: 2176
		public enum ChangeKingdomActionDetail
		{
			// Token: 0x040023F4 RID: 9204
			JoinAsMercenary,
			// Token: 0x040023F5 RID: 9205
			JoinKingdom,
			// Token: 0x040023F6 RID: 9206
			JoinKingdomByDefection,
			// Token: 0x040023F7 RID: 9207
			LeaveKingdom,
			// Token: 0x040023F8 RID: 9208
			LeaveWithRebellion,
			// Token: 0x040023F9 RID: 9209
			LeaveAsMercenary,
			// Token: 0x040023FA RID: 9210
			LeaveByClanDestruction,
			// Token: 0x040023FB RID: 9211
			CreateKingdom,
			// Token: 0x040023FC RID: 9212
			LeaveByKingdomDestruction
		}
	}
}
