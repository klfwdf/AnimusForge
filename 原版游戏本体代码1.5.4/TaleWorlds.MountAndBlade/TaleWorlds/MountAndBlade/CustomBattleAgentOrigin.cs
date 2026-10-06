using System;
using TaleWorlds.Core;

namespace TaleWorlds.MountAndBlade;

public class CustomBattleAgentOrigin : IAgentOriginBase
{
	private readonly UniqueTroopDescriptor _descriptor;

	private readonly bool _isPlayerSide;

	private CustomBattleTroopSupplier _troopSupplier;

	private bool _isRemoved;

	private bool _hasThrownWeapon;

	private bool _hasHeavyArmor;

	private bool _hasShield;

	private bool _hasSpear;

	public CustomBattleCombatant CustomBattleCombatant { get; private set; }

	IBattleCombatant IAgentOriginBase.BattleCombatant => CustomBattleCombatant;

	public BasicCharacterObject Troop { get; private set; }

	bool IAgentOriginBase.HasThrownWeapon => _hasThrownWeapon;

	bool IAgentOriginBase.HasHeavyArmor => _hasHeavyArmor;

	bool IAgentOriginBase.HasShield => _hasShield;

	bool IAgentOriginBase.HasSpear => _hasSpear;

	public int Rank { get; private set; }

	public Banner Banner => CustomBattleCombatant.Banner;

	public bool IsUnderPlayersCommand => _isPlayerSide;

	public bool IsInSameArmyAsPlayer => _isPlayerSide;

	public uint FactionColor => CustomBattleCombatant.BasicCulture.Color;

	public uint FactionColor2 => CustomBattleCombatant.BasicCulture.Color2;

	public int Seed => Troop.GetDefaultFaceSeed(Rank);

	public int UniqueSeed => _descriptor.UniqueSeed;

	public CustomBattleAgentOrigin(CustomBattleCombatant customBattleCombatant, BasicCharacterObject characterObject, CustomBattleTroopSupplier troopSupplier, bool isPlayerSide, int rank = -1, UniqueTroopDescriptor uniqueNo = default(UniqueTroopDescriptor))
	{
		CustomBattleCombatant = customBattleCombatant;
		Troop = characterObject;
		_descriptor = ((!uniqueNo.IsValid) ? new UniqueTroopDescriptor(Game.Current.NextUniqueTroopSeed) : uniqueNo);
		Rank = ((rank == -1) ? MBRandom.RandomInt(10000) : rank);
		_troopSupplier = troopSupplier;
		_isPlayerSide = isPlayerSide;
		AgentOriginUtilities.GetDefaultTroopTraits(Troop, out _hasThrownWeapon, out _hasSpear, out _hasShield, out _hasHeavyArmor);
	}

	public void SetWounded()
	{
		if (!_isRemoved)
		{
			_troopSupplier.OnTroopWounded();
			_isRemoved = true;
		}
	}

	public void SetKilled()
	{
		if (!_isRemoved)
		{
			_troopSupplier.OnTroopKilled();
			_isRemoved = true;
		}
	}

	public void SetRouted(bool isOrderRetreat)
	{
		if (!_isRemoved)
		{
			_troopSupplier.OnTroopRouted();
			_isRemoved = true;
		}
	}

	public void OnAgentRemoved(float agentHealth)
	{
	}

	void IAgentOriginBase.OnScoreHit(BasicCharacterObject victim, BasicCharacterObject captain, int damage, bool isFatal, bool isTeamKill, WeaponComponentData attackerWeapon)
	{
	}

	public void SetBanner(Banner banner)
	{
		throw new NotImplementedException();
	}

	TroopTraitsMask IAgentOriginBase.GetTraitsMask()
	{
		return AgentOriginUtilities.GetDefaultTraitsMask(this);
	}
}
