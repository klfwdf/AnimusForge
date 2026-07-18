using System;
using System.Linq;
using AnimusForge.SiegeAftermathIntervention;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade;

namespace AnimusForge;

/// <summary>
/// Castle-GCCZ-only adapter that leaves campaign wall sections untouched while
/// removing initialized gates and siege machines from the aftermath scene.
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
		RemoveDefensiveDevices(mission);
	}

	private static void RemoveDefensiveDevices(Mission mission)
	{
		// Deactivating a mission object mutates ActiveMissionObjects. Snapshot every
		// target before touching one, otherwise the first removed gate aborts enumeration.
		CastleGate[] gates = mission.ActiveMissionObjects
			.FindAllWithType<CastleGate>()
			.Where(gate => gate != null)
			.ToArray();
		SiegeWeapon[] siegeWeapons = mission.ActiveMissionObjects
			.FindAllWithType<SiegeWeapon>()
			.Where(siegeWeapon => siegeWeapon != null)
			.ToArray();

		int gatesRemoved = 0;
		int siegeWeaponsRemoved = 0;
		int failures = 0;

		foreach (CastleGate gate in gates)
		{
			if (TryRemoveGate(mission, gate))
			{
				gatesRemoved++;
			}
			else
			{
				failures++;
			}
		}

		foreach (SiegeWeapon siegeWeapon in siegeWeapons)
		{
			if (TryRemoveSiegeWeapon(siegeWeapon))
			{
				siegeWeaponsRemoved++;
			}
			else
			{
				failures++;
			}
		}

		string summary = SiegeCastleDefensiveDeviceCleanupProfile.BuildSummary(
			gatesRemoved,
			siegeWeaponsRemoved,
			failures);
		Logger.Log("CastleAftermath", summary);
		GcczDiagnosticLog.Log("CastleDefensiveCleanup", summary);
	}

	private static bool TryRemoveGate(Mission mission, CastleGate gate)
	{
		try
		{
			try
			{
				// Opening first preserves the vanilla passable navmesh state before the
				// visual object and all of its collision bodies are removed.
				gate.OpenDoor();
			}
			catch (Exception openError)
			{
				if (!GameNetwork.IsClientOrReplay && gate.NavigationMeshId >= 0)
				{
					mission.Scene.SetAbilityOfFacesWithId(gate.NavigationMeshId, isEnabled: true);
				}
				Logger.Log("CastleAftermath", "Open castle gate before removal failed; passable navmesh fallback applied. Entity="
					+ gate.GameEntity.Name + ", Error=" + openError.Message);
			}

			DestructableComponent component = gate.DestructionComponent;
			if (component != null)
			{
				component.SetDisabledAndMakeInvisible(isParentObject: true, disableFaces: false);
			}
			gate.SetDisabledAndMakeInvisible(isParentObject: true, disableFaces: false);
			GameEntity.CreateFromWeakEntity(gate.GameEntity)
				?.SetPhysicsState(isEnabled: false, setChildren: true);
			return gate.IsDisabled && !gate.GameEntity.IsVisibleIncludeParents();
		}
		catch (Exception ex)
		{
			Logger.Log("CastleAftermath", "Remove castle gate failed. Entity="
				+ (gate?.GameEntity.Name ?? "N/A") + ", Error=" + ex.Message);
			return false;
		}
	}

	private static bool TryRemoveSiegeWeapon(SiegeWeapon siegeWeapon)
	{
		try
		{
			DestructableComponent component = siegeWeapon.DestructionComponent;
			if (component != null)
			{
				component.SetDisabledAndMakeInvisible(isParentObject: true, disableFaces: false);
			}
			siegeWeapon.SetDisabledAndMakeInvisible(isParentObject: true, disableFaces: false);
			GameEntity.CreateFromWeakEntity(siegeWeapon.GameEntity)
				?.SetPhysicsState(isEnabled: false, setChildren: true);
			return siegeWeapon.IsDisabled && !siegeWeapon.GameEntity.IsVisibleIncludeParents();
		}
		catch (Exception ex)
		{
			Logger.Log("CastleAftermath", "Remove castle siege weapon failed. Entity="
				+ (siegeWeapon?.GameEntity.Name ?? "N/A") + ", Error=" + ex.Message);
			return false;
		}
	}
}
