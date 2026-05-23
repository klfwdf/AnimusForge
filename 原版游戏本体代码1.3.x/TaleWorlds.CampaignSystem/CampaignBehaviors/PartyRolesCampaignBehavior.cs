using System;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors
{
	// Token: 0x02000427 RID: 1063
	public class PartyRolesCampaignBehavior : CampaignBehaviorBase
	{
		// Token: 0x0600431C RID: 17180 RVA: 0x00144B60 File Offset: 0x00142D60
		public override void RegisterEvents()
		{
			CampaignEvents.HeroKilledEvent.AddNonSerializedListener(this, new Action<Hero, Hero, KillCharacterAction.KillCharacterActionDetail, bool>(this.OnHeroKilled));
			CampaignEvents.OnGovernorChangedEvent.AddNonSerializedListener(this, new Action<Town, Hero, Hero>(this.OnGovernorChanged));
			CampaignEvents.MobilePartyCreated.AddNonSerializedListener(this, new Action<MobileParty>(this.OnPartySpawned));
			CampaignEvents.CompanionRemoved.AddNonSerializedListener(this, new Action<Hero, RemoveCompanionAction.RemoveCompanionDetail>(this.OnCompanionRemoved));
			CampaignEvents.OnHeroGetsBusyEvent.AddNonSerializedListener(this, new Action<Hero, HeroGetsBusyReasons>(this.OnHeroGetsBusy));
			CampaignEvents.HeroPrisonerTaken.AddNonSerializedListener(this, new Action<PartyBase, Hero>(this.OnHeroPrisonerTaken));
			CampaignEvents.OnHeroChangedClanEvent.AddNonSerializedListener(this, new Action<Hero, Clan>(this.OnHeroChangedClan));
		}

		// Token: 0x0600431D RID: 17181 RVA: 0x00144C0E File Offset: 0x00142E0E
		public override void SyncData(IDataStore dataStore)
		{
		}

		// Token: 0x0600431E RID: 17182 RVA: 0x00144C10 File Offset: 0x00142E10
		private void OnHeroKilled(Hero victim, Hero killer, KillCharacterAction.KillCharacterActionDetail detail, bool showNotification = true)
		{
			if (victim.Clan == Clan.PlayerClan)
			{
				this.RemovePartyRoleIfExist(victim);
			}
		}

		// Token: 0x0600431F RID: 17183 RVA: 0x00144C26 File Offset: 0x00142E26
		private void OnHeroPrisonerTaken(PartyBase party, Hero prisoner)
		{
			if (prisoner.Clan == Clan.PlayerClan)
			{
				this.RemovePartyRoleIfExist(prisoner);
			}
		}

		// Token: 0x06004320 RID: 17184 RVA: 0x00144C3C File Offset: 0x00142E3C
		private void OnGovernorChanged(Town fortification, Hero oldGovernor, Hero newGovernor)
		{
			if (((newGovernor != null) ? newGovernor.Clan : null) == Clan.PlayerClan)
			{
				this.RemovePartyRoleIfExist(newGovernor);
			}
		}

		// Token: 0x06004321 RID: 17185 RVA: 0x00144C58 File Offset: 0x00142E58
		private void OnPartySpawned(MobileParty spawnedParty)
		{
			if (spawnedParty.IsLordParty && spawnedParty.ActualClan == Clan.PlayerClan)
			{
				foreach (TroopRosterElement troopRosterElement in spawnedParty.MemberRoster.GetTroopRoster())
				{
					if (troopRosterElement.Character.IsHero)
					{
						this.RemovePartyRoleIfExist(troopRosterElement.Character.HeroObject);
					}
				}
			}
		}

		// Token: 0x06004322 RID: 17186 RVA: 0x00144CDC File Offset: 0x00142EDC
		private void OnCompanionRemoved(Hero companion, RemoveCompanionAction.RemoveCompanionDetail detail)
		{
			this.RemovePartyRoleIfExist(companion);
		}

		// Token: 0x06004323 RID: 17187 RVA: 0x00144CE5 File Offset: 0x00142EE5
		private void OnHeroGetsBusy(Hero hero, HeroGetsBusyReasons heroGetsBusyReason)
		{
			if (hero.Clan == Clan.PlayerClan)
			{
				this.RemovePartyRoleIfExist(hero);
			}
		}

		// Token: 0x06004324 RID: 17188 RVA: 0x00144CFB File Offset: 0x00142EFB
		private void OnHeroChangedClan(Hero hero, Clan oldClan)
		{
			if (oldClan == Clan.PlayerClan)
			{
				this.RemovePartyRoleIfExist(hero);
			}
		}

		// Token: 0x06004325 RID: 17189 RVA: 0x00144D0C File Offset: 0x00142F0C
		private void RemovePartyRoleIfExist(Hero hero)
		{
			foreach (WarPartyComponent warPartyComponent in Clan.PlayerClan.WarPartyComponents)
			{
				if (warPartyComponent.MobileParty.GetHeroPartyRole(hero) != PartyRole.None)
				{
					warPartyComponent.MobileParty.RemoveHeroPartyRole(hero);
				}
			}
		}
	}
}
