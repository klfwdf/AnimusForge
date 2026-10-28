using System.Collections.Generic;
using System.Xml;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade;

public abstract class MPPerkEffectBase
{
	public virtual bool IsTickRequired => false;

	public bool IsDisabledInWarmup { get; protected set; }

	public virtual void OnUpdate(Agent agent, bool newState)
	{
	}

	public virtual void OnTick(MissionPeer peer, int tickCount)
	{
		if (MultiplayerOptions.OptionType.NumberOfBotsPerFormation.GetIntValue() > 0)
		{
			MBReadOnlyList<IFormationUnit> mBReadOnlyList = peer?.ControlledFormation?.Arrangement.GetAllUnits();
			if (mBReadOnlyList == null)
			{
				return;
			}
			{
				foreach (IFormationUnit item in mBReadOnlyList)
				{
					if (item is Agent agent && agent.IsActive())
					{
						OnTick(agent, tickCount);
					}
				}
				return;
			}
		}
		if (peer != null && peer.ControlledAgent?.IsActive() == true)
		{
			OnTick(peer.ControlledAgent, tickCount);
		}
	}

	public virtual void OnTick(Agent agent, int tickCount)
	{
	}

	public virtual float GetDamage(WeaponComponentData attackerWeapon, DamageTypes damageType, bool isAlternativeAttack)
	{
		return 0f;
	}

	public virtual float GetMountDamage(WeaponComponentData attackerWeapon, DamageTypes damageType, bool isAlternativeAttack)
	{
		return 0f;
	}

	public virtual float GetDamageTaken(WeaponComponentData attackerWeapon, DamageTypes damageType)
	{
		return 0f;
	}

	public virtual float GetMountDamageTaken(WeaponComponentData attackerWeapon, DamageTypes damageType)
	{
		return 0f;
	}

	public virtual float GetSpeedBonusEffectiveness(Agent attacker, WeaponComponentData attackerWeapon, DamageTypes damageType)
	{
		return 0f;
	}

	public virtual float GetShieldDamage(bool isCorrectSideBlock)
	{
		return 0f;
	}

	public virtual float GetShieldDamageTaken(bool isCorrectSideBlock)
	{
		return 0f;
	}

	public virtual float GetRangedAccuracy()
	{
		return 0f;
	}

	public virtual float GetThrowingWeaponSpeed(WeaponComponentData attackerWeapon)
	{
		return 0f;
	}

	public virtual float GetDamageInterruptionThreshold()
	{
		return 0f;
	}

	public virtual float GetMountManeuver()
	{
		return 0f;
	}

	public virtual float GetMountSpeed()
	{
		return 0f;
	}

	public virtual float GetRangedHeadShotDamage()
	{
		return 0f;
	}

	public virtual int GetGoldOnKill(float attackerValue, float victimValue)
	{
		return 0;
	}

	public virtual int GetGoldOnAssist()
	{
		return 0;
	}

	public virtual int GetRewardedGoldOnAssist()
	{
		return 0;
	}

	public virtual bool GetIsTeamRewardedOnDeath()
	{
		return false;
	}

	public virtual void CalculateRewardedGoldOnDeath(Agent agent, List<(MissionPeer, int)> teamMembers)
	{
	}

	public virtual float GetDrivenPropertyBonus(DrivenProperty drivenProperty, float baseValue)
	{
		return 0f;
	}

	public virtual float GetEncumbrance(bool isOnBody)
	{
		return 0f;
	}

	protected abstract void Deserialize(XmlNode node);
}
