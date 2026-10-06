using Helpers;
using NavalDLC.CharacterDevelopment;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;

namespace NavalDLC.GameComponents;

public class NavalDLCPartyHealingModel : PartyHealingModel
{
	public override float GetSurgeryChance(PartyBase party)
	{
		return base.BaseModel.GetSurgeryChance(party);
	}

	public override float GetSurvivalChance(PartyBase party, CharacterObject agentCharacter, DamageTypes damageType, bool canDamageKillEvenIfBlunt, PartyBase enemyParty = null)
	{
		return base.BaseModel.GetSurvivalChance(party, agentCharacter, damageType, canDamageKillEvenIfBlunt, enemyParty);
	}

	public override int GetSkillXpFromHealingTroop(PartyBase party)
	{
		return base.BaseModel.GetSkillXpFromHealingTroop(party);
	}

	public override ExplainedNumber GetDailyHealingForRegulars(PartyBase partyBase, bool isPrisoner, bool includeDescriptions = false)
	{
		ExplainedNumber stat = base.BaseModel.GetDailyHealingForRegulars(partyBase, isPrisoner, includeDescriptions);
		if (partyBase.IsMobile)
		{
			PerkHelper.AddPerkBonusForParty(NavalPerks.Boatswain.Resilience, partyBase.MobileParty, isPrimaryBonus: false, ref stat);
		}
		return stat;
	}

	public override ExplainedNumber GetDailyHealingHpForHeroes(PartyBase partyBase, bool isPrisoners, bool includeDescriptions = false)
	{
		return base.BaseModel.GetDailyHealingHpForHeroes(partyBase, isPrisoners, includeDescriptions);
	}

	public override float GetSiegeBombardmentHitSurgeryChance(PartyBase party)
	{
		return base.BaseModel.GetSiegeBombardmentHitSurgeryChance(party);
	}

	public override ExplainedNumber GetBattleEndHealingAmount(PartyBase partyBase, Hero hero)
	{
		ExplainedNumber battleEndHealingAmount = base.BaseModel.GetBattleEndHealingAmount(partyBase, hero);
		if (hero.GetPerkValue(NavalPerks.Boatswain.Resilience))
		{
			battleEndHealingAmount.Add(NavalPerks.Boatswain.Resilience.PrimaryBonus * (float)(hero.MaxHitPoints - hero.HitPoints), NavalPerks.Boatswain.Resilience.Name);
		}
		return battleEndHealingAmount;
	}
}
