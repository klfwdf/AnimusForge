using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors;

public class PerkActivationHandlerCampaignBehavior : CampaignBehaviorBase
{
	public override void RegisterEvents()
	{
		CampaignEvents.PerkOpenedEvent.AddNonSerializedListener(this, OnPerkOpened);
	}

	private void OnPerkOpened(Hero hero, PerkObject perk)
	{
		if (hero != null)
		{
			if (perk == DefaultPerks.OneHanded.Trainer || perk == DefaultPerks.OneHanded.UnwaveringDefense || perk == DefaultPerks.TwoHanded.ThickHides || perk == DefaultPerks.Athletics.WellBuilt || perk == DefaultPerks.Medicine.PreventiveMedicine)
			{
				hero.HitPoints += (int)perk.PrimaryBonus;
			}
			else if (perk == DefaultPerks.Crafting.VigorousSmith)
			{
				hero.HeroDeveloper.AddAttribute(DefaultCharacterAttributes.Vigor, 1, checkUnspentPoints: false);
			}
			else if (perk == DefaultPerks.Crafting.StrongSmith)
			{
				hero.HeroDeveloper.AddAttribute(DefaultCharacterAttributes.Control, 1, checkUnspentPoints: false);
			}
			else if (perk == DefaultPerks.Crafting.EnduringSmith)
			{
				hero.HeroDeveloper.AddAttribute(DefaultCharacterAttributes.Endurance, 1, checkUnspentPoints: false);
			}
			else if (perk == DefaultPerks.Crafting.WeaponMasterSmith)
			{
				hero.HeroDeveloper.AddFocus(DefaultSkills.OneHanded, 1, checkUnspentFocusPoints: false);
				hero.HeroDeveloper.AddFocus(DefaultSkills.TwoHanded, 1, checkUnspentFocusPoints: false);
			}
			else if (perk == DefaultPerks.Athletics.Durable)
			{
				hero.HeroDeveloper.AddAttribute(DefaultCharacterAttributes.Endurance, 1, checkUnspentPoints: false);
			}
			else if (perk == DefaultPerks.Athletics.Steady)
			{
				hero.HeroDeveloper.AddAttribute(DefaultCharacterAttributes.Control, 1, checkUnspentPoints: false);
			}
			else if (perk == DefaultPerks.Athletics.Strong)
			{
				hero.HeroDeveloper.AddAttribute(DefaultCharacterAttributes.Vigor, 1, checkUnspentPoints: false);
			}
			if (hero == Hero.MainHero && (perk == DefaultPerks.OneHanded.Prestige || perk == DefaultPerks.TwoHanded.Hope || perk == DefaultPerks.Athletics.ImposingStature || perk == DefaultPerks.Bow.MerryMen || perk == DefaultPerks.Tactics.HordeLeader || perk == DefaultPerks.Scouting.MountedScouts || perk == DefaultPerks.Leadership.Authority || perk == DefaultPerks.Leadership.LeaderOfMasses || perk == DefaultPerks.Leadership.UltimateLeader))
			{
				PartyBase.MainParty.MemberRoster.UpdateVersion();
			}
			if (perk.PrimaryRole == PartyRole.Captain)
			{
				hero.UpdatePowerModifier();
			}
		}
	}

	public override void SyncData(IDataStore dataStore)
	{
	}
}
