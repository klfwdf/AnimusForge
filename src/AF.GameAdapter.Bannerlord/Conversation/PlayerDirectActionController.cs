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
    Follow, StopFollow, LordsHall, AcceptIssue, TurnInQuest
}

internal sealed class PlayerDirectActionChoice
{
    internal readonly string Token, Label;
    // Live identity check is supplied by the candidate's authoritative owner, not a name lookup.
    internal readonly Func<bool> IsCurrent;
    internal readonly IReadOnlyList<string> BatchAssetTokens;
    internal PlayerDirectActionChoice(string token, string label, Func<bool> isCurrent = null, IEnumerable<string> batchAssetTokens = null)
    { Token = token; Label = label; IsCurrent = isCurrent; BatchAssetTokens = batchAssetTokens?.ToArray(); }
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
            Pair(PlayerDirectActionKind.AcceptIssue, ConversationUiLocalization.Text("AF_CONV_UI_Direct_013", "Accept quest")), Pair(PlayerDirectActionKind.TurnInQuest, ConversationUiLocalization.Text("AF_CONV_UI_Direct_014", "Turn in quest")) };
    private static KeyValuePair<PlayerDirectActionKind, string> Pair(PlayerDirectActionKind kind, string label) => new(kind, label);
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
        internal string Title, TargetName;
        internal bool Closed;
        internal Func<bool> IsCurrent;
        internal Func<PlayerDirectActionKind, string> Unavailable;
        internal Func<PlayerDirectActionKind, List<PlayerDirectActionChoice>> Candidates;
        internal Func<PlayerDirectActionKind, PlayerDirectActionChoice, bool> Revalidate;
        internal Action<PlayerDirectActionKind, PlayerDirectActionChoice, string> Execute;
        internal Action Finish;
    }

    internal void Open(string name, Func<bool> isCurrent, Func<PlayerDirectActionKind, string> unavailable,
        Func<PlayerDirectActionKind, List<PlayerDirectActionChoice>> candidates,
        Func<PlayerDirectActionKind, PlayerDirectActionChoice, bool> revalidate,
        Action<PlayerDirectActionKind, PlayerDirectActionChoice, string> execute, Action onFinished)
    {
        if (_active != null) Close(_active);
        var s = _active = new Session { TargetName = name, Title = ConversationUiLocalization.Text("AF_CONV_UI_Direct_015", "Direct actions — ") + name, IsCurrent = isCurrent, Unavailable = unavailable,
            Candidates = candidates, Revalidate = revalidate, Execute = execute, Finish = onFinished };
        Advance(s, () => {
            if (!isCurrent()) { Reject(s, ConversationUiLocalization.Text("AF_CONV_UI_Direct_016", "The target or conversation has expired. Reopen Direct actions.")); return; }
            var elements = PlayerDirectActionProtocol.Actions.Select(pair => {
                string reason = unavailable(pair.Key);
                return new InquiryElement(pair.Key, pair.Value, null, string.IsNullOrEmpty(reason), reason ?? ConversationUiLocalization.Text("AF_CONV_UI_Direct_017", "Execute existing tag rules without calling AI."));
            }).ToList();
            long stamp = ++_revision;
            MBInformationManager.ShowMultiSelectionInquiry(new MultiSelectionInquiryData(s.Title,
                ConversationUiLocalization.Text("AF_CONV_UI_Direct_018", "Only the current target is affected. Eligibility, quantity, costs and side effects follow the existing mechanics."), elements, true, 1, 1, ConversationUiLocalization.Text("AF_CONV_UI_Direct_019", "OK"), ConversationUiLocalization.Text("AF_CONV_UI_Direct_020", "Cancel"),
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
            Reject(s, ConversationUiLocalization.Text("AF_CONV_UI_Direct_022", "An error occurred while reading parameters or submitting the action. Direct actions was closed without automatic retry. Check the original mechanic's state."));
        }
    }
    private void Text(Session s, string prompt, Action<string> next)
    {
        long stamp = ++_revision;
        InformationManager.ShowTextInquiry(new TextInquiryData(s.Title, prompt, true, true, ConversationUiLocalization.Text("AF_CONV_UI_Direct_019", "OK"), ConversationUiLocalization.Text("AF_CONV_UI_Direct_020", "Cancel"),
            value => { if (Consume(s, stamp)) Advance(s, () => next(value?.Trim() ?? "")); },
            () => { if (Consume(s, stamp)) Close(s); }), pauseGameActiveState: true);
    }
    private void Confirm(Session s, string message, Action next)
    {
        long stamp = ++_revision;
        InformationManager.ShowInquiry(new InquiryData(s.Title, message, true, true, ConversationUiLocalization.Text("AF_CONV_UI_Direct_023", "Confirm action"), ConversationUiLocalization.Text("AF_CONV_UI_Direct_020", "Cancel"),
            () => { if (Consume(s, stamp)) Advance(s, next); }, () => { if (Consume(s, stamp)) Close(s); }), pauseGameActiveState: true);
    }
    private void Begin(Session s, PlayerDirectActionKind kind)
    {
        string reason = s.Unavailable(kind);
        if (!string.IsNullOrEmpty(reason)) { Reject(s, reason); return; }
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
    private void Choose(Session s, PlayerDirectActionKind kind)
    {
        var choices = s.Candidates(kind) ?? new List<PlayerDirectActionChoice>();
        if (choices.Count == 0) { Reject(s, ConversationUiLocalization.Text("AF_CONV_UI_Direct_034", "This target has no eligible items, assets, troops, prisoners or outstanding debts under the existing mechanics.")); return; }
        var elements = choices.Select(x => new InquiryElement(x.Token, x.Label, null, true, ConversationUiLocalization.Text("AF_CONV_UI_Direct_035", "Use the existing mechanics; recheck the selected candidate before execution."))).ToList();
        long stamp = ++_revision;
        MBInformationManager.ShowMultiSelectionInquiry(new MultiSelectionInquiryData(s.Title, ConversationUiLocalization.Text("AF_CONV_UI_Direct_036", "Select:"), elements, true, 1, 1, ConversationUiLocalization.Text("AF_CONV_UI_Direct_019", "OK"), ConversationUiLocalization.Text("AF_CONV_UI_Direct_020", "Cancel"),
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
        MBInformationManager.ShowMultiSelectionInquiry(new MultiSelectionInquiryData(s.Title, ConversationUiLocalization.Text("AF_CONV_UI_Direct_043", "Selected: ") + choice.Label, elements, true, 1, 1, ConversationUiLocalization.Text("AF_CONV_UI_Direct_019", "OK"), ConversationUiLocalization.Text("AF_CONV_UI_Direct_020", "Cancel"),
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
        { Reject(s, ConversationUiLocalization.Text("AF_CONV_UI_Direct_046", "The target, scene or candidate has changed. No action was executed. Reopen Direct actions.")); return; }
        Close(s); // Release popup focus and scene movement suppression before applying effects.
        if (!s.IsCurrent()) { InformationManager.DisplayMessage(new InformationMessage(ConversationUiLocalization.Text("AF_CONV_UI_Direct_047", "The conversation has expired. No action was executed."))); return; }
        s.Execute(kind, choice, tag);
    }
}
