using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.CharacterDevelopment;

public class DefaultPersonalityTraitEffects
{
	private TraitEffectObject _calculatingWorkshop;

	private TraitEffectObject _calculatingSiegePrep;

	private TraitEffectObject _calculatingNotableRelationEffect;

	private TraitEffectObject _calculatingChargeDamage;

	private TraitEffectObject _calculatingCombatMorale;

	private TraitEffectObject _calculatingLongerDisorganizeEffect;

	private TraitEffectObject _generosityMercenaryRecruitment;

	private TraitEffectObject _generosityMoraleGain;

	private TraitEffectObject _generosityFoodCost;

	private TraitEffectObject _generosityTownProject;

	private TraitEffectObject _generosityUpkeepReduction;

	private TraitEffectObject _generosityFlatMorale;

	private TraitEffectObject _honorRelationGain;

	private TraitEffectObject _honorLoyaltyGain;

	private TraitEffectObject _honorCrimeDecaySlow;

	private TraitEffectObject _honorCrimeIncreaseSlow;

	private TraitEffectObject _honorRecruitPenaltyReduction;

	private TraitEffectObject _honorClanLeaderRelation;

	private TraitEffectObject _mercyPrisonerCaptureMerciful;

	private TraitEffectObject _mercyPrisonerCaptureCruel;

	private TraitEffectObject _mercyHearthGrowth;

	private TraitEffectObject _mercyLordRansom;

	private TraitEffectObject _mercyTroopRansom;

	private TraitEffectObject _mercyRaidLoot;

	private TraitEffectObject _mercyRebellionChance;

	private TraitEffectObject _valorLossMoraleResist;

	private TraitEffectObject _valorBattleRenown;

	private TraitEffectObject _valorInjuryRecovery;

	private TraitEffectObject _valorPrisonerEscape;

	private TraitEffectObject _valorSiegeCasualty;

	private TraitEffectObject _valorSiegePrep;

	private static DefaultPersonalityTraitEffects Instance => Campaign.Current.DefaultPersonalityTraitEffects;

	public static TraitEffectObject CalculatingWorkshopEffect => Instance._calculatingWorkshop;

	public static TraitEffectObject CalculatingSiegePrepEffect => Instance._calculatingSiegePrep;

	public static TraitEffectObject CalculatingNotableRelationEffect => Instance._calculatingNotableRelationEffect;

	public static TraitEffectObject CalculatingChargeDamageEffect => Instance._calculatingChargeDamage;

	public static TraitEffectObject CalculatingCombatMoraleEffect => Instance._calculatingCombatMorale;

	public static TraitEffectObject CalculatingLongerDisorganizeEffect => Instance._calculatingLongerDisorganizeEffect;

	public static TraitEffectObject GenerosityMercenaryRecruitmentEffect => Instance._generosityMercenaryRecruitment;

	public static TraitEffectObject GenerosityMoraleGainEffect => Instance._generosityMoraleGain;

	public static TraitEffectObject GenerosityFoodCostEffect => Instance._generosityFoodCost;

	public static TraitEffectObject GenerosityTownProjectEffect => Instance._generosityTownProject;

	public static TraitEffectObject GenerosityUpkeepReductionEffect => Instance._generosityUpkeepReduction;

	public static TraitEffectObject GenerosityFlatMoraleEffect => Instance._generosityFlatMorale;

	public static TraitEffectObject HonorRelationGainEffect => Instance._honorRelationGain;

	public static TraitEffectObject HonorLoyaltyGainEffect => Instance._honorLoyaltyGain;

	public static TraitEffectObject HonorCrimeDecaySlowEffect => Instance._honorCrimeDecaySlow;

	public static TraitEffectObject HonorCrimeIncreaseSlowEffect => Instance._honorCrimeIncreaseSlow;

	public static TraitEffectObject HonorRecruitPenaltyReductionEffect => Instance._honorRecruitPenaltyReduction;

	public static TraitEffectObject HonorClanLeaderRelationEffect => Instance._honorClanLeaderRelation;

	public static TraitEffectObject MercyPrisonerCaptureMercifulEffect => Instance._mercyPrisonerCaptureMerciful;

	public static TraitEffectObject MercyPrisonerCaptureCruelEffect => Instance._mercyPrisonerCaptureCruel;

	public static TraitEffectObject MercyHearthGrowthEffect => Instance._mercyHearthGrowth;

	public static TraitEffectObject MercyLordRansomEffect => Instance._mercyLordRansom;

	public static TraitEffectObject MercyTroopRansomEffect => Instance._mercyTroopRansom;

	public static TraitEffectObject MercyRaidLootEffect => Instance._mercyRaidLoot;

	public static TraitEffectObject MercyRebellionChanceEffect => Instance._mercyRebellionChance;

	public static TraitEffectObject ValorLossMoraleResistEffect => Instance._valorLossMoraleResist;

	public static TraitEffectObject ValorBattleRenownEffect => Instance._valorBattleRenown;

	public static TraitEffectObject ValorInjuryRecoveryEffect => Instance._valorInjuryRecovery;

	public static TraitEffectObject ValorPrisonerEscapeEffect => Instance._valorPrisonerEscape;

	public static TraitEffectObject ValorSiegeCasualtyEffect => Instance._valorSiegeCasualty;

	public static TraitEffectObject ValorSiegePrepEffect => Instance._valorSiegePrep;

	public DefaultPersonalityTraitEffects()
	{
		RegisterAll();
		InitializeAll();
	}

	private void RegisterAll()
	{
		_calculatingWorkshop = Create("calculating_workshop");
		_calculatingSiegePrep = Create("calculating_siege_prep");
		_calculatingNotableRelationEffect = Create("calculating_notable_relation");
		_calculatingChargeDamage = Create("calculating_charge_damage");
		_calculatingCombatMorale = Create("calculating_combat_morale");
		_calculatingLongerDisorganizeEffect = Create("longer_disorganize");
		_generosityMercenaryRecruitment = Create("generosity_mercenary_recruitment");
		_generosityMoraleGain = Create("generosity_morale_gain");
		_generosityFoodCost = Create("generosity_food_cost");
		_generosityTownProject = Create("generosity_town_project");
		_generosityUpkeepReduction = Create("generosity_upkeep_reduction");
		_generosityFlatMorale = Create("generosity_flat_morale");
		_honorRelationGain = Create("honor_relation_gain");
		_honorLoyaltyGain = Create("honor_loyalty_gain");
		_honorCrimeDecaySlow = Create("honor_crime_decay_slow");
		_honorCrimeIncreaseSlow = Create("honor_crime_increase_slow");
		_honorRecruitPenaltyReduction = Create("honor_recruit_penalty_reduction");
		_honorClanLeaderRelation = Create("honor_clan_leader_relation");
		_mercyPrisonerCaptureMerciful = Create("mercy_prisoner_capture_merciful");
		_mercyPrisonerCaptureCruel = Create("mercy_prisoner_capture_cruel");
		_mercyHearthGrowth = Create("mercy_hearth_growth");
		_mercyLordRansom = Create("mercy_lord_ransom");
		_mercyTroopRansom = Create("mercy_troop_ransom");
		_mercyRaidLoot = Create("mercy_raid_loot");
		_mercyRebellionChance = Create("mercy_rebellion_chance");
		_valorLossMoraleResist = Create("valor_loss_morale_resist");
		_valorBattleRenown = Create("valor_battle_renown");
		_valorInjuryRecovery = Create("valor_injury_recovery");
		_valorPrisonerEscape = Create("valor_prisoner_escape");
		_valorSiegeCasualty = Create("valor_siege_casualty");
		_valorSiegePrep = Create("valor_siege_prep");
	}

	private static TraitEffectObject Create(string stringId)
	{
		return Game.Current.ObjectManager.RegisterPresumedObject(new TraitEffectObject(stringId));
	}

	private void InitializeAll()
	{
		_calculatingWorkshop.Initialize("{=*}Workshop income when governing a town", DefaultTraits.Calculating, new float[5] { 0f, 0f, 0f, 0.1f, 0.2f }, isPositiveEffect: true, EffectIncrementType.AddFactor);
		_calculatingSiegePrep.Initialize("{=*}Siege engine construction progress when leading a siege", DefaultTraits.Calculating, new float[5] { 0f, 0f, 0f, 0.1f, 0.2f }, isPositiveEffect: true, EffectIncrementType.AddFactor);
		_calculatingNotableRelationEffect.Initialize("{=*}Relation when meeting a town notable for the first time", DefaultTraits.Calculating, new float[5] { 0f, 0f, 0f, -3f, -6f }, isPositiveEffect: false, EffectIncrementType.Add);
		_calculatingChargeDamage.Initialize("{=*}Damage of troops given a charge order while leading a party or army", DefaultTraits.Calculating, new float[5] { 0.1f, 0.05f, 0f, 0f, 0f }, isPositiveEffect: true, EffectIncrementType.AddFactor);
		_calculatingCombatMorale.Initialize("{=*}Combat starting morale while leading a party or army", DefaultTraits.Calculating, new float[5] { 0.2f, 0.1f, 0f, 0f, 0f }, isPositiveEffect: true, EffectIncrementType.AddFactor);
		_calculatingLongerDisorganizeEffect.Initialize("{=*}Disorganized state duration", DefaultTraits.Calculating, new float[5] { 1f, 0.5f, 0f, 0f, 0f }, isPositiveEffect: false, EffectIncrementType.AddFactor);
		_generosityMercenaryRecruitment.Initialize("{=*}Mercenaries joining during recruitment", DefaultTraits.Generosity, new float[5] { 0f, 0f, 0f, 0.25f, 0.5f }, isPositiveEffect: true, EffectIncrementType.AddFactor);
		_generosityMoraleGain.Initialize("{=*}Morale gain while leading a party", DefaultTraits.Generosity, new float[5] { 0f, 0f, 0f, 0.1f, 0.2f }, isPositiveEffect: true, EffectIncrementType.AddFactor);
		_generosityFoodCost.Initialize("{=*}Food consumption while leading a party or governing a settlement", DefaultTraits.Generosity, new float[5] { 0f, 0f, 0f, 0.1f, 0.1f }, isPositiveEffect: false, EffectIncrementType.AddFactor);
		_generosityTownProject.Initialize("{=*}Settlement project reserve effectiveness while governing a settlement", DefaultTraits.Generosity, new float[5] { 0.2f, 0.1f, 0f, 0f, 0f }, isPositiveEffect: true, EffectIncrementType.AddFactor);
		_generosityUpkeepReduction.Initialize("{=*}Troop wages while leading a party or governing a settlement", DefaultTraits.Generosity, new float[5] { -0.1f, -0.05f, 0f, 0f, 0f }, isPositiveEffect: true, EffectIncrementType.AddFactor);
		_generosityFlatMorale.Initialize("{=*}Daily morale while leading a party", DefaultTraits.Generosity, new float[5] { -1f, -1f, 0f, 0f, 0f }, isPositiveEffect: false, EffectIncrementType.Add);
		_honorRelationGain.Initialize("{=*}Relationship gain rate", DefaultTraits.Honor, new float[5] { 0f, 0f, 0f, 0.1f, 0.2f }, isPositiveEffect: true, EffectIncrementType.AddFactor);
		_honorLoyaltyGain.Initialize("{=*}Loyalty gain while governing a settlement", DefaultTraits.Honor, new float[5] { 0f, 0f, 0f, 0.1f, 0.2f }, isPositiveEffect: true, EffectIncrementType.AddFactor);
		_honorCrimeDecaySlow.Initialize("{=*}Crime rate decay", DefaultTraits.Honor, new float[5] { 0f, 0f, 0f, -0.4f, -0.8f }, isPositiveEffect: false, EffectIncrementType.AddFactor);
		_honorCrimeIncreaseSlow.Initialize("{=*}Crime rate gain", DefaultTraits.Honor, new float[5] { -0.8f, -0.4f, 0f, 0f, 0f }, isPositiveEffect: true, EffectIncrementType.AddFactor);
		_honorRecruitPenaltyReduction.Initialize("{=*}Morale penalty when recruiting bandits and prisoners", DefaultTraits.Honor, new float[5] { -0.25f, -0.15f, 0f, 0f, 0f }, isPositiveEffect: true, EffectIncrementType.AddFactor);
		_honorClanLeaderRelation.Initialize("{=*}Relationship gain with clan leaders", DefaultTraits.Honor, new float[5] { -0.1f, -0.05f, 0f, 0f, 0f }, isPositiveEffect: false, EffectIncrementType.AddFactor);
		_mercyPrisonerCaptureMerciful.Initialize("{=*}Prisoner capture chance while leading a party", DefaultTraits.Mercy, new float[5] { 0f, 0f, 0f, 0.25f, 0.5f }, isPositiveEffect: true, EffectIncrementType.AddFactor);
		_mercyHearthGrowth.Initialize("{=*}Hearth growth rate while governing a settlement", DefaultTraits.Mercy, new float[5] { 0f, 0f, 0f, 0.05f, 0.1f }, isPositiveEffect: true, EffectIncrementType.AddFactor);
		_mercyLordRansom.Initialize("{=*}Lord ransom income", DefaultTraits.Mercy, new float[5] { 0f, 0f, 0f, 0f, -0.2f }, isPositiveEffect: false, EffectIncrementType.AddFactor);
		_mercyTroopRansom.Initialize("{=*}Troop ransom income", DefaultTraits.Mercy, new float[5] { 0f, 0f, 0f, -0.2f, 0f }, isPositiveEffect: false, EffectIncrementType.AddFactor);
		_mercyRaidLoot.Initialize("{=*}Loot from raids", DefaultTraits.Mercy, new float[5] { 0.4f, 0.2f, 0f, 0f, 0f }, isPositiveEffect: true, EffectIncrementType.AddFactor);
		_mercyRebellionChance.Initialize("{=*}Rebellion chance while governing a settlement", DefaultTraits.Mercy, new float[5] { -0.4f, -0.2f, 0f, 0f, 0f }, isPositiveEffect: true, EffectIncrementType.AddFactor);
		_mercyPrisonerCaptureCruel.Initialize("{=*}Prisoner capture chance while leading a party", DefaultTraits.Mercy, new float[5] { -0.5f, -0.25f, 0f, 0f, 0f }, isPositiveEffect: false, EffectIncrementType.AddFactor);
		_valorLossMoraleResist.Initialize("{=*}Morale damage taken from battlefield losses", DefaultTraits.Valor, new float[5] { 0f, 0f, 0f, -0.1f, -0.2f }, isPositiveEffect: true, EffectIncrementType.AddFactor);
		_valorBattleRenown.Initialize("{=*}Renown gain after battles", DefaultTraits.Valor, new float[5] { 0f, 0f, 0f, 0.1f, 0.2f }, isPositiveEffect: true, EffectIncrementType.AddFactor);
		_valorInjuryRecovery.Initialize("{=*}Personal injury recovery rate", DefaultTraits.Valor, new float[5] { 0f, 0f, 0f, -0.05f, -0.1f }, isPositiveEffect: false, EffectIncrementType.AddFactor);
		_valorPrisonerEscape.Initialize("{=*}Prisoner escape chance while leading a party or army", DefaultTraits.Valor, new float[5] { -0.5f, -0.25f, 0f, 0f, 0f }, isPositiveEffect: true, EffectIncrementType.AddFactor);
		_valorSiegeCasualty.Initialize("{=*}Damage to troops during siege bombardment and simulation", DefaultTraits.Valor, new float[5] { -0.2f, -0.1f, 0f, 0f, 0f }, isPositiveEffect: true, EffectIncrementType.AddFactor);
		_valorSiegePrep.Initialize("{=*}Siege preparation speed", DefaultTraits.Valor, new float[5] { -0.3f, -0.15f, 0f, 0f, 0f }, isPositiveEffect: false, EffectIncrementType.AddFactor);
	}
}
