using System;
using System.Collections.Generic;
using AnimusForge.SiegeAftermathIntervention;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace AnimusForge;

/// <summary>
/// Castle-GCCZ-only adapter that leaves campaign wall sections untouched while
/// destroying initialized gates and siege machines through native hit handling.
/// </summary>
internal static class CastleAftermathDefensiveDeviceRuntimeBridge
{
	internal static void AttachMissionBehavior(Mission mission)
	{
		if (mission == null || mission.GetMissionBehavior<CastleAftermathDefensiveDeviceMissionBehavior>() != null)
		{
			return;
		}
		mission.AddMissionBehavior(new CastleAftermathDefensiveDeviceMissionBehavior());
	}
}

internal sealed class CastleAftermathDefensiveDeviceMissionBehavior : MissionLogic
{
	private float _elapsedSeconds;
	private bool _completed;

	public override MissionBehaviorType BehaviorType => MissionBehaviorType.Other;

	public override void OnMissionTick(float dt)
	{
		base.OnMissionTick(dt);
		if (_completed)
		{
			return;
		}
		Mission mission = base.Mission;
		if (mission == null || mission.IsMissionEnding)
		{
			_completed = true;
			return;
		}
		if (!CastleAftermathRuntimeBridge.IsCastleAftermathMission(mission))
		{
			return;
		}

		_elapsedSeconds += Math.Max(0f, dt);
		if (_elapsedSeconds < SiegeCastleDefensiveDeviceCleanupProfile.PreparationDelaySeconds)
		{
			return;
		}

		_completed = true;
		DestroyDefensiveDevices(mission);
	}

	private static void DestroyDefensiveDevices(Mission mission)
	{
		int gatesDestroyed = 0;
		int siegeWeaponsDestroyed = 0;
		int failures = 0;
		var processed = new HashSet<DestructableComponent>();

		foreach (CastleGate gate in mission.ActiveMissionObjects.FindAllWithType<CastleGate>())
		{
			DestructableComponent component = gate?.DestructionComponent;
			if (component == null || !processed.Add(component) || component.IsDestroyed)
			{
				continue;
			}
			if (TryDestroy(component))
			{
				gatesDestroyed++;
			}
			else
			{
				failures++;
			}
		}

		foreach (SiegeWeapon siegeWeapon in mission.ActiveMissionObjects.FindAllWithType<SiegeWeapon>())
		{
			DestructableComponent component = siegeWeapon?.DestructionComponent;
			if (component == null || !processed.Add(component) || component.IsDestroyed)
			{
				continue;
			}
			if (TryDestroy(component))
			{
				siegeWeaponsDestroyed++;
			}
			else
			{
				failures++;
			}
		}

		string summary = SiegeCastleDefensiveDeviceCleanupProfile.BuildSummary(
			gatesDestroyed,
			siegeWeaponsDestroyed,
			failures);
		Logger.Log("CastleAftermath", summary);
		GcczDiagnosticLog.Log("CastleDefensiveCleanup", summary);
	}

	private static bool TryDestroy(DestructableComponent component)
	{
		try
		{
			ItemObject impactItem = Game.Current?.ObjectManager?.GetObject<ItemObject>("boulder")
				?? Game.Current?.ObjectManager?.GetObject<ItemObject>("ballista_projectile")
				?? Game.Current?.ObjectManager?.GetObject<ItemObject>(SiegeCastleLordDuelProfile.DefaultWeaponItemId);
			if (impactItem == null)
			{
				return false;
			}

			MissionWeapon impactWeapon = new MissionWeapon(impactItem, null, null);
			int damage = Math.Max(
				1,
				(int)Math.Ceiling(component.MaxHitPoint) + SiegeCastleDefensiveDeviceCleanupProfile.DestructionDamageMargin);
			Vec3 impactPosition = component.GameEntity.GlobalPosition;
			bool stoneOnly = component.DestroyedByStoneOnly;
			try
			{
				// Native TriggerOnHit drives destruction states, gate callbacks, particles and navmesh updates.
				component.DestroyedByStoneOnly = false;
				component.TriggerOnHit(
					Agent.Main,
					damage,
					impactPosition,
					Vec3.Forward,
					in impactWeapon,
					-1,
					null);
			}
			finally
			{
				component.DestroyedByStoneOnly = stoneOnly;
			}
			return component.IsDestroyed;
		}
		catch (Exception ex)
		{
			string entityName = component == null ? "N/A" : component.GameEntity.Name;
			Logger.Log("CastleAftermath", "Destroy castle defensive device failed. Entity="
				+ entityName + ", Error=" + ex.Message);
			return false;
		}
	}
}
