using System;
using System.Collections.Generic;
using System.Linq;
using AnimusForge.ExpeditionParade.Campaign;
using AnimusForge.ExpeditionParade.Core;

namespace AnimusForge.ExpeditionParade.Mission;

internal sealed class ParadeFormationController
{
	private static readonly IReadOnlyDictionary<ParadeFormationState, ParadeFormationState[]> AllowedTransitions =
		new Dictionary<ParadeFormationState, ParadeFormationState[]>
		{
			[ParadeFormationState.Pending] = new[] { ParadeFormationState.Spawning, ParadeFormationState.Aborted },
			[ParadeFormationState.Spawning] = new[] { ParadeFormationState.Assembling, ParadeFormationState.Aborted },
			[ParadeFormationState.Assembling] = new[] { ParadeFormationState.MarchingInside, ParadeFormationState.Stuck, ParadeFormationState.Aborted },
			[ParadeFormationState.MarchingInside] = new[] { ParadeFormationState.PassingGate, ParadeFormationState.Stuck, ParadeFormationState.Aborted },
			[ParadeFormationState.PassingGate] = new[] { ParadeFormationState.MarchingOutside, ParadeFormationState.Stuck, ParadeFormationState.Aborted },
			[ParadeFormationState.MarchingOutside] = new[] { ParadeFormationState.Exiting, ParadeFormationState.Stuck, ParadeFormationState.Aborted },
			[ParadeFormationState.Exiting] = new[] { ParadeFormationState.Completed, ParadeFormationState.Stuck, ParadeFormationState.Aborted },
			[ParadeFormationState.Stuck] = new[] { ParadeFormationState.Repath, ParadeFormationState.Aborted },
			[ParadeFormationState.Repath] = new[] { ParadeFormationState.NarrowFormation, ParadeFormationState.RecoverToRoute, ParadeFormationState.Aborted },
			[ParadeFormationState.NarrowFormation] = new[] { ParadeFormationState.RecoverToRoute, ParadeFormationState.Aborted },
			[ParadeFormationState.RecoverToRoute] = new[]
			{
				ParadeFormationState.Assembling,
				ParadeFormationState.MarchingInside,
				ParadeFormationState.PassingGate,
				ParadeFormationState.MarchingOutside,
				ParadeFormationState.Exiting,
				ParadeFormationState.Aborted
			},
			[ParadeFormationState.Completed] = Array.Empty<ParadeFormationState>(),
			[ParadeFormationState.Aborted] = Array.Empty<ParadeFormationState>()
		};

	private ParadeFormationState _stateBeforeRecovery;

	internal ParadeFormationController(string id, ParadeTroopCategory category, IEnumerable<ParadeRosterSnapshot.Entry> entries, int initialColumns)
	{
		Id = string.IsNullOrWhiteSpace(id) ? throw new ArgumentException("Formation id is required.", nameof(id)) : id;
		Category = category;
		Entries = (entries ?? Enumerable.Empty<ParadeRosterSnapshot.Entry>()).Where(entry => entry?.DisplayCount > 0).ToArray();
		if (Entries.Count == 0)
		{
			throw new ArgumentException("Formation requires at least one displayed troop.", nameof(entries));
		}
		DisplayCount = Entries.Sum(entry => entry.DisplayCount);
		CurrentColumns = Math.Max(1, Math.Min(initialColumns, DisplayCount));
		State = ParadeFormationState.Pending;
	}

	internal string Id { get; }

	internal ParadeTroopCategory Category { get; }

	internal IReadOnlyList<ParadeRosterSnapshot.Entry> Entries { get; }

	internal int DisplayCount { get; }

	internal int CurrentColumns { get; private set; }

	internal int RecoveryAttempts { get; private set; }

	internal ParadeFormationState State { get; private set; }

	internal bool HasClearedAssembly => State is ParadeFormationState.PassingGate
		or ParadeFormationState.MarchingOutside
		or ParadeFormationState.Exiting
		or ParadeFormationState.Completed;

	internal bool TryTransition(ParadeFormationState next, out string failure)
	{
		if (!AllowedTransitions.TryGetValue(State, out ParadeFormationState[] allowed) || !allowed.Contains(next))
		{
			failure = "invalid_formation_transition:" + State + "->" + next;
			return false;
		}
		if (next == ParadeFormationState.Stuck)
		{
			_stateBeforeRecovery = State;
		}
		if (next == ParadeFormationState.NarrowFormation)
		{
			CurrentColumns = Math.Max(1, CurrentColumns - 1);
		}
		if (next == ParadeFormationState.Repath)
		{
			RecoveryAttempts++;
		}
		State = next;
		failure = string.Empty;
		return true;
	}

	internal bool TryResumeAfterRecovery(out string failure)
	{
		if (State != ParadeFormationState.RecoverToRoute)
		{
			failure = "formation_not_recovering";
			return false;
		}
		return TryTransition(_stateBeforeRecovery, out failure);
	}
}
