using System;
using System.Collections.Generic;
using AnimusForge.ExpeditionParade.Campaign;
using AnimusForge.ExpeditionParade.Configuration;
using AnimusForge.ExpeditionParade.Core;
using AnimusForge.ExpeditionParade.Mission;
using AnimusForge.ExpeditionParade.Presentation;
using AnimusForge.ExpeditionParade.Routing;

namespace AnimusForge.ExpeditionParade.Runtime;

internal static class ParadeFrameworkRuntime
{
	private static readonly object Sync = new();
	private static ParadeSettings _settings;
	private static ParadeCoordinator _activeCoordinator;
	private static IParadeCameraController _cameraController = new NullParadeCameraController();
	private static IParadeDebugOverlay _debugOverlay = new NullParadeDebugOverlay();

	internal static ParadeSettings Settings
	{
		get
		{
			lock (Sync)
			{
				return _settings?.Clone();
			}
		}
	}

	internal static ParadeCoordinator ActiveCoordinator
	{
		get
		{
			lock (Sync)
			{
				return _activeCoordinator;
			}
		}
	}

	internal static IParadeRouteCache RouteCache { get; } = new MemoryParadeRouteCache();

	internal static IParadeCameraController CameraController
	{
		get
		{
			lock (Sync)
			{
				return _cameraController;
			}
		}
	}

	internal static IParadeDebugOverlay DebugOverlay
	{
		get
		{
			lock (Sync)
			{
				return _debugOverlay;
			}
		}
	}

	internal static void ConfigurePresentation(IParadeCameraController cameraController, IParadeDebugOverlay debugOverlay)
	{
		lock (Sync)
		{
			_cameraController = cameraController ?? new NullParadeCameraController();
			_debugOverlay = debugOverlay ?? new NullParadeDebugOverlay();
		}
	}

	internal static ParadeOperationResult Initialize(ParadeSettings settings)
	{
		if (settings == null)
		{
			return ParadeOperationResult.Failure("settings_missing", "Parade settings were null.");
		}
		IReadOnlyList<string> validationErrors = settings.Validate();
		if (validationErrors.Count > 0)
		{
			return ParadeOperationResult.Failure("settings_invalid", string.Join(",", validationErrors));
		}

		lock (Sync)
		{
			_settings = settings.Clone();
		}
		return ParadeOperationResult.Success("framework_initialized");
	}

	internal static ParadeOperationResult TryCreateCoordinator(string settlementId, ParadeRosterSnapshot roster, out ParadeCoordinator coordinator)
	{
		lock (Sync)
		{
			coordinator = null;
			if (_settings == null)
			{
				return ParadeOperationResult.Failure("framework_not_initialized", string.Empty);
			}
			if (_activeCoordinator != null && _activeCoordinator.State is not ParadeLifecycleState.Completed and not ParadeLifecycleState.Aborted)
			{
				return ParadeOperationResult.Failure("parade_session_already_active", _activeCoordinator.SessionId);
			}
			if (roster?.TotalDisplayCount <= 0)
			{
				return ParadeOperationResult.Failure("roster_empty", string.Empty);
			}

			coordinator = new ParadeCoordinator(settlementId, roster, _settings, new ParadeCleanupService());
			_activeCoordinator = coordinator;
			return ParadeOperationResult.Success("coordinator_created", coordinator.SessionId);
		}
	}

	internal static void ReleaseCoordinator(ParadeCoordinator coordinator)
	{
		lock (Sync)
		{
			if (ReferenceEquals(_activeCoordinator, coordinator))
			{
				_activeCoordinator = null;
			}
		}
	}
}
