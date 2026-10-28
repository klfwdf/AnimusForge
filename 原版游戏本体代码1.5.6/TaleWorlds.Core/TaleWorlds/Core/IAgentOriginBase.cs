namespace TaleWorlds.Core;

public interface IAgentOriginBase
{
	bool IsUnderPlayersCommand { get; }

	bool IsInSameArmyAsPlayer { get; }

	uint FactionColor { get; }

	uint FactionColor2 { get; }

	IBattleCombatant BattleCombatant { get; }

	int UniqueSeed { get; }

	int Seed { get; }

	Banner Banner { get; }

	BasicCharacterObject Troop { get; }

	bool HasThrownWeapon { get; }

	bool HasHeavyArmor { get; }

	bool HasShield { get; }

	bool HasSpear { get; }

	void SetWounded();

	void SetKilled();

	void SetRouted(bool isOrderRetreat);

	void OnAgentRemoved(float agentHealth);

	void OnScoreHit(BasicCharacterObject victim, BasicCharacterObject formationCaptain, int damage, bool isFatal, bool isTeamKill, WeaponComponentData attackerWeapon);

	void SetBanner(Banner banner);

	TroopTraitsMask GetTraitsMask();
}
