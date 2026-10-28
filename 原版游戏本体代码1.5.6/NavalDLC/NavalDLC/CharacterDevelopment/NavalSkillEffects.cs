using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace NavalDLC.CharacterDevelopment;

public class NavalSkillEffects
{
	private SkillEffect _effectWindBonus;

	private SkillEffect _effectNavalAutoBattleSimulationAdvantage;

	private SkillEffect _effectNavalAutoBattleCombatPenaltyNegation;

	private SkillEffect _effectNavalBattleCombatPenaltyNegation;

	private SkillEffect _effectNavalBattleUnderwaterBreathingDurationBonus;

	private SkillEffect _effectShipDamageReduction;

	private static NavalSkillEffects Instance => NavalDLCManager.Instance.NavalSkillEffects;

	public static SkillEffect WindBonus => Instance._effectWindBonus;

	public static SkillEffect NavalAutoBattleSimulationAdvantage => Instance._effectNavalAutoBattleSimulationAdvantage;

	public static SkillEffect NavalAutoBattleCombatPenaltyNegation => Instance._effectNavalAutoBattleCombatPenaltyNegation;

	public static SkillEffect NavalBattleCombatPenaltyNegation => Instance._effectNavalBattleCombatPenaltyNegation;

	public static SkillEffect NavalBattleUnderwaterBreathingDurationBonus => Instance._effectNavalBattleUnderwaterBreathingDurationBonus;

	public static SkillEffect ShipDamageReduction => Instance._effectShipDamageReduction;

	public NavalSkillEffects()
	{
		RegisterAll();
	}

	private void RegisterAll()
	{
		_effectWindBonus = Create("WindBonus");
		_effectNavalAutoBattleSimulationAdvantage = Create("NavalAutoBattleSimulationAdvantage");
		_effectNavalAutoBattleCombatPenaltyNegation = Create("NavalAutoBattleCombatPenaltyNegation");
		_effectNavalBattleCombatPenaltyNegation = Create("NavalBattleCombatPenaltyNegation");
		_effectNavalBattleUnderwaterBreathingDurationBonus = Create("NavalBattleUnderwaterBreathingDurationBonus");
		_effectShipDamageReduction = Create("ShipDamageReduction");
		InitializeAll();
	}

	private SkillEffect Create(string stringId)
	{
		return Game.Current.ObjectManager.RegisterPresumedObject(new SkillEffect(stringId));
	}

	private void InitializeAll()
	{
		_effectWindBonus.Initialize(new TextObject("{=LxA3WTjm}Sailing speed increased by {a0}%"), NavalSkills.Shipmaster, PartyRole.Navigator, 0.0005f, EffectIncrementType.AddFactor);
		_effectNavalAutoBattleSimulationAdvantage.Initialize(new TextObject("{=Z2uaBxah}Naval simulation advantage: +{a0}%"), NavalSkills.Mariner, PartyRole.PartyLeader, 0.001f, EffectIncrementType.AddFactor);
		_effectNavalAutoBattleCombatPenaltyNegation.Initialize(new TextObject("{=7XMyYI9e}Naval Auto Battle Combat Penalty Negation Effect"), NavalSkills.Mariner, PartyRole.PartyLeader, 0.5f, EffectIncrementType.AddFactor);
		_effectNavalBattleCombatPenaltyNegation.Initialize(new TextObject("{=k6EubLby}Naval Battle Combat Penalty Negation Effect"), NavalSkills.Mariner, PartyRole.Personal, -0.005f, EffectIncrementType.AddFactor, 0f, -1f);
		_effectNavalBattleUnderwaterBreathingDurationBonus.Initialize(new TextObject("{=95kCGbUp}Naval battle underwater breathing duration: +{a0} Seconds"), NavalSkills.Mariner, PartyRole.Personal, 0.005f, EffectIncrementType.AddFactor, 0f, 0f, 20f);
		_effectShipDamageReduction.Initialize(new TextObject("{=CyZvyfRa}Reduce ships' received damage by {a0}%"), NavalSkills.Boatswain, PartyRole.FirstMate, -0.0025f, EffectIncrementType.AddFactor);
	}
}
