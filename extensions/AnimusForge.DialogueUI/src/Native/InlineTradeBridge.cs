using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;

namespace AnimusForge.DialogueUI.Native;

// UI-only adapter. Host callbacks retain eligibility, ownership, transfer and AFEF rules.
// Reflection is resolved once; capture exists only around a synchronous host call.
internal sealed class InlineTradeBridge : IDisposable
{
    [ThreadStatic] private static InlineTradeBridge _capture;
    [ThreadStatic] private static InlineTradeBridge _committing;
    private static readonly FieldInfo OptionsField = AccessTools.Field(typeof(ShoutBehavior), "_shoutTradeOptions");
    private static readonly FieldInfo PendingField = AccessTools.Field(typeof(ShoutBehavior), "_shoutPendingTradeItems");
    private static readonly FieldInfo PendingIndexField = AccessTools.Field(typeof(ShoutBehavior), "_shoutPendingTradeItemIndex");
    private static readonly FieldInfo ActionOnlyField = AccessTools.Field(typeof(ShoutBehavior), "_shoutTradeActionOnly");
    private static readonly FieldInfo FinishedField = AccessTools.Field(typeof(ShoutBehavior), "_shoutTradeActionOnlyFinished");
    private static readonly MethodInfo ResourceSelectedMethod = AccessTools.Method(typeof(ShoutBehavior), "OnShoutTradeResourcesSelected");
    private static readonly Type OptionType = typeof(ShoutBehavior).GetNestedType("ShoutTradeResourceOption", BindingFlags.NonPublic);
    private static readonly Type PendingType = typeof(ShoutBehavior).GetNestedType("ShoutPendingTradeItem", BindingFlags.NonPublic);
    private static readonly FieldInfo NameField = AccessTools.Field(OptionType, "Name");
    private static readonly FieldInfo AmountField = AccessTools.Field(OptionType, "AvailableAmount");
    private static readonly FieldInfo GoldField = AccessTools.Field(OptionType, "IsGold");
    private static readonly FieldInfo UnitValueField = AccessTools.Field(OptionType, "InventoryUnitValue");
    private static readonly FieldInfo PartyField = AccessTools.Field(OptionType, "PartyEntry");
    private static readonly FieldInfo SettlementField = AccessTools.Field(OptionType, "SettlementEntry");
    private static readonly FieldInfo PendingAmountField = AccessTools.Field(PendingType, "Amount");
    private static readonly string[] IdentityFields = { "IsGold", "ItemId", "PartyEntry", "SettlementEntry" };
    private static readonly FieldInfo[] OptionIdentity = Array.ConvertAll(IdentityFields, name => AccessTools.Field(OptionType, name));
    private static readonly FieldInfo[] PendingIdentity = Array.ConvertAll(IdentityFields, name => AccessTools.Field(PendingType, name));
    private static readonly MethodInfo CommitMethod = AccessTools.Method(typeof(ShoutBehavior), "CommitShoutTradeActionOnly");
    private MultiSelectionInquiryData _menu;
    private MultiSelectionInquiryData _resources;
    private object _owner;
    private bool _finished = true;
    private bool _disposed;
    private bool _inCommit;
    private Action _completion;
    private IList _hostOptions;
    private static readonly string[] Modes = { "give", "show", "give_troops", "give_prisoners", "give_settlements" };
    private readonly List<int> _commitAmounts = new();
    private readonly List<TradeOption> _commitOptions = new();
    internal readonly List<TradeOption> Options = new();
    internal static bool Available { get; private set; }

    internal static void Install(Harmony harmony)
    {
        Available = false;
        foreach (var field in OptionIdentity) if (field == null) throw new MissingMemberException("Trade option identity unavailable.");
        foreach (var field in PendingIdentity) if (field == null) throw new MissingMemberException("Pending trade identity unavailable.");
        var amountMethod = AccessTools.Method(typeof(ShoutBehavior), "ShowShoutTradeAmountInquiry");
        var inquiry = AccessTools.Method(typeof(MBInformationManager), "ShowMultiSelectionInquiry");
        foreach (var member in new MemberInfo[] { OptionsField, PendingField, PendingIndexField, ActionOnlyField,
            NameField, AmountField, GoldField, UnitValueField, PartyField, SettlementField, PendingAmountField,
            CommitMethod, amountMethod, inquiry, FinishedField, ResourceSelectedMethod })
            if (member == null) throw new MissingMemberException("DialogueUI inline trade host contract is missing.");
        harmony.Patch(inquiry, prefix: new HarmonyMethod(typeof(InlineTradeBridge), nameof(CaptureInquiry)));
        harmony.Patch(amountMethod, prefix: new HarmonyMethod(typeof(InlineTradeBridge), nameof(ApplyInlineAmounts)));
        Available = true;
    }

    private static bool CaptureInquiry(MultiSelectionInquiryData __0)
    {
        var bridge = _capture;
        if (!Available || bridge == null || !bridge.OwnsFlow() || __0?.AffirmativeAction == null) return true;
        if (bridge._menu == null)
        {
            // Require the exact host menu contract; a nested third-party inquiry stays native.
            if (__0.MaxSelectableOptionCount != 1 || __0.InquiryElements?.Count != Modes.Length) return true;
            var declaring = __0.AffirmativeAction.Method.DeclaringType;
            while (declaring != null && declaring != typeof(ShoutBehavior)) declaring = declaring.DeclaringType;
            if (declaring == null) return true;
            foreach (string mode in Modes)
                if (!__0.InquiryElements.Exists(e => e?.Identifier is string id && id == mode)) return true;
            bridge._menu = __0;
        }
        else
        {
            if (bridge._resources != null || !ReferenceEquals(__0.AffirmativeAction.Target, bridge._owner)
                || __0.AffirmativeAction.Method != ResourceSelectedMethod) return true;
            bridge._resources = __0;
        }
        return false;
    }

    private bool OwnsFlow() => !_finished && _owner != null && _completion != null
        && ReferenceEquals(FinishedField.GetValue(_owner), _completion)
        && ActionOnlyField.GetValue(_owner) is true;

    internal bool Load(string mode)
    {
        if (_inCommit) return false;
        Cancel();
        if (_disposed || !Available || _inCommit || _capture != null || Array.IndexOf(Modes, mode) < 0) return false;
        _owner = Campaign.Current?.GetCampaignBehavior<ShoutBehavior>();
        if (_owner == null || ActionOnlyField.GetValue(_owner) is true) { _owner = null; return false; }
        _finished = false;
        _completion = () => _finished = true;
        bool loaded = false;
        _capture = this;
        try
        {
            if (!ShoutBehavior.OpenNativeConversationGiveShowForExternal(_completion) || _menu == null || !OwnsFlow()) return false;
            InquiryElement choice = _menu.InquiryElements.Find(e => string.Equals(e.Identifier as string, mode, StringComparison.Ordinal));
            if (choice == null || !choice.IsEnabled) return false;
            _menu.AffirmativeAction(new List<InquiryElement> { choice });
            if (_resources == null || !OwnsFlow()) return false;
            var options = OptionsField.GetValue(_owner) as IList;
            if (options == null) return false;
            _hostOptions = options;
            foreach (var element in _resources.InquiryElements)
            {
                if (element.Identifier is not int index || index < 0 || index >= options.Count) continue;
                object option = options[index];
                var party = PartyField.GetValue(option) as MyBehavior.PartyTransferPromptEntry;
                var asset = SettlementField.GetValue(option) as MyBehavior.SettlementTransferPromptEntry;
                bool gold = (bool)GoldField.GetValue(option);
                int value = gold ? 1 : asset != null ? Math.Max(0, asset.GuidePriceDenars)
                    : party != null ? Math.Max(0, mode == "give_troops" ? party.HirePriceDenarsPerUnit : party.BuyPriceDenarsPerUnit)
                    : Math.Max(1, (int)UnitValueField.GetValue(option));
                Options.Add(new TradeOption(element, option, (string)NameField.GetValue(option),
                    (int)AmountField.GetValue(option), value, gold ? "货币" : asset?.TypeLabel ?? (party == null ? "物品" : mode == "give_troops" ? "部队" : "俘虏")));
            }
            loaded = Options.Count > 0;
            return loaded;
        }
        finally
        {
            _capture = null;
            if (!loaded) Cancel();
        }
    }

    internal void Commit(IReadOnlyList<TradeChoice> choices)
    {
        if (_disposed || _inCommit || !OwnsFlow() || _resources == null || choices == null || choices.Count == 0
            || choices.Count > _resources.MaxSelectableOptionCount)
            throw new InvalidOperationException("给予列表已失效，请重新打开。");
        var liveOptions = OptionsField.GetValue(_owner) as IList;
        if (!ReferenceEquals(liveOptions, _hostOptions)) throw new InvalidOperationException("资源列表已更新，请重新打开。");
        var selected = new List<InquiryElement>();
        _commitAmounts.Clear();
        _commitOptions.Clear();
        var unique = new HashSet<int>();
        foreach (var choice in choices)
        {
            var option = choice?.Option;
            if (option?.Element?.Identifier is not int index || index < 0 || !option.Element.IsEnabled
                || !unique.Add(index) || !Options.Contains(option) || liveOptions == null || index >= liveOptions.Count
                || !ReferenceEquals(liveOptions[index], option.HostOption) || choice.Amount < 1 || choice.Amount > option.Available)
                throw new InvalidOperationException("资源或数量已失效，请重新选择。");
            selected.Add(option.Element);
            _commitAmounts.Add(choice.Amount);
            _commitOptions.Add(option);
        }
        _inCommit = true;
        _committing = this;
        try { _resources.AffirmativeAction(selected); }
        finally { _committing = null; _inCommit = false; _commitAmounts.Clear(); _commitOptions.Clear(); Cancel(); }
    }

    private static bool ApplyInlineAmounts(ShoutBehavior __instance)
    {
        var bridge = _committing;
        if (bridge == null || !ReferenceEquals(bridge._owner, __instance)) return true;
        var pending = PendingField.GetValue(__instance) as IList;
        if (!bridge.OwnsFlow() || pending == null || pending.Count != bridge._commitAmounts.Count)
            throw new InvalidOperationException("Inline trade selection no longer matches host state.");
        // Validate the entire mapping before writing any amount or calling the host transfer.
        for (int i = 0; i < pending.Count; i++)
            for (int key = 0; key < IdentityFields.Length; key++)
                if (!Equals(PendingIdentity[key].GetValue(pending[i]), OptionIdentity[key].GetValue(bridge._commitOptions[i].HostOption)))
                    throw new InvalidOperationException("Inline trade item order changed before commit.");
        for (int i = 0; i < pending.Count; i++) PendingAmountField.SetValue(pending[i], bridge._commitAmounts[i]);
        PendingIndexField.SetValue(__instance, pending.Count);
        CommitMethod.Invoke(__instance, null);
        return false;
    }

    internal void Cancel()
    {
        if (_inCommit) return;
        var cancel = _resources?.NegativeAction ?? _menu?.NegativeAction;
        bool finish = Available && OwnsFlow();
        _resources = null; _menu = null; _owner = null; _hostOptions = null; _completion = null; _finished = true; Options.Clear();
        if (finish)
        {
            try { cancel?.Invoke(new List<InquiryElement>()); }
            catch (Exception ex) { DialogueUiRuntime.Log("Inline trade cancel: " + ex); }
        }
    }
    public void Dispose() { if (_disposed) return; Cancel(); _disposed = true; }
}

internal sealed class TradeOption
{
    internal readonly InquiryElement Element;
    internal readonly object HostOption;
    internal readonly string Name, Category;
    internal readonly int Available, UnitValue;
    internal TradeOption(InquiryElement element, object hostOption, string name, int available, int unitValue, string category)
    { Element = element; HostOption = hostOption; Name = name ?? element.Title; Available = Math.Max(0, available); UnitValue = unitValue; Category = category; }
}
internal sealed class TradeChoice
{
    internal readonly TradeOption Option;
    internal readonly int Amount;
    internal TradeChoice(TradeOption option, int amount) { Option = option; Amount = amount; }
}
