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
		if (hero == null)
		{
			return;
		}
		if (perk == DefaultPerks.OneHanded.Trainer || perk == DefaultPerks.OneHanded.UnwaveringDefense || perk == DefaultPerks.TwoHanded.ThickHides || perk == DefaultPerks.Athletics.WellBuilt || perk == DefaultPerks.Medicine.PreventiveMedicine)
		{
			hero.HitPoints += (int)perk.PrimaryBonus;
		}
		else if (perk == DefaultPerks.Crafting.VigorousSmith)
		{
			int changeAmount = 1;
			hero.HeroDeveloper.AddAttribute(DefaultCharacterAttributes.Vigor, changeAmount, checkUnspentPoints: false);
		}
		else if (perk == DefaultPerks.Crafting.StrongSmith)
		{
			int changeAmount2 = 1;
			hero.HeroDeveloper.AddAttribute(DefaultCharacterAttributes.Control, changeAmount2, checkUnspentPoints: false);
		}
		else if (perk == DefaultPerks.Crafting.EnduringSmith)
		{
			int changeAmount3 = 1;
			hero.HeroDeveloper.AddAttribute(DefaultCharacterAttributes.Endurance, changeAmount3, checkUnspentPoints: false);
		}
		else if (perk == DefaultPerks.Crafting.WeaponMasterSmith)
		{
			int changeAmount4 = 1;
			int focus = hero.HeroDeveloper.GetFocus(DefaultSkills.OneHanded);
			int focus2 = hero.HeroDeveloper.GetFocus(DefaultSkills.TwoHanded);
			if (focus < Campaign.Current.Models.CharacterDevelopmentModel.MaxFocusPerSkill)
			{
				hero.HeroDeveloper.AddFocus(DefaultSkills.OneHanded, changeAmount4, checkUnspentFocusPoints: false);
			}
			if (focus2 < Campaign.Current.Models.CharacterDevelopmentModel.MaxFocusPerSkill)
			{
				hero.HeroDeveloper.AddFocus(DefaultSkills.TwoHanded, changeAmount4, checkUnspentFocusPoints: false);
			}
		}
		else if (perk == DefaultPerks.Athletics.Durable)
		{
			int changeAmount5 = 1;
			hero.HeroDeveloper.AddAttribute(DefaultCharacterAttributes.Endurance, changeAmount5, checkUnspentPoints: false);
		}
		else if (perk == DefaultPerks.Athletics.Steady)
		{
			int changeAmount6 = 1;
			hero.HeroDeveloper.AddAttribute(DefaultCharacterAttributes.Control, changeAmount6, checkUnspentPoints: false);
		}
		else if (perk == DefaultPerks.Athletics.Strong)
		{
			int changeAmount7 = 1;
			hero.HeroDeveloper.AddAttribute(DefaultCharacterAttributes.Vigor, changeAmount7, checkUnspentPoints: false);
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

	public override void SyncData(IDataStore dataStore)
	{
	}
}
