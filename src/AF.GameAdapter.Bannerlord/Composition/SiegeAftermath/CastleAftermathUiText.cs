using System;
using System.Text.RegularExpressions;
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
            ("SENDER", sender ?? Text("AF_CASTLE_UI_LETTER_SENDER", "a clan member")),
            ("RECIPIENT", recipient ?? Text("AF_CASTLE_UI_LETTER_RECIPIENT", "the clan leader")));
    internal static string NoUnassigned => Text("AF_CASTLE_UI_NO_UNASSIGNED", "[Castle aftermath] No regular prisoners remain unassigned. To revise the previous plan, explicitly say you have changed your mind, want to start over, or overturn the previous arrangements.");
    internal static string SlaughterStarted(int affected)
        => Text("AF_CASTLE_UI_SLAUGHTER_STARTED", "[Castle aftermath] Formation 1 has begun attacking the {COUNT} selected prisoners; only prisoners who actually die in the scene are removed from the roster.", ("COUNT", affected.ToString()));
    internal static string Staged(SiegeCastleActionKind action, int staged, int unassigned)
        => Text("AF_CASTLE_UI_STAGED", "[Castle aftermath] Provisionally assigned {COUNT} prisoners to '{ACTION}'; {UNASSIGNED} remain unassigned. The plan is executed when you leave.",
            ("ACTION", Disposition(action)), ("COUNT", staged.ToString()), ("UNASSIGNED", unassigned.ToString()));
    internal static string DeferredFailed(SiegeCastleActionKind action, int count, string reasonCode)
        => Text("AF_CASTLE_UI_DEFERRED_FAILED", "[Castle aftermath] Resolution on departure failed: no prisoners in '{ACTION}' ({COUNT}) were affected. Reason: {REASON}. These prisoners remain captives.",
            ("ACTION", Disposition(action)), ("COUNT", count.ToString()), ("REASON", DeferredReason(reasonCode)));
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
            ? Text("AF_CASTLE_UI_LORD_RECRUITMENT", "[Castle aftermath] {LORD}: {BRANCH}.", ("LORD", Name(lordName)), ("BRANCH", RecruitmentBranch(branch)))
            : Text("AF_CASTLE_UI_LORD_RECRUITMENT_STATUS", "[Castle aftermath] {LORD}: {BRANCH}; {STATUS}", ("LORD", Name(lordName)), ("BRANCH", RecruitmentBranch(branch)), ("STATUS", RecruitmentStatus(branch, status.Trim())));
    internal static string LordSale(string lordName, int gold)
        => Text("AF_CASTLE_UI_LORD_SALE", "[Castle aftermath] {LORD} was handed to the ransom broker for {GOLD} denars at the current native tavern ransom price; the lord was released from captivity and left the scene.",
            ("LORD", Name(lordName)), ("GOLD", Count(gold)));
    internal static string LordSaleFailed(string lordName, string reasonCode)
        => Text("AF_CASTLE_UI_LORD_SALE_FAILED", "[Castle aftermath] Could not sell {LORD}; no gold or final disposition was applied. {REASON}", ("LORD", Name(lordName)), ("REASON", SaleReason(reasonCode)));
    internal static string ExecutionQueued(string lordName)
        => Text("AF_CASTLE_UI_EXECUTION_QUEUED", "[Castle aftermath] Execution was ordered for {LORD}; opening the native confirmation. Cancelling will not change the lord's life or captivity status.", ("LORD", Name(lordName)));
    internal static string ExecutionFailed(string lordName)
        => Text("AF_CASTLE_UI_EXECUTION_FAILED", "[Castle aftermath] Could not open the native execution confirmation for {LORD}; no death was applied. Try again later; details were logged.", ("LORD", Name(lordName)));
    internal static string ExecutionCancelled(string lordName)
        => Text("AF_CASTLE_UI_EXECUTION_CANCELLED", "[Castle aftermath] Execution of {LORD} was cancelled; the lord remains alive and captive, and you may negotiate again.", ("LORD", Name(lordName)));
    internal static string ExecutionPersistenceFailed(string lordName)
        => Text("AF_CASTLE_UI_EXECUTION_PERSISTENCE_FAILED", "[Castle aftermath] The native execution confirmation for {LORD} ended, but the native game did not record the character's death. This disposition had no effect; you may retry later.", ("LORD", Name(lordName)));
    internal static string ExecutionFinalizationPending(string lordName)
        => Text("AF_CASTLE_UI_EXECUTION_FINALIZATION_PENDING", "[Castle aftermath] The execution death marker for {LORD} was recorded, but native character finalization is still pending. Further disposition and normal departure are unavailable in this scene.", ("LORD", Name(lordName)));
    internal static string ExecutionConfirmationPending(string lordName)
        => Text("AF_CASTLE_UI_EXECUTION_CONFIRMATION_PENDING", "[Castle aftermath] The native execution confirmation for {LORD} is still open; confirm or cancel it first.", ("LORD", Name(lordName)));
    internal static string ExecutionCompleted(string lordName, bool sceneDeathApplied)
        => sceneDeathApplied
            ? Text("AF_CASTLE_UI_EXECUTION_COMPLETED", "[Castle aftermath] You confirmed the execution of {LORD}, who fell in the castle scene. The native character death has been persisted; relationships and clan consequences are handled by the native campaign system.", ("LORD", Name(lordName)))
            : Text("AF_CASTLE_UI_EXECUTION_COMPLETED_NO_SCENE", "[Castle aftermath] You confirmed the execution of {LORD}; the scene death animation failed, but the death has been persisted. Relationships and clan consequences are handled by the native campaign system.", ("LORD", Name(lordName)));
    internal static string RecruitmentCapacity(int requested, int staged, int freeSlots)
        => Math.Max(0, staged) <= 0
            ? Text("AF_CASTLE_UI_CAPACITY_NONE", "[Castle aftermath] No main-party slots are available for recruitment ({FREE} free, reserved by earlier recruitment groups). This recruitment was not staged; free slots or choose another disposition.", ("FREE", Count(freeSlots)))
            : Text("AF_CASTLE_UI_CAPACITY_TRIM", "[Castle aftermath] The main party has insufficient free slots. This recruitment was reduced from {REQUESTED} to {STAGED}; the other prisoners remain unassigned and can be handled separately.", ("REQUESTED", Count(requested)), ("STAGED", Count(staged)));

    private static string Disposition(SiegeCastleActionKind action) => action switch
    {
        SiegeCastleActionKind.ReleasePrisoners => Text("AF_CASTLE_UI_ACTION_RELEASE", "Release regular prisoners"),
        SiegeCastleActionKind.SellPrisoners => Text("AF_CASTLE_UI_ACTION_SELL", "Sell regular prisoners at native tavern prices"),
        SiegeCastleActionKind.RecruitPrisonersVoluntary => Text("AF_CASTLE_UI_ACTION_RECRUIT_VOLUNTARY", "Voluntarily recruit regular prisoners"),
        SiegeCastleActionKind.RecruitPrisonersForced => Text("AF_CASTLE_UI_ACTION_RECRUIT_FORCED", "Forcibly recruit regular prisoners"),
        SiegeCastleActionKind.LaborPrisonersVoluntary => Text("AF_CASTLE_UI_ACTION_LABOR_VOLUNTARY", "Voluntary penal labor"),
        SiegeCastleActionKind.LaborPrisonersForced => Text("AF_CASTLE_UI_ACTION_LABOR_FORCED", "Forced penal labor"),
        SiegeCastleActionKind.RepairCastleLaborVoluntary => Text("AF_CASTLE_UI_ACTION_REPAIR_VOLUNTARY", "Voluntary castle repair labor"),
        SiegeCastleActionKind.RepairCastleLaborForced => Text("AF_CASTLE_UI_ACTION_REPAIR_FORCED", "Forced castle repair labor"),
        SiegeCastleActionKind.InstructorPrisonersVoluntary => Text("AF_CASTLE_UI_ACTION_INSTRUCTOR_VOLUNTARY", "Voluntary instructor service"),
        SiegeCastleActionKind.InstructorPrisonersForced => Text("AF_CASTLE_UI_ACTION_INSTRUCTOR_FORCED", "Forced instructor service"),
        SiegeCastleActionKind.SlaughterPrisoners => Text("AF_CASTLE_UI_ACTION_SLAUGHTER", "Slaughter regular prisoners in the scene"),
        _ => Text("AF_CASTLE_UI_ACTION_UNKNOWN", "No disposition specified")
    };

    private static string RecruitmentBranch(SiegeCastleLordRecruitmentBranch branch) => branch switch
    {
        SiegeCastleLordRecruitmentBranch.ClanLeaderJoinPlayerKingdom => Text("AF_CASTLE_UI_BRANCH_KINGDOM", "The clan leader brings the entire clan into the player's kingdom"),
        SiegeCastleLordRecruitmentBranch.ClanLeaderRequestRulerAudience => Text("AF_CASTLE_UI_BRANCH_AUDIENCE", "The clan leader asks the player for an audience with their ruler"),
        SiegeCastleLordRecruitmentBranch.ClanLeaderSupportPlayerClaim => Text("AF_CASTLE_UI_BRANCH_CLAIM", "The clan leader expresses support for crowning the independent player"),
        SiegeCastleLordRecruitmentBranch.IntroduceClanLeaderByLetter => Text("AF_CASTLE_UI_BRANCH_LETTER", "A clan member writes an introduction letter to their clan leader"),
        SiegeCastleLordRecruitmentBranch.JoinPlayerAsCompanion => Text("AF_CASTLE_UI_BRANCH_COMPANION", "A clan member leaves their former clan and becomes the player's companion"),
        _ => Text("AF_CASTLE_UI_BRANCH_UNKNOWN", "Lord recruitment branch is not yet determined")
    };

    private static string DeferredReason(string reasonCode)
    {
        string reason = (reasonCode ?? string.Empty).Trim();
        switch (reason)
        {
            case SiegeCastlePrisonerDispositionProfile.PartyCapacityFullReason:
                return Text("AF_CASTLE_UI_DEFERRED_CAPACITY", "The main party is full");
            case SiegeCastlePrisonerDispositionProfile.RosterUnavailableReason:
                return Text("AF_CASTLE_UI_DEFERRED_ROSTER", "The prisoner or troop roster is unavailable");
            case SiegeCastlePrisonerDispositionProfile.NoMatchingRegularPrisonersReason:
                return Text("AF_CASTLE_UI_DEFERRED_NO_MATCH", "The roster has no remaining regular prisoners belonging to this group");
            case SiegeCastlePrisonerDispositionProfile.NoSellableRegularPrisonersReason:
                return Text("AF_CASTLE_UI_DEFERRED_NO_SELLABLE", "The roster has no remaining sellable regular prisoners belonging to this group");
            case SiegeCastlePrisonerDispositionProfile.UnsupportedDeferredTerminalActionReason:
                return Text("AF_CASTLE_UI_DEFERRED_UNSUPPORTED", "This staged disposition cannot currently be resolved on departure");
        }
        if (reason.StartsWith(SiegeCastlePrisonerDispositionProfile.RemovedForReasonPrefix, StringComparison.OrdinalIgnoreCase))
            return Text("AF_CASTLE_UI_DEFERRED_NO_MATCH", "The roster has no remaining regular prisoners belonging to this group");
        if (reason.StartsWith(SiegeCastlePrisonerDispositionProfile.ExceptionReasonPrefix, StringComparison.OrdinalIgnoreCase))
            return Text("AF_CASTLE_UI_DEFERRED_EXCEPTION", "An exception occurred during resolution; details were logged");
        return Text("AF_CASTLE_UI_DEFERRED_OTHER", "The disposition did not take effect; details were logged");
    }

    private static string SaleReason(string reasonCode) => (reasonCode ?? string.Empty).Trim() switch
    {
        "lord_sale_roster_unavailable" => Text("AF_CASTLE_UI_SALE_ROSTER", "The main party's prisoner roster is unavailable."),
        "lord_sale_target_invalid" => Text("AF_CASTLE_UI_SALE_INVALID", "The target is dead or cannot be ransomed."),
        "lord_not_selected_main_party_prisoner" => Text("AF_CASTLE_UI_SALE_NOT_SELECTED", "The target is no longer a selected lord held by the player's main party."),
        "lord_prisoner_locked_from_ransom" => Text("AF_CASTLE_UI_SALE_LOCKED", "A native quest or roster lock prevents this lord from being ransomed, including at a tavern."),
        "ransom_value_model_unavailable" => Text("AF_CASTLE_UI_SALE_MODEL", "The native ransom valuation model is unavailable."),
        "vanilla_lord_sale_did_not_release_target" => Text("AF_CASTLE_UI_SALE_NOT_RELEASED", "The native ransom action did not release this lord from captivity."),
        "" => Text("AF_CASTLE_UI_SALE_UNAVAILABLE", "The target cannot currently be ransomed."),
        _ => Text("AF_CASTLE_UI_SALE_OTHER", "The native ransom action failed; details were logged.")
    };

    private static readonly Regex LetterQueued = new Regex("^引见信已写好，将在 (?<days>[0-9]+) 天后直接送达族长 (?<recipient>.+)；不会生成AF信使部队，当前领主仍保持俘虏身份。$", RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100));

    private static string RecruitmentStatus(SiegeCastleLordRecruitmentBranch branch, string status)
    {
        const string cleanupKingdom = " 政治转投已经完成，但俘虏或场景清理未完全完成，详情已写入日志。";
        const string cleanupCompanion = " 同伴加入已经完成，但场景清理未完全完成，详情已写入日志。";
        if (status.EndsWith(cleanupKingdom, StringComparison.Ordinal))
            return Text("AF_CASTLE_UI_STATUS_KINGDOM_CLEANUP", "{STATUS} Political allegiance changed, but prisoner or scene cleanup was incomplete; details were logged.", ("STATUS", RecruitmentStatusCore(branch, status.Substring(0, status.Length - cleanupKingdom.Length))));
        if (status.EndsWith(cleanupCompanion, StringComparison.Ordinal))
            return Text("AF_CASTLE_UI_STATUS_COMPANION_CLEANUP", "{STATUS} Companion recruitment completed, but scene cleanup was incomplete; details were logged.", ("STATUS", RecruitmentStatusCore(branch, status.Substring(0, status.Length - cleanupCompanion.Length))));
        return RecruitmentStatusCore(branch, status);
    }

    private static string RecruitmentStatusCore(SiegeCastleLordRecruitmentBranch branch, string status)
    {
        // These exact forms are produced by the bridge; unknown text remains authoritative.
        switch (status)
        {
            case "目标领主无效或已经无法处置。": return Text("AF_CASTLE_UI_STATUS_INVALID", "The target lord is invalid or can no longer be handled.");
            case "该领主不在本次带入城堡处置的俘虏名单中。": return Text("AF_CASTLE_UI_STATUS_NOT_SELECTED", "This lord is not among the prisoners selected for this castle aftermath.");
            case "领主收编分支尚未明确。": return Text("AF_CASTLE_UI_STATUS_UNKNOWN", "The lord recruitment branch is not yet determined.");
            case "AF奖励系统当前不可用。": return Text("AF_CASTLE_UI_STATUS_REWARD_UNAVAILABLE", "The AF reward system is unavailable.");
            case "未找到可接收引见信的在世族长。": return Text("AF_CASTLE_UI_STATUS_NO_LEADER", "No living clan leader is available to receive the introduction letter.");
            case "领主或族长缺少稳定角色标识。": return Text("AF_CASTLE_UI_STATUS_NO_ID", "The lord or clan leader has no stable character identifier.");
            case "请求玩家将自己带去面见玩家所属王国的统治者；家族归属和俘虏状态暂未改变。": return Text("AF_CASTLE_UI_STATUS_AUDIENCE", "Asked the player to take them to meet the ruler of the player's kingdom; clan allegiance and captivity remain unchanged.");
            case "表达了支持玩家称王的政治意向；不会在玩家尚无王国时凭空创建国家或转移家族。": return Text("AF_CASTLE_UI_STATUS_CLAIM", "Expressed support for crowning the player; no kingdom is created or clan transferred while the player has no kingdom.");
        }
        const string exceptionPrefix = "执行领主收编分支时发生异常：";
        if (status.StartsWith(exceptionPrefix, StringComparison.Ordinal))
            return Text("AF_CASTLE_UI_STATUS_EXCEPTION", "An exception occurred while applying lord recruitment: {ERROR}", ("ERROR", status.Substring(exceptionPrefix.Length)));
        Match letter;
        try { letter = LetterQueued.Match(status); }
        catch (RegexMatchTimeoutException) { return status; }
        if (letter.Success)
            return Text("AF_CASTLE_UI_STATUS_LETTER_QUEUED", "The introduction letter is ready and will reach clan leader {RECIPIENT} in {DAYS} days; no AF courier party is created, and the current lord remains a prisoner.", ("RECIPIENT", letter.Groups["recipient"].Value), ("DAYS", letter.Groups["days"].Value));
        return branch == SiegeCastleLordRecruitmentBranch.ClanLeaderJoinPlayerKingdom
            ? EconomyUiTexts.FactionIdentityStatusForDisplay(status)
            : SocialUiLocalization.RecruitmentStatusForDisplay(status);
    }
}
