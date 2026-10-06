using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem;

public class DefaultSkillEffects
{
	private SkillEffect _effectOneHandedSpeed;

	private SkillEffect _effectOneHandedDamage;

	private SkillEffect _effectTwoHandedSpeed;

	private SkillEffect _effectTwoHandedDamage;

	private SkillEffect _effectPolearmSpeed;

	private SkillEffect _effectPolearmDamage;

	private SkillEffect _effectBowDamage;

	private SkillEffect _effectBowAccuracy;

	private SkillEffect _effectThrowingSpeed;

	private SkillEffect _effectThrowingDamage;

	private SkillEffect _effectThrowingAccuracy;

	private SkillEffect _effectCrossbowReloadSpeed;

	private SkillEffect _effectCrossbowAccuracy;

	private SkillEffect _effectHorseSpeed;

	private SkillEffect _effectHorseManeuver;

	private SkillEffect _effectMountedWeaponDamagePenalty;

	private SkillEffect _effectMountedWeaponSpeedPenalty;

	private SkillEffect _effectDismountResistance;

	private SkillEffect _effectAthleticsSpeedFactor;

	private SkillEffect _effectAthleticsWeightFactor;

	private SkillEffect _effectKnockBackResistance;

	private SkillEffect _effectKnockDownResistance;

	private SkillEffect _effectSmithingLevel;

	private SkillEffect _effectTacticsAdvantage;

	private SkillEffect _effectTacticsTroopSacrificeReduction;

	private SkillEffect _effectTrackingRadius;

	private SkillEffect _effectTrackingSpottingDistance;

	private SkillEffect _effectTrackingTrackInformation;

	private SkillEffect _effectRogueryLootBonus;

	private SkillEffect _effectCharmRelationBonus;

	private SkillEffect _effectTradePenaltyReduction;

	private SkillEffect _effectSurgeonSurvivalBonus;

	private SkillEffect _effectSiegeEngineProductionBonus;

	private SkillEffect _effectTownProjectBuildingBonus;

	private SkillEffect _effectHealingRateBonusForHeroes;

	private SkillEffect _effectHealingRateBonusForRegulars;

	private SkillEffect _effectGovernorHealingRateBonus;

	private SkillEffect _effectLeadershipMoraleBonus;

	private SkillEffect _effectLeadershipGarrisonSizeBonus;

	private SkillEffect _effectStewardPartySizeBonus;

	private SkillEffect _effectSneakDamage;

	private SkillEffect _effectCrouchedSpeed;

	private SkillEffect _effectNoiseSuppression;

	private static DefaultSkillEffects Instance => Campaign.Current.DefaultSkillEffects;

	public static SkillEffect OneHandedSpeed => Instance._effectOneHandedSpeed;

	public static SkillEffect OneHandedDamage => Instance._effectOneHandedDamage;

	public static SkillEffect TwoHandedSpeed => Instance._effectTwoHandedSpeed;

	public static SkillEffect TwoHandedDamage => Instance._effectTwoHandedDamage;

	public static SkillEffect PolearmSpeed => Instance._effectPolearmSpeed;

	public static SkillEffect PolearmDamage => Instance._effectPolearmDamage;

	public static SkillEffect BowDamage => Instance._effectBowDamage;

	public static SkillEffect BowAccuracy => Instance._effectBowAccuracy;

	public static SkillEffect ThrowingSpeed => Instance._effectThrowingSpeed;

	public static SkillEffect ThrowingDamage => Instance._effectThrowingDamage;

	public static SkillEffect ThrowingAccuracy => Instance._effectThrowingAccuracy;

	public static SkillEffect CrossbowReloadSpeed => Instance._effectCrossbowReloadSpeed;

	public static SkillEffect CrossbowAccuracy => Instance._effectCrossbowAccuracy;

	public static SkillEffect HorseSpeed => Instance._effectHorseSpeed;

	public static SkillEffect HorseManeuver => Instance._effectHorseManeuver;

	public static SkillEffect MountedWeaponDamagePenalty => Instance._effectMountedWeaponDamagePenalty;

	public static SkillEffect MountedWeaponSpeedPenalty => Instance._effectMountedWeaponSpeedPenalty;

	public static SkillEffect DismountResistance => Instance._effectDismountResistance;

	public static SkillEffect AthleticsSpeedFactor => Instance._effectAthleticsSpeedFactor;

	public static SkillEffect AthleticsWeightFactor => Instance._effectAthleticsWeightFactor;

	public static SkillEffect KnockBackResistance => Instance._effectKnockBackResistance;

	public static SkillEffect KnockDownResistance => Instance._effectKnockDownResistance;

	public static SkillEffect SmithingLevel => Instance._effectSmithingLevel;

	public static SkillEffect TacticsAdvantage => Instance._effectTacticsAdvantage;

	public static SkillEffect TacticsTroopSacrificeReduction => Instance._effectTacticsTroopSacrificeReduction;

	public static SkillEffect TrackingRadius => Instance._effectTrackingRadius;

	public static SkillEffect TrackingSpottingDistance => Instance._effectTrackingSpottingDistance;

	public static SkillEffect TrackingTrackInformation => Instance._effectTrackingTrackInformation;

	public static SkillEffect RogueryLootBonus => Instance._effectRogueryLootBonus;

	public static SkillEffect CharmRelationBonus => Instance._effectCharmRelationBonus;

	public static SkillEffect TradePenaltyReduction => Instance._effectTradePenaltyReduction;

	public static SkillEffect SurgeonSurvivalBonus => Instance._effectSurgeonSurvivalBonus;

	public static SkillEffect SiegeEngineProductionBonus => Instance._effectSiegeEngineProductionBonus;

	public static SkillEffect TownProjectBuildingBonus => Instance._effectTownProjectBuildingBonus;

	public static SkillEffect HealingRateBonusForHeroes => Instance._effectHealingRateBonusForHeroes;

	public static SkillEffect HealingRateBonusForRegulars => Instance._effectHealingRateBonusForRegulars;

	public static SkillEffect GovernorHealingRateBonus => Instance._effectGovernorHealingRateBonus;

	public static SkillEffect LeadershipMoraleBonus => Instance._effectLeadershipMoraleBonus;

	public static SkillEffect LeadershipGarrisonSizeBonus => Instance._effectLeadershipGarrisonSizeBonus;

	public static SkillEffect StewardPartySizeBonus => Instance._effectStewardPartySizeBonus;

	public static SkillEffect SneakDamage => Instance._effectSneakDamage;

	public static SkillEffect CrouchedSpeed => Instance._effectCrouchedSpeed;

	public static SkillEffect NoiseSuppression => Instance._effectNoiseSuppression;

	public DefaultSkillEffects()
	{
		RegisterAll();
	}

	private void RegisterAll()
	{
		_effectOneHandedSpeed = Create("OneHandedSpeed");
		_effectOneHandedDamage = Create("OneHandedDamage");
		_effectTwoHandedSpeed = Create("TwoHandedSpeed");
		_effectTwoHandedDamage = Create("TwoHandedDamage");
		_effectPolearmSpeed = Create("PolearmSpeed");
		_effectPolearmDamage = Create("PolearmDamage");
		_effectBowDamage = Create("BowDamage");
		_effectBowAccuracy = Create("BowAccuracy");
		_effectThrowingSpeed = Create("ThrowingSpeed");
		_effectThrowingDamage = Create("ThrowingDamage");
		_effectThrowingAccuracy = Create("ThrowingAccuracy");
		_effectCrossbowReloadSpeed = Create("CrossbowReloadSpeed");
		_effectCrossbowAccuracy = Create("CrossbowAccuracy");
		_effectHorseSpeed = Create("HorseSpeed");
		_effectHorseManeuver = Create("HorseManeuver");
		_effectMountedWeaponDamagePenalty = Create("MountedWeaponDamagePenalty");
		_effectMountedWeaponSpeedPenalty = Create("MountedWeaponSpeedPenalty");
		_effectDismountResistance = Create("DismountResistance");
		_effectAthleticsSpeedFactor = Create("AthleticsSpeedFactor");
		_effectAthleticsWeightFactor = Create("AthleticsWeightFactor");
		_effectKnockBackResistance = Create("KnockBackResistance");
		_effectKnockDownResistance = Create("KnockDownResistance");
		_effectSmithingLevel = Create("SmithingLevel");
		_effectTacticsAdvantage = Create("TacticsAdvantage");
		_effectTacticsTroopSacrificeReduction = Create("TacticsTroopSacrificeReduction");
		_effectTrackingRadius = Create("TrackingRadius");
		_effectTrackingSpottingDistance = Create("TrackingSpottingDistance");
		_effectTrackingTrackInformation = Create("TrackingTrackInformation");
		_effectRogueryLootBonus = Create("RogueryLootBonus");
		_effectCharmRelationBonus = Create("CharmRelationBonus");
		_effectTradePenaltyReduction = Create("TradePenaltyReduction");
		_effectLeadershipMoraleBonus = Create("LeadershipMoraleBonus");
		_effectLeadershipGarrisonSizeBonus = Create("LeadershipGarrisonSizeBonus");
		_effectSurgeonSurvivalBonus = Create("SurgeonSurvivalBonus");
		_effectHealingRateBonusForHeroes = Create("HealingRateBonusForHeroes");
		_effectHealingRateBonusForRegulars = Create("HealingRateBonusForRegulars");
		_effectGovernorHealingRateBonus = Create("GovernorHealingRateBonus");
		_effectSiegeEngineProductionBonus = Create("SiegeEngineProductionBonus");
		_effectTownProjectBuildingBonus = Create("TownProjectBuildingBonus");
		_effectStewardPartySizeBonus = Create("StewardPartySizeBonus");
		_effectSneakDamage = Create("SneakDamage");
		_effectCrouchedSpeed = Create("CrouchedSpeed");
		_effectNoiseSuppression = Create("NoiseSuppression");
		InitializeAll();
	}

	private SkillEffect Create(string stringId)
	{
		return Game.Current.ObjectManager.RegisterPresumedObject(new SkillEffect(stringId));
	}

	private void InitializeAll()
	{
		_effectOneHandedSpeed.Initialize(new TextObject("{=hjxRvb9l}One handed weapon speed: +{a0}%"), DefaultSkills.OneHanded, PartyRole.Personal, 0.0007f, EffectIncrementType.AddFactor);
		_effectOneHandedDamage.Initialize(new TextObject("{=baUFKAbd}One handed weapon damage: +{a0}%"), DefaultSkills.OneHanded, PartyRole.Personal, 0.0015f, EffectIncrementType.AddFactor);
		_effectTwoHandedSpeed.Initialize(new TextObject("{=Np94rYMz}Two handed weapon speed: +{a0}%"), DefaultSkills.TwoHanded, PartyRole.Personal, 0.0006f, EffectIncrementType.AddFactor);
		_effectTwoHandedDamage.Initialize(new TextObject("{=QkbbLb4v}Two handed weapon damage: +{a0}%"), DefaultSkills.TwoHanded, PartyRole.Personal, 0.0016f, EffectIncrementType.AddFactor);
		_effectPolearmSpeed.Initialize(new TextObject("{=2ATI9qVM}Polearm weapon speed: +{a0}%"), DefaultSkills.Polearm, PartyRole.Personal, 0.0006f, EffectIncrementType.AddFactor);
		_effectPolearmDamage.Initialize(new TextObject("{=17cIGVQE}Polearm weapon damage: +{a0}%"), DefaultSkills.Polearm, PartyRole.Personal, 0.0007f, EffectIncrementType.AddFactor);
		_effectBowDamage.Initialize(new TextObject("{=RUZHJMQO}Bow Damage: +{a0}%"), DefaultSkills.Bow, PartyRole.Personal, 0.0011f, EffectIncrementType.AddFactor);
		_effectBowAccuracy.Initialize(new TextObject("{=sQCS90Wq}Bow Accuracy: +{a0}%"), DefaultSkills.Bow, PartyRole.Personal, -0.0009f, EffectIncrementType.AddFactor);
		_effectThrowingSpeed.Initialize(new TextObject("{=Z0CoeojG}Thrown weapon speed: +{a0}%"), DefaultSkills.Throwing, PartyRole.Personal, 0.0007f, EffectIncrementType.AddFactor);
		_effectThrowingDamage.Initialize(new TextObject("{=TQMGppEk}Thrown weapon damage: +{a0}%"), DefaultSkills.Throwing, PartyRole.Personal, 0.0006f, EffectIncrementType.AddFactor);
		_effectThrowingAccuracy.Initialize(new TextObject("{=SfKrjKuO}Thrown weapon accuracy: +{a0}%"), DefaultSkills.Throwing, PartyRole.Personal, -0.0006f, EffectIncrementType.AddFactor);
		_effectCrossbowReloadSpeed.Initialize(new TextObject("{=W0Zu4iDz}Crossbow reload speed: +{a0}%"), DefaultSkills.Crossbow, PartyRole.Personal, 0.0007f, EffectIncrementType.AddFactor);
		_effectCrossbowAccuracy.Initialize(new TextObject("{=JwWnpD40}Crossbow accuracy: +{a0}%"), DefaultSkills.Crossbow, PartyRole.Personal, -0.0005f, EffectIncrementType.AddFactor);
		_effectHorseSpeed.Initialize(new TextObject("{=Y07OcP1T}Horse speed: +{a0}"), DefaultSkills.Riding, PartyRole.Personal, 0.002f, EffectIncrementType.AddFactor);
		_effectHorseManeuver.Initialize(new TextObject("{=AahNTeXY}Horse maneuver: +{a0}"), DefaultSkills.Riding, PartyRole.Personal, 0.0004f, EffectIncrementType.AddFactor);
		_effectMountedWeaponDamagePenalty.Initialize(new TextObject("{=0dbwEczK}Mounted weapon damage penalty: {a0}%"), DefaultSkills.Riding, PartyRole.Personal, 0.002f, EffectIncrementType.AddFactor, -0.2f, float.MinValue, 0f);
		_effectMountedWeaponSpeedPenalty.Initialize(new TextObject("{=oE5etyy0}Mounted weapon speed & reload penalty: {a0}%"), DefaultSkills.Riding, PartyRole.Personal, 0.003f, EffectIncrementType.AddFactor, -0.3f, float.MinValue, 0f);
		_effectDismountResistance.Initialize(new TextObject("{=kbHJVxAo}Dismount resistance: {a0}% of max. hitpoints"), DefaultSkills.Riding, PartyRole.Personal, 0.001f, EffectIncrementType.AddFactor, 0.4f);
		_effectAthleticsSpeedFactor.Initialize(new TextObject("{=rgb6vdon}Running speed increased by {a0}%"), DefaultSkills.Athletics, PartyRole.Personal, 0.001f, EffectIncrementType.AddFactor);
		_effectAthleticsWeightFactor.Initialize(new TextObject("{=WaUuhxwv}Weight penalty reduced by: {a0}%"), DefaultSkills.Athletics, PartyRole.Personal, -0.001f, EffectIncrementType.AddFactor);
		_effectKnockBackResistance.Initialize(new TextObject("{=TyjDHQUv}Knock back resistance: {a0}% of max. hitpoints"), DefaultSkills.Athletics, PartyRole.Personal, 0.001f, EffectIncrementType.AddFactor, 0.15f);
		_effectKnockDownResistance.Initialize(new TextObject("{=tlNZIH3l}Knock down resistance: {a0}% of max. hitpoints"), DefaultSkills.Athletics, PartyRole.Personal, 0.001f, EffectIncrementType.AddFactor, 0.4f);
		_effectSmithingLevel.Initialize(new TextObject("{=ImN8Cfk6}Max difficulty of weapon that can be smithed without penalty: {a0}"), DefaultSkills.Crafting, PartyRole.Personal, 1f, EffectIncrementType.Add);
		_effectTacticsAdvantage.Initialize(new TextObject("{=XO3SOlZx}Simulation advantage: +{a0}%"), DefaultSkills.Tactics, PartyRole.Personal, 0.001f, EffectIncrementType.AddFactor);
		_effectTacticsTroopSacrificeReduction.Initialize(new TextObject("{=VHdyQYKI}Decrease the sacrificed troop number when trying to get away +{a0}%"), DefaultSkills.Tactics, PartyRole.Personal, -0.001f, EffectIncrementType.AddFactor);
		_effectTrackingRadius.Initialize(new TextObject("{=kqJipMqc}Track detection radius +{a0}%"), DefaultSkills.Scouting, PartyRole.Scout, 0.1f, EffectIncrementType.Add);
		_effectTrackingSpottingDistance.Initialize(new TextObject("{=lbrOAvKj}Spotting distance +{a0}%"), DefaultSkills.Scouting, PartyRole.Scout, 0.06f, EffectIncrementType.Add);
		_effectTrackingTrackInformation.Initialize(new TextObject("{=uNls3bOP}Track information level: {a0}"), DefaultSkills.Scouting, PartyRole.Scout, 0.04f, EffectIncrementType.Add);
		_effectRogueryLootBonus.Initialize(new TextObject("{=bN3bLDb2}Battle Loot +{a0}%"), DefaultSkills.Roguery, PartyRole.PartyLeader, 0.0025f, EffectIncrementType.AddFactor);
		_effectCharmRelationBonus.Initialize(new TextObject("{=c5dsio8Q}Relation increase with NPCs +{a0}%"), DefaultSkills.Charm, PartyRole.Personal, 0.005f, EffectIncrementType.AddFactor);
		_effectTradePenaltyReduction.Initialize(new TextObject("{=uq7JwT1Z}Trade penalty Reduction +{a0}%"), DefaultSkills.Trade, PartyRole.PartyLeader, 0.002f, EffectIncrementType.AddFactor);
		_effectLeadershipMoraleBonus.Initialize(new TextObject("{=n3bFiuVu}Increase morale of the parties under your command +{a0}"), DefaultSkills.Leadership, PartyRole.Personal, 0.1f, EffectIncrementType.Add);
		_effectLeadershipGarrisonSizeBonus.Initialize(new TextObject("{=cSt26auo}Increase garrison size by +{a0}"), DefaultSkills.Leadership, PartyRole.Personal, 0.2f, EffectIncrementType.Add);
		_effectSurgeonSurvivalBonus.Initialize(new TextObject("{=w4BzNJYl}Casualty survival chance +{a0}%"), DefaultSkills.Medicine, PartyRole.Surgeon, 0.01f, EffectIncrementType.Add);
		_effectHealingRateBonusForHeroes.Initialize(new TextObject("{=fUvs4g40}Healing rate increase for heroes +{a0}%"), DefaultSkills.Medicine, PartyRole.Surgeon, 0.005f, EffectIncrementType.AddFactor);
		_effectHealingRateBonusForRegulars.Initialize(new TextObject("{=A310vHqJ}Healing rate increase for troops +{a0}%"), DefaultSkills.Medicine, PartyRole.Surgeon, 0.01f, EffectIncrementType.AddFactor);
		_effectGovernorHealingRateBonus.Initialize(new TextObject("{=6mQGst9s}Healing rate increase +{a0}%"), DefaultSkills.Medicine, PartyRole.Governor, 0.001f, EffectIncrementType.AddFactor);
		_effectSiegeEngineProductionBonus.Initialize(new TextObject("{=spbYlf0y}Faster siege engine production +{a0}%"), DefaultSkills.Engineering, PartyRole.Engineer, 0.001f, EffectIncrementType.AddFactor);
		_effectTownProjectBuildingBonus.Initialize(new TextObject("{=2paRqO8u}Faster building production +{a0}%"), DefaultSkills.Engineering, PartyRole.Governor, 0.0025f, EffectIncrementType.AddFactor);
		_effectStewardPartySizeBonus.Initialize(new TextObject("{=jNDUXetG}Increase party size by +{a0}"), DefaultSkills.Steward, PartyRole.Quartermaster, 0.25f, EffectIncrementType.Add);
		_effectSneakDamage.Initialize(new TextObject("{=vDieFIKM}Sneak attack damage +{a0}%"), DefaultSkills.Roguery, PartyRole.Personal, 0.002f, EffectIncrementType.AddFactor, 0.5f);
		_effectCrouchedSpeed.Initialize(new TextObject("{=sTgjLrPX}Crouched speed +{a0}%"), DefaultSkills.Roguery, PartyRole.Personal, 0.0005f, EffectIncrementType.AddFactor);
		_effectNoiseSuppression.Initialize(new TextObject("{=GzLd3ca9}Noise suppression -{a0}%"), DefaultSkills.Roguery, PartyRole.Personal, 0.0025f, EffectIncrementType.AddFactor);
	}
}
