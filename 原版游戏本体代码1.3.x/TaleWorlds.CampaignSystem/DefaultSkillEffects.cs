using System;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem
{
	// Token: 0x02000080 RID: 128
	public class DefaultSkillEffects
	{
		// Token: 0x1700041E RID: 1054
		// (get) Token: 0x060010A3 RID: 4259 RVA: 0x0004F589 File Offset: 0x0004D789
		private static DefaultSkillEffects Instance
		{
			get
			{
				return Campaign.Current.DefaultSkillEffects;
			}
		}

		// Token: 0x1700041F RID: 1055
		// (get) Token: 0x060010A4 RID: 4260 RVA: 0x0004F595 File Offset: 0x0004D795
		public static SkillEffect OneHandedSpeed
		{
			get
			{
				return DefaultSkillEffects.Instance._effectOneHandedSpeed;
			}
		}

		// Token: 0x17000420 RID: 1056
		// (get) Token: 0x060010A5 RID: 4261 RVA: 0x0004F5A1 File Offset: 0x0004D7A1
		public static SkillEffect OneHandedDamage
		{
			get
			{
				return DefaultSkillEffects.Instance._effectOneHandedDamage;
			}
		}

		// Token: 0x17000421 RID: 1057
		// (get) Token: 0x060010A6 RID: 4262 RVA: 0x0004F5AD File Offset: 0x0004D7AD
		public static SkillEffect TwoHandedSpeed
		{
			get
			{
				return DefaultSkillEffects.Instance._effectTwoHandedSpeed;
			}
		}

		// Token: 0x17000422 RID: 1058
		// (get) Token: 0x060010A7 RID: 4263 RVA: 0x0004F5B9 File Offset: 0x0004D7B9
		public static SkillEffect TwoHandedDamage
		{
			get
			{
				return DefaultSkillEffects.Instance._effectTwoHandedDamage;
			}
		}

		// Token: 0x17000423 RID: 1059
		// (get) Token: 0x060010A8 RID: 4264 RVA: 0x0004F5C5 File Offset: 0x0004D7C5
		public static SkillEffect PolearmSpeed
		{
			get
			{
				return DefaultSkillEffects.Instance._effectPolearmSpeed;
			}
		}

		// Token: 0x17000424 RID: 1060
		// (get) Token: 0x060010A9 RID: 4265 RVA: 0x0004F5D1 File Offset: 0x0004D7D1
		public static SkillEffect PolearmDamage
		{
			get
			{
				return DefaultSkillEffects.Instance._effectPolearmDamage;
			}
		}

		// Token: 0x17000425 RID: 1061
		// (get) Token: 0x060010AA RID: 4266 RVA: 0x0004F5DD File Offset: 0x0004D7DD
		public static SkillEffect BowDamage
		{
			get
			{
				return DefaultSkillEffects.Instance._effectBowDamage;
			}
		}

		// Token: 0x17000426 RID: 1062
		// (get) Token: 0x060010AB RID: 4267 RVA: 0x0004F5E9 File Offset: 0x0004D7E9
		public static SkillEffect BowAccuracy
		{
			get
			{
				return DefaultSkillEffects.Instance._effectBowAccuracy;
			}
		}

		// Token: 0x17000427 RID: 1063
		// (get) Token: 0x060010AC RID: 4268 RVA: 0x0004F5F5 File Offset: 0x0004D7F5
		public static SkillEffect ThrowingSpeed
		{
			get
			{
				return DefaultSkillEffects.Instance._effectThrowingSpeed;
			}
		}

		// Token: 0x17000428 RID: 1064
		// (get) Token: 0x060010AD RID: 4269 RVA: 0x0004F601 File Offset: 0x0004D801
		public static SkillEffect ThrowingDamage
		{
			get
			{
				return DefaultSkillEffects.Instance._effectThrowingDamage;
			}
		}

		// Token: 0x17000429 RID: 1065
		// (get) Token: 0x060010AE RID: 4270 RVA: 0x0004F60D File Offset: 0x0004D80D
		public static SkillEffect ThrowingAccuracy
		{
			get
			{
				return DefaultSkillEffects.Instance._effectThrowingAccuracy;
			}
		}

		// Token: 0x1700042A RID: 1066
		// (get) Token: 0x060010AF RID: 4271 RVA: 0x0004F619 File Offset: 0x0004D819
		public static SkillEffect CrossbowReloadSpeed
		{
			get
			{
				return DefaultSkillEffects.Instance._effectCrossbowReloadSpeed;
			}
		}

		// Token: 0x1700042B RID: 1067
		// (get) Token: 0x060010B0 RID: 4272 RVA: 0x0004F625 File Offset: 0x0004D825
		public static SkillEffect CrossbowAccuracy
		{
			get
			{
				return DefaultSkillEffects.Instance._effectCrossbowAccuracy;
			}
		}

		// Token: 0x1700042C RID: 1068
		// (get) Token: 0x060010B1 RID: 4273 RVA: 0x0004F631 File Offset: 0x0004D831
		public static SkillEffect HorseSpeed
		{
			get
			{
				return DefaultSkillEffects.Instance._effectHorseSpeed;
			}
		}

		// Token: 0x1700042D RID: 1069
		// (get) Token: 0x060010B2 RID: 4274 RVA: 0x0004F63D File Offset: 0x0004D83D
		public static SkillEffect HorseManeuver
		{
			get
			{
				return DefaultSkillEffects.Instance._effectHorseManeuver;
			}
		}

		// Token: 0x1700042E RID: 1070
		// (get) Token: 0x060010B3 RID: 4275 RVA: 0x0004F649 File Offset: 0x0004D849
		public static SkillEffect MountedWeaponDamagePenalty
		{
			get
			{
				return DefaultSkillEffects.Instance._effectMountedWeaponDamagePenalty;
			}
		}

		// Token: 0x1700042F RID: 1071
		// (get) Token: 0x060010B4 RID: 4276 RVA: 0x0004F655 File Offset: 0x0004D855
		public static SkillEffect MountedWeaponSpeedPenalty
		{
			get
			{
				return DefaultSkillEffects.Instance._effectMountedWeaponSpeedPenalty;
			}
		}

		// Token: 0x17000430 RID: 1072
		// (get) Token: 0x060010B5 RID: 4277 RVA: 0x0004F661 File Offset: 0x0004D861
		public static SkillEffect DismountResistance
		{
			get
			{
				return DefaultSkillEffects.Instance._effectDismountResistance;
			}
		}

		// Token: 0x17000431 RID: 1073
		// (get) Token: 0x060010B6 RID: 4278 RVA: 0x0004F66D File Offset: 0x0004D86D
		public static SkillEffect AthleticsSpeedFactor
		{
			get
			{
				return DefaultSkillEffects.Instance._effectAthleticsSpeedFactor;
			}
		}

		// Token: 0x17000432 RID: 1074
		// (get) Token: 0x060010B7 RID: 4279 RVA: 0x0004F679 File Offset: 0x0004D879
		public static SkillEffect AthleticsWeightFactor
		{
			get
			{
				return DefaultSkillEffects.Instance._effectAthleticsWeightFactor;
			}
		}

		// Token: 0x17000433 RID: 1075
		// (get) Token: 0x060010B8 RID: 4280 RVA: 0x0004F685 File Offset: 0x0004D885
		public static SkillEffect KnockBackResistance
		{
			get
			{
				return DefaultSkillEffects.Instance._effectKnockBackResistance;
			}
		}

		// Token: 0x17000434 RID: 1076
		// (get) Token: 0x060010B9 RID: 4281 RVA: 0x0004F691 File Offset: 0x0004D891
		public static SkillEffect KnockDownResistance
		{
			get
			{
				return DefaultSkillEffects.Instance._effectKnockDownResistance;
			}
		}

		// Token: 0x17000435 RID: 1077
		// (get) Token: 0x060010BA RID: 4282 RVA: 0x0004F69D File Offset: 0x0004D89D
		public static SkillEffect SmithingLevel
		{
			get
			{
				return DefaultSkillEffects.Instance._effectSmithingLevel;
			}
		}

		// Token: 0x17000436 RID: 1078
		// (get) Token: 0x060010BB RID: 4283 RVA: 0x0004F6A9 File Offset: 0x0004D8A9
		public static SkillEffect TacticsAdvantage
		{
			get
			{
				return DefaultSkillEffects.Instance._effectTacticsAdvantage;
			}
		}

		// Token: 0x17000437 RID: 1079
		// (get) Token: 0x060010BC RID: 4284 RVA: 0x0004F6B5 File Offset: 0x0004D8B5
		public static SkillEffect TacticsTroopSacrificeReduction
		{
			get
			{
				return DefaultSkillEffects.Instance._effectTacticsTroopSacrificeReduction;
			}
		}

		// Token: 0x17000438 RID: 1080
		// (get) Token: 0x060010BD RID: 4285 RVA: 0x0004F6C1 File Offset: 0x0004D8C1
		public static SkillEffect TrackingRadius
		{
			get
			{
				return DefaultSkillEffects.Instance._effectTrackingRadius;
			}
		}

		// Token: 0x17000439 RID: 1081
		// (get) Token: 0x060010BE RID: 4286 RVA: 0x0004F6CD File Offset: 0x0004D8CD
		public static SkillEffect TrackingSpottingDistance
		{
			get
			{
				return DefaultSkillEffects.Instance._effectTrackingSpottingDistance;
			}
		}

		// Token: 0x1700043A RID: 1082
		// (get) Token: 0x060010BF RID: 4287 RVA: 0x0004F6D9 File Offset: 0x0004D8D9
		public static SkillEffect TrackingTrackInformation
		{
			get
			{
				return DefaultSkillEffects.Instance._effectTrackingTrackInformation;
			}
		}

		// Token: 0x1700043B RID: 1083
		// (get) Token: 0x060010C0 RID: 4288 RVA: 0x0004F6E5 File Offset: 0x0004D8E5
		public static SkillEffect RogueryLootBonus
		{
			get
			{
				return DefaultSkillEffects.Instance._effectRogueryLootBonus;
			}
		}

		// Token: 0x1700043C RID: 1084
		// (get) Token: 0x060010C1 RID: 4289 RVA: 0x0004F6F1 File Offset: 0x0004D8F1
		public static SkillEffect CharmRelationBonus
		{
			get
			{
				return DefaultSkillEffects.Instance._effectCharmRelationBonus;
			}
		}

		// Token: 0x1700043D RID: 1085
		// (get) Token: 0x060010C2 RID: 4290 RVA: 0x0004F6FD File Offset: 0x0004D8FD
		public static SkillEffect TradePenaltyReduction
		{
			get
			{
				return DefaultSkillEffects.Instance._effectTradePenaltyReduction;
			}
		}

		// Token: 0x1700043E RID: 1086
		// (get) Token: 0x060010C3 RID: 4291 RVA: 0x0004F709 File Offset: 0x0004D909
		public static SkillEffect SurgeonSurvivalBonus
		{
			get
			{
				return DefaultSkillEffects.Instance._effectSurgeonSurvivalBonus;
			}
		}

		// Token: 0x1700043F RID: 1087
		// (get) Token: 0x060010C4 RID: 4292 RVA: 0x0004F715 File Offset: 0x0004D915
		public static SkillEffect SiegeEngineProductionBonus
		{
			get
			{
				return DefaultSkillEffects.Instance._effectSiegeEngineProductionBonus;
			}
		}

		// Token: 0x17000440 RID: 1088
		// (get) Token: 0x060010C5 RID: 4293 RVA: 0x0004F721 File Offset: 0x0004D921
		public static SkillEffect TownProjectBuildingBonus
		{
			get
			{
				return DefaultSkillEffects.Instance._effectTownProjectBuildingBonus;
			}
		}

		// Token: 0x17000441 RID: 1089
		// (get) Token: 0x060010C6 RID: 4294 RVA: 0x0004F72D File Offset: 0x0004D92D
		public static SkillEffect HealingRateBonusForHeroes
		{
			get
			{
				return DefaultSkillEffects.Instance._effectHealingRateBonusForHeroes;
			}
		}

		// Token: 0x17000442 RID: 1090
		// (get) Token: 0x060010C7 RID: 4295 RVA: 0x0004F739 File Offset: 0x0004D939
		public static SkillEffect HealingRateBonusForRegulars
		{
			get
			{
				return DefaultSkillEffects.Instance._effectHealingRateBonusForRegulars;
			}
		}

		// Token: 0x17000443 RID: 1091
		// (get) Token: 0x060010C8 RID: 4296 RVA: 0x0004F745 File Offset: 0x0004D945
		public static SkillEffect GovernorHealingRateBonus
		{
			get
			{
				return DefaultSkillEffects.Instance._effectGovernorHealingRateBonus;
			}
		}

		// Token: 0x17000444 RID: 1092
		// (get) Token: 0x060010C9 RID: 4297 RVA: 0x0004F751 File Offset: 0x0004D951
		public static SkillEffect LeadershipMoraleBonus
		{
			get
			{
				return DefaultSkillEffects.Instance._effectLeadershipMoraleBonus;
			}
		}

		// Token: 0x17000445 RID: 1093
		// (get) Token: 0x060010CA RID: 4298 RVA: 0x0004F75D File Offset: 0x0004D95D
		public static SkillEffect LeadershipGarrisonSizeBonus
		{
			get
			{
				return DefaultSkillEffects.Instance._effectLeadershipGarrisonSizeBonus;
			}
		}

		// Token: 0x17000446 RID: 1094
		// (get) Token: 0x060010CB RID: 4299 RVA: 0x0004F769 File Offset: 0x0004D969
		public static SkillEffect StewardPartySizeBonus
		{
			get
			{
				return DefaultSkillEffects.Instance._effectStewardPartySizeBonus;
			}
		}

		// Token: 0x17000447 RID: 1095
		// (get) Token: 0x060010CC RID: 4300 RVA: 0x0004F775 File Offset: 0x0004D975
		public static SkillEffect SneakDamage
		{
			get
			{
				return DefaultSkillEffects.Instance._effectSneakDamage;
			}
		}

		// Token: 0x17000448 RID: 1096
		// (get) Token: 0x060010CD RID: 4301 RVA: 0x0004F781 File Offset: 0x0004D981
		public static SkillEffect CrouchedSpeed
		{
			get
			{
				return DefaultSkillEffects.Instance._effectCrouchedSpeed;
			}
		}

		// Token: 0x17000449 RID: 1097
		// (get) Token: 0x060010CE RID: 4302 RVA: 0x0004F78D File Offset: 0x0004D98D
		public static SkillEffect NoiseSuppression
		{
			get
			{
				return DefaultSkillEffects.Instance._effectNoiseSuppression;
			}
		}

		// Token: 0x060010CF RID: 4303 RVA: 0x0004F799 File Offset: 0x0004D999
		public DefaultSkillEffects()
		{
			this.RegisterAll();
		}

		// Token: 0x060010D0 RID: 4304 RVA: 0x0004F7A8 File Offset: 0x0004D9A8
		private void RegisterAll()
		{
			this._effectOneHandedSpeed = this.Create("OneHandedSpeed");
			this._effectOneHandedDamage = this.Create("OneHandedDamage");
			this._effectTwoHandedSpeed = this.Create("TwoHandedSpeed");
			this._effectTwoHandedDamage = this.Create("TwoHandedDamage");
			this._effectPolearmSpeed = this.Create("PolearmSpeed");
			this._effectPolearmDamage = this.Create("PolearmDamage");
			this._effectBowDamage = this.Create("BowDamage");
			this._effectBowAccuracy = this.Create("BowAccuracy");
			this._effectThrowingSpeed = this.Create("ThrowingSpeed");
			this._effectThrowingDamage = this.Create("ThrowingDamage");
			this._effectThrowingAccuracy = this.Create("ThrowingAccuracy");
			this._effectCrossbowReloadSpeed = this.Create("CrossbowReloadSpeed");
			this._effectCrossbowAccuracy = this.Create("CrossbowAccuracy");
			this._effectHorseSpeed = this.Create("HorseSpeed");
			this._effectHorseManeuver = this.Create("HorseManeuver");
			this._effectMountedWeaponDamagePenalty = this.Create("MountedWeaponDamagePenalty");
			this._effectMountedWeaponSpeedPenalty = this.Create("MountedWeaponSpeedPenalty");
			this._effectDismountResistance = this.Create("DismountResistance");
			this._effectAthleticsSpeedFactor = this.Create("AthleticsSpeedFactor");
			this._effectAthleticsWeightFactor = this.Create("AthleticsWeightFactor");
			this._effectKnockBackResistance = this.Create("KnockBackResistance");
			this._effectKnockDownResistance = this.Create("KnockDownResistance");
			this._effectSmithingLevel = this.Create("SmithingLevel");
			this._effectTacticsAdvantage = this.Create("TacticsAdvantage");
			this._effectTacticsTroopSacrificeReduction = this.Create("TacticsTroopSacrificeReduction");
			this._effectTrackingRadius = this.Create("TrackingRadius");
			this._effectTrackingSpottingDistance = this.Create("TrackingSpottingDistance");
			this._effectTrackingTrackInformation = this.Create("TrackingTrackInformation");
			this._effectRogueryLootBonus = this.Create("RogueryLootBonus");
			this._effectCharmRelationBonus = this.Create("CharmRelationBonus");
			this._effectTradePenaltyReduction = this.Create("TradePenaltyReduction");
			this._effectLeadershipMoraleBonus = this.Create("LeadershipMoraleBonus");
			this._effectLeadershipGarrisonSizeBonus = this.Create("LeadershipGarrisonSizeBonus");
			this._effectSurgeonSurvivalBonus = this.Create("SurgeonSurvivalBonus");
			this._effectHealingRateBonusForHeroes = this.Create("HealingRateBonusForHeroes");
			this._effectHealingRateBonusForRegulars = this.Create("HealingRateBonusForRegulars");
			this._effectGovernorHealingRateBonus = this.Create("GovernorHealingRateBonus");
			this._effectSiegeEngineProductionBonus = this.Create("SiegeEngineProductionBonus");
			this._effectTownProjectBuildingBonus = this.Create("TownProjectBuildingBonus");
			this._effectStewardPartySizeBonus = this.Create("StewardPartySizeBonus");
			this._effectSneakDamage = this.Create("SneakDamage");
			this._effectCrouchedSpeed = this.Create("CrouchedSpeed");
			this._effectNoiseSuppression = this.Create("NoiseSuppression");
			this.InitializeAll();
		}

		// Token: 0x060010D1 RID: 4305 RVA: 0x0004FA96 File Offset: 0x0004DC96
		private SkillEffect Create(string stringId)
		{
			return Game.Current.ObjectManager.RegisterPresumedObject<SkillEffect>(new SkillEffect(stringId));
		}

		// Token: 0x060010D2 RID: 4306 RVA: 0x0004FAB0 File Offset: 0x0004DCB0
		private void InitializeAll()
		{
			this._effectOneHandedSpeed.Initialize(new TextObject("{=hjxRvb9l}One handed weapon speed: +{a0}%", null), DefaultSkills.OneHanded, PartyRole.Personal, 0.0007f, EffectIncrementType.AddFactor, 0f, float.MinValue, float.MaxValue);
			this._effectOneHandedDamage.Initialize(new TextObject("{=baUFKAbd}One handed weapon damage: +{a0}%", null), DefaultSkills.OneHanded, PartyRole.Personal, 0.0015f, EffectIncrementType.AddFactor, 0f, float.MinValue, float.MaxValue);
			this._effectTwoHandedSpeed.Initialize(new TextObject("{=Np94rYMz}Two handed weapon speed: +{a0}%", null), DefaultSkills.TwoHanded, PartyRole.Personal, 0.0006f, EffectIncrementType.AddFactor, 0f, float.MinValue, float.MaxValue);
			this._effectTwoHandedDamage.Initialize(new TextObject("{=QkbbLb4v}Two handed weapon damage: +{a0}%", null), DefaultSkills.TwoHanded, PartyRole.Personal, 0.0016f, EffectIncrementType.AddFactor, 0f, float.MinValue, float.MaxValue);
			this._effectPolearmSpeed.Initialize(new TextObject("{=2ATI9qVM}Polearm weapon speed: +{a0}%", null), DefaultSkills.Polearm, PartyRole.Personal, 0.0006f, EffectIncrementType.AddFactor, 0f, float.MinValue, float.MaxValue);
			this._effectPolearmDamage.Initialize(new TextObject("{=17cIGVQE}Polearm weapon damage: +{a0}%", null), DefaultSkills.Polearm, PartyRole.Personal, 0.0007f, EffectIncrementType.AddFactor, 0f, float.MinValue, float.MaxValue);
			this._effectBowDamage.Initialize(new TextObject("{=RUZHJMQO}Bow Damage: +{a0}%", null), DefaultSkills.Bow, PartyRole.Personal, 0.0011f, EffectIncrementType.AddFactor, 0f, float.MinValue, float.MaxValue);
			this._effectBowAccuracy.Initialize(new TextObject("{=sQCS90Wq}Bow Accuracy: +{a0}%", null), DefaultSkills.Bow, PartyRole.Personal, -0.0009f, EffectIncrementType.AddFactor, 0f, float.MinValue, float.MaxValue);
			this._effectThrowingSpeed.Initialize(new TextObject("{=Z0CoeojG}Thrown weapon speed: +{a0}%", null), DefaultSkills.Throwing, PartyRole.Personal, 0.0007f, EffectIncrementType.AddFactor, 0f, float.MinValue, float.MaxValue);
			this._effectThrowingDamage.Initialize(new TextObject("{=TQMGppEk}Thrown weapon damage: +{a0}%", null), DefaultSkills.Throwing, PartyRole.Personal, 0.0006f, EffectIncrementType.AddFactor, 0f, float.MinValue, float.MaxValue);
			this._effectThrowingAccuracy.Initialize(new TextObject("{=SfKrjKuO}Thrown weapon accuracy: +{a0}%", null), DefaultSkills.Throwing, PartyRole.Personal, -0.0006f, EffectIncrementType.AddFactor, 0f, float.MinValue, float.MaxValue);
			this._effectCrossbowReloadSpeed.Initialize(new TextObject("{=W0Zu4iDz}Crossbow reload speed: +{a0}%", null), DefaultSkills.Crossbow, PartyRole.Personal, 0.0007f, EffectIncrementType.AddFactor, 0f, float.MinValue, float.MaxValue);
			this._effectCrossbowAccuracy.Initialize(new TextObject("{=JwWnpD40}Crossbow accuracy: +{a0}%", null), DefaultSkills.Crossbow, PartyRole.Personal, -0.0005f, EffectIncrementType.AddFactor, 0f, float.MinValue, float.MaxValue);
			this._effectHorseSpeed.Initialize(new TextObject("{=Y07OcP1T}Horse speed: +{a0}", null), DefaultSkills.Riding, PartyRole.Personal, 0.002f, EffectIncrementType.AddFactor, 0f, float.MinValue, float.MaxValue);
			this._effectHorseManeuver.Initialize(new TextObject("{=AahNTeXY}Horse maneuver: +{a0}", null), DefaultSkills.Riding, PartyRole.Personal, 0.0004f, EffectIncrementType.AddFactor, 0f, float.MinValue, float.MaxValue);
			this._effectMountedWeaponDamagePenalty.Initialize(new TextObject("{=0dbwEczK}Mounted weapon damage penalty: {a0}%", null), DefaultSkills.Riding, PartyRole.Personal, 0.002f, EffectIncrementType.AddFactor, -0.2f, float.MinValue, 0f);
			this._effectMountedWeaponSpeedPenalty.Initialize(new TextObject("{=oE5etyy0}Mounted weapon speed & reload penalty: {a0}%", null), DefaultSkills.Riding, PartyRole.Personal, 0.003f, EffectIncrementType.AddFactor, -0.3f, float.MinValue, 0f);
			this._effectDismountResistance.Initialize(new TextObject("{=kbHJVxAo}Dismount resistance: {a0}% of max. hitpoints", null), DefaultSkills.Riding, PartyRole.Personal, 0.001f, EffectIncrementType.AddFactor, 0.4f, float.MinValue, float.MaxValue);
			this._effectAthleticsSpeedFactor.Initialize(new TextObject("{=rgb6vdon}Running speed increased by {a0}%", null), DefaultSkills.Athletics, PartyRole.Personal, 0.001f, EffectIncrementType.AddFactor, 0f, float.MinValue, float.MaxValue);
			this._effectAthleticsWeightFactor.Initialize(new TextObject("{=WaUuhxwv}Weight penalty reduced by: {a0}%", null), DefaultSkills.Athletics, PartyRole.Personal, -0.001f, EffectIncrementType.AddFactor, 0f, float.MinValue, float.MaxValue);
			this._effectKnockBackResistance.Initialize(new TextObject("{=TyjDHQUv}Knock back resistance: {a0}% of max. hitpoints", null), DefaultSkills.Athletics, PartyRole.Personal, 0.001f, EffectIncrementType.AddFactor, 0.15f, float.MinValue, float.MaxValue);
			this._effectKnockDownResistance.Initialize(new TextObject("{=tlNZIH3l}Knock down resistance: {a0}% of max. hitpoints", null), DefaultSkills.Athletics, PartyRole.Personal, 0.001f, EffectIncrementType.AddFactor, 0.4f, float.MinValue, float.MaxValue);
			this._effectSmithingLevel.Initialize(new TextObject("{=ImN8Cfk6}Max difficulty of weapon that can be smithed without penalty: {a0}", null), DefaultSkills.Crafting, PartyRole.Personal, 1f, EffectIncrementType.Add, 0f, float.MinValue, float.MaxValue);
			this._effectTacticsAdvantage.Initialize(new TextObject("{=XO3SOlZx}Simulation advantage: +{a0}%", null), DefaultSkills.Tactics, PartyRole.Personal, 0.001f, EffectIncrementType.AddFactor, 0f, float.MinValue, float.MaxValue);
			this._effectTacticsTroopSacrificeReduction.Initialize(new TextObject("{=VHdyQYKI}Decrease the sacrificed troop number when trying to get away +{a0}%", null), DefaultSkills.Tactics, PartyRole.Personal, -0.001f, EffectIncrementType.AddFactor, 0f, float.MinValue, float.MaxValue);
			this._effectTrackingRadius.Initialize(new TextObject("{=kqJipMqc}Track detection radius +{a0}%", null), DefaultSkills.Scouting, PartyRole.Scout, 0.1f, EffectIncrementType.Add, 0f, float.MinValue, float.MaxValue);
			this._effectTrackingSpottingDistance.Initialize(new TextObject("{=lbrOAvKj}Spotting distance +{a0}%", null), DefaultSkills.Scouting, PartyRole.Scout, 0.06f, EffectIncrementType.Add, 0f, float.MinValue, float.MaxValue);
			this._effectTrackingTrackInformation.Initialize(new TextObject("{=uNls3bOP}Track information level: {a0}", null), DefaultSkills.Scouting, PartyRole.Scout, 0.04f, EffectIncrementType.Add, 0f, float.MinValue, float.MaxValue);
			this._effectRogueryLootBonus.Initialize(new TextObject("{=bN3bLDb2}Battle Loot +{a0}%", null), DefaultSkills.Roguery, PartyRole.PartyLeader, 0.0025f, EffectIncrementType.AddFactor, 0f, float.MinValue, float.MaxValue);
			this._effectCharmRelationBonus.Initialize(new TextObject("{=c5dsio8Q}Relation increase with NPCs +{a0}%", null), DefaultSkills.Charm, PartyRole.Personal, 0.005f, EffectIncrementType.AddFactor, 0f, float.MinValue, float.MaxValue);
			this._effectTradePenaltyReduction.Initialize(new TextObject("{=uq7JwT1Z}Trade penalty Reduction +{a0}%", null), DefaultSkills.Trade, PartyRole.PartyLeader, 0.002f, EffectIncrementType.AddFactor, 0f, float.MinValue, float.MaxValue);
			this._effectLeadershipMoraleBonus.Initialize(new TextObject("{=n3bFiuVu}Increase morale of the parties under your command +{a0}", null), DefaultSkills.Leadership, PartyRole.Personal, 0.1f, EffectIncrementType.Add, 0f, float.MinValue, float.MaxValue);
			this._effectLeadershipGarrisonSizeBonus.Initialize(new TextObject("{=cSt26auo}Increase garrison size by +{a0}", null), DefaultSkills.Leadership, PartyRole.Personal, 0.2f, EffectIncrementType.Add, 0f, float.MinValue, float.MaxValue);
			this._effectSurgeonSurvivalBonus.Initialize(new TextObject("{=w4BzNJYl}Casualty survival chance +{a0}%", null), DefaultSkills.Medicine, PartyRole.Surgeon, 0.0025f, EffectIncrementType.Add, 0f, float.MinValue, float.MaxValue);
			this._effectHealingRateBonusForHeroes.Initialize(new TextObject("{=fUvs4g40}Healing rate increase for heroes +{a0}%", null), DefaultSkills.Medicine, PartyRole.Surgeon, 0.005f, EffectIncrementType.AddFactor, 0f, float.MinValue, float.MaxValue);
			this._effectHealingRateBonusForRegulars.Initialize(new TextObject("{=A310vHqJ}Healing rate increase for troops +{a0}%", null), DefaultSkills.Medicine, PartyRole.Surgeon, 0.01f, EffectIncrementType.AddFactor, 0f, float.MinValue, float.MaxValue);
			this._effectGovernorHealingRateBonus.Initialize(new TextObject("{=6mQGst9s}Healing rate increase +{a0}%", null), DefaultSkills.Medicine, PartyRole.Governor, 0.001f, EffectIncrementType.AddFactor, 0f, float.MinValue, float.MaxValue);
			this._effectSiegeEngineProductionBonus.Initialize(new TextObject("{=spbYlf0y}Faster siege engine production +{a0}%", null), DefaultSkills.Engineering, PartyRole.Engineer, 0.001f, EffectIncrementType.AddFactor, 0f, float.MinValue, float.MaxValue);
			this._effectTownProjectBuildingBonus.Initialize(new TextObject("{=2paRqO8u}Faster building production +{a0}%", null), DefaultSkills.Engineering, PartyRole.Governor, 0.0025f, EffectIncrementType.AddFactor, 0f, float.MinValue, float.MaxValue);
			this._effectStewardPartySizeBonus.Initialize(new TextObject("{=jNDUXetG}Increase party size by +{a0}", null), DefaultSkills.Steward, PartyRole.Quartermaster, 0.25f, EffectIncrementType.Add, 0f, float.MinValue, float.MaxValue);
			this._effectSneakDamage.Initialize(new TextObject("{=vDieFIKM}Sneak attack damage +{a0}%", null), DefaultSkills.Roguery, PartyRole.Personal, 0.002f, EffectIncrementType.AddFactor, 0.5f, float.MinValue, float.MaxValue);
			this._effectCrouchedSpeed.Initialize(new TextObject("{=sTgjLrPX}Crouched speed +{a0}%", null), DefaultSkills.Roguery, PartyRole.Personal, 0.0005f, EffectIncrementType.AddFactor, 0f, float.MinValue, float.MaxValue);
			this._effectNoiseSuppression.Initialize(new TextObject("{=GzLd3ca9}Noise suppression -{a0}%", null), DefaultSkills.Roguery, PartyRole.Personal, 0.0025f, EffectIncrementType.AddFactor, 0f, float.MinValue, float.MaxValue);
		}

		// Token: 0x0400050B RID: 1291
		private SkillEffect _effectOneHandedSpeed;

		// Token: 0x0400050C RID: 1292
		private SkillEffect _effectOneHandedDamage;

		// Token: 0x0400050D RID: 1293
		private SkillEffect _effectTwoHandedSpeed;

		// Token: 0x0400050E RID: 1294
		private SkillEffect _effectTwoHandedDamage;

		// Token: 0x0400050F RID: 1295
		private SkillEffect _effectPolearmSpeed;

		// Token: 0x04000510 RID: 1296
		private SkillEffect _effectPolearmDamage;

		// Token: 0x04000511 RID: 1297
		private SkillEffect _effectBowDamage;

		// Token: 0x04000512 RID: 1298
		private SkillEffect _effectBowAccuracy;

		// Token: 0x04000513 RID: 1299
		private SkillEffect _effectThrowingSpeed;

		// Token: 0x04000514 RID: 1300
		private SkillEffect _effectThrowingDamage;

		// Token: 0x04000515 RID: 1301
		private SkillEffect _effectThrowingAccuracy;

		// Token: 0x04000516 RID: 1302
		private SkillEffect _effectCrossbowReloadSpeed;

		// Token: 0x04000517 RID: 1303
		private SkillEffect _effectCrossbowAccuracy;

		// Token: 0x04000518 RID: 1304
		private SkillEffect _effectHorseSpeed;

		// Token: 0x04000519 RID: 1305
		private SkillEffect _effectHorseManeuver;

		// Token: 0x0400051A RID: 1306
		private SkillEffect _effectMountedWeaponDamagePenalty;

		// Token: 0x0400051B RID: 1307
		private SkillEffect _effectMountedWeaponSpeedPenalty;

		// Token: 0x0400051C RID: 1308
		private SkillEffect _effectDismountResistance;

		// Token: 0x0400051D RID: 1309
		private SkillEffect _effectAthleticsSpeedFactor;

		// Token: 0x0400051E RID: 1310
		private SkillEffect _effectAthleticsWeightFactor;

		// Token: 0x0400051F RID: 1311
		private SkillEffect _effectKnockBackResistance;

		// Token: 0x04000520 RID: 1312
		private SkillEffect _effectKnockDownResistance;

		// Token: 0x04000521 RID: 1313
		private SkillEffect _effectSmithingLevel;

		// Token: 0x04000522 RID: 1314
		private SkillEffect _effectTacticsAdvantage;

		// Token: 0x04000523 RID: 1315
		private SkillEffect _effectTacticsTroopSacrificeReduction;

		// Token: 0x04000524 RID: 1316
		private SkillEffect _effectTrackingRadius;

		// Token: 0x04000525 RID: 1317
		private SkillEffect _effectTrackingSpottingDistance;

		// Token: 0x04000526 RID: 1318
		private SkillEffect _effectTrackingTrackInformation;

		// Token: 0x04000527 RID: 1319
		private SkillEffect _effectRogueryLootBonus;

		// Token: 0x04000528 RID: 1320
		private SkillEffect _effectCharmRelationBonus;

		// Token: 0x04000529 RID: 1321
		private SkillEffect _effectTradePenaltyReduction;

		// Token: 0x0400052A RID: 1322
		private SkillEffect _effectSurgeonSurvivalBonus;

		// Token: 0x0400052B RID: 1323
		private SkillEffect _effectSiegeEngineProductionBonus;

		// Token: 0x0400052C RID: 1324
		private SkillEffect _effectTownProjectBuildingBonus;

		// Token: 0x0400052D RID: 1325
		private SkillEffect _effectHealingRateBonusForHeroes;

		// Token: 0x0400052E RID: 1326
		private SkillEffect _effectHealingRateBonusForRegulars;

		// Token: 0x0400052F RID: 1327
		private SkillEffect _effectGovernorHealingRateBonus;

		// Token: 0x04000530 RID: 1328
		private SkillEffect _effectLeadershipMoraleBonus;

		// Token: 0x04000531 RID: 1329
		private SkillEffect _effectLeadershipGarrisonSizeBonus;

		// Token: 0x04000532 RID: 1330
		private SkillEffect _effectStewardPartySizeBonus;

		// Token: 0x04000533 RID: 1331
		private SkillEffect _effectSneakDamage;

		// Token: 0x04000534 RID: 1332
		private SkillEffect _effectCrouchedSpeed;

		// Token: 0x04000535 RID: 1333
		private SkillEffect _effectNoiseSuppression;
	}
}
