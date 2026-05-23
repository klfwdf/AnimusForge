using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces;

public abstract class CombatXpModel : MBGameModel<CombatXpModel>
{
	public enum MissionTypeEnum
	{
		Battle,
		PracticeFight,
		Tournament,
		SimulationBattle,
		NoXp
	}

	public abstract float CaptainRadius { get; }

	public abstract SkillObject GetSkillForWeapon(WeaponComponentData weapon, bool isSiegeEngineHit);

	public abstract ExplainedNumber GetXpFromHit(CharacterObject attackerTroop, CharacterObject captain, CharacterObject attackedTroop, PartyBase attackerParty, int damage, bool isFatal, MissionTypeEnum missionType);

	public abstract float GetXpMultiplierFromShotDifficulty(float shotDifficulty);
}
