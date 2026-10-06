using TaleWorlds.Core;

namespace TaleWorlds.MountAndBlade;

public class BasicBattleAgentOrigin : IAgentOriginBase
{
	private BasicCharacterObject _troop;

	private bool _hasThrownWeapon;

	private bool _hasHeavyArmor;

	private bool _hasShield;

	private bool _hasSpear;

	bool IAgentOriginBase.IsUnderPlayersCommand => false;

	bool IAgentOriginBase.IsInSameArmyAsPlayer => false;

	uint IAgentOriginBase.FactionColor => 0u;

	uint IAgentOriginBase.FactionColor2 => 0u;

	IBattleCombatant IAgentOriginBase.BattleCombatant => null;

	int IAgentOriginBase.UniqueSeed => 0;

	int IAgentOriginBase.Seed => 0;

	Banner IAgentOriginBase.Banner => null;

	BasicCharacterObject IAgentOriginBase.Troop => _troop;

	bool IAgentOriginBase.HasThrownWeapon => _hasThrownWeapon;

	bool IAgentOriginBase.HasHeavyArmor => _hasHeavyArmor;

	bool IAgentOriginBase.HasShield => _hasShield;

	bool IAgentOriginBase.HasSpear => _hasSpear;

	public BasicBattleAgentOrigin(BasicCharacterObject troop)
	{
		_troop = troop;
		AgentOriginUtilities.GetDefaultTroopTraits(_troop, out _hasThrownWeapon, out _hasSpear, out _hasShield, out _hasHeavyArmor);
	}

	void IAgentOriginBase.SetWounded()
	{
	}

	void IAgentOriginBase.SetKilled()
	{
	}

	void IAgentOriginBase.SetRouted(bool isOrderRetreat)
	{
	}

	void IAgentOriginBase.OnAgentRemoved(float agentHealth)
	{
	}

	void IAgentOriginBase.OnScoreHit(BasicCharacterObject victim, BasicCharacterObject captain, int damage, bool isFatal, bool isTeamKill, WeaponComponentData attackerWeapon)
	{
	}

	void IAgentOriginBase.SetBanner(Banner banner)
	{
	}

	TroopTraitsMask IAgentOriginBase.GetTraitsMask()
	{
		return AgentOriginUtilities.GetDefaultTraitsMask(this);
	}
}
