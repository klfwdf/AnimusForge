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
    internal static readonly IReadOnlyList<KeyValuePair<PlayerDirectActionKind, string>> Actions =
        new[] {
            Pair(PlayerDirectActionKind.Gold, "NPC 给我钱"), Pair(PlayerDirectActionKind.Item, "NPC 给我物品"),
            Pair(PlayerDirectActionKind.FixedAsset, "NPC 转交固定资产"), Pair(PlayerDirectActionKind.Troops, "NPC 交给我部队"),
            Pair(PlayerDirectActionKind.Prisoners, "NPC 交给我俘虏"), Pair(PlayerDirectActionKind.Debt, "记录玩家欠款／承诺"),
            Pair(PlayerDirectActionKind.ResolveDebt, "解除债务"), Pair(PlayerDirectActionKind.Companion, "当前英雄成为同伴"),
            Pair(PlayerDirectActionKind.Family, "当前英雄成为家族成员"), Pair(PlayerDirectActionKind.Follow, "跟随我"),
            Pair(PlayerDirectActionKind.StopFollow, "停止跟随"), Pair(PlayerDirectActionKind.LordsHall, "进入领主大厅"),
            Pair(PlayerDirectActionKind.AcceptIssue, "接取任务"), Pair(PlayerDirectActionKind.TurnInQuest, "交付任务") };
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
        internal string Title;
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
        var s = _active = new Session { Title = "直接动作 — " + name, IsCurrent = isCurrent, Unavailable = unavailable,
            Candidates = candidates, Revalidate = revalidate, Execute = execute, Finish = onFinished };
        Advance(s, () => {
            if (!isCurrent()) { Reject(s, "当前目标或会话已过期，请重新打开直接动作。"); return; }
            var elements = PlayerDirectActionProtocol.Actions.Select(pair => {
                string reason = unavailable(pair.Key);
                return new InquiryElement(pair.Key, pair.Value, null, string.IsNullOrEmpty(reason), reason ?? "按现有标签规则执行，不调用 AI。");
            }).ToList();
            long stamp = ++_revision;
            MBInformationManager.ShowMultiSelectionInquiry(new MultiSelectionInquiryData(s.Title,
                "仅对当前目标执行。资格、数量、费用与副作用沿用原机制。", elements, true, 1, 1, "确定", "取消",
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
        if (!s.IsCurrent()) { Reject(s, "当前目标或会话已过期，未执行动作。"); return false; }
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
            Reject(s, "读取动作参数或提交时出现异常，已关闭直接动作；不会自动重试。请检查原机制状态。");
        }
    }
    private void Text(Session s, string prompt, Action<string> next)
    {
        long stamp = ++_revision;
        InformationManager.ShowTextInquiry(new TextInquiryData(s.Title, prompt, true, true, "确定", "取消",
            value => { if (Consume(s, stamp)) Advance(s, () => next(value?.Trim() ?? "")); },
            () => { if (Consume(s, stamp)) Close(s); }), pauseGameActiveState: true);
    }
    private void Confirm(Session s, string message, Action next)
    {
        long stamp = ++_revision;
        InformationManager.ShowInquiry(new InquiryData(s.Title, message, true, true, "确认执行", "取消",
            () => { if (Consume(s, stamp)) Advance(s, next); }, () => { if (Consume(s, stamp)) Close(s); }), pauseGameActiveState: true);
    }
    private void Begin(Session s, PlayerDirectActionKind kind)
    {
        string reason = s.Unavailable(kind);
        if (!string.IsNullOrEmpty(reason)) { Reject(s, reason); return; }
        switch (kind)
        {
            case PlayerDirectActionKind.Gold:
                Text(s, "输入 NPC 给你的金额（正整数）。不足时仍按原标签补足规则执行。", amount => Commit(s, kind, null, amount)); break;
            case PlayerDirectActionKind.Debt:
                Text(s, "输入玩家欠款／承诺价值（正整数）：", amount => Text(s, "输入期限天数（0 保留原有无期限语义）：", days => {
                    if (!int.TryParse(days, out int deadline) || deadline < 0) { Reject(s, "期限必须是非负整数。"); return; }
                    Text(s, "输入备注（不能包含方括号或换行）：", note => Commit(s, kind, null, amount, deadline, note));
                })); break;
            case PlayerDirectActionKind.Item: case PlayerDirectActionKind.FixedAsset:
            case PlayerDirectActionKind.Troops: case PlayerDirectActionKind.Prisoners: case PlayerDirectActionKind.ResolveDebt:
                Choose(s, kind); break;
            case PlayerDirectActionKind.Companion: case PlayerDirectActionKind.Family:
                Confirm(s, "目标：" + s.Title.Substring("直接动作 — ".Length) + "\n将按原标签转入玩家家族，身份为"
                    + (kind == PlayerDirectActionKind.Companion ? "同伴。" : "家族成员／领主。") + "原有身份、部队和归属副作用全部保留。",
                    () => Commit(s, kind, null, null)); break;
            default: Commit(s, kind, null, null); break;
        }
    }
    private void Choose(Session s, PlayerDirectActionKind kind)
    {
        var choices = s.Candidates(kind) ?? new List<PlayerDirectActionChoice>();
        if (choices.Count == 0) { Reject(s, "当前目标没有符合原机制的可选物品、资产、部队、俘虏或未解除债务。"); return; }
        var elements = choices.Select(x => new InquiryElement(x.Token, x.Label, null, true, "按原机制执行；执行前复核当前候选。")).ToList();
        long stamp = ++_revision;
        MBInformationManager.ShowMultiSelectionInquiry(new MultiSelectionInquiryData(s.Title, "请选择：", elements, true, 1, 1, "确定", "取消",
            selected => {
                if (!Consume(s, stamp)) return;
                Advance(s, () => {
                    var item = selected?.Count == 1 ? selected[0] : null;
                    int index = item == null ? -1 : elements.IndexOf(item);
                    if (index < 0) { Close(s); return; }
                    var choice = choices[index];
                    if (kind == PlayerDirectActionKind.FixedAsset)
                        Confirm(s, "目标：" + s.Title + "\n转交资产：" + choice.Label + "\n归属将转给玩家，沿用原标签所有检查与后果。", () => Commit(s, kind, choice, "1"));
                    else if (kind == PlayerDirectActionKind.ResolveDebt) Commit(s, kind, choice, null);
                    else if (TransferQuantitySpec.IsAllValue(choice.Token)) Commit(s, kind, choice, "ALL");
                    else Quantity(s, kind, choice);
                });
            }, _ => { if (Consume(s, stamp)) Close(s); }), pauseGameActiveState: true);
    }
    private void Quantity(Session s, PlayerDirectActionKind kind, PlayerDirectActionChoice choice)
    {
        var elements = new List<InquiryElement> {
            new("quantity", "输入数量", null, true, "输入正整数；补足或实际转移数量仍按原标签规则。"),
            new("ALL", "该项全部", null, true, "只对已选单项输出 ALL，保留原语义。") };
        long stamp = ++_revision;
        MBInformationManager.ShowMultiSelectionInquiry(new MultiSelectionInquiryData(s.Title, "已选：" + choice.Label, elements, true, 1, 1, "确定", "取消",
            selected => {
                if (!Consume(s, stamp)) return;
                Advance(s, () => {
                    var item = selected?.Count == 1 ? selected[0] : null;
                    if (item == null || !elements.Contains(item)) { Close(s); return; }
                    if (Equals(item.Identifier, "ALL")) Commit(s, kind, choice, "ALL");
                    else Text(s, "已选：" + choice.Label + "\n输入正整数数量：", amount => Commit(s, kind, choice, amount));
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
        { Reject(s, "参数无效，未执行动作。请重新选择并输入正整数数量／金额或允许的 ALL。"); return; }
        if (!s.IsCurrent() || !string.IsNullOrEmpty(s.Unavailable(kind)) || !s.Revalidate(kind, choice))
        { Reject(s, "目标、场景或候选已经变化，未执行动作。请重新打开直接动作。"); return; }
        Close(s); // Release popup focus and scene movement suppression before applying effects.
        if (!s.IsCurrent()) { InformationManager.DisplayMessage(new InformationMessage("会话已过期，未执行动作。")); return; }
        s.Execute(kind, choice, tag);
    }
}
