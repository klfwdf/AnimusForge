using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Core.ImageIdentifiers;
using TaleWorlds.Library;

namespace AnimusForge.DialogueUI.Native;

// The panel owns snapshots only while open. Rebuilds happen on user actions, never per frame.
public sealed class DialogueAuxiliaryVM : ViewModel
{
    private readonly NativeOverlayVM _owner;
    private readonly InlineTradeBridge _trade = new();
    private readonly List<AnimusForgeDialogueHistoryEntry> _entries = new();
    private readonly List<ResourceRowVM> _resources = new();
    private readonly List<ResourceRowVM> _filteredResources = new();
    private Hero _hero;
    private CharacterObject _character;
    private object _manager;
    private object _conversationAgent, _conversationCharacter;
    private int _token;
    private bool _open, _historyMode, _suspended, _busy, _disposed;
    private string _targetName = "", _search = "", _mode = "give", _status = "", _filter = "all";
    private int? _day;
    private int _resourcePage;
    private float _searchDelay;
    private bool _searchPending;
    private const int ResourcePageSize = 20;
    private static readonly MethodInfo Navigate = AccessTools.Method(typeof(ShoutBehavior).Assembly.GetType("AnimusForge.EncyclopediaEntityLinkNavigationCoordinator"), "Request");
    private static readonly MethodInfo CancelHistory = AccessTools.Method(typeof(AnimusForgeConversationHistoryLogVM), "CancelDeferredFormatting");
    internal int LayoutVersion { get; private set; }

    internal DialogueAuxiliaryVM(NativeOverlayVM owner) { _owner = owner; }
    [DataSourceProperty] public bool IsOpen => _open;
    [DataSourceProperty] public bool IsVisible => _open && !_suspended;
    [DataSourceProperty] public bool IsHistory => _historyMode;
    [DataSourceProperty] public bool IsTrade => !_historyMode;
    [DataSourceProperty] public string Title => _historyMode ? "对话历史" : "给予 / 展示";
    [DataSourceProperty] public string TargetName => _targetName;
    [DataSourceProperty] public string Status => _status;
    [DataSourceProperty] public bool CanInteract => IsVisible && !_busy && !_disposed;
    [DataSourceProperty] public bool CanSubmit => CanInteract && IsTrade && _owner.Original.IsInputEnabled && SelectedItems.Count > 0 && SelectedItems.All(x => x.IsAmountValid);
    [DataSourceProperty] public bool IsGive => _mode != "show";
    [DataSourceProperty] public bool IsShow => _mode == "show";
    [DataSourceProperty] public bool IsItems => _mode == "give" || _mode == "show";
    [DataSourceProperty] public bool IsTroops => _mode == "give_troops";
    [DataSourceProperty] public bool IsPrisoners => _mode == "give_prisoners";
    [DataSourceProperty] public bool IsAssets => _mode == "give_settlements";
    [DataSourceProperty] public bool CanShow => CanInteract && IsItems;
    [DataSourceProperty] public bool IsAllFilter => _filter == "all";
    [DataSourceProperty] public bool IsDialogueFilter => _filter == "dialogue";
    [DataSourceProperty] public bool IsActionFilter => _filter == "action";
    [DataSourceProperty] public string SelectionTitle => IsShow ? "本次展示" : "本次给予";
    [DataSourceProperty] public string ConfirmText => (IsShow ? "确认展示" : "确认给予") + " · " + SelectedItems.Count + " 项";
    [DataSourceProperty] public string SelectionCount => SelectedItems.Count + " 项";
    [DataSourceProperty] public string TotalValue => SelectedItems.Sum(x => (decimal)x.Amount * x.Option.UnitValue).ToString("N0") + " 第纳尔";
    [DataSourceProperty] public string TradeNote => IsShow ? "仅展示，不转移资源；动作记入历史。" : "确认即移交；收起不提交，动作记入历史。";
    [DataSourceProperty] public string ResourcePageText => (_filteredResources.Count == 0 ? 0 : _resourcePage + 1) + " / " + Math.Max(1, (_filteredResources.Count + ResourcePageSize - 1) / ResourcePageSize) + " 页 · " + _filteredResources.Count + " 项";
    [DataSourceProperty] public string ResourceCountText => "可用资源  " + _filteredResources.Count;
    [DataSourceProperty] public bool IsSearchEmpty => string.IsNullOrEmpty(_search);
    [DataSourceProperty] public bool CanResourcePrevious => _resourcePage > 0;
    [DataSourceProperty] public bool CanResourceNext => (_resourcePage + 1) * ResourcePageSize < _filteredResources.Count;
    [DataSourceProperty] public bool IsResourceEmpty => ResourceItems.Count == 0;
    [DataSourceProperty] public bool IsSelectionEmpty => SelectedItems.Count == 0;
    [DataSourceProperty] public MBBindingList<ResourceRowVM> ResourceItems { get; } = new();
    [DataSourceProperty] public MBBindingList<ResourceRowVM> SelectedItems { get; } = new();
    [DataSourceProperty] public MBBindingList<HistoryDayVM> HistoryDays { get; } = new();
    [DataSourceProperty] public AnimusForgeConversationHistoryLogVM History { get; private set; }
    [DataSourceProperty] public string SearchText
    {
        get => _search;
        set { string text = AnimusForgeTextInputSanitizer.SanitizeSingleLine(value ?? "", 80); if (_search == text) return; _search = text; OnPropertyChanged(nameof(SearchText)); OnPropertyChanged(nameof(IsSearchEmpty)); _searchDelay = 0; _searchPending = true; }
    }

    internal void Open(bool history)
    {
        if (_disposed || _busy || (!history && !_owner.Original.IsInputEnabled)) return;
        if (!_open)
        {
            var manager = Campaign.Current?.ConversationManager;
            if (manager?.IsConversationInProgress != true) return;
            _manager = manager; _token = manager.ActiveToken;
            _conversationAgent = manager.OneToOneConversationAgent;
            _conversationCharacter = manager.OneToOneConversationCharacter;
            ShoutBehavior.TryGetNativeConversationLinkTargetForExternal(out _hero, out _character);
            _open = true; _owner.StopTyping();
        }
        if (history) ShowHistory(); else ShowTrade();
    }
    internal void Tick(float dt)
    {
        if (!_open || _disposed) return;
        if (!IsCurrentTarget()) { Close(); return; }
        if (_suspended) return;
        if (_searchPending && (_searchDelay += dt) >= .15f)
        {
            _searchPending = false;
            if (_historyMode) RebuildHistory(); else FilterResources();
        }
    }
    private bool IsCurrentTarget()
    {
        var manager = Campaign.Current?.ConversationManager;
        return ReferenceEquals(manager, _manager) && manager?.IsConversationInProgress == true && manager.ActiveToken == _token
            && ReferenceEquals(manager.OneToOneConversationAgent, _conversationAgent)
            && ReferenceEquals(manager.OneToOneConversationCharacter, _conversationCharacter);
    }
    public void ShowHistory()
    {
        if (!_open || !CanInteract) return;
        if (!IsCurrentTarget()) { Close(); return; }
        _trade.Cancel(); ReleaseResources();
        _historyMode = true; _search = ""; _searchPending = false; _filter = "all"; _day = null; _status = "";
        try { LoadHistory(); }
        catch (Exception ex) { ReleaseHistory(); _status = "历史暂时无法读取，请收起后重试。"; DialogueUiRuntime.Log("Inline history: " + ex); }
        RefreshState(); _owner.AuxiliaryStateChanged();
    }
    public void ShowTrade()
    {
        if (!_open || !CanInteract) return;
        if (!IsCurrentTarget()) { Close(); return; }
        if (!_owner.Original.IsInputEnabled) { _status = "请等待当前对话处理完成后再操作资源。"; RefreshState(); return; }
        _historyMode = false; ReleaseHistory(); _entries.Clear(); HistoryDays.Clear();
        _mode = "give"; _search = ""; _searchPending = false;
        LoadResources(); RefreshState(); _owner.AuxiliaryStateChanged();
    }
    public void Close()
    {
        if (!_open || _busy) return;
        _open = false; _suspended = false; _searchPending = false;
        _trade.Cancel(); ReleaseResources(); ReleaseHistory(); _entries.Clear(); HistoryDays.Clear();
        _manager = null; _conversationAgent = null; _conversationCharacter = null;
        _hero = null; _character = null; _owner.StopTyping();
        RefreshState(); _owner.AuxiliaryStateChanged();
    }
    private void LoadHistory()
    {
        _entries.Clear(); HistoryDays.Clear();
        ShoutBehavior.TryGetNativeConversationPersistentHistoryTargetForExternal(out var hero, out var name, out var memoryId);
        _hero = hero ?? _hero; _targetName = name ?? "当前对话对象";
        var entries = _hero != null ? MyBehavior.GetDialogueHistoryEntriesForExternal(_hero, 260)
            : !string.IsNullOrWhiteSpace(memoryId) ? MyBehavior.GetDialogueHistoryEntriesByIdForExternal(memoryId, 260) : new List<AnimusForgeDialogueHistoryEntry>();
        if (entries.Count == 0) entries = ShoutBehavior.GetNativeConversationSessionHistoryEntriesForExternal(260);
        _entries.AddRange(entries.Where(x => x != null));
        RefreshDayLabels();
        RebuildHistory();
    }
    private void RefreshDayLabels()
    {
        HistoryDays.Clear();
        HistoryDays.Add(new HistoryDayVM("全部日期", null, SelectDay, !_day.HasValue));
        foreach (var group in _entries.GroupBy(x => x.GameDayIndex).OrderByDescending(x => x.Key))
            HistoryDays.Add(new HistoryDayVM((group.First().GameDate ?? "第 " + group.Key + " 日") + " · " + group.Count() + " 条", group.Key, SelectDay, _day == group.Key));
        LayoutVersion++;
    }
    // Click-driven only. The history VM removes the row in place and keeps its page; here the master list
    // is kept in sync so filters and day counts reflect the deletion without re-reading the store.
    private bool DeleteHistoryEntry(AnimusForgeDialogueHistoryEntry entry)
    {
        if (!CanInteract || entry == null) return false;
        if (!IsCurrentTarget()) { Close(); return false; }
        bool deleted = MyBehavior.DeleteDialogueHistoryLineForExternal(entry.MemoryId, entry.GameDayIndex, entry.LineOrdinal, entry.Text, out string status);
        _status = status ?? "";
        if (deleted)
        {
            _entries.Remove(entry);
            // Later lines of the same day moved up by one in the store; keep their ordinals addressable.
            foreach (var other in _entries)
                if (other.GameDayIndex == entry.GameDayIndex && other.LineOrdinal > entry.LineOrdinal
                    && string.Equals(other.MemoryId, entry.MemoryId, StringComparison.Ordinal))
                    other.LineOrdinal--;
            RefreshDayLabels();
        }
        RefreshState();
        return deleted;
    }
    // Click/Enter-driven only. The host rewrites the line and updates the entry in place (same reference
    // as in _entries), so filters and searches see the new text without re-reading the store.
    private bool EditHistoryEntry(AnimusForgeDialogueHistoryEntry entry, string text)
    {
        if (!CanInteract || entry == null) return false;
        if (!IsCurrentTarget()) { Close(); return false; }
        bool edited = MyBehavior.EditDialogueHistoryLineForExternal(entry, AnimusForgeTextInputSanitizer.SanitizeSingleLine(text ?? "", AnimusForgeTextInputSanitizer.MaxNativeConversationChars), out string status);
        _status = status ?? "";
        if (edited) _owner.StopTyping();
        RefreshState();
        return edited;
    }
    private static bool IsFact(AnimusForgeDialogueHistoryEntry e) => (e.Kind ?? "").IndexOf("fact", StringComparison.OrdinalIgnoreCase) >= 0 || (e.Kind ?? "").StartsWith("afef", StringComparison.OrdinalIgnoreCase) || (e.Text ?? "").Contains("[AFEF");
    private void RebuildHistory()
    {
        var entries = _entries.Where(e => (!_day.HasValue || e.GameDayIndex == _day.Value)
            && (_filter == "all" || (_filter == "action") == IsFact(e))
            && (string.IsNullOrEmpty(_search) || Contains(e.Text, _search) || Contains(e.Speaker, _search))).ToList();
        // Filter, day and search rebuilds keep the player's edit/delete toggle instead of hiding the row controls again.
        bool editMode = History?.IsEditMode == true, deleteMode = History?.IsDeleteMode == true;
        ReleaseHistory();
        History = new AnimusForgeConversationHistoryLogVM(_targetName, entries, _hero, _character, Close, OpenEncyclopedia,
            DialogueUiOptions.ShowHistoryDelete ? DeleteHistoryEntry : null, EditHistoryEntry, StartTyping, StopTyping);
        if (editMode) History.ToggleEditMode(); else if (deleteMode) History.ToggleDeleteMode();
        LayoutVersion++;
        OnPropertyChanged(nameof(History));
    }
    private void SelectDay(int? day) { if (!CanInteract) return; _day = day; foreach (var item in HistoryDays) item.SetSelected(item.Day == day); RebuildHistory(); }
    public void FilterAll() { _filter = "all"; RebuildHistory(); RefreshState(); }
    public void FilterDialogue() { _filter = "dialogue"; RebuildHistory(); RefreshState(); }
    public void FilterActions() { _filter = "action"; RebuildHistory(); RefreshState(); }
    public void LatestHistory() { while (History?.CanLoadNewerPage == true) History.LoadNewerPage(); }
    private void OpenEncyclopedia(string link)
    {
        if (!CanInteract || Navigate == null) return;
        Navigate.Invoke(null, new object[] { link, (Action)(() => SetSuspended(true)), (Action)(() => SetSuspended(false)) });
    }
    private void SetSuspended(bool value)
    {
        if (_disposed || !_open) return;
        _suspended = value; _owner.StopTyping(); RefreshState(); _owner.AuxiliaryStateChanged();
    }
    public void SelectItems() => SelectMode("give");
    public void SelectTroops() => SelectMode("give_troops");
    public void SelectPrisoners() => SelectMode("give_prisoners");
    public void SelectAssets() => SelectMode("give_settlements");
    public void SelectGive() { if (IsItems) SelectMode("give"); }
    public void SelectShow() { if (IsItems) SelectMode("show"); }
    private void SelectMode(string mode)
    {
        if (!CanInteract || _mode == mode) return;
        if (!IsCurrentTarget()) { Close(); return; }
        _mode = mode; _search = ""; _searchPending = false;
        _owner.StopTyping(); LoadResources(); RefreshState(); _owner.AuxiliaryStateChanged();
    }
    private void LoadResources()
    {
        ReleaseResources();
        ShoutBehavior.TryGetNativeConversationHistoryTargetForExternal(out _, out _targetName);
        try
        {
            bool loaded = _trade.Load(_mode);
            _status = loaded ? "" : !InlineTradeBridge.Available ? "当前宿主版本不支持内联交易，未执行操作。" : "当前类别没有可用资源，或此目标不符合接收条件。";
            foreach (var option in _trade.Options) _resources.Add(new ResourceRowVM(option, SelectionChanged, StartTyping, StopTyping));
            FilterResources();
        }
        catch (Exception ex) { _trade.Cancel(); _status = "无法读取资源，请收起后重试。"; DialogueUiRuntime.Log("Inline trade load: " + ex); }
        RefreshState();
    }
    private void FilterResources()
    {
        _filteredResources.Clear();
        _filteredResources.AddRange(_resources.Where(x => string.IsNullOrEmpty(_search) || Contains(x.Name, _search) || Contains(x.Category, _search)));
        _resourcePage = 0; RenderResources();
    }
    private static bool Contains(string text, string query) => (text ?? "").IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0;
    private void RenderResources()
    {
        LayoutVersion++;
        ResourceItems.Clear();
        for (int i = _resourcePage * ResourcePageSize; i < Math.Min((_resourcePage + 1) * ResourcePageSize, _filteredResources.Count); i++) ResourceItems.Add(_filteredResources[i]);
        RefreshState();
    }
    public void PreviousResources() { if (CanResourcePrevious) { _resourcePage--; RenderResources(); } }
    public void NextResources() { if (CanResourceNext) { _resourcePage++; RenderResources(); } }
    private void SelectionChanged(ResourceRowVM row)
    {
        if (!CanInteract || !_resources.Contains(row)) return;
        if (row.IsSelected && !SelectedItems.Contains(row)) { SelectedItems.Add(row); LayoutVersion++; }
        if (!row.IsSelected && SelectedItems.Remove(row)) LayoutVersion++;
        RefreshState();
    }
    public void ClearSelection() { foreach (var row in SelectedItems.ToArray()) row.Remove(); }
    public void Confirm()
    {
        if (!IsCurrentTarget()) { Close(); return; }
        if (!CanSubmit || !_owner.Original.IsInputEnabled) return;
        _busy = true; RefreshState(); _owner.StopTyping();
        try
        {
            _trade.Commit(SelectedItems.Select(x => new TradeChoice(x.Option, x.Amount)).ToList());
            _status = "操作已交由游戏处理；实际结果以游戏提示和对话历史为准。";
        }
        catch (Exception ex) { _status = "操作未完成，请重新选择。"; DialogueUiRuntime.Log("Inline trade commit: " + ex); }
        finally { _busy = false; ReleaseResources(); RefreshState(); }
        if (_disposed || !_open || !IsCurrentTarget()) { Close(); return; }
        string result = _status; LoadResources(); _status = result; RefreshState();
        _owner.AuxiliaryStateChanged();
    }
    public void StartTyping() => _owner.StartTyping();
    public void StopTyping() => _owner.StopTyping();
    internal void RefreshInteraction() => OnPropertyChanged(nameof(CanSubmit));
    private void ReleaseHistory()
    {
        var previous = History; History = null;
        if (previous == null) return;
        try { CancelHistory?.Invoke(previous, null); }
        finally { previous.Items.Clear(); previous.OnFinalize(); OnPropertyChanged(nameof(History)); }
    }
    private void ReleaseResources() { ResourceItems.Clear(); SelectedItems.Clear(); foreach (var r in _resources) r.OnFinalize(); _resources.Clear(); _filteredResources.Clear(); }
    private void RefreshState()
    {
        foreach (string property in StateProperties) OnPropertyChanged(property);
    }
    private static readonly string[] StateProperties = { nameof(IsOpen), nameof(IsVisible), nameof(IsHistory), nameof(IsTrade), nameof(Title), nameof(TargetName), nameof(Status), nameof(CanInteract), nameof(CanSubmit), nameof(IsGive), nameof(IsShow), nameof(IsItems), nameof(IsTroops), nameof(IsPrisoners), nameof(IsAssets), nameof(CanShow), nameof(IsAllFilter), nameof(IsDialogueFilter), nameof(IsActionFilter), nameof(SelectionTitle), nameof(ConfirmText), nameof(SelectionCount), nameof(TotalValue), nameof(TradeNote), nameof(ResourcePageText), nameof(CanResourcePrevious), nameof(CanResourceNext), nameof(IsResourceEmpty), nameof(IsSelectionEmpty), nameof(SearchText), nameof(IsSearchEmpty), nameof(ResourceCountText) };
    public override void OnFinalize() { if (_disposed) return; _busy = false; Close(); _trade.Dispose(); _disposed = true; base.OnFinalize(); }
}

public sealed class HistoryDayVM : ViewModel
{
    private readonly Action<int?> _select;
    internal readonly int? Day;
    private bool _selected;
    internal HistoryDayVM(string label, int? day, Action<int?> select, bool selected) { Label = label; Day = day; _select = select; _selected = selected; }
    [DataSourceProperty] public string Label { get; }
    [DataSourceProperty] public bool IsSelected => _selected;
    internal void SetSelected(bool value) { _selected = value; OnPropertyChanged(nameof(IsSelected)); }
    public void ExecuteSelect() => _select(Day);
}

public sealed class ResourceRowVM : ViewModel
{
    internal readonly TradeOption Option;
    private readonly Action<ResourceRowVM> _changed;
    private readonly Action _startTyping, _stopTyping;
    private readonly ImageIdentifier _image;
    private readonly bool _gold;
    private bool _selected;
    private bool _disposed;
    private string _quantity = "1";
    internal ResourceRowVM(TradeOption option, Action<ResourceRowVM> changed, Action start, Action stop)
    { Option = option; _changed = changed; _startTyping = start; _stopTyping = stop; _image = ResourceVisuals.Create(option.HostOption, out _gold); }
    [DataSourceProperty] public string Name => Option.Name;
    [DataSourceProperty] public string Category => Option.Category;
    [DataSourceProperty] public string ImageId => _image?.Id ?? "";
    [DataSourceProperty] public string ImageArgs => _image?.AdditionalArgs ?? "";
    [DataSourceProperty] public string ImageProvider => _image?.TextureProviderName ?? "";
    [DataSourceProperty] public bool HasImage => _image != null;
    [DataSourceProperty] public bool IsGold => _gold;
    [DataSourceProperty] public string AvailableText => Option.Available.ToString("N0");
    [DataSourceProperty] public string AvailableHint => "可用 " + AvailableText;
    [DataSourceProperty] public string ValueText => Option.UnitValue.ToString("N0");
    [DataSourceProperty] public bool IsSelected => _selected;
    [DataSourceProperty] public bool CanSelect => !_disposed && Option.Available > 0 && Option.Element.IsEnabled;
    [DataSourceProperty] public string SelectText => _selected ? "已选" : "选择";
    [DataSourceProperty] public bool IsAmountValid => int.TryParse(_quantity, NumberStyles.None, CultureInfo.InvariantCulture, out int value) && value > 0 && value <= Option.Available;
    // Pen quantity field: dark #25190F fill, so valid text is the light #EBD5A7.
    [DataSourceProperty] public string QuantityColor => IsAmountValid ? "#EBD5A7FF" : "#E0826EFF";
    internal int Amount => IsAmountValid ? int.Parse(_quantity, CultureInfo.InvariantCulture) : 0;
    [DataSourceProperty] public string Quantity
    {
        get => _quantity;
        set { if (_disposed) return; string v = (value ?? "").Trim(); if (v == _quantity) return; _quantity = v; OnPropertyChanged(nameof(Quantity)); OnPropertyChanged(nameof(IsAmountValid)); OnPropertyChanged(nameof(QuantityColor)); _changed(this); }
    }
    public void Toggle() { if (!CanSelect) return; _selected = !_selected; OnPropertyChanged(nameof(IsSelected)); OnPropertyChanged(nameof(SelectText)); _changed(this); }
    public void Remove() { if (_selected) Toggle(); }
    public void Increment() => Quantity = Math.Min(Option.Available, (long)Math.Max(0, Amount) + 1).ToString(CultureInfo.InvariantCulture);
    public void Decrement() => Quantity = Math.Max(1, Amount - 1).ToString(CultureInfo.InvariantCulture);
    public void SelectAll() => Quantity = Option.Available.ToString(CultureInfo.InvariantCulture);
    public void StartTyping() => _startTyping();
    public void StopTyping() => _stopTyping();
    public override void OnFinalize() { _disposed = true; base.OnFinalize(); }
}

// Row thumbnails built once per resource snapshot (open, category switch, commit), never per frame.
// Same ImageIdentifier constructors the AF host already uses on both the 1.3 and 1.4 lines.
internal static class ResourceVisuals
{
    private static readonly Type OptionType = typeof(ShoutBehavior).GetNestedType("ShoutTradeResourceOption", BindingFlags.NonPublic);
    private static readonly FieldInfo GoldField = AccessTools.Field(OptionType, "IsGold");
    private static readonly FieldInfo ItemField = AccessTools.Field(OptionType, "Item");
    private static readonly FieldInfo PartyField = AccessTools.Field(OptionType, "PartyEntry");
    private static readonly FieldInfo SettlementField = AccessTools.Field(OptionType, "SettlementEntry");

    internal static ImageIdentifier Create(object option, out bool gold)
    {
        gold = false;
        if (option == null || OptionType == null || !OptionType.IsInstanceOfType(option)) return null;
        try
        {
            if (GoldField?.GetValue(option) is true) { gold = true; return null; }
            if (ItemField?.GetValue(option) is ItemObject item) return new ItemImageIdentifier(item);
            if (PartyField?.GetValue(option) is MyBehavior.PartyTransferPromptEntry party && party.Character != null)
                return new CharacterImageIdentifier(CharacterCode.CreateFrom(party.Character));
            if (SettlementField?.GetValue(option) is MyBehavior.SettlementTransferPromptEntry asset)
            {
                Banner banner = asset.Settlement?.OwnerClan?.Banner ?? asset.OwnerHero?.Clan?.Banner;
                if (banner != null) return new BannerImageIdentifier(banner);
            }
        }
        catch (Exception ex) { DialogueUiRuntime.LogOnce("resource-visual", "Resource thumbnail unavailable: " + ex.GetType().Name + ": " + ex.Message); }
        return null;
    }
}
