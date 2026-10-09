using System;
using AnimusForge;

internal static class Program
{
	private static int _checks;
	private static void Check(bool condition, string name)
	{
		_checks++;
		if (!condition) throw new InvalidOperationException(name);
	}
	private static NpcHideoutClearTargetState Ready() => new NpcHideoutClearTargetState
	{ Exists = true, Spotted = true, HasBandits = true, AttackTimeReady = true };
	private static int Main()
	{
		Check(NpcHideoutClearPolicy.CanOffer(Ready()), "known ordinary hideout offered");
		var target = Ready(); target.Exists = false;
		Check(!NpcHideoutClearPolicy.CanOffer(target), "missing target rejected");
		target = Ready(); target.Spotted = false;
		Check(!NpcHideoutClearPolicy.CanOffer(target), "undiscovered target rejected");
		target = Ready(); target.QuestBusy = true;
		Check(NpcHideoutClearPolicy.EvaluateTarget(target) == NpcHideoutTargetDecision.Protected, "quest-protected target rejected");
		target = Ready(); target.HasBandits = false;
		Check(NpcHideoutClearPolicy.EvaluateTarget(target) == NpcHideoutTargetDecision.AlreadyCleared, "externally cleared target not claimed");
		target = Ready(); target.HasBattle = true;
		Check(!NpcHideoutClearPolicy.CanOffer(target), "active battle not offered");
		Check(NpcHideoutClearPolicy.EvaluateTarget(target) == NpcHideoutTargetDecision.Wait, "accepted task waits rather than hijacking battle");
		target = Ready(); target.AttackTimeReady = false;
		Check(NpcHideoutClearPolicy.EvaluateTarget(target) == NpcHideoutTargetDecision.Wait, "native attack cooldown preserved");
		Check(NpcHideoutClearPolicy.MustDeferStop(true, true, true), "active clear battle retains cancellation/save ownership");
		Check(!NpcHideoutClearPolicy.MustDeferStop(true, true, false), "finalized battle can safely stop");
		Check(!NpcHideoutClearPolicy.MustDeferStop(true, false, true), "uncommitted task does not claim unrelated battle");
		Check(!NpcHideoutClearPolicy.MustDeferStop(false, true, true), "other tasks retain existing cancellation behavior");
		Check(NpcHideoutClearPolicy.SuppressPlayerCompletion(true, false), "own NPC battle isolates player callbacks");
		Check(!NpcHideoutClearPolicy.SuppressPlayerCompletion(false, false), "unrelated NPC battle untouched");
		Check(!NpcHideoutClearPolicy.SuppressPlayerCompletion(true, true), "player participating raid remains native");
		Check(!NpcHideoutClearPolicy.SuppressPlayerCompletion(false, true), "ordinary player raid untouched");
		Check(NpcHideoutClearPolicy.ResolveBattle(true, true, false) == NpcHideoutBattleOutcome.Success, "actual victory and no bandits confirms success");
		Check(NpcHideoutClearPolicy.ResolveBattle(true, true, true) == NpcHideoutBattleOutcome.Incomplete, "victory with survivors is not clearance");
		Check(NpcHideoutClearPolicy.ResolveBattle(true, false, true) == NpcHideoutBattleOutcome.Failure, "defeat preserved");
		Check(NpcHideoutClearPolicy.ResolveBattle(false, true, false) == NpcHideoutBattleOutcome.Incomplete, "missing winner is not a success");
		Check(WorldMapOrderCoordinator.TryParseToken("[ACTION:WORLDMAP_ORDER:CLEAR_HIDEOUT:settlement:hideout_1:5]", out var token) && token.Kind == "CLEAR_HIDEOUT" && token.Parts.Length == 4, "new token enters shared parser");
		Check(WorldMapOrderCoordinator.ClassifyCommand("ClearHideout") == WorldMapCommandRoute.ClearHideout, "new command reaches runtime route");
		Check(WorldMapOrderCoordinator.ClassifyCommand("AttackHero") == WorldMapCommandRoute.AttackHero, "existing attack unchanged");
		Check(WorldMapOrderCoordinator.ClassifyCommand("future") == WorldMapCommandRoute.Unknown, "unknown command remains rejected");
		Check(WorldMapOrderCoordinator.SelectAdmissionRoute(true, true, true, false) == WorldMapOrderAdmissionRoute.CompanionPartyCreation, "main-party task still splits troops");
		Check(WorldMapOrderCoordinator.SelectAdmissionRoute(true, false, true, true) == WorldMapOrderAdmissionRoute.GovernorExpedition, "governor task still uses expedition route");
		var sequence = new WorldMapOrderSequenceCoordinator();
		Check(sequence.Observe(true) == WorldMapOrderSequenceDecision.AcceptLeadingStop, "leading STOP retained");
		Check(sequence.Observe(false) == WorldMapOrderSequenceDecision.AcceptCommand, "clear task follows leading STOP");
		Check(sequence.Observe(true) == WorldMapOrderSequenceDecision.RejectNonLeadingStop, "late STOP still rejected");
		Console.WriteLine("PASS " + _checks + " production policy/routing checks; no Campaign or Mission simulated.");
		return 0;
	}
}
