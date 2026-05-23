using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces;

public abstract class PartyHealingModel : MBGameModel<PartyHealingModel>
{
	public abstract float GetSurgeryChance(PartyBase party);

	public abstract float GetSurvivalChance(PartyBase party, CharacterObject agentCharacter, DamageTypes damageType, bool canDamageKillEvenIfBlunt, PartyBase enemyParty = null);

	public abstract int GetSkillXpFromHealingTroop(PartyBase party);

	public abstract ExplainedNumber GetDailyHealingForRegulars(PartyBase partyBase, bool isPrisoner, bool includeDescriptions = false);

	public abstract ExplainedNumber GetDailyHealingHpForHeroes(PartyBase partyBase, bool isPrisoners, bool includeDescriptions = false);

	public abstract int GetHeroesEffectedHealingAmount(Hero hero, float healingRate);

	public abstract float GetSiegeBombardmentHitSurgeryChance(PartyBase party);

	public abstract ExplainedNumber GetBattleEndHealingAmount(PartyBase partyBase, Hero hero);
}
