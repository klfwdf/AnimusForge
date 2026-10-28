namespace TaleWorlds.Core;

public class DefaultBannerEffects
{
	private BannerEffect _increasedMeleeDamage;

	private BannerEffect _increasedMeleeDamageAgainstMountedTroops;

	private BannerEffect _increasedRangedDamage;

	private BannerEffect _increasedChargeDamage;

	private BannerEffect _decreasedChargeDamage;

	private BannerEffect _decreasedRangedAccuracyPenalty;

	private BannerEffect _decreasedMoraleShock;

	private BannerEffect _decreasedMeleeAttackDamage;

	private BannerEffect _decreasedRangedAttackDamage;

	private BannerEffect _decreasedShieldDamage;

	private BannerEffect _increasedTroopMovementSpeed;

	private BannerEffect _increasedMountMovementSpeed;

	private BannerEffect _increasedMoraleShockByMeleeTroops;

	private static DefaultBannerEffects Instance => Game.Current.DefaultBannerEffects;

	public static BannerEffect IncreasedMeleeDamage => Instance._increasedMeleeDamage;

	public static BannerEffect IncreasedMeleeDamageAgainstMountedTroops => Instance._increasedMeleeDamageAgainstMountedTroops;

	public static BannerEffect IncreasedRangedDamage => Instance._increasedRangedDamage;

	public static BannerEffect IncreasedChargeDamage => Instance._increasedChargeDamage;

	public static BannerEffect DecreasedChargeDamage => Instance._decreasedChargeDamage;

	public static BannerEffect DecreasedRangedAccuracyPenalty => Instance._decreasedRangedAccuracyPenalty;

	public static BannerEffect DecreasedMoraleShock => Instance._decreasedMoraleShock;

	public static BannerEffect DecreasedMeleeAttackDamage => Instance._decreasedMeleeAttackDamage;

	public static BannerEffect DecreasedRangedAttackDamage => Instance._decreasedRangedAttackDamage;

	public static BannerEffect DecreasedShieldDamage => Instance._decreasedShieldDamage;

	public static BannerEffect IncreasedTroopMovementSpeed => Instance._increasedTroopMovementSpeed;

	public static BannerEffect IncreasedMountMovementSpeed => Instance._increasedMountMovementSpeed;

	public static BannerEffect IncreasedMoraleShockByMeleeTroops => Instance._increasedMoraleShockByMeleeTroops;

	public DefaultBannerEffects()
	{
		RegisterAll();
	}

	private void RegisterAll()
	{
		_increasedMeleeDamage = Create("IncreasedMeleeDamage");
		_increasedMeleeDamageAgainstMountedTroops = Create("IncreasedMeleeDamageAgainstMountedTroops");
		_increasedRangedDamage = Create("IncreasedRangedDamage");
		_increasedChargeDamage = Create("IncreasedChargeDamage");
		_decreasedChargeDamage = Create("DecreasedChargeDamage");
		_decreasedRangedAccuracyPenalty = Create("DecreasedRangedAccuracyPenalty");
		_decreasedMoraleShock = Create("DecreasedMoraleShock");
		_decreasedMeleeAttackDamage = Create("DecreasedMeleeAttackDamage");
		_decreasedRangedAttackDamage = Create("DecreasedRangedAttackDamage");
		_decreasedShieldDamage = Create("DecreasedShieldDamage");
		_increasedTroopMovementSpeed = Create("IncreasedTroopMovementSpeed");
		_increasedMountMovementSpeed = Create("IncreasedMountMovementSpeed");
		_increasedMoraleShockByMeleeTroops = Create("IncreasedMoraleShockByMeleeTroops");
		InitializeAll();
	}

	private BannerEffect Create(string stringId)
	{
		return Game.Current.ObjectManager.RegisterPresumedObject(new BannerEffect(stringId));
	}

	private void InitializeAll()
	{
		_increasedMeleeDamage.Initialize("{=unaWKloT}Increased Melee Damage", "{=8ZNOgT8Z}{BONUS_AMOUNT}% melee damage to troops in your formation.", 0.05f, 0.1f, 0.15f, EffectIncrementType.AddFactor);
		_increasedMeleeDamageAgainstMountedTroops.Initialize("{=t0Qzb7CY}Increased Melee Damage Against Mounted Troops", "{=sxGmF0tC}{BONUS_AMOUNT}% melee damage by troops in your formation against cavalry.", 0.1f, 0.2f, 0.3f, EffectIncrementType.AddFactor);
		_increasedRangedDamage.Initialize("{=Ch5NpCd0}Increased Ranged Damage", "{=labbKop6}{BONUS_AMOUNT}% ranged damage to troops in your formation.", 0.04f, 0.06f, 0.08f, EffectIncrementType.AddFactor);
		_increasedChargeDamage.Initialize("{=O2oBC9sH}Increased Charge Damage", "{=Z2xgnrDa}{BONUS_AMOUNT}% charge damage to mounted troops in your formation.", 0.1f, 0.2f, 0.3f, EffectIncrementType.AddFactor);
		_decreasedChargeDamage.Initialize("{=PkFT0D9a}Decreased Charge Damage", "{=Z2xgnrDa}{BONUS_AMOUNT}% charge damage to mounted troops in your formation.", -0.1f, -0.2f, -0.3f, EffectIncrementType.AddFactor);
		_decreasedRangedAccuracyPenalty.Initialize("{=MkBPRCuF}Decreased Ranged Accuracy Penalty", "{=Gu0Wxxul}{BONUS_AMOUNT}% accuracy penalty for ranged troops in your formation.", -0.04f, -0.06f, -0.08f, EffectIncrementType.AddFactor);
		_decreasedMoraleShock.Initialize("{=nOMT0Cw6}Decreased Morale Shock", "{=W0agPHes}{BONUS_AMOUNT}% morale penalty from casualties to troops in your formation.", -0.1f, -0.2f, -0.3f, EffectIncrementType.AddFactor);
		_decreasedMeleeAttackDamage.Initialize("{=a3Vc59WV}Decreased Taken Melee Attack Damage", "{=ORFrCYSn}{BONUS_AMOUNT}% damage by melee attacks to troops in your formation.", -0.05f, -0.1f, -0.15f, EffectIncrementType.AddFactor);
		_decreasedRangedAttackDamage.Initialize("{=p0JFbL7G}Decreased Taken Ranged Attack Damage", "{=W0agPHes}{BONUS_AMOUNT}% morale penalty from casualties to troops in your formation.", -0.05f, -0.1f, -0.15f, EffectIncrementType.AddFactor);
		_decreasedShieldDamage.Initialize("{=T79exjaP}Decreased Taken Shield Damage", "{=klGEDUmw}{BONUS_AMOUNT}% damage to shields of troops in your formation.", -0.15f, -0.25f, -0.3f, EffectIncrementType.AddFactor);
		_increasedTroopMovementSpeed.Initialize("{=PbJAOKKZ}Increased Troop Movement Speed", "{=nqWulUTP}{BONUS_AMOUNT}% movement speed to infantry in your formation.", 0.15f, 0.25f, 0.3f, EffectIncrementType.AddFactor);
		_increasedMountMovementSpeed.Initialize("{=nMfxbc0Y}Increased Mount Movement Speed", "{=g0l7W5xQ}{BONUS_AMOUNT}% movement speed to mounts in your formation.", 0.05f, 0.08f, 0.1f, EffectIncrementType.AddFactor);
		_increasedMoraleShockByMeleeTroops.Initialize("{=nOMT0Cw6}Increased Morale Shock", "{=!}INCREASED MORALE SHOCK BY MELEE TROOPS DESCRIPTION", 0.1f, 0.2f, 0.3f, EffectIncrementType.AddFactor);
	}
}
