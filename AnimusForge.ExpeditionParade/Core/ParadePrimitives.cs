using System;

namespace AnimusForge.ExpeditionParade.Core;

internal enum ParadeSettlementKind
{
	Town,
	Castle,
	Village
}

internal enum ParadeTroopCategory
{
	Vanguard,
	Infantry,
	Ranged,
	HorseArcher,
	Cavalry,
	RearGuard
}

internal enum ParadeLifecycleState
{
	Created,
	Planning,
	Ready,
	Running,
	CleaningUp,
	Completed,
	Aborted
}

internal enum ParadeFormationState
{
	Pending,
	Spawning,
	Assembling,
	MarchingInside,
	PassingGate,
	MarchingOutside,
	Exiting,
	Completed,
	Stuck,
	Repath,
	NarrowFormation,
	RecoverToRoute,
	Aborted
}

internal enum ParadeAbortReason
{
	None,
	EligibilityChanged,
	NoHealthyTroops,
	AgentBudgetExceeded,
	RouteUnavailable,
	RouteInvalidated,
	MissionEnded,
	PlayerCancelled,
	RuntimeFailure
}

internal sealed class ParadeOperationResult
{
	private ParadeOperationResult(bool succeeded, string code, string message)
	{
		Succeeded = succeeded;
		Code = code ?? string.Empty;
		Message = message ?? string.Empty;
	}

	internal bool Succeeded { get; }

	internal string Code { get; }

	internal string Message { get; }

	internal static ParadeOperationResult Success(string code = "ok", string message = "")
	{
		return new ParadeOperationResult(true, code, message);
	}

	internal static ParadeOperationResult Failure(string code, string message)
	{
		if (string.IsNullOrWhiteSpace(code))
		{
			throw new ArgumentException("Failure code is required.", nameof(code));
		}
		return new ParadeOperationResult(false, code, message);
	}

	public override string ToString()
	{
		return Code + (string.IsNullOrWhiteSpace(Message) ? string.Empty : ": " + Message);
	}
}
