using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors;

public class PrisonerCaptureCampaignBehavior : CampaignBehaviorBase
{
	public override void RegisterEvents()
	{
		CampaignEvents.OnSettlementOwnerChangedEvent.AddNonSerializedListener(this, OnSettlementOwnerChanged);
		CampaignEvents.WarDeclared.AddNonSerializedListener(this, OnWarDeclared);
		CampaignEvents.OnClanChangedKingdomEvent.AddNonSerializedListener(this, OnClanChangedKingdom);
	}

	public override void SyncData(IDataStore dataStore)
	{
	}

	private void OnClanChangedKingdom(Clan clan, Kingdom oldKingdom, Kingdom newKingdom, ChangeKingdomAction.ChangeKingdomActionDetail detail, bool showNotification)
	{
		for (int i = 0; i < clan.Settlements.Count; i++)
		{
			Settlement settlement = clan.Settlements[i];
			if (settlement.IsFortification)
			{
				HandleSettlementHeroes(settlement);
			}
		}
	}

	private void OnWarDeclared(IFaction faction1, IFaction faction2, DeclareWarAction.DeclareWarDetail detail)
	{
		for (int i = 0; i < faction1.Settlements.Count; i++)
		{
			Settlement settlement = faction1.Settlements[i];
			if (settlement.IsFortification)
			{
				HandleSettlementHeroes(settlement);
			}
		}
		for (int j = 0; j < faction2.Settlements.Count; j++)
		{
			Settlement settlement2 = faction2.Settlements[j];
			if (settlement2.IsFortification)
			{
				HandleSettlementHeroes(settlement2);
			}
		}
	}

	private void OnSettlementOwnerChanged(Settlement settlement, bool openToClaim, Hero newOwner, Hero oldOwner, Hero capturerHero, ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail detail)
	{
		if (settlement.IsFortification)
		{
			HandleSettlementHeroes(settlement);
		}
	}

	private void HandleSettlementHeroes(Settlement settlement)
	{
		for (int num = settlement.HeroesWithoutParty.Count - 1; num >= 0; num--)
		{
			Hero hero = settlement.HeroesWithoutParty[num];
			if (SettlementHeroCaptureCommonCondition(hero))
			{
				TakePrisonerAction.Apply(hero.CurrentSettlement.Party, hero);
			}
		}
		for (int num2 = settlement.Parties.Count - 1; num2 >= 0; num2--)
		{
			MobileParty mobileParty = settlement.Parties[num2];
			if (mobileParty.IsLordParty && (mobileParty.Army == null || (mobileParty.Army != null && mobileParty.Army.LeaderParty == mobileParty && !mobileParty.Army.Parties.Contains(MobileParty.MainParty))) && mobileParty.MapEvent == null && SettlementHeroCaptureCommonCondition(mobileParty.LeaderHero))
			{
				LeaveSettlementAction.ApplyForParty(mobileParty);
			}
		}
	}

	private bool SettlementHeroCaptureCommonCondition(Hero hero)
	{
		if (hero != null && hero != Hero.MainHero && !hero.IsWanderer && !hero.IsNotable && hero.HeroState != Hero.CharacterStates.Prisoner && hero.HeroState != Hero.CharacterStates.Dead && hero.MapFaction != null && hero.CurrentSettlement != null)
		{
			return hero.MapFaction.IsAtWarWith(hero.CurrentSettlement.MapFaction);
		}
		return false;
	}
}
