using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Engine;

namespace TaleWorlds.MountAndBlade.Missions;

public class MissionSiegeWeaponsController : IMissionSiegeWeaponsController
{
	private readonly List<MissionSiegeWeapon> _weapons;

	private readonly List<MissionSiegeWeapon> _undeployedWeapons;

	private readonly Dictionary<DestructableComponent, MissionSiegeWeapon> _deployedWeapons;

	private BattleSideEnum _side;

	public MissionSiegeWeaponsController(BattleSideEnum side, List<MissionSiegeWeapon> weapons)
	{
		_side = side;
		_weapons = weapons;
		_undeployedWeapons = new List<MissionSiegeWeapon>(_weapons);
		_deployedWeapons = new Dictionary<DestructableComponent, MissionSiegeWeapon>();
	}

	public int GetMaxDeployableWeaponCount(Type t)
	{
		int num = 0;
		foreach (MissionSiegeWeapon weapon in _weapons)
		{
			if (GetSiegeWeaponBaseType(weapon.Type) == t)
			{
				num++;
			}
		}
		return num;
	}

	public IEnumerable<IMissionSiegeWeapon> GetSiegeWeapons()
	{
		return _weapons.Cast<IMissionSiegeWeapon>();
	}

	public void OnWeaponDeployed(SiegeWeapon missionWeapon)
	{
		SiegeEngineType missionWeaponType = missionWeapon.GetSiegeEngineType();
		int index = _undeployedWeapons.FindIndex((MissionSiegeWeapon uw) => uw.Type == missionWeaponType);
		MissionSiegeWeapon missionSiegeWeapon = _undeployedWeapons[index];
		DestructableComponent destructionComponent = missionWeapon.DestructionComponent;
		destructionComponent.MaxHitPoint = missionSiegeWeapon.MaxHealth;
		destructionComponent.HitPoint = missionSiegeWeapon.InitialHealth;
		destructionComponent.OnHitTaken += OnWeaponHit;
		destructionComponent.OnDestroyed += OnWeaponDestroyed;
		_undeployedWeapons.RemoveAt(index);
		_deployedWeapons.Add(destructionComponent, missionSiegeWeapon);
	}

	public void OnWeaponUndeployed(SiegeWeapon missionWeapon)
	{
		DestructableComponent destructionComponent = missionWeapon.DestructionComponent;
		_deployedWeapons.TryGetValue(destructionComponent, out var value);
		SiegeEngineType siegeEngineType = missionWeapon.GetSiegeEngineType();
		destructionComponent.MaxHitPoint = siegeEngineType.BaseHitPoints;
		destructionComponent.HitPoint = destructionComponent.MaxHitPoint;
		destructionComponent.OnHitTaken -= OnWeaponHit;
		destructionComponent.OnDestroyed -= OnWeaponDestroyed;
		_deployedWeapons.Remove(destructionComponent);
		_undeployedWeapons.Add(value);
	}

	private void OnWeaponHit(DestructableComponent target, Agent attackerAgent, in MissionWeapon weapon, ScriptComponentBehavior attackerScriptComponentBehavior, int inflictedDamage)
	{
		if (target.BattleSide == _side && _deployedWeapons.TryGetValue(target, out var value))
		{
			float health = Math.Max(0f, value.Health - (float)inflictedDamage);
			value.SetHealth(health);
		}
	}

	private void OnWeaponDestroyed(DestructableComponent target, Agent attackerAgent, in MissionWeapon weapon, ScriptComponentBehavior attackerScriptComponentBehavior, int inflictedDamage)
	{
		if (target.BattleSide == _side && _deployedWeapons.TryGetValue(target, out var value))
		{
			value.SetHealth(0f);
			target.OnHitTaken -= OnWeaponHit;
			target.OnDestroyed -= OnWeaponDestroyed;
			_deployedWeapons.Remove(target);
		}
	}

	public static Type GetWeaponType(ScriptComponentBehavior weapon)
	{
		if (weapon is UsableGameObjectGroup)
		{
			return weapon.GameEntity.GetChildren().SelectMany((WeakGameEntity c) => c.GetScriptComponents()).First((ScriptComponentBehavior s) => s is IFocusable)
				.GetType();
		}
		return weapon.GetType();
	}

	private static Type GetSiegeWeaponBaseType(SiegeEngineType siegeWeaponType)
	{
		if (siegeWeaponType == DefaultSiegeEngineTypes.Ladder)
		{
			return typeof(SiegeLadder);
		}
		if (siegeWeaponType == DefaultSiegeEngineTypes.Ballista)
		{
			return typeof(Ballista);
		}
		if (siegeWeaponType == DefaultSiegeEngineTypes.FireBallista)
		{
			return typeof(FireBallista);
		}
		if (siegeWeaponType == DefaultSiegeEngineTypes.Ram)
		{
			return typeof(BatteringRam);
		}
		if (siegeWeaponType == DefaultSiegeEngineTypes.SiegeTower)
		{
			return typeof(SiegeTower);
		}
		if (siegeWeaponType == DefaultSiegeEngineTypes.Onager || siegeWeaponType == DefaultSiegeEngineTypes.Catapult)
		{
			return typeof(Mangonel);
		}
		if (siegeWeaponType == DefaultSiegeEngineTypes.FireOnager || siegeWeaponType == DefaultSiegeEngineTypes.FireCatapult)
		{
			return typeof(FireMangonel);
		}
		if (siegeWeaponType == DefaultSiegeEngineTypes.Trebuchet)
		{
			return typeof(Trebuchet);
		}
		return null;
	}
}
