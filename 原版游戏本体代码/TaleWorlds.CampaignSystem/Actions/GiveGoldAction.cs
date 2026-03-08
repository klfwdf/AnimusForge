using System;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.Actions
{
	// Token: 0x020004B3 RID: 1203
	public static class GiveGoldAction
	{
		// Token: 0x060049D5 RID: 18901 RVA: 0x001733D0 File Offset: 0x001715D0
		private static void ApplyInternal(Hero giverHero, PartyBase giverParty, Hero recipientHero, PartyBase recipientParty, int goldAmount, bool showQuickInformation, string transactionStringId = "")
		{
			if (giverHero != null)
			{
				goldAmount = MathF.Min(giverHero.Gold, goldAmount);
				giverHero.ChangeHeroGold(-goldAmount);
			}
			else if (giverParty != null && giverParty.IsMobile)
			{
				goldAmount = MathF.Min(giverParty.MobileParty.PartyTradeGold, goldAmount);
				giverParty.MobileParty.PartyTradeGold -= goldAmount;
			}
			else if (giverParty != null && giverParty.IsSettlement)
			{
				SettlementComponent settlementComponent = giverParty.Settlement.SettlementComponent;
				goldAmount = MathF.Min(settlementComponent.Gold, goldAmount);
				settlementComponent.ChangeGold(-goldAmount);
			}
			if (recipientHero != null)
			{
				recipientHero.ChangeHeroGold(goldAmount);
			}
			else if (recipientParty != null && recipientParty.IsMobile)
			{
				recipientParty.MobileParty.PartyTradeGold += goldAmount;
			}
			else if (recipientParty != null && recipientParty.IsSettlement)
			{
				recipientParty.Settlement.SettlementComponent.ChangeGold(goldAmount);
			}
			CampaignEventDispatcher.Instance.OnHeroOrPartyTradedGold(new ValueTuple<Hero, PartyBase>(giverHero, giverParty), new ValueTuple<Hero, PartyBase>(recipientHero, recipientParty), new ValueTuple<int, string>(goldAmount, transactionStringId), showQuickInformation);
		}

		// Token: 0x060049D6 RID: 18902 RVA: 0x001734CA File Offset: 0x001716CA
		public static void ApplyBetweenCharacters(Hero giverHero, Hero recipientHero, int amount, bool disableNotification = false)
		{
			GiveGoldAction.ApplyInternal(giverHero, null, recipientHero, null, amount, !disableNotification && (giverHero == Hero.MainHero || recipientHero == Hero.MainHero), "");
		}

		// Token: 0x060049D7 RID: 18903 RVA: 0x001734F4 File Offset: 0x001716F4
		public static void ApplyForCharacterToSettlement(Hero giverHero, Settlement settlement, int amount, bool disableNotification = false)
		{
			GiveGoldAction.ApplyInternal(giverHero, null, null, settlement.Party, amount, !disableNotification && giverHero == Hero.MainHero, "");
		}

		// Token: 0x060049D8 RID: 18904 RVA: 0x00173518 File Offset: 0x00171718
		public static void ApplyForSettlementToCharacter(Settlement giverSettlement, Hero recipientHero, int amount, bool disableNotification = false)
		{
			GiveGoldAction.ApplyInternal(recipientHero, null, null, giverSettlement.Party, -amount, !disableNotification && recipientHero == Hero.MainHero, "");
		}

		// Token: 0x060049D9 RID: 18905 RVA: 0x0017353D File Offset: 0x0017173D
		public static void ApplyForSettlementToParty(Settlement giverSettlement, PartyBase recipientParty, int amount, bool disableNotification = false)
		{
			GiveGoldAction.ApplyInternal(null, giverSettlement.Party, null, recipientParty, amount, !disableNotification && recipientParty.LeaderHero == Hero.MainHero, "");
		}

		// Token: 0x060049DA RID: 18906 RVA: 0x00173566 File Offset: 0x00171766
		public static void ApplyForPartyToSettlement(PartyBase giverParty, Settlement settlement, int amount, bool disableNotification = false)
		{
			GiveGoldAction.ApplyInternal(null, giverParty, null, settlement.Party, amount, !disableNotification && ((giverParty != null) ? giverParty.LeaderHero : null) == Hero.MainHero, "");
		}

		// Token: 0x060049DB RID: 18907 RVA: 0x00173595 File Offset: 0x00171795
		public static void ApplyForPartyToCharacter(PartyBase giverParty, Hero recipientHero, int amount, bool disableNotification = false)
		{
			GiveGoldAction.ApplyInternal(null, giverParty, recipientHero, null, amount, !disableNotification && giverParty != null && (giverParty.LeaderHero == Hero.MainHero || recipientHero == Hero.MainHero), "");
		}

		// Token: 0x060049DC RID: 18908 RVA: 0x001735C7 File Offset: 0x001717C7
		public static void ApplyForCharacterToParty(Hero giverHero, PartyBase receipentParty, int amount, bool disableNotification = false)
		{
			GiveGoldAction.ApplyInternal(giverHero, null, null, receipentParty, amount, !disableNotification && (giverHero == Hero.MainHero || receipentParty.LeaderHero == Hero.MainHero), "");
		}

		// Token: 0x060049DD RID: 18909 RVA: 0x001735F6 File Offset: 0x001717F6
		public static void ApplyForPartyToParty(PartyBase giverParty, PartyBase receipentParty, int amount, bool disableNotification = false)
		{
			GiveGoldAction.ApplyInternal(null, giverParty, null, receipentParty, amount, !disableNotification && (giverParty.LeaderHero == Hero.MainHero || receipentParty.LeaderHero == Hero.MainHero), "");
		}
	}
}
