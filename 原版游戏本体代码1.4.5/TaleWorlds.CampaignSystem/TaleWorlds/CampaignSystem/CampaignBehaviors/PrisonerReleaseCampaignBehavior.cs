using Helpers;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors;

public class PrisonerReleaseCampaignBehavior : CampaignBehaviorBase
{
	public override void RegisterEvents()
	{
		CampaignEvents.OnGameLoadedEvent.AddNonSerializedListener(this, OnGameLoaded);
		CampaignEvents.DailyTickHeroEvent.AddNonSerializedListener(this, DailyHeroTick);
		CampaignEvents.HourlyTickPartyEvent.AddNonSerializedListener(this, HourlyPartyTick);
		CampaignEvents.MakePeace.AddNonSerializedListener(this, OnMakePeaceEvent);
		CampaignEvents.OnClanChangedKingdomEvent.AddNonSerializedListener(this, ClanChangedKingdom);
		CampaignEvents.OnSettlementOwnerChangedEvent.AddNonSerializedListener(this, OnSettlementOwnerChanged);
	}

	private void OnGameLoaded(CampaignGameStarter campaignGameStarter)
	{
		if (!MBSaveLoad.IsUpdatingGameVersion || !(MBSaveLoad.LastLoadedGameVersion < ApplicationVersion.FromString("v1.2.0")))
		{
			return;
		}
		foreach (Hero allAliveHero in Hero.AllAliveHeroes)
		{
			if (allAliveHero == Hero.MainHero)
			{
				continue;
			}
			if (allAliveHero.IsPrisoner)
			{
				bool flag = allAliveHero.PartyBelongedToAsPrisoner != null && allAliveHero.PartyBelongedToAsPrisoner.IsMobile && allAliveHero.PartyBelongedToAsPrisoner.MobileParty.IsMilitia;
				bool flag2 = allAliveHero.PartyBelongedToAsPrisoner != null && !allAliveHero.PartyBelongedToAsPrisoner.MapFaction.IsAtWarWith(allAliveHero.MapFaction);
				if (allAliveHero.PartyBelongedToAsPrisoner == null)
				{
					if (allAliveHero.CurrentSettlement == null)
					{
						MakeHeroFugitiveAction.Apply(allAliveHero);
					}
				}
				else if (flag || flag2)
				{
					EndCaptivityAction.ApplyByEscape(allAliveHero);
					MakeHeroFugitiveAction.Apply(allAliveHero);
				}
			}
			else if (allAliveHero.PartyBelongedToAsPrisoner != null)
			{
				allAliveHero.PartyBelongedToAsPrisoner.PrisonRoster.RemoveTroop(allAliveHero.CharacterObject);
				MakeHeroFugitiveAction.Apply(allAliveHero);
			}
		}
	}

	private void ClanChangedKingdom(Clan clan, Kingdom oldKingdom, Kingdom newKingdom, ChangeKingdomAction.ChangeKingdomActionDetail detail, bool showNotification = true)
	{
		if (detail == ChangeKingdomAction.ChangeKingdomActionDetail.CreateKingdom)
		{
			return;
		}
		ReleasePrisonersInternal(clan);
		if (oldKingdom != null)
		{
			ReleasePrisonersInternal(oldKingdom);
			foreach (IFaction item in oldKingdom.FactionsAtWarWith)
			{
				ReleasePrisonersInternal(item);
			}
		}
		if (newKingdom != null)
		{
			OnAfterClanJoinedKingdom(clan, newKingdom);
			ReleasePrisonersInternal(newKingdom);
		}
	}

	private void OnAfterClanJoinedKingdom(Clan clan, Kingdom newKingdom)
	{
		foreach (Kingdom item in Kingdom.All)
		{
			if (item != newKingdom && item.IsAtWarWith(clan) && !item.IsAtWarWith(newKingdom))
			{
				OnMakePeace(clan, item);
			}
		}
	}

	private void OnMakePeaceEvent(IFaction side1Faction, IFaction side2Faction, MakePeaceAction.MakePeaceDetail detail)
	{
		OnMakePeace(side1Faction, side2Faction);
	}

	private void OnMakePeace(IFaction side1Faction, IFaction side2Faction)
	{
		ReleasePrisonersInternal(side1Faction);
		ReleasePrisonersInternal(side2Faction);
	}

	private static void ReleasePrisonersInternal(IFaction faction)
	{
		foreach (Settlement settlement in faction.Settlements)
		{
			for (int num = settlement.Party.PrisonRoster.Count - 1; num >= 0; num--)
			{
				if (settlement.Party.PrisonRoster.GetElementNumber(num) > 0)
				{
					TroopRosterElement elementCopyAtIndex = settlement.Party.PrisonRoster.GetElementCopyAtIndex(num);
					if (elementCopyAtIndex.Character.IsHero && elementCopyAtIndex.Character.HeroObject != Hero.MainHero && !elementCopyAtIndex.Character.HeroObject.MapFaction.IsAtWarWith(faction.MapFaction))
					{
						EndCaptivityAction.ApplyByPeace(elementCopyAtIndex.Character.HeroObject);
						CampaignEventDispatcher.Instance.OnPrisonersChangeInSettlement(settlement, null, elementCopyAtIndex.Character.HeroObject, takenFromDungeon: true);
					}
				}
			}
		}
		Clan clan = ((faction.IsClan || faction.IsMinorFaction) ? ((Clan)faction) : null);
		Kingdom kingdom = (faction.IsKingdomFaction ? ((Kingdom)faction) : null);
		if (clan != null)
		{
			ReleasePrisonersForClan(clan, faction);
		}
		else
		{
			if (kingdom == null)
			{
				return;
			}
			foreach (Clan clan2 in kingdom.Clans)
			{
				ReleasePrisonersForClan(clan2, faction);
			}
		}
	}

	private static void ReleasePrisonersForClan(Clan clan, IFaction faction)
	{
		foreach (Hero aliveLord in clan.AliveLords)
		{
			foreach (CaravanPartyComponent ownedCaravan in aliveLord.OwnedCaravans)
			{
				ReleasePartyPrisoners(ownedCaravan.MobileParty, faction);
			}
		}
		foreach (Hero companion in clan.Companions)
		{
			foreach (CaravanPartyComponent ownedCaravan2 in companion.OwnedCaravans)
			{
				ReleasePartyPrisoners(ownedCaravan2.MobileParty, faction);
			}
		}
		foreach (WarPartyComponent warPartyComponent in clan.WarPartyComponents)
		{
			ReleasePartyPrisoners(warPartyComponent.MobileParty, faction);
		}
		foreach (Settlement settlement in clan.Settlements)
		{
			if (settlement.IsVillage && settlement.Village.VillagerPartyComponent != null)
			{
				ReleasePartyPrisoners(settlement.Village.VillagerPartyComponent.MobileParty, faction);
			}
			else if ((settlement.IsCastle || settlement.IsTown) && settlement.Town.GarrisonParty != null)
			{
				ReleasePartyPrisoners(settlement.Town.GarrisonParty, faction);
			}
		}
	}

	private static void ReleasePartyPrisoners(MobileParty mobileParty, IFaction faction)
	{
		for (int num = mobileParty.PrisonRoster.Count - 1; num >= 0; num--)
		{
			if (mobileParty.Party.PrisonRoster.GetElementNumber(num) > 0)
			{
				TroopRosterElement elementCopyAtIndex = mobileParty.Party.PrisonRoster.GetElementCopyAtIndex(num);
				if (elementCopyAtIndex.Character.IsHero && elementCopyAtIndex.Character.HeroObject != Hero.MainHero && !elementCopyAtIndex.Character.HeroObject.MapFaction.IsAtWarWith(faction.MapFaction))
				{
					if (elementCopyAtIndex.Character.HeroObject.PartyBelongedToAsPrisoner == mobileParty.Party)
					{
						EndCaptivityAction.ApplyByPeace(elementCopyAtIndex.Character.HeroObject);
					}
					else
					{
						mobileParty.Party.PrisonRoster.RemoveTroop(elementCopyAtIndex.Character);
					}
				}
			}
		}
	}

	public override void SyncData(IDataStore dataStore)
	{
	}

	private void DailyHeroTick(Hero hero)
	{
		if (!hero.IsPrisoner || hero.PartyBelongedToAsPrisoner == null || hero == Hero.MainHero)
		{
			return;
		}
		float num = 0.04f;
		if (hero.PartyBelongedToAsPrisoner.IsMobile && hero.PartyBelongedToAsPrisoner.MobileParty.CurrentSettlement == null)
		{
			num *= 5f - MathF.Pow(MathF.Min(81, hero.PartyBelongedToAsPrisoner.NumberOfHealthyMembers), 0.25f);
		}
		if (hero.PartyBelongedToAsPrisoner == PartyBase.MainParty || (hero.PartyBelongedToAsPrisoner.IsSettlement && hero.PartyBelongedToAsPrisoner.Settlement.OwnerClan == Clan.PlayerClan) || (hero.PartyBelongedToAsPrisoner.IsMobile && hero.PartyBelongedToAsPrisoner.MobileParty.CurrentSettlement != null && hero.PartyBelongedToAsPrisoner.MobileParty.CurrentSettlement.OwnerClan == Clan.PlayerClan))
		{
			num *= 0.5f;
		}
		ExplainedNumber stat = new ExplainedNumber(num);
		if (hero.PartyBelongedToAsPrisoner.IsSettlement && hero.PartyBelongedToAsPrisoner.Settlement.Town != null && hero.PartyBelongedToAsPrisoner.Settlement.Town.Governor != null)
		{
			Town town = hero.PartyBelongedToAsPrisoner.Settlement.Town;
			if (hero.PartyBelongedToAsPrisoner.Settlement.IsTown || hero.PartyBelongedToAsPrisoner.Settlement.IsCastle)
			{
				if (town.Governor.GetPerkValue(DefaultPerks.Roguery.SweetTalker))
				{
					stat.AddFactor(DefaultPerks.Roguery.SweetTalker.SecondaryBonus, DefaultPerks.Roguery.SweetTalker.Description);
				}
				if (town.Governor.GetPerkValue(DefaultPerks.Engineering.DungeonArchitect))
				{
					stat.AddFactor(DefaultPerks.Engineering.DungeonArchitect.SecondaryBonus, DefaultPerks.Engineering.DungeonArchitect.Description);
				}
				if (town.Governor.GetPerkValue(DefaultPerks.Riding.MountedPatrols))
				{
					stat.AddFactor(DefaultPerks.Riding.MountedPatrols.SecondaryBonus, DefaultPerks.Riding.MountedPatrols.Description);
				}
			}
		}
		if (hero.PartyBelongedToAsPrisoner.IsMobile)
		{
			if (hero.GetPerkValue(DefaultPerks.Roguery.FleetFooted))
			{
				stat.AddFactor(DefaultPerks.Roguery.FleetFooted.SecondaryBonus);
			}
			if (hero.PartyBelongedToAsPrisoner.MobileParty.HasPerk(DefaultPerks.Riding.MountedPatrols))
			{
				PerkHelper.AddPerkBonusForParty(DefaultPerks.Riding.MountedPatrols, hero.PartyBelongedToAsPrisoner.MobileParty, isPrimaryBonus: true, ref stat);
			}
			if (hero.PartyBelongedToAsPrisoner.MobileParty.HasPerk(DefaultPerks.Roguery.RansomBroker))
			{
				PerkHelper.AddPerkBonusForParty(DefaultPerks.Roguery.RansomBroker, hero.PartyBelongedToAsPrisoner.MobileParty, isPrimaryBonus: false, ref stat);
			}
		}
		if (hero.PartyBelongedToAsPrisoner.IsMobile && !hero.PartyBelongedToAsPrisoner.MobileParty.IsCurrentlyAtSea)
		{
			PerkHelper.AddPerkBonusForParty(DefaultPerks.Scouting.KeenSight, hero.PartyBelongedToAsPrisoner.MobileParty, isPrimaryBonus: false, ref stat);
		}
		if (MBRandom.RandomFloat < stat.ResultNumber)
		{
			EndCaptivityAction.ApplyByEscape(hero);
		}
	}

	private void HourlyPartyTick(MobileParty mobileParty)
	{
		int prisonerSizeLimit = mobileParty.Party.PrisonerSizeLimit;
		if (mobileParty.MapEvent != null || mobileParty.SiegeEvent != null || mobileParty.PrisonRoster.TotalManCount <= prisonerSizeLimit)
		{
			return;
		}
		int num = mobileParty.PrisonRoster.TotalManCount - prisonerSizeLimit;
		for (int i = 0; i < num; i++)
		{
			bool flag = mobileParty.PrisonRoster.TotalRegulars > 0;
			float randomFloat = MBRandom.RandomFloat;
			int num2 = (flag ? ((int)((float)mobileParty.PrisonRoster.TotalRegulars * randomFloat)) : ((int)((float)mobileParty.PrisonRoster.TotalManCount * randomFloat)));
			CharacterObject character = null;
			foreach (TroopRosterElement item in mobileParty.PrisonRoster.GetTroopRoster())
			{
				if (!item.Character.IsHero || !flag)
				{
					num2 -= item.Number;
					if (num2 <= 0)
					{
						character = item.Character;
						break;
					}
				}
			}
			ApplyEscapeChanceToExceededPrisoners(character, mobileParty);
		}
	}

	private void ApplyEscapeChanceToExceededPrisoners(CharacterObject character, MobileParty capturerParty)
	{
		ExplainedNumber explainedNumber = new ExplainedNumber(0.1f);
		if (!capturerParty.IsCurrentlyAtSea && capturerParty.HasPerk(DefaultPerks.Athletics.Stamina, checkSecondaryRole: true))
		{
			explainedNumber.AddFactor(-0.1f, DefaultPerks.Athletics.Stamina.Name);
		}
		if (!capturerParty.IsGarrison && !capturerParty.IsMilitia && !character.IsPlayerCharacter && MBRandom.RandomFloat < explainedNumber.ResultNumber)
		{
			if (character.IsHero)
			{
				EndCaptivityAction.ApplyByEscape(character.HeroObject);
			}
			else
			{
				capturerParty.PrisonRoster.AddToCounts(character, -1);
			}
		}
	}

	private void OnSettlementOwnerChanged(Settlement settlement, bool openToClaim, Hero newOwner, Hero oldOwner, Hero capturerHero, ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail detail)
	{
		foreach (TroopRosterElement item in settlement.Party.PrisonRoster.GetTroopRoster())
		{
			if (item.Character.IsHero && item.Character.HeroObject != Hero.MainHero && !item.Character.HeroObject.MapFaction.IsAtWarWith(settlement.MapFaction))
			{
				if (item.Character.HeroObject.PartyBelongedToAsPrisoner == settlement.Party && item.Character.HeroObject.IsPrisoner)
				{
					EndCaptivityAction.ApplyByReleasedAfterBattle(item.Character.HeroObject);
				}
				else
				{
					settlement.Party.PrisonRoster.RemoveTroop(item.Character, item.Number);
				}
			}
		}
	}
}
