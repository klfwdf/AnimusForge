using System;
using System.Linq;
using AnimusForge.ExpeditionParade.Core;
using AnimusForge.ExpeditionParade.Routing;
using AnimusForge.ExpeditionParade.Runtime;
using TaleWorlds.MountAndBlade;

namespace AnimusForge.ExpeditionParade.Mission;

internal sealed class ParadeMissionLogic : MissionLogic
{
	private readonly ParadeCoordinator _coordinator;
	private readonly IParadeMissionRuntimeAdapter _runtimeAdapter;
	private bool _initializationAttempted;

	internal ParadeMissionLogic(ParadeCoordinator coordinator, IParadeMissionRuntimeAdapter runtimeAdapter)
	{
		_coordinator = coordinator ?? throw new ArgumentNullException(nameof(coordinator));
		_runtimeAdapter = runtimeAdapter ?? throw new ArgumentNullException(nameof(runtimeAdapter));
		_coordinator.RegisterCleanup("mission_runtime_adapter", () => _runtimeAdapter.Cleanup(_coordinator, base.Mission));
		_coordinator.RegisterCleanup("presentation", () =>
		{
			ParadeFrameworkRuntime.DebugOverlay.Hide();
			ParadeFrameworkRuntime.CameraController.End();
		});
	}

	public override MissionBehaviorType BehaviorType => MissionBehaviorType.Other;

	public override void OnMissionTick(float dt)
	{
		base.OnMissionTick(dt);
		if (_coordinator.State is ParadeLifecycleState.Completed or ParadeLifecycleState.Aborted)
		{
			return;
		}
		if (!_initializationAttempted)
		{
			_initializationAttempted = true;
			InitializeRouteAndStart();
			return;
		}

		try
		{
			if (_coordinator.Settings.EnableDebugDrawing)
			{
				ParadeFrameworkRuntime.DebugOverlay.UpdateFormations(_coordinator.Formations);
			}
			ParadeOperationResult tickResult = _runtimeAdapter.Tick(_coordinator, base.Mission, dt);
			if (!tickResult.Succeeded)
			{
				ParadeFrameworkRuntime.DebugOverlay.ShowFailure(tickResult.Code, tickResult.Message);
				_coordinator.Abort(ParadeAbortReason.RuntimeFailure, tickResult.ToString());
				return;
			}
			if (_coordinator.Formations.Count > 0
				&& _coordinator.Formations.All(formation => formation.State == ParadeFormationState.Completed))
			{
				_coordinator.CompleteIfAllFormationsFinished();
			}
		}
		catch (Exception ex)
		{
			ParadeFrameworkRuntime.DebugOverlay.ShowFailure("mission_tick_exception", ex.Message);
			_coordinator.Abort(ParadeAbortReason.RuntimeFailure, "mission_tick_exception:" + ex.GetType().Name + ":" + ex.Message);
		}
	}

	protected override void OnEndMission()
	{
		if (_coordinator.State is not ParadeLifecycleState.Completed and not ParadeLifecycleState.Aborted)
		{
			_coordinator.Abort(ParadeAbortReason.MissionEnded, "mission_ended_before_parade_completion");
		}
		ParadeFrameworkRuntime.ReleaseCoordinator(_coordinator);
		base.OnEndMission();
	}

	private void InitializeRouteAndStart()
	{
		try
		{
			ParadeOperationResult planning = _coordinator.BeginPlanning();
			if (!planning.Succeeded)
			{
				ParadeFrameworkRuntime.DebugOverlay.ShowFailure(planning.Code, planning.Message);
				return;
			}
			ParadeRouteResolution route = _runtimeAdapter.ResolveRoute(_coordinator, base.Mission);
			ParadeOperationResult accepted = _coordinator.AcceptRoute(route);
			if (accepted.Succeeded)
			{
				if (_coordinator.Settings.EnableDebugDrawing)
				{
					ParadeFrameworkRuntime.DebugOverlay.ShowRoute(route.Plan);
				}
				if (_coordinator.Settings.EnableFreeCamera)
				{
					ParadeFrameworkRuntime.CameraController.Begin(_coordinator);
				}
				ParadeOperationResult started = _coordinator.Start();
				if (!started.Succeeded)
				{
					ParadeFrameworkRuntime.DebugOverlay.ShowFailure(started.Code, started.Message);
					_coordinator.Abort(ParadeAbortReason.RuntimeFailure, started.ToString());
				}
			}
			else
			{
				ParadeFrameworkRuntime.DebugOverlay.ShowFailure(accepted.Code, accepted.Message);
			}
		}
		catch (Exception ex)
		{
			ParadeFrameworkRuntime.DebugOverlay.ShowFailure("mission_initialization_exception", ex.Message);
			_coordinator.Abort(ParadeAbortReason.RuntimeFailure, "mission_initialization_exception:" + ex.GetType().Name + ":" + ex.Message);
		}
	}
}
