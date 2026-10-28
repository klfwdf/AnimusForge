using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors;

public class PartyRolesCampaignBehavior : CampaignBehaviorBase
{
	public override void RegisterEvents()
	{
		CampaignEvents.HeroKilledEvent.AddNonSerializedListener(this, OnHeroKilled);
		CampaignEvents.OnGovernorChangedEvent.AddNonSerializedListener(this, OnGovernorChanged);
		CampaignEvents.MobilePartyCreated.AddNonSerializedListener(this, OnPartySpawned);
		CampaignEvents.CompanionRemoved.AddNonSerializedListener(this, OnCompanionRemoved);
		CampaignEvents.OnHeroGetsBusyEvent.AddNonSerializedListener(this, OnHeroGetsBusy);
		CampaignEvents.HeroPrisonerTaken.AddNonSerializedListener(this, OnHeroPrisonerTaken);
		CampaignEvents.OnHeroChangedClanEvent.AddNonSerializedListener(this, OnHeroChangedClan);
	}

	public override void SyncData(IDataStore dataStore)
	{
	}

	private void OnHeroKilled(Hero victim, Hero killer, KillCharacterAction.KillCharacterActionDetail detail, bool showNotification = true)
	{
		if (victim.Clan == Clan.PlayerClan)
		{
			RemoveAllPartyRolesOfHeroIfExist(victim);
		}
	}

	private void OnHeroPrisonerTaken(PartyBase party, Hero prisoner)
	{
		if (prisoner.Clan == Clan.PlayerClan)
		{
			RemoveAllPartyRolesOfHeroIfExist(prisoner);
		}
	}

	private void OnGovernorChanged(Town fortification, Hero oldGovernor, Hero newGovernor)
	{
		if (newGovernor?.Clan == Clan.PlayerClan)
		{
			RemoveAllPartyRolesOfHeroIfExist(newGovernor);
		}
	}

	private void OnPartySpawned(MobileParty spawnedParty)
	{
		if (!spawnedParty.IsLordParty || spawnedParty.ActualClan != Clan.PlayerClan)
		{
			return;
		}
		foreach (TroopRosterElement item in spawnedParty.MemberRoster.GetTroopRoster())
		{
			if (item.Character.IsHero)
			{
				RemoveAllPartyRolesOfHeroIfExist(item.Character.HeroObject);
			}
		}
	}

	private void OnCompanionRemoved(Hero companion, RemoveCompanionAction.RemoveCompanionDetail detail)
	{
		RemoveAllPartyRolesOfHeroIfExist(companion);
	}

	private void OnHeroGetsBusy(Hero hero, HeroGetsBusyReasons heroGetsBusyReason)
	{
		if (hero.Clan == Clan.PlayerClan)
		{
			RemoveAllPartyRolesOfHeroIfExist(hero);
		}
	}

	private void OnHeroChangedClan(Hero hero, Clan oldClan)
	{
		if (oldClan == Clan.PlayerClan)
		{
			RemoveAllPartyRolesOfHeroIfExist(hero);
		}
	}

	private void RemoveAllPartyRolesOfHeroIfExist(Hero hero)
	{
		foreach (WarPartyComponent warPartyComponent in Clan.PlayerClan.WarPartyComponents)
		{
			warPartyComponent.MobileParty.RemoveAllPartyRolesOfHero(hero);
		}
	}
}
