using System;
using AnimusForge.SiegeAftermathIntervention;
using TaleWorlds.Localization;

namespace AnimusForge;

// Resolve only at the existing notification boundary. Shared facts and profiles stay unchanged.
internal static class CastleAftermathUiText
{
    private static string Text(string id, string fallback, params (string Name, string Value)[] variables)
    {
        TextObject text = new TextObject("{=" + id + "}" + fallback);
        foreach (var variable in variables)
        {
            text.SetTextVariable(variable.Name, variable.Value ?? string.Empty);
        }
        return text.ToString();
    }

    private static string Count(int value) => Math.Max(0, value).ToString();
    private static string Name(string value) => string.IsNullOrWhiteSpace(value)
        ? Text("AF_CASTLE_UI_LORD", "the captured lord") : value.Trim();

    internal static string LetterDelivered(string sender, string recipient)
        => Text("AF_CASTLE_UI_LETTER_DELIVERED", "[Castle aftermath letter] {SENDER}'s introduction letter has reached {RECIPIENT}; no courier party was created.",
            ("SENDER", sender), ("RECIPIENT", recipient));
    internal static string NoUnassigned => Text("AF_CASTLE_UI_NO_UNASSIGNED", "[Castle aftermath] No regular prisoners remain unassigned. To revise the previous plan, explicitly say you have changed your mind, want to start over, or overturn the previous arrangements.");
    internal static string SlaughterStarted(int affected)
        => Text("AF_CASTLE_UI_SLAUGHTER_STARTED", "[Castle aftermath] Formation 1 has begun attacking the {COUNT} selected prisoners; only prisoners who actually die in the scene are removed from the roster.", ("COUNT", affected.ToString()));
    internal static string Staged(string action, int staged, int unassigned)
        => Text("AF_CASTLE_UI_STAGED", "[Castle aftermath] Provisionally assigned {COUNT} prisoners to '{ACTION}'; {UNASSIGNED} remain unassigned. The plan is executed when you leave.",
            ("ACTION", action), ("COUNT", staged.ToString()), ("UNASSIGNED", unassigned.ToString()));
    internal static string DeferredFailed(string action, int count, string reason)
        => Text("AF_CASTLE_UI_DEFERRED_FAILED", "[Castle aftermath] Resolution on departure failed: no prisoners in '{ACTION}' ({COUNT}) were affected. Reason: {REASON}. These prisoners remain captives.",
            ("ACTION", action), ("COUNT", count.ToString()), ("REASON", reason));
    internal static string RecruitmentPartial(int planned, int affected)
        => Text("AF_CASTLE_UI_RECRUITMENT_PARTIAL", "[Castle aftermath] Recruitment was only partially completed: {PLANNED} planned, {COUNT} joined (the main party is full). The others remain captives.",
            ("PLANNED", planned.ToString()), ("COUNT", affected.ToString()));
    internal static string DuelFailed => Text("AF_CASTLE_UI_DUEL_FAILED", "[Castle duel] The duel could not be set up in this scene. Prisoner disposition remains unchanged.");
    internal static string CareInsufficient => Text("AF_CASTLE_UI_CARE_INSUFFICIENT", "[Castle aftermath] The escort has insufficient food. Prisoner care was not completed and no supplies were deducted.");
    internal static string Care(int affected, int trustDelta)
        => Text("AF_CASTLE_UI_CARE", "[Castle aftermath] Supplies were distributed to {COUNT} regular prisoners according to troop tier, and mistreatment was forbidden; prisoner trust {TRUST}.",
            ("COUNT", Count(affected)), ("TRUST", trustDelta > 0 ? "+" + trustDelta : trustDelta.ToString()));
    internal static string Armaments(int affected, int items, int kinds, int gold)
        => Text("AF_CASTLE_UI_ARMAMENTS", "[Castle aftermath] Arms and belongings were confiscated from {COUNT} regular prisoners and sent directly to your inventory: {ITEMS} items / {KINDS} types, {GOLD} denars; no loot screen is opened.",
            ("COUNT", Count(affected)), ("ITEMS", Count(items)), ("KINDS", Count(kinds)), ("GOLD", Count(gold)));
    internal static string SlaughterKills(int killedTotal, int remaining)
        => Text("AF_CASTLE_UI_SLAUGHTER_KILLS", "[Castle aftermath] {KILLED} regular prisoners have actually died; {REMAINING} still need to be killed in the scene.",
            ("KILLED", Count(killedTotal)), ("REMAINING", Count(remaining)));
    internal static string LordProcess(SiegeCastleActionKind action, string lordName, int items = 0)
        => action == SiegeCastleActionKind.ReceiveArmaments
            ? Text("AF_CASTLE_UI_LORD_ARMAMENTS", "[Castle aftermath] {LORD}'s weapons and armor were confiscated: {ITEMS} items, sent directly to your inventory.", ("LORD", Name(lordName)), ("ITEMS", Count(items)))
            : Text("AF_CASTLE_UI_LORD_CARE", "[Castle aftermath] Supplies were provided to {LORD} and mistreatment was forbidden; this process concerns only this lord.", ("LORD", Name(lordName)));
    internal static string LordRecruitment(SiegeCastleLordRecruitmentBranch branch, string lordName, string status)
        => string.IsNullOrWhiteSpace(status)
            ? Text("AF_CASTLE_UI_LORD_RECRUITMENT", "[Castle aftermath] {LORD}: {BRANCH}.", ("LORD", Name(lordName)), ("BRANCH", SiegeCastleLordRecruitmentBranchProfile.Describe(branch)))
            : Text("AF_CASTLE_UI_LORD_RECRUITMENT_STATUS", "[Castle aftermath] {LORD}: {BRANCH}; {STATUS}", ("LORD", Name(lordName)), ("BRANCH", SiegeCastleLordRecruitmentBranchProfile.Describe(branch)), ("STATUS", status.Trim()));
    internal static string LordSale(string lordName, int gold)
        => Text("AF_CASTLE_UI_LORD_SALE", "[Castle aftermath] {LORD} was handed to the ransom broker for {GOLD} denars at the current native tavern ransom price; the lord was released from captivity and left the scene.",
            ("LORD", Name(lordName)), ("GOLD", Count(gold)));
    internal static string ExecutionQueued(string lordName)
        => Text("AF_CASTLE_UI_EXECUTION_QUEUED", "[Castle aftermath] Execution was ordered for {LORD}; opening the native confirmation. Cancelling will not change the lord's life or captivity status.", ("LORD", Name(lordName)));
    internal static string ExecutionFailed(string lordName)
        => Text("AF_CASTLE_UI_EXECUTION_FAILED", "[Castle aftermath] Could not open the native execution confirmation for {LORD}; no death was applied. Try again later; details were logged.", ("LORD", Name(lordName)));
    internal static string ExecutionCompleted(string lordName, bool sceneDeathApplied)
        => sceneDeathApplied
            ? Text("AF_CASTLE_UI_EXECUTION_COMPLETED", "[Castle aftermath] You confirmed the execution of {LORD}, who fell in the castle scene. The native character death has been persisted; relationships and clan consequences are handled by the native campaign system.", ("LORD", Name(lordName)))
            : Text("AF_CASTLE_UI_EXECUTION_COMPLETED_NO_SCENE", "[Castle aftermath] You confirmed the execution of {LORD}; the scene death animation failed, but the death has been persisted. Relationships and clan consequences are handled by the native campaign system.", ("LORD", Name(lordName)));
    internal static string RecruitmentCapacity(int requested, int staged, int freeSlots)
        => Math.Max(0, staged) <= 0
            ? Text("AF_CASTLE_UI_CAPACITY_NONE", "[Castle aftermath] No main-party slots are available for recruitment ({FREE} free, reserved by earlier recruitment groups). This recruitment was not staged; free slots or choose another disposition.", ("FREE", Count(freeSlots)))
            : Text("AF_CASTLE_UI_CAPACITY_TRIM", "[Castle aftermath] The main party has insufficient free slots. This recruitment was reduced from {REQUESTED} to {STAGED}; the other prisoners remain unassigned and can be handled separately.", ("REQUESTED", Count(requested)), ("STAGED", Count(staged)));
}
