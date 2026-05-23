using System;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors
{
	// Token: 0x02000434 RID: 1076
	public class PrisonerCaptureCampaignBehavior : CampaignBehaviorBase
	{
		// Token: 0x06004461 RID: 17505 RVA: 0x0014D8AC File Offset: 0x0014BAAC
		public override void RegisterEvents()
		{
			CampaignEvents.OnSettlementOwnerChangedEvent.AddNonSerializedListener(this, new Action<Settlement, bool, Hero, Hero, Hero, ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail>(this.OnSettlementOwnerChanged));
			CampaignEvents.WarDeclared.AddNonSerializedListener(this, new Action<IFaction, IFaction, DeclareWarAction.DeclareWarDetail>(this.OnWarDeclared));
			CampaignEvents.OnClanChangedKingdomEvent.AddNonSerializedListener(this, new Action<Clan, Kingdom, Kingdom, ChangeKingdomAction.ChangeKingdomActionDetail, bool>(this.OnClanChangedKingdom));
		}

		// Token: 0x06004462 RID: 17506 RVA: 0x0014D8FE File Offset: 0x0014BAFE
		public override void SyncData(IDataStore dataStore)
		{
		}

		// Token: 0x06004463 RID: 17507 RVA: 0x0014D900 File Offset: 0x0014BB00
		private void OnClanChangedKingdom(Clan clan, Kingdom oldKingdom, Kingdom newKingdom, ChangeKingdomAction.ChangeKingdomActionDetail detail, bool showNotification)
		{
			for (int i = 0; i < clan.Settlements.Count; i++)
			{
				Settlement settlement = clan.Settlements[i];
				if (settlement.IsFortification)
				{
					this.HandleSettlementHeroes(settlement);
				}
			}
		}

		// Token: 0x06004464 RID: 17508 RVA: 0x0014D940 File Offset: 0x0014BB40
		private void OnWarDeclared(IFaction faction1, IFaction faction2, DeclareWarAction.DeclareWarDetail detail)
		{
			for (int i = 0; i < faction1.Settlements.Count; i++)
			{
				Settlement settlement = faction1.Settlements[i];
				if (settlement.IsFortification)
				{
					this.HandleSettlementHeroes(settlement);
				}
			}
			for (int j = 0; j < faction2.Settlements.Count; j++)
			{
				Settlement settlement2 = faction2.Settlements[j];
				if (settlement2.IsFortification)
				{
					this.HandleSettlementHeroes(settlement2);
				}
			}
		}

		// Token: 0x06004465 RID: 17509 RVA: 0x0014D9B1 File Offset: 0x0014BBB1
		private void OnSettlementOwnerChanged(Settlement settlement, bool openToClaim, Hero newOwner, Hero oldOwner, Hero capturerHero, ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail detail)
		{
			if (settlement.IsFortification)
			{
				this.HandleSettlementHeroes(settlement);
			}
		}

		// Token: 0x06004466 RID: 17510 RVA: 0x0014D9C4 File Offset: 0x0014BBC4
		private void HandleSettlementHeroes(Settlement settlement)
		{
			for (int i = settlement.HeroesWithoutParty.Count - 1; i >= 0; i--)
			{
				Hero hero = settlement.HeroesWithoutParty[i];
				if (this.SettlementHeroCaptureCommonCondition(hero))
				{
					TakePrisonerAction.Apply(hero.CurrentSettlement.Party, hero);
				}
			}
			for (int j = settlement.Parties.Count - 1; j >= 0; j--)
			{
				MobileParty mobileParty = settlement.Parties[j];
				if (mobileParty.IsLordParty && (mobileParty.Army == null || (mobileParty.Army != null && mobileParty.Army.LeaderParty == mobileParty && !mobileParty.Army.Parties.Contains(MobileParty.MainParty))) && mobileParty.MapEvent == null && this.SettlementHeroCaptureCommonCondition(mobileParty.LeaderHero))
				{
					LeaveSettlementAction.ApplyForParty(mobileParty);
				}
			}
		}

		// Token: 0x06004467 RID: 17511 RVA: 0x0014DA90 File Offset: 0x0014BC90
		private bool SettlementHeroCaptureCommonCondition(Hero hero)
		{
			return hero != null && hero != Hero.MainHero && !hero.IsWanderer && !hero.IsNotable && hero.HeroState != Hero.CharacterStates.Prisoner && hero.HeroState != Hero.CharacterStates.Dead && hero.MapFaction != null && hero.CurrentSettlement != null && hero.MapFaction.IsAtWarWith(hero.CurrentSettlement.MapFaction);
		}
	}
}
