using NavalDLC.CharacterDevelopment;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;

namespace NavalDLC.CampaignBehaviors;

public class NavalVeteransWisdomCampaignBehaviour : CampaignBehaviorBase
{
	public override void RegisterEvents()
	{
		CampaignEvents.DailyTickPartyEvent.AddNonSerializedListener(this, OnDailyTickParty);
		CampaignEvents.PerkOpenedEvent.AddNonSerializedListener(this, OnPerkOpened);
	}

	private void OnPerkOpened(Hero hero, PerkObject perk)
	{
		if (hero == Hero.MainHero && (perk == NavalPerks.Boatswain.NavalHorde || perk == NavalPerks.Boatswain.Optimization || perk == NavalPerks.Boatswain.GildedPurse))
		{
			MobileParty.MainParty.ItemRoster.UpdateVersion();
		}
	}

	private void OnDailyTickParty(MobileParty party)
	{
		Hero perkOwnerHero = null;
		if (!party.HasPerk(NavalPerks.Boatswain.VeteransWisdom, out perkOwnerHero))
		{
			return;
		}
		int level = party.GetEffectiveRoleHolder(PartyRole.PartyLeader).Level;
		foreach (TroopRosterElement item in party.MemberRoster.GetTroopRoster())
		{
			if (item.Character.IsHero && item.Character.HeroObject.CompanionOf == party.ActualClan)
			{
				float randomFloat = MBRandom.RandomFloat;
				SkillObject skill = ((randomFloat < 0.33f) ? NavalSkills.Mariner : ((!(randomFloat < 0.66f)) ? NavalSkills.Shipmaster : NavalSkills.Boatswain));
				item.Character.HeroObject.AddSkillXp(skill, NavalPerks.Boatswain.VeteransWisdom.PrimaryBonus * (float)level);
			}
		}
	}

	public override void SyncData(IDataStore dataStore)
	{
	}
}
