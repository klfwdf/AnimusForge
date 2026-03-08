using System;
using Helpers;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.Actions
{
	// Token: 0x020004B0 RID: 1200
	public static class GainKingdomInfluenceAction
	{
		// Token: 0x060049C5 RID: 18885 RVA: 0x00173108 File Offset: 0x00171308
		private static void ApplyInternal(Hero hero, MobileParty party, float gainedInfluence, GainKingdomInfluenceAction.InfluenceGainingReason detail)
		{
			Clan clan = null;
			if (hero != null)
			{
				if (hero.CompanionOf != null)
				{
					clan = hero.CompanionOf;
				}
				else if (hero.Clan != null)
				{
					clan = hero.Clan;
				}
			}
			else if (party.ActualClan != null)
			{
				clan = party.ActualClan;
			}
			else if (party.Owner != null)
			{
				clan = party.Owner.Clan;
			}
			if (clan == null || clan.Kingdom == null)
			{
				return;
			}
			MobileParty mobileParty = party ?? hero.PartyBelongedTo;
			if (detail != GainKingdomInfluenceAction.InfluenceGainingReason.BeingAtArmy && detail == GainKingdomInfluenceAction.InfluenceGainingReason.ClanSupport)
			{
				gainedInfluence = 0.5f;
			}
			if (detail != GainKingdomInfluenceAction.InfluenceGainingReason.Default && detail != GainKingdomInfluenceAction.InfluenceGainingReason.GivingFood && detail != GainKingdomInfluenceAction.InfluenceGainingReason.JoinFaction && detail != GainKingdomInfluenceAction.InfluenceGainingReason.ClanSupport && ((Kingdom)clan.MapFaction).ActivePolicies.Contains(DefaultPolicies.MilitaryCoronae))
			{
				gainedInfluence *= 1.2f;
			}
			ExplainedNumber explainedNumber = new ExplainedNumber(gainedInfluence, false, null);
			if (detail == GainKingdomInfluenceAction.InfluenceGainingReason.Battle && gainedInfluence > 0f)
			{
				PerkHelper.AddPerkBonusForParty(DefaultPerks.Tactics.PreBattleManeuvers, mobileParty, true, ref explainedNumber, false);
			}
			if (detail == GainKingdomInfluenceAction.InfluenceGainingReason.CaptureSettlement && (hero != null || mobileParty.LeaderHero != null))
			{
				Hero hero2 = hero ?? mobileParty.LeaderHero;
				PerkHelper.AddPerkBonusForCharacter(DefaultPerks.Tactics.Besieged, hero2.CharacterObject, false, ref explainedNumber, false);
			}
			ChangeClanInfluenceAction.Apply(clan, explainedNumber.ResultNumber);
			if (detail == GainKingdomInfluenceAction.InfluenceGainingReason.DonatePrisoners && party == MobileParty.MainParty)
			{
				MBTextManager.SetTextVariable("INFLUENCE", (int)gainedInfluence);
				MBTextManager.SetTextVariable("NEW_INFLUENCE", (int)clan.Influence);
				InformationManager.DisplayMessage(new InformationMessage(GameTexts.FindText("str_influence_gain_message", null).ToString()));
			}
			if (detail == GainKingdomInfluenceAction.InfluenceGainingReason.Battle && hero == Hero.MainHero)
			{
				MBTextManager.SetTextVariable("INFLUENCE", (int)gainedInfluence);
				MBTextManager.SetTextVariable("NEW_INFLUENCE", (int)clan.Influence);
				InformationManager.DisplayMessage(new InformationMessage(GameTexts.FindText("str_influence_gain_message", null).ToString()));
			}
			if (detail == GainKingdomInfluenceAction.InfluenceGainingReason.SiegeSafePassage && hero == Hero.MainHero)
			{
				MBTextManager.SetTextVariable("INFLUENCE", -(int)gainedInfluence);
				InformationManager.DisplayMessage(new InformationMessage(GameTexts.FindText("str_leave_siege_lose_influence_message", null).ToString()));
			}
		}

		// Token: 0x060049C6 RID: 18886 RVA: 0x001732DA File Offset: 0x001714DA
		public static void ApplyForBattle(Hero hero, float value)
		{
			GainKingdomInfluenceAction.ApplyInternal(hero, null, value, GainKingdomInfluenceAction.InfluenceGainingReason.Battle);
		}

		// Token: 0x060049C7 RID: 18887 RVA: 0x001732E5 File Offset: 0x001714E5
		public static void ApplyForGivingFood(Hero hero1, Hero hero2, float value)
		{
			GainKingdomInfluenceAction.ApplyInternal(hero1, null, value, GainKingdomInfluenceAction.InfluenceGainingReason.GivingFood);
			GainKingdomInfluenceAction.ApplyInternal(hero2, null, -value, GainKingdomInfluenceAction.InfluenceGainingReason.GivingFood);
		}

		// Token: 0x060049C8 RID: 18888 RVA: 0x001732FA File Offset: 0x001714FA
		public static void ApplyForDefault(Hero hero, float value)
		{
			GainKingdomInfluenceAction.ApplyInternal(hero, null, value, GainKingdomInfluenceAction.InfluenceGainingReason.Default);
		}

		// Token: 0x060049C9 RID: 18889 RVA: 0x00173305 File Offset: 0x00171505
		public static void ApplyForJoiningFaction(Hero hero, float value)
		{
			GainKingdomInfluenceAction.ApplyInternal(hero, null, value, GainKingdomInfluenceAction.InfluenceGainingReason.JoinFaction);
		}

		// Token: 0x060049CA RID: 18890 RVA: 0x00173310 File Offset: 0x00171510
		public static void ApplyForDonatePrisoners(MobileParty donatingParty, float value)
		{
			GainKingdomInfluenceAction.ApplyInternal(null, donatingParty, value, GainKingdomInfluenceAction.InfluenceGainingReason.DonatePrisoners);
		}

		// Token: 0x060049CB RID: 18891 RVA: 0x0017331C File Offset: 0x0017151C
		public static void ApplyForRaidingEnemyVillage(MobileParty side1Party, float value)
		{
			GainKingdomInfluenceAction.ApplyInternal(null, side1Party, value, GainKingdomInfluenceAction.InfluenceGainingReason.Raiding);
		}

		// Token: 0x060049CC RID: 18892 RVA: 0x00173327 File Offset: 0x00171527
		public static void ApplyForBesiegingEnemySettlement(MobileParty side1Party, float value)
		{
			GainKingdomInfluenceAction.ApplyInternal(null, side1Party, value, GainKingdomInfluenceAction.InfluenceGainingReason.Besieging);
		}

		// Token: 0x060049CD RID: 18893 RVA: 0x00173332 File Offset: 0x00171532
		public static void ApplyForSiegeSafePassageBarter(MobileParty side1Party, float value)
		{
			GainKingdomInfluenceAction.ApplyInternal(null, side1Party, value, GainKingdomInfluenceAction.InfluenceGainingReason.SiegeSafePassage);
		}

		// Token: 0x060049CE RID: 18894 RVA: 0x0017333E File Offset: 0x0017153E
		public static void ApplyForCapturingEnemySettlement(MobileParty side1Party, float value)
		{
			GainKingdomInfluenceAction.ApplyInternal(null, side1Party, value, GainKingdomInfluenceAction.InfluenceGainingReason.CaptureSettlement);
		}

		// Token: 0x060049CF RID: 18895 RVA: 0x00173349 File Offset: 0x00171549
		public static void ApplyForLeavingTroopToGarrison(Hero hero, float value)
		{
			GainKingdomInfluenceAction.ApplyInternal(hero, null, value, GainKingdomInfluenceAction.InfluenceGainingReason.LeaveGarrison);
		}

		// Token: 0x060049D0 RID: 18896 RVA: 0x00173354 File Offset: 0x00171554
		public static void ApplyForBoardGameWon(Hero hero, float value)
		{
			GainKingdomInfluenceAction.ApplyInternal(hero, null, value, GainKingdomInfluenceAction.InfluenceGainingReason.BoardGameWon);
		}

		// Token: 0x0200088C RID: 2188
		private enum InfluenceGainingReason
		{
			// Token: 0x04002431 RID: 9265
			Default,
			// Token: 0x04002432 RID: 9266
			BeingAtArmy,
			// Token: 0x04002433 RID: 9267
			Battle,
			// Token: 0x04002434 RID: 9268
			Raiding,
			// Token: 0x04002435 RID: 9269
			Besieging,
			// Token: 0x04002436 RID: 9270
			CaptureSettlement,
			// Token: 0x04002437 RID: 9271
			JoinFaction,
			// Token: 0x04002438 RID: 9272
			GivingFood,
			// Token: 0x04002439 RID: 9273
			LeaveGarrison,
			// Token: 0x0400243A RID: 9274
			BoardGameWon,
			// Token: 0x0400243B RID: 9275
			ClanSupport,
			// Token: 0x0400243C RID: 9276
			DonatePrisoners,
			// Token: 0x0400243D RID: 9277
			SiegeSafePassage
		}
	}
}
