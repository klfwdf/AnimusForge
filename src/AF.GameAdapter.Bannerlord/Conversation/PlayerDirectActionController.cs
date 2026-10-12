using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace AnimusForge;

internal enum PlayerDirectActionKind
{
    Gold, Item, FixedAsset, Troops, Prisoners, Debt, ResolveDebt, Companion, Family,
    Follow, StopFollow, LordsHall, AcceptIssue, TurnInQuest,
    GoToSettlement, PatrolSettlement, FollowTarget, AttackTarget, ClearHideout, MergeToPlayer
}

internal enum PlayerDirectActionContext { Scene, WorldMap, Restricted }

internal sealed class PlayerDirectActionChoice
{
    internal readonly string Token, Label;
    // Live identity check is supplied by the candidate's authoritative owner, not a name lookup.
    internal readonly Func<bool> IsCurrent;
    internal readonly IReadOnlyList<string> BatchAssetTokens;
    internal readonly string TargetType;
    internal readonly int DefaultDays;
    internal PlayerDirectActionChoice(string token, string label, Func<bool> isCurrent = null, IEnumerable<string> batchAssetTokens = null,
        string targetType = null, int defaultDays = 1)
    { Token = token; Label = label; IsCurrent = isCurrent; BatchAssetTokens = batchAssetTokens?.ToArray(); TargetType = targetType; DefaultDays = defaultDays; }
}

internal static class PlayerDirectActionProtocol
{
    internal static IReadOnlyList<KeyValuePair<PlayerDirectActionKind, string>> Actions =>
        new[] {
            Pair(PlayerDirectActionKind.Gold, ConversationUiLocalization.Text("AF_CONV_UI_Direct_001", "NPC gives me gold")), Pair(PlayerDirectActionKind.Item, ConversationUiLocalization.Text("AF_CONV_UI_Direct_002", "NPC gives me items")),
            Pair(PlayerDirectActionKind.FixedAsset, ConversationUiLocalization.Text("AF_CONV_UI_Direct_003", "NPC transfers fixed assets to me")), Pair(PlayerDirectActionKind.Troops, ConversationUiLocalization.Text("AF_CONV_UI_Direct_004", "NPC transfers troops to me")),
            Pair(PlayerDirectActionKind.Prisoners, ConversationUiLocalization.Text("AF_CONV_UI_Direct_005", "NPC transfers prisoners to me")), Pair(PlayerDirectActionKind.Debt, ConversationUiLocalization.Text("AF_CONV_UI_Direct_006", "Record the player's debt or promise")),
            Pair(PlayerDirectActionKind.ResolveDebt, ConversationUiLocalization.Text("AF_CONV_UI_Direct_007", "Clear a debt")), Pair(PlayerDirectActionKind.Companion, ConversationUiLocalization.Text("AF_CONV_UI_Direct_008", "Make the current hero a companion")),
            Pair(PlayerDirectActionKind.Family, ConversationUiLocalization.Text("AF_CONV_UI_Direct_009", "Make the current hero a clan member")), Pair(PlayerDirectActionKind.Follow, ConversationUiLocalization.Text("AF_CONV_UI_Direct_010", "Follow me")),
            Pair(PlayerDirectActionKind.StopFollow, ConversationUiLocalization.Text("AF_CONV_UI_Direct_011", "Stop following")), Pair(PlayerDirectActionKind.LordsHall, ConversationUiLocalization.Text("AF_CONV_UI_Direct_012", "Enter the lord's hall")),
            Pair(PlayerDirectActionKind.AcceptIssue, ConversationUiLocalization.Text("AF_CONV_UI_Direct_013", "Accept quest")), Pair(PlayerDirectActionKind.TurnInQuest, ConversationUiLocalization.Text("AF_CONV_UI_Direct_014", "Turn in quest")),
            Pair(PlayerDirectActionKind.GoToSettlement, MapText("Go", "Go to a location")), Pair(PlayerDirectActionKind.PatrolSettlement, MapText("Patrol", "Patrol a location")),
            Pair(PlayerDirectActionKind.FollowTarget, MapText("FollowTarget", "Follow a world-map target")), Pair(PlayerDirectActionKind.AttackTarget, MapText("Attack", "Attack a world-map target")),
            Pair(PlayerDirectActionKind.ClearHideout, MapText("ClearHideout", "Clear a hideout")), Pair(PlayerDirectActionKind.MergeToPlayer, MapText("Merge", "Return to the player's party")) };
    internal static string MapText(string key, string fallback, params (string Name, object Value)[] variables) =>
        ConversationUiLocalization.Text("AF_CONV_UI_Direct_Map_" + key, fallback, variables);
    private static KeyValuePair<PlayerDirectActionKind, string> Pair(PlayerDirectActionKind kind, string label) => new(kind, label);
    internal static string ActionName(PlayerDirectActionKind kind, PlayerDirectActionContext context) =>
        kind == PlayerDirectActionKind.StopFollow && context == PlayerDirectActionContext.WorldMap
            ? MapText("Stop", "Stop world-map orders") : Actions.Single(x => x.Key == kind).Value;
    internal static bool IsWorldMap(PlayerDirectActionKind kind, PlayerDirectActionContext context) =>
        kind is PlayerDirectActionKind.GoToSettlement or PlayerDirectActionKind.PatrolSettlement or PlayerDirectActionKind.FollowTarget
            or PlayerDirectActionKind.AttackTarget or PlayerDirectActionKind.ClearHideout or PlayerDirectActionKind.MergeToPlayer
        || (context == PlayerDirectActionContext.WorldMap
            && (kind == PlayerDirectActionKind.Follow || kind == PlayerDirectActionKind.StopFollow));
    internal static string WorldMapKind(PlayerDirectActionKind kind) => kind switch {
        PlayerDirectActionKind.Follow or PlayerDirectActionKind.FollowTarget => "FOLLOW",
        PlayerDirectActionKind.StopFollow => "STOP", PlayerDirectActionKind.GoToSettlement => "GO_TO_SETTLEMENT",
        PlayerDirectActionKind.PatrolSettlement => "PATROL_SETTLEMENT", PlayerDirectActionKind.AttackTarget => "ATTACK",
        PlayerDirectActionKind.ClearHideout => "CLEAR_HIDEOUT", PlayerDirectActionKind.MergeToPlayer => "MERGE_TO_PLAYER", _ => ""
    };
    internal static string TagTemplate(PlayerDirectActionKind kind, PlayerDirectActionContext context = PlayerDirectActionContext.Scene) => kind switch
    {
        PlayerDirectActionKind.Gold => "[ACTION:GIVE_ASSET:GOLD:<amount>]",
        PlayerDirectActionKind.Item => "[ACTION:GIVE_ASSET:<item_id>:<quantity|ALL>]",
        PlayerDirectActionKind.FixedAsset => "[ACTION:GIVE_ASSET:<asset_id>:1]",
        PlayerDirectActionKind.Troops => "[ATT:<index|ALL>:<quantity|ALL>]",
        PlayerDirectActionKind.Prisoners => "[ATP:<index|ALL>:<quantity|ALL>]",
        PlayerDirectActionKind.Debt => "[AD:<value>:<days>:P:<note>]",
        PlayerDirectActionKind.ResolveDebt => "[ADP:<debt_id>]",
        PlayerDirectActionKind.Follow when context == PlayerDirectActionContext.WorldMap => "[ACTION:WORLDMAP_ORDER:FOLLOW:hero:<player_hero_id>:<days>]",
        PlayerDirectActionKind.StopFollow when context == PlayerDirectActionContext.WorldMap => "[ACTION:WORLDMAP_ORDER:STOP]",
        PlayerDirectActionKind.GoToSettlement => "[ACTION:WORLDMAP_ORDER:GO_TO_SETTLEMENT:settlement:<id>:<days>]",
        PlayerDirectActionKind.PatrolSettlement => "[ACTION:WORLDMAP_ORDER:PATROL_SETTLEMENT:settlement:<id>:<days>]",
        PlayerDirectActionKind.FollowTarget => "[ACTION:WORLDMAP_ORDER:FOLLOW:<hero|party>:<id>:<days>]",
        PlayerDirectActionKind.AttackTarget => "[ACTION:WORLDMAP_ORDER:ATTACK:<hero|party|settlement>:<id>:<days>:<AI|FORCE>]",
        PlayerDirectActionKind.ClearHideout => "[ACTION:WORLDMAP_ORDER:CLEAR_HIDEOUT:settlement:<id>:<days>]",
        PlayerDirectActionKind.MergeToPlayer => "[ACTION:WORLDMAP_ORDER:MERGE_TO_PLAYER:<days>]",
        _ => TryBuild(kind, null, null, 0, null, out string tag) ? tag : ""
    };
    internal static bool TryBuildWorldMap(PlayerDirectActionKind kind, PlayerDirectActionChoice choice, int days, string mode, bool replace, out string tag)
    {
        tag = null;
        string command = WorldMapKind(kind);
        if (command == "STOP") { tag = "[ACTION:WORLDMAP_ORDER:STOP]"; return !replace; }
        if (command.Length == 0 || days <= 0) return false;
        string arguments;
        if (command == "MERGE_TO_PLAYER") arguments = days.ToString(CultureInfo.InvariantCulture);
        else
        {
            if (choice == null || !SafeToken(choice.Token) || choice.Token.Contains(":")) return false;
            bool targetAllowed = command == "FOLLOW" ? choice.TargetType == "hero" || choice.TargetType == "party"
                : command == "ATTACK" ? choice.TargetType == "hero" || choice.TargetType == "party" || choice.TargetType == "settlement"
                : choice.TargetType == "settlement";
            if (!targetAllowed || (kind == PlayerDirectActionKind.Follow && choice.TargetType != "hero")) return false;
            arguments = choice.TargetType + ":" + choice.Token + ":" + days.ToString(CultureInfo.InvariantCulture);
            if (command == "ATTACK")
            {
                if (mode != "AI" && mode != "FORCE") return false;
                arguments += ":" + mode;
            }
        }
        tag = (replace ? "[ACTION:WORLDMAP_ORDER:STOP] " : "") + "[ACTION:WORLDMAP_ORDER:" + command + ":" + arguments + "]";
        return true;
    }
    private static bool SafeToken(string token) => !string.IsNullOrWhiteSpace(token) && token.IndexOfAny(new[] {'[', ']', '\r', '\n'}) < 0;
    internal static bool TryBuild(PlayerDirectActionKind kind, string token, string quantity, int days, string note, out string tag)
    {
        tag = null;
        switch (kind)
        {
            case PlayerDirectActionKind.Gold:
            case PlayerDirectActionKind.Item:
            case PlayerDirectActionKind.FixedAsset:
                string asset = kind == PlayerDirectActionKind.Gold ? "GOLD" : token;
                string count = kind == PlayerDirectActionKind.FixedAsset ? "1" : quantity;
                if (!SafeToken(asset) || !TransferQuantitySpec.TryParse(count, out var amount)
                    || (kind == PlayerDirectActionKind.Gold && amount.IsAll)
                    || TransferQuantitySpec.IsAllValue(asset)) return false;
                tag = GiveAssetTagCodec.Prefix + asset + ":" + (amount.IsAll ? "ALL" : amount.Amount.ToString(CultureInfo.InvariantCulture)) + "]";
                return GiveAssetTagCodec.TryParseWhole(tag, out var parsed) && parsed.AssetToken == asset;
            case PlayerDirectActionKind.Troops:
            case PlayerDirectActionKind.Prisoners:
                if (!TransferQuantitySpec.TryParse(quantity, out var rosterAmount)) return false;
                bool all = TransferQuantitySpec.IsAllValue(token);
                if ((all && !rosterAmount.IsAll) || (!all && (!int.TryParse(token, out int index) || index <= 0))) return false;
                tag = (kind == PlayerDirectActionKind.Troops ? "[ATT:" : "[ATP:") + token + ":"
                    + (rosterAmount.IsAll ? "ALL" : rosterAmount.Amount.ToString(CultureInfo.InvariantCulture)) + "]";
                return true;
            case PlayerDirectActionKind.Debt:
                if (!int.TryParse(quantity, out int value) || value <= 0 || days < 0
                    || (note ?? "").IndexOfAny(new[] {'[', ']', '\r', '\n'}) >= 0) return false;
                tag = "[AD:" + value.ToString(CultureInfo.InvariantCulture) + ":" + days.ToString(CultureInfo.InvariantCulture) + ":P:" + (note ?? "") + "]";
                return true;
            case PlayerDirectActionKind.ResolveDebt:
                if (!SafeToken(token) || token.Contains(":")) return false;
                tag = "[ADP:" + token + "]"; return true;
            case PlayerDirectActionKind.Companion: tag = "[A:H_J_P_P_C]"; return true;
            case PlayerDirectActionKind.Family: tag = "[A:H_J_P_P_L]"; return true;
            case PlayerDirectActionKind.Follow: tag = "[ACTION:SCENE_FOLLOW_PLAYER]"; return true;
            case PlayerDirectActionKind.StopFollow: tag = "[ACTION:SCENE_STOP_FOLLOW]"; return true;
            case PlayerDirectActionKind.LordsHall: tag = "[ACTION:OPEN_LORDS_HALL]"; return true;
            case PlayerDirectActionKind.AcceptIssue: tag = "[ACTION:ISSUE_ACCEPT_SELF]"; return true;
            case PlayerDirectActionKind.TurnInQuest: tag = "[ACTION:QUEST_TURN_IN]"; return true;
            default: return false;
        }
    }
}

// Main-thread, click-only inquiry coordinator. No Tick work, persistent state, chat, LLM or audio.
internal sealed class PlayerDirectActionController
{
    private long _revision;
    private Session _active;
    private sealed class Session
    {
        internal string Title, TargetName, TagInfo = "";
        internal bool Closed;
        internal Func<bool> IsCurrent;
        internal Func<PlayerDirectActionKind, string> Unavailable;
        internal Func<PlayerDirectActionKind, List<PlayerDirectActionChoice>> Candidates;
        internal Func<PlayerDirectActionKind, PlayerDirectActionChoice, bool> Revalidate;
        internal Action<PlayerDirectActionKind, PlayerDirectActionChoice, string> Execute;
        internal Action Finish;
        internal PlayerDirectActionContext Context;
        internal Func<string, string> ValidateWorldMap;
    }

    internal void Open(string name, Func<bool> isCurrent, Func<PlayerDirectActionKind, string> unavailable,
        Func<PlayerDirectActionKind, List<PlayerDirectActionChoice>> candidates,
        Func<PlayerDirectActionKind, PlayerDirectActionChoice, bool> revalidate,
        Action<PlayerDirectActionKind, PlayerDirectActionChoice, string> execute, Action onFinished,
        PlayerDirectActionContext context = PlayerDirectActionContext.Scene, Func<string, string> validateWorldMap = null)
    {
        if (_active != null) Close(_active);
        var s = _active = new Session { TargetName = name, Title = ConversationUiLocalization.Text("AF_CONV_UI_Direct_015", "Manual tags — ") + name, IsCurrent = isCurrent, Unavailable = unavailable,
            Candidates = candidates, Revalidate = revalidate, Execute = execute, Finish = onFinished, Context = context, ValidateWorldMap = validateWorldMap };
        Advance(s, () => {
            if (!isCurrent()) { Reject(s, ConversationUiLocalization.Text("AF_CONV_UI_Direct_016", "The target or conversation has expired. Reopen Manual tags.")); return; }
            var elements = PlayerDirectActionProtocol.Actions.Select(pair => {
                string reason = unavailable(pair.Key);
                string tagInfo = ConversationUiLocalization.Text("AF_CONV_UI_Direct_TagTemplate", "Tag: {TAG}", ("TAG", PlayerDirectActionProtocol.TagTemplate(pair.Key, s.Context)));
                string label = pair.Key == PlayerDirectActionKind.StopFollow
                    ? PlayerDirectActionProtocol.ActionName(pair.Key, s.Context) : pair.Value;
                return new InquiryElement(pair.Key, label, null, string.IsNullOrEmpty(reason), tagInfo + "\n" + (reason ?? ConversationUiLocalization.Text("AF_CONV_UI_Direct_017", "Execute existing tag rules without calling AI.")));
            }).ToList();
            long stamp = ++_revision;
            MBInformationManager.ShowMultiSelectionInquiry(new MultiSelectionInquiryData(s.Title,
                ConversationUiLocalization.Text("AF_CONV_UI_Direct_018", "Select a tag. Parameter fields open after selection; tags without parameters run on confirmation. Only the current target is affected. Eligibility, quantity, costs and side effects follow the existing mechanics."), elements, true, 1, 1, ConversationUiLocalization.Text("AF_CONV_UI_Direct_019", "OK"), ConversationUiLocalization.Text("AF_CONV_UI_Direct_020", "Cancel"),
                selected => {
                    if (!Consume(s, stamp)) return;
                    var item = selected?.Count == 1 ? selected[0] : null;
                    if (item?.Identifier is not PlayerDirectActionKind kind || !elements.Contains(item) || !item.IsEnabled) { Close(s); return; }
                    Advance(s, () => Begin(s, kind));
                }, _ => { if (Consume(s, stamp)) Close(s); }), pauseGameActiveState: true);
        });
    }

    private bool Consume(Session s, long stamp)
    {
        if (s.Closed || !ReferenceEquals(_active, s) || stamp != _revision) return false;
        ++_revision;
        if (!s.IsCurrent()) { Reject(s, ConversationUiLocalization.Text("AF_CONV_UI_Direct_021", "The target or conversation has expired. No action was executed.")); return false; }
        return true;
    }
    private void Close(Session s)
    {
        if (s.Closed) return;
        s.Closed = true;
        if (ReferenceEquals(_active, s)) _active = null;
        ++_revision;
        s.Finish?.Invoke();
    }
    private void Reject(Session s, string text) { Close(s); InformationManager.DisplayMessage(new InformationMessage(text)); }
    private void Advance(Session s, Action next)
    {
        try { next(); }
        catch (Exception ex)
        {
            Logger.Log("PlayerDirectActions", "inquiry failed: " + ex.Message);
            Reject(s, ConversationUiLocalization.Text("AF_CONV_UI_Direct_022", "An error occurred while reading parameters or submitting the action. Manual tags was closed without automatic retry. Check the original mechanic's state."));
        }
    }
    private void Text(Session s, string prompt, Action<string> next)
    {
        long stamp = ++_revision;
        InformationManager.ShowTextInquiry(new TextInquiryData(s.Title, s.TagInfo + prompt, true, true, ConversationUiLocalization.Text("AF_CONV_UI_Direct_019", "OK"), ConversationUiLocalization.Text("AF_CONV_UI_Direct_020", "Cancel"),
            value => { if (Consume(s, stamp)) Advance(s, () => next(value?.Trim() ?? "")); },
            () => { if (Consume(s, stamp)) Close(s); }), pauseGameActiveState: true);
    }
    private void Confirm(Session s, string message, Action next)
    {
        long stamp = ++_revision;
        InformationManager.ShowInquiry(new InquiryData(s.Title, s.TagInfo + message, true, true, ConversationUiLocalization.Text("AF_CONV_UI_Direct_023", "Confirm action"), ConversationUiLocalization.Text("AF_CONV_UI_Direct_020", "Cancel"),
            () => { if (Consume(s, stamp)) Advance(s, next); }, () => { if (Consume(s, stamp)) Close(s); }), pauseGameActiveState: true);
    }
    private void Begin(Session s, PlayerDirectActionKind kind)
    {
        string reason = s.Unavailable(kind);
        if (!string.IsNullOrEmpty(reason)) { Reject(s, reason); return; }
        s.Title += " — " + PlayerDirectActionProtocol.ActionName(kind, s.Context);
        s.TagInfo = ConversationUiLocalization.Text("AF_CONV_UI_Direct_TagTemplate", "Tag: {TAG}", ("TAG", PlayerDirectActionProtocol.TagTemplate(kind, s.Context))) + "\n";
        if (PlayerDirectActionProtocol.IsWorldMap(kind, s.Context)) { BeginWorldMap(s, kind); return; }
        switch (kind)
        {
            case PlayerDirectActionKind.Gold:
                Text(s, ConversationUiLocalization.Text("AF_CONV_UI_Direct_024", "Enter the gold amount the NPC gives you (positive integer). Insufficient funds still follow the original tag's shortfall-completion rules."), amount => Commit(s, kind, null, amount)); break;
            case PlayerDirectActionKind.Debt:
                Text(s, ConversationUiLocalization.Text("AF_CONV_UI_Direct_025", "Enter the value of the player's debt or promise (positive integer):"), amount => Text(s, ConversationUiLocalization.Text("AF_CONV_UI_Direct_026", "Enter the deadline in days (0 retains the existing unlimited-deadline meaning):"), days => {
                    if (!int.TryParse(days, out int deadline) || deadline < 0) { Reject(s, ConversationUiLocalization.Text("AF_CONV_UI_Direct_027", "The deadline must be a non-negative integer.")); return; }
                    Text(s, ConversationUiLocalization.Text("AF_CONV_UI_Direct_028", "Enter a note (no square brackets or line breaks):"), note => Commit(s, kind, null, amount, deadline, note));
                })); break;
            case PlayerDirectActionKind.Item: case PlayerDirectActionKind.FixedAsset:
            case PlayerDirectActionKind.Troops: case PlayerDirectActionKind.Prisoners: case PlayerDirectActionKind.ResolveDebt:
                Choose(s, kind); break;
            case PlayerDirectActionKind.Companion: case PlayerDirectActionKind.Family:
                Confirm(s, ConversationUiLocalization.Text("AF_CONV_UI_Direct_029", "Target: ") + s.TargetName + ConversationUiLocalization.Text("AF_CONV_UI_Direct_030", "\nThe existing tag will move this hero into the player's clan as a ")
                    + (kind == PlayerDirectActionKind.Companion ? ConversationUiLocalization.Text("AF_CONV_UI_Direct_031", "companion. ") : ConversationUiLocalization.Text("AF_CONV_UI_Direct_032", "clan member/lord. ")) + ConversationUiLocalization.Text("AF_CONV_UI_Direct_033", "All existing identity, party and ownership side effects are retained."),
                    () => Commit(s, kind, null, null)); break;
            default: Commit(s, kind, null, null); break;
        }
    }
    private static string MapText(string key, string fallback, params (string Name, object Value)[] variables) =>
        PlayerDirectActionProtocol.MapText(key, fallback, variables);

    private void BeginWorldMap(Session s, PlayerDirectActionKind kind)
    {
        if (kind == PlayerDirectActionKind.StopFollow)
        {
            Confirm(s, ConversationUiLocalization.Text("AF_CONV_UI_Direct_029", "Target: ") + s.TargetName + "\n"
                + MapText("StopWarning", "This clears the NPC's entire world-map command queue, not just following. Battles still obey the original safe-exit rules."),
                () => CommitWorldMap(s, kind, null, 0, "AI", false));
            return;
        }
        if (kind == PlayerDirectActionKind.MergeToPlayer) { WorldMapDays(s, kind, null, "AI"); return; }
        var choices = s.Candidates(kind) ?? new List<PlayerDirectActionChoice>();
        if (choices.Count == 0) { Reject(s, MapText("NoTargets", "There are no valid world-map targets for this command.")); return; }
        if (kind == PlayerDirectActionKind.Follow) { WorldMapDays(s, kind, choices.Single(), "AI"); return; }
        // Native search filters this one captured list; it never recaptures game objects per keystroke.
        var elements = choices.Select(x => new InquiryElement(x, x.Label, null, true, x.TargetType + ":" + x.Token)).ToList();
        long stamp = ++_revision;
        MBInformationManager.ShowMultiSelectionInquiry(new MultiSelectionInquiryData(s.Title,
            s.TagInfo + MapText("SelectTarget", "Select a real target. Use the name search; type and ID distinguish duplicate names."),
            elements, true, 1, 1, ConversationUiLocalization.Text("AF_CONV_UI_Direct_019", "OK"), ConversationUiLocalization.Text("AF_CONV_UI_Direct_020", "Cancel"),
            selected => {
                if (!Consume(s, stamp)) return;
                Advance(s, () => {
                    var item = selected?.Count == 1 ? selected[0] : null;
                    if (item == null || !elements.Contains(item) || item.Identifier is not PlayerDirectActionChoice choice) { Close(s); return; }
                    if (!s.Revalidate(kind, choice)) { Reject(s, MapText("Expired", "The world-map actor or target has expired. Nothing was submitted.")); return; }
                    if (kind == PlayerDirectActionKind.AttackTarget) WorldMapMode(s, kind, choice);
                    else WorldMapDays(s, kind, choice, "AI");
                });
            }, _ => { if (Consume(s, stamp)) Close(s); }, isSeachAvailable: true), pauseGameActiveState: true);
    }

    private void WorldMapMode(Session s, PlayerDirectActionKind kind, PlayerDirectActionChoice choice)
    {
        var elements = new List<InquiryElement> {
            new("AI", MapText("ModeAI", "AI (default: original attack decisions)"), null, true, "AI"),
            new("FORCE", MapText("ModeForce", "FORCE (original forced-attack rules and consequences)"), null, true, "FORCE") };
        long stamp = ++_revision;
        MBInformationManager.ShowMultiSelectionInquiry(new MultiSelectionInquiryData(s.Title, s.TagInfo + choice.Label,
            elements, true, 0, 1, ConversationUiLocalization.Text("AF_CONV_UI_Direct_019", "OK"), ConversationUiLocalization.Text("AF_CONV_UI_Direct_020", "Cancel"),
            selected => {
                if (!Consume(s, stamp)) return;
                var item = selected?.Count == 1 ? selected[0] : null;
                if (selected?.Count > 1 || (item != null && !elements.Contains(item))) { Close(s); return; }
                Advance(s, () => WorldMapDays(s, kind, choice, item?.Identifier as string ?? "AI"));
            }, _ => { if (Consume(s, stamp)) Close(s); }), pauseGameActiveState: true);
    }

    private void WorldMapDays(Session s, PlayerDirectActionKind kind, PlayerDirectActionChoice choice, string mode)
    {
        int defaultDays = choice?.DefaultDays ?? 1;
        string purpose = kind == PlayerDirectActionKind.GoToSettlement ? MapText("StayDays", "Days to stay after arrival")
            : kind == PlayerDirectActionKind.Follow || kind == PlayerDirectActionKind.FollowTarget ? MapText("FollowDays", "Days to follow after reaching the target")
            : kind == PlayerDirectActionKind.PatrolSettlement ? MapText("PatrolDays", "Days to patrol after arrival")
            : MapText("DeadlineDays", "Command deadline in days");
        Text(s, (choice?.Label ?? "") + "\n" + purpose + "\n" + MapText("DaysInput", "Enter a positive integer; leave blank for the original default ({DAYS}).", ("DAYS", defaultDays)), value => {
            int days = defaultDays;
            if (value.Length > 0 && (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out days) || days <= 0))
            { Reject(s, MapText("InvalidDays", "Days must be a positive integer within the supported integer range.")); return; }
            WorldMapQueue(s, kind, choice, days, mode);
        });
    }

    private void WorldMapQueue(Session s, PlayerDirectActionKind kind, PlayerDirectActionChoice choice, int days, string mode)
    {
        var elements = new List<InquiryElement> {
            new("append", MapText("Append", "Append (default)"), null, true, MapText("AppendHint", "Keep existing tasks and add this command at the end.")),
            new("replace", MapText("Replace", "Replace existing tasks"), null, true, MapText("ReplaceHint", "Clear the old queue first. Replacement is refused during battle, safe exit or protected external orders.")) };
        long stamp = ++_revision;
        MBInformationManager.ShowMultiSelectionInquiry(new MultiSelectionInquiryData(s.Title,
            MapText("QueueInput", "Choose append or replace. Confirming with no selection defaults to append."), elements, true, 0, 1,
            ConversationUiLocalization.Text("AF_CONV_UI_Direct_019", "OK"), ConversationUiLocalization.Text("AF_CONV_UI_Direct_020", "Cancel"),
            selected => {
                if (!Consume(s, stamp)) return;
                var item = selected?.Count == 1 ? selected[0] : null;
                if (selected?.Count > 1 || (item != null && !elements.Contains(item))) { Close(s); return; }
                Advance(s, () => ConfirmWorldMap(s, kind, choice, days, mode, item?.Identifier as string == "replace"));
            }, _ => { if (Consume(s, stamp)) Close(s); }), pauseGameActiveState: true);
    }

    private void ConfirmWorldMap(Session s, PlayerDirectActionKind kind, PlayerDirectActionChoice choice, int days, string mode, bool replace)
    {
        string description = MapText("Confirm", "Actor: {ACTOR}\nTarget: {TARGET}\nDays: {DAYS}\nMode: {MODE}\nQueue: {QUEUE}\n",
            ("ACTOR", s.TargetName), ("TARGET", choice?.Label ?? MapText("PlayerParty", "Player's party")), ("DAYS", days),
            ("MODE", kind == PlayerDirectActionKind.AttackTarget ? mode : "—"),
            ("QUEUE", replace ? MapText("Replace", "Replace existing tasks") : MapText("Append", "Append (default)")));
        if (replace) description += MapText("ReplaceHint", "Clear the old queue first. Replacement is refused during battle, safe exit or protected external orders.") + "\n";
        if (kind == PlayerDirectActionKind.GoToSettlement)
            description += MapText("GoWarning", "Original rules may convert travel to an attack if the destination is hostile or under hostile siege.") + "\n";
        if (kind == PlayerDirectActionKind.AttackTarget || kind == PlayerDirectActionKind.ClearHideout)
            description += MapText("AttackWarning", "Combat, diplomacy, losses and other consequences follow the original command rules.") + "\n";
        if (kind == PlayerDirectActionKind.MergeToPlayer)
            description += MapText("MergeWarning", "The original return/merge mechanic may transfer members and assets and dissolve the independent party.") + "\n";
        description += MapText("ExitWarning", "If a companion task party or governor expedition must be formed, this conversation and, when needed, the current scene will close automatically. Submission is not completion.");
        Confirm(s, description, () => CommitWorldMap(s, kind, choice, days, mode, replace));
    }

    private void CommitWorldMap(Session s, PlayerDirectActionKind kind, PlayerDirectActionChoice choice, int days, string mode, bool replace)
    {
        if (!s.IsCurrent() || !string.IsNullOrEmpty(s.Unavailable(kind)) || !s.Revalidate(kind, choice)
            || !PlayerDirectActionProtocol.TryBuildWorldMap(kind, choice, days, mode, replace, out string raw))
        { Reject(s, MapText("Expired", "The world-map actor or target has expired. Nothing was submitted.")); return; }
        string reason = s.ValidateWorldMap?.Invoke(raw);
        if (!string.IsNullOrEmpty(reason)) { Reject(s, reason); return; }
        Close(s);
        if (!s.IsCurrent()) { InformationManager.DisplayMessage(new InformationMessage(MapText("Expired", "The world-map actor or target has expired. Nothing was submitted."))); return; }
        s.Execute(kind, choice, raw);
    }

    private void Choose(Session s, PlayerDirectActionKind kind)
    {
        var choices = s.Candidates(kind) ?? new List<PlayerDirectActionChoice>();
        if (choices.Count == 0) { Reject(s, ConversationUiLocalization.Text("AF_CONV_UI_Direct_034", "This target has no eligible items, assets, troops, prisoners or outstanding debts under the existing mechanics.")); return; }
        var elements = choices.Select(x => new InquiryElement(x.Token, x.Label, null, true, ConversationUiLocalization.Text("AF_CONV_UI_Direct_035", "Use the existing mechanics; recheck the selected candidate before execution."))).ToList();
        long stamp = ++_revision;
        MBInformationManager.ShowMultiSelectionInquiry(new MultiSelectionInquiryData(s.Title, s.TagInfo + ConversationUiLocalization.Text("AF_CONV_UI_Direct_036", "Select:"), elements, true, 1, 1, ConversationUiLocalization.Text("AF_CONV_UI_Direct_019", "OK"), ConversationUiLocalization.Text("AF_CONV_UI_Direct_020", "Cancel"),
            selected => {
                if (!Consume(s, stamp)) return;
                Advance(s, () => {
                    var item = selected?.Count == 1 ? selected[0] : null;
                    int index = item == null ? -1 : elements.IndexOf(item);
                    if (index < 0) { Close(s); return; }
                    var choice = choices[index];
                    if (kind == PlayerDirectActionKind.FixedAsset)
                        Confirm(s, ConversationUiLocalization.Text("AF_CONV_UI_Direct_029", "Target: ") + s.Title + ConversationUiLocalization.Text("AF_CONV_UI_Direct_037", "\nTransfer asset: ") + choice.Label + ConversationUiLocalization.Text("AF_CONV_UI_Direct_038", "\nOwnership will pass to the player, subject to all existing tag checks and consequences."), () => Commit(s, kind, choice, "1"));
                    else if (kind == PlayerDirectActionKind.ResolveDebt) Commit(s, kind, choice, null);
                    else if (TransferQuantitySpec.IsAllValue(choice.Token)) Commit(s, kind, choice, "ALL");
                    else Quantity(s, kind, choice);
                });
            }, _ => { if (Consume(s, stamp)) Close(s); }), pauseGameActiveState: true);
    }
    private void Quantity(Session s, PlayerDirectActionKind kind, PlayerDirectActionChoice choice)
    {
        var elements = new List<InquiryElement> {
            new("quantity", ConversationUiLocalization.Text("AF_CONV_UI_Direct_039", "Enter quantity"), null, true, ConversationUiLocalization.Text("AF_CONV_UI_Direct_040", "Enter a positive integer; shortfall completion or actual transferred quantities follow the existing tag rules.")),
            new("ALL", ConversationUiLocalization.Text("AF_CONV_UI_Direct_041", "All of this entry"), null, true, ConversationUiLocalization.Text("AF_CONV_UI_Direct_042", "Emit ALL only for the selected entry, retaining its existing meaning.")) };
        long stamp = ++_revision;
        MBInformationManager.ShowMultiSelectionInquiry(new MultiSelectionInquiryData(s.Title, s.TagInfo + ConversationUiLocalization.Text("AF_CONV_UI_Direct_043", "Selected: ") + choice.Label, elements, true, 1, 1, ConversationUiLocalization.Text("AF_CONV_UI_Direct_019", "OK"), ConversationUiLocalization.Text("AF_CONV_UI_Direct_020", "Cancel"),
            selected => {
                if (!Consume(s, stamp)) return;
                Advance(s, () => {
                    var item = selected?.Count == 1 ? selected[0] : null;
                    if (item == null || !elements.Contains(item)) { Close(s); return; }
                    if (Equals(item.Identifier, "ALL")) Commit(s, kind, choice, "ALL");
                    else Text(s, ConversationUiLocalization.Text("AF_CONV_UI_Direct_043", "Selected: ") + choice.Label + ConversationUiLocalization.Text("AF_CONV_UI_Direct_044", "\nEnter a positive integer quantity:"), amount => Commit(s, kind, choice, amount));
                });
            }, _ => { if (Consume(s, stamp)) Close(s); }), pauseGameActiveState: true);
    }
    private void Commit(Session s, PlayerDirectActionKind kind, PlayerDirectActionChoice choice, string amount, int days = 0, string note = null)
    {
        string tag = null;
        bool valid;
        if (kind == PlayerDirectActionKind.Item && TransferQuantitySpec.IsAllValue(choice?.Token))
        {
            // GIVE_ASSET:ALL:ALL is rejected by the existing normal owner. The UI bulk
            // choice is a batch of existing per-item ALL tags, never a new tag grammar.
            var tags = new List<string>();
            valid = choice.BatchAssetTokens?.Count > 0;
            foreach (string token in choice.BatchAssetTokens ?? Array.Empty<string>())
            {
                if (!PlayerDirectActionProtocol.TryBuild(kind, token, "ALL", 0, null, out string itemTag)) { valid = false; break; }
                tags.Add(itemTag);
            }
            if (valid) tag = string.Join(" ", tags);
        }
        else valid = PlayerDirectActionProtocol.TryBuild(kind, choice?.Token, amount, days, note, out tag);
        if (!valid)
        { Reject(s, ConversationUiLocalization.Text("AF_CONV_UI_Direct_045", "Invalid parameters. No action was executed. Select again and enter a positive quantity/amount or an allowed ALL.")); return; }
        if (!s.IsCurrent() || !string.IsNullOrEmpty(s.Unavailable(kind)) || !s.Revalidate(kind, choice))
        { Reject(s, ConversationUiLocalization.Text("AF_CONV_UI_Direct_046", "The target, scene or candidate has changed. No action was executed. Reopen Manual tags.")); return; }
        Close(s); // Release popup focus and scene movement suppression before applying effects.
        if (!s.IsCurrent()) { InformationManager.DisplayMessage(new InformationMessage(ConversationUiLocalization.Text("AF_CONV_UI_Direct_047", "The conversation has expired. No action was executed."))); return; }
        s.Execute(kind, choice, tag);
    }
}
