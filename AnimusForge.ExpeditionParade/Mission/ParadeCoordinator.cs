using System;
using System.Collections.Generic;
using System.Linq;
using AnimusForge.ExpeditionParade.Campaign;
using AnimusForge.ExpeditionParade.Configuration;
using AnimusForge.ExpeditionParade.Core;
using AnimusForge.ExpeditionParade.Diagnostics;
using AnimusForge.ExpeditionParade.Routing;

namespace AnimusForge.ExpeditionParade.Mission;

internal sealed class ParadeCoordinator
{
	private readonly ParadeCleanupService _cleanup;
	private readonly List<ParadeFormationController> _formations = new();

	internal ParadeCoordinator(string settlementId, ParadeRosterSnapshot roster, ParadeSettings settings, ParadeCleanupService cleanup)
	{
		SessionId = Guid.NewGuid().ToString("N");
		SettlementId = string.IsNullOrWhiteSpace(settlementId) ? throw new ArgumentException("Settlement id is required.", nameof(settlementId)) : settlementId;
		Roster = roster ?? throw new ArgumentNullException(nameof(roster));
		Settings = (settings ?? throw new ArgumentNullException(nameof(settings))).Clone();
		_cleanup = cleanup ?? throw new ArgumentNullException(nameof(cleanup));
		Crowd = new CrowdReactionController();
		State = ParadeLifecycleState.Created;
		Diagnostics = new ParadeSessionDiagnostics(SessionId, SettlementId);
		Diagnostics.SetField("roster_healthy", Roster.TotalHealthyCount);
		Diagnostics.SetField("roster_display", Roster.TotalDisplayCount);
	}

	internal string SessionId { get; }

	internal string SettlementId { get; }

	internal ParadeRosterSnapshot Roster { get; }

	internal ParadeSettings Settings { get; }

	internal ParadeLifecycleState State { get; private set; }

	internal ParadeAbortReason AbortReason { get; private set; }

	internal string TerminalDetail { get; private set; } = string.Empty;

	internal ParadeRoutePlan Route { get; private set; }

	internal ParadeSessionDiagnostics Diagnostics { get; }

	internal CrowdReactionController Crowd { get; }

	internal IReadOnlyList<ParadeFormationController> Formations => _formations;

	internal void RegisterCleanup(string key, Action cleanup)
	{
		_cleanup.Register(key, cleanup);
	}

	internal ParadeOperationResult BeginPlanning()
	{
		if (State != ParadeLifecycleState.Created)
		{
			return ParadeOperationResult.Failure("invalid_lifecycle_transition", State + "->Planning");
		}
		if (Roster.TotalDisplayCount <= 0)
		{
			return Abort(ParadeAbortReason.NoHealthyTroops, "roster_has_no_displayed_troops");
		}
		State = ParadeLifecycleState.Planning;
		Diagnostics.RecordEvent("planning_started");
		return ParadeOperationResult.Success();
	}

	internal ParadeOperationResult AcceptRoute(ParadeRouteResolution resolution)
	{
		if (State != ParadeLifecycleState.Planning)
		{
			return ParadeOperationResult.Failure("invalid_lifecycle_transition", State + "->Ready");
		}
		if (resolution?.Succeeded != true || resolution.Plan == null)
		{
			return Abort(ParadeAbortReason.RouteUnavailable, string.Join(";", resolution?.Diagnostics ?? Array.Empty<string>()));
		}

		Route = resolution.Plan;
		Diagnostics.SetField("route_source", resolution.Source);
		Diagnostics.SetField("route_candidate", Route.CandidateId);
		Diagnostics.SetField("route_minimum_width", Route.MinimumPassageWidth);
		Diagnostics.RecordEvent("route_accepted", string.Join(";", resolution.Diagnostics));
		BuildFormationQueue();
		if (_formations.Count == 0)
		{
			return Abort(ParadeAbortReason.NoHealthyTroops, "formation_queue_empty");
		}
		State = ParadeLifecycleState.Ready;
		return ParadeOperationResult.Success();
	}

	internal ParadeOperationResult Start()
	{
		if (State != ParadeLifecycleState.Ready)
		{
			return ParadeOperationResult.Failure("invalid_lifecycle_transition", State + "->Running");
		}
		State = ParadeLifecycleState.Running;
		Diagnostics.RecordEvent("parade_started", "formations=" + _formations.Count);
		return StartNextEligibleFormation();
	}

	internal ParadeOperationResult StartNextEligibleFormation()
	{
		if (State != ParadeLifecycleState.Running)
		{
			return ParadeOperationResult.Failure("parade_not_running", State.ToString());
		}
		ParadeFormationController pending = _formations.FirstOrDefault(formation => formation.State == ParadeFormationState.Pending);
		if (pending == null)
		{
			return ParadeOperationResult.Success("no_pending_formation");
		}
		int index = _formations.IndexOf(pending);
		if (index > 0 && !_formations[index - 1].HasClearedAssembly)
		{
			return ParadeOperationResult.Failure("formation_spacing_guard", _formations[index - 1].Id);
		}
		if (!pending.TryTransition(ParadeFormationState.Spawning, out string failure))
		{
			return ParadeOperationResult.Failure("formation_start_failed", failure);
		}
		Diagnostics.RecordEvent("formation_started", pending.Id);
		return ParadeOperationResult.Success("formation_started", pending.Id);
	}

	internal ParadeOperationResult CompleteIfAllFormationsFinished()
	{
		if (State != ParadeLifecycleState.Running)
		{
			return ParadeOperationResult.Failure("parade_not_running", State.ToString());
		}
		if (_formations.Any(formation => formation.State != ParadeFormationState.Completed))
		{
			return ParadeOperationResult.Failure("formations_incomplete", string.Join(",", _formations.Where(formation => formation.State != ParadeFormationState.Completed).Select(formation => formation.Id)));
		}
		return Finish(ParadeLifecycleState.Completed, ParadeAbortReason.None, "all_formations_completed");
	}

	internal ParadeOperationResult Abort(ParadeAbortReason reason, string detail)
	{
		if (State is ParadeLifecycleState.Completed or ParadeLifecycleState.Aborted)
		{
			return ParadeOperationResult.Success("already_terminal", State.ToString());
		}
		foreach (ParadeFormationController formation in _formations.Where(formation => formation.State is not ParadeFormationState.Completed and not ParadeFormationState.Aborted))
		{
			formation.TryTransition(ParadeFormationState.Aborted, out _);
		}
		Diagnostics.RecordEvent("parade_aborted", reason + ":" + detail);
		ParadeOperationResult cleanupResult = Finish(ParadeLifecycleState.Aborted, reason, detail);
		return ParadeOperationResult.Failure(
			"parade_aborted_" + reason.ToString().ToLowerInvariant(),
			cleanupResult.Message);
	}

	private void BuildFormationQueue()
	{
		_formations.Clear();
		foreach (IGrouping<ParadeTroopCategory, ParadeRosterSnapshot.Entry> group in Roster.Entries
			.Where(entry => entry.DisplayCount > 0)
			.GroupBy(entry => entry.Category)
			.OrderBy(group => group.Key))
		{
			int initialColumns = Math.Min(4, Math.Max(1, group.Sum(entry => entry.DisplayCount)));
			_formations.Add(new ParadeFormationController("formation_" + group.Key.ToString().ToLowerInvariant(), group.Key, group, initialColumns));
		}
	}

	private ParadeOperationResult Finish(ParadeLifecycleState terminalState, ParadeAbortReason reason, string detail)
	{
		Diagnostics.RecordEvent("cleanup_started", terminalState.ToString());
		State = ParadeLifecycleState.CleaningUp;
		IReadOnlyList<string> cleanupFailures = _cleanup.RunOnce();
		AbortReason = reason;
		TerminalDetail = (detail ?? string.Empty) + (cleanupFailures.Count == 0 ? string.Empty : ";cleanup=" + string.Join("|", cleanupFailures));
		State = terminalState;
		Diagnostics.SetField("terminal_state", terminalState);
		Diagnostics.SetField("terminal_detail", TerminalDetail);
		Diagnostics.SetField("cleanup_failures", cleanupFailures.Count);
		return cleanupFailures.Count == 0
			? ParadeOperationResult.Success(terminalState.ToString().ToLowerInvariant(), TerminalDetail)
			: ParadeOperationResult.Failure("cleanup_partial_failure", TerminalDetail);
	}
}
