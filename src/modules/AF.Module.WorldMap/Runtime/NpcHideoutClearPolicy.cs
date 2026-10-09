namespace AnimusForge;

internal enum NpcHideoutTargetDecision { Ready, Missing, AlreadyCleared, Protected, Wait }
internal enum NpcHideoutBattleOutcome { Success, Failure, Incomplete }

internal struct NpcHideoutClearTargetState
{
	internal bool Exists;
	internal bool Spotted;
	internal bool HasBandits;
	internal bool QuestBusy;
	internal bool HasBattle;
	internal bool AttackTimeReady;
}

/// <summary>Detached decisions shared by admission, runtime and the native callback guard.</summary>
internal static class NpcHideoutClearPolicy
{
	internal const int DefaultDays = 5;

	internal static NpcHideoutTargetDecision EvaluateTarget(NpcHideoutClearTargetState target)
	{
		if (!target.Exists) return NpcHideoutTargetDecision.Missing;
		if (target.QuestBusy) return NpcHideoutTargetDecision.Protected;
		if (!target.HasBandits) return NpcHideoutTargetDecision.AlreadyCleared;
		if (!target.Spotted) return NpcHideoutTargetDecision.Missing;
		return target.HasBattle || !target.AttackTimeReady ? NpcHideoutTargetDecision.Wait : NpcHideoutTargetDecision.Ready;
	}

	internal static bool CanOffer(NpcHideoutClearTargetState target) => EvaluateTarget(target) == NpcHideoutTargetDecision.Ready;
	internal static bool MustDeferStop(bool clearTask, bool battleCommitted, bool actorInBattle) => clearTask && battleCommitted && actorInBattle;
	internal static bool SuppressPlayerCompletion(bool ownedBattle, bool playerParticipated) => ownedBattle && !playerParticipated;
	internal static NpcHideoutBattleOutcome ResolveBattle(bool hasWinner, bool attackerWon, bool banditsRemain)
	{
		if (!hasWinner) return NpcHideoutBattleOutcome.Incomplete;
		if (!attackerWon) return NpcHideoutBattleOutcome.Failure;
		return banditsRemain ? NpcHideoutBattleOutcome.Incomplete : NpcHideoutBattleOutcome.Success;
	}
}
