using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Library;
using AnimusForge.DialogueUI.Native;

namespace AnimusForge.DialogueUI.Scene;

// Give/show panel inside the scene session (Pen 02 给予与展示 layout). Options come from the host,
// which also validates and commits; choices are staged and delivered with the next line.
// Rebuilt only on open / category switch / search; search is debounced like the dialogue panel.
public sealed class SceneTradeVM : ViewModel
{
    private const int PageSize = 20;
    private readonly Action<string> _status;
    private readonly Action _visibilityChanged;
    private readonly List<ResourceRowVM> _rows = new();
    private readonly List<ResourceRowVM> _filtered = new();
    private string _mode = "give", _search = "", _message = "";
    private bool _open, _searchPending;
    private float _searchDelay;
    private int _page;

    internal int LayoutVersion { get; private set; }
    internal SceneTradeVM(Action<string> status, Action visibilityChanged) { _status = status; _visibilityChanged = visibilityChanged; }

    [DataSourceProperty] public bool IsOpen => _open;
    [DataSourceProperty] public bool CanInteract => _open;
    [DataSourceProperty] public bool IsGive => _mode != "show";
    [DataSourceProperty] public bool IsShow => _mode == "show";
    [DataSourceProperty] public bool IsItems => _mode == "give" || _mode == "show";
    [DataSourceProperty] public bool IsTroops => _mode == "give_troops";
    [DataSourceProperty] public bool IsPrisoners => _mode == "give_prisoners";
    [DataSourceProperty] public bool IsAssets => _mode == "give_settlements";
    [DataSourceProperty] public bool CanShow => IsItems;
    [DataSourceProperty] public string Title => IsShow ? "展示物品" : "给予 / 展示";
    [DataSourceProperty] public string SelectionTitle => IsShow ? "本次展示" : "本次给予";
    [DataSourceProperty] public string ConfirmText => (IsShow ? "准备展示" : "准备给予") + " · " + SelectedItems.Count + " 项";
    [DataSourceProperty] public bool CanSubmit => _open && SelectedItems.Count > 0 && SelectedItems.All(x => x.IsAmountValid);
    [DataSourceProperty] public string TotalValue => SelectedItems.Sum(x => (decimal)x.Amount * x.Option.UnitValue).ToString("N0") + " 第纳尔";
    [DataSourceProperty] public string TradeNote => "确认后回到对话；给予随你下一句话一起交付，关闭不执行。";
    [DataSourceProperty] public string StatusText => _message;
    [DataSourceProperty] public string ResourceCountText => "可用资源  " + _filtered.Count;
    [DataSourceProperty] public string ResourcePageText => (_filtered.Count == 0 ? 0 : _page + 1) + " / " + Math.Max(1, (_filtered.Count + PageSize - 1) / PageSize) + " 页 · " + _filtered.Count + " 项";
    [DataSourceProperty] public bool CanResourcePrevious => _page > 0;
    [DataSourceProperty] public bool CanResourceNext => (_page + 1) * PageSize < _filtered.Count;
    [DataSourceProperty] public bool IsResourceEmpty => ResourceItems.Count == 0;
    [DataSourceProperty] public bool IsSelectionEmpty => SelectedItems.Count == 0;
    [DataSourceProperty] public bool IsSearchEmpty => string.IsNullOrEmpty(_search);
    [DataSourceProperty] public MBBindingList<ResourceRowVM> ResourceItems { get; } = new();
    [DataSourceProperty] public MBBindingList<ResourceRowVM> SelectedItems { get; } = new();

    [DataSourceProperty]
    public string SearchText
    {
        get => _search;
        set
        {
            string text = AnimusForgeTextInputSanitizer.SanitizeSingleLine(value ?? "", 80);
            if (text == _search) return;
            _search = text;
            OnPropertyChanged(nameof(SearchText));
            OnPropertyChanged(nameof(IsSearchEmpty));
            _searchDelay = 0f;
            _searchPending = true;
        }
    }

    internal void Open(string mode)
    {
        _open = true;
        _mode = string.IsNullOrWhiteSpace(mode) ? "give" : mode;
        _search = "";
        Load();
    }

    internal void Tick(float dt)
    {
        if (_open && _searchPending && (_searchDelay += dt) >= 0.15f)
        {
            _searchPending = false;
            Filter();
        }
    }

    public void SelectItems() => Switch("give");
    public void SelectTroops() => Switch("give_troops");
    public void SelectPrisoners() => Switch("give_prisoners");
    public void SelectAssets() => Switch("give_settlements");
    public void SelectGive() { if (IsItems) Switch("give"); }
    public void SelectShow() { if (IsItems) Switch("show"); }
    public void PreviousResources() { if (CanResourcePrevious) { _page--; Render(); } }
    public void NextResources() { if (CanResourceNext) { _page++; Render(); } }
    public void ClearSelection() { foreach (var row in SelectedItems.ToArray()) row.Remove(); }
    public void StartTyping() { }
    public void StopTyping() { }

    // Hands the choices to the host (which re-validates); nothing is transferred until the next line.
    public void Confirm()
    {
        if (!CanSubmit) return;
        var indices = SelectedItems.Select(x => (int)x.Option.Element.Identifier).ToList();
        var amounts = SelectedItems.Select(x => x.Amount).ToList();
        if (ShoutBehavior.StageScenePresentationTradeForExternal(indices, amounts, out string status))
        {
            _status("已准备好：发送下一句话时一起交付。");
            Hide();
        }
        else SetMessage(status);
    }

    // Close without staging drops the host-side option list; a staged gift survives until sent/cancelled.
    public void Close()
    {
        if (!ShoutBehavior.HasScenePresentationStagedTradeForExternal) ShoutBehavior.CancelScenePresentationTradeForExternal();
        Hide();
    }

    private void Hide()
    {
        _open = false;
        Release();
        Refresh();
        _visibilityChanged?.Invoke();
    }

    private void Switch(string mode)
    {
        if (!_open || _mode == mode) return;
        _mode = mode;
        _search = "";
        Load();
    }

    private void Load()
    {
        Release();
        List<ScenePresentationTradeOption> options = ShoutBehavior.LoadScenePresentationTradeOptionsForExternal(_mode, out string status);
        foreach (var option in options)
        {
            var element = new InquiryElement(option.Index, option.Name, null, option.Available > 0, "");
            var trade = new TradeOption(element, option.HostOption, option.Name, option.Available, option.UnitValue, option.Category);
            _rows.Add(new ResourceRowVM(trade, SelectionChanged, StartTyping, StopTyping));
        }
        SetMessage(options.Count == 0 ? status : "");
        OnPropertyChanged(nameof(SearchText));
        OnPropertyChanged(nameof(IsSearchEmpty));
        Filter();
    }

    private void Filter()
    {
        _filtered.Clear();
        _filtered.AddRange(_rows.Where(x => string.IsNullOrEmpty(_search)
            || (x.Name ?? "").IndexOf(_search, StringComparison.OrdinalIgnoreCase) >= 0
            || (x.Category ?? "").IndexOf(_search, StringComparison.OrdinalIgnoreCase) >= 0));
        _page = 0;
        Render();
    }

    private void Render()
    {
        LayoutVersion++;
        ResourceItems.Clear();
        for (int i = _page * PageSize; i < Math.Min((_page + 1) * PageSize, _filtered.Count); i++) ResourceItems.Add(_filtered[i]);
        Refresh();
    }

    private void SelectionChanged(ResourceRowVM row)
    {
        if (!_open || !_rows.Contains(row)) return;
        if (row.IsSelected && !SelectedItems.Contains(row)) { SelectedItems.Add(row); LayoutVersion++; }
        if (!row.IsSelected && SelectedItems.Remove(row)) LayoutVersion++;
        Refresh();
    }

    private void SetMessage(string message)
    {
        _message = message ?? "";
        OnPropertyChanged(nameof(StatusText));
    }

    private void Release()
    {
        ResourceItems.Clear();
        SelectedItems.Clear();
        foreach (var row in _rows) row.OnFinalize();
        _rows.Clear();
        _filtered.Clear();
        _searchPending = false;
    }

    private static readonly string[] StateProperties = { nameof(IsOpen), nameof(CanInteract), nameof(IsGive), nameof(IsShow), nameof(IsItems), nameof(IsTroops), nameof(IsPrisoners), nameof(IsAssets), nameof(CanShow), nameof(Title), nameof(SelectionTitle), nameof(ConfirmText), nameof(CanSubmit), nameof(TotalValue), nameof(ResourceCountText), nameof(ResourcePageText), nameof(CanResourcePrevious), nameof(CanResourceNext), nameof(IsResourceEmpty), nameof(IsSelectionEmpty) };

    private void Refresh()
    {
        foreach (string property in StateProperties) OnPropertyChanged(property);
    }

    public override void OnFinalize()
    {
        Release();
        base.OnFinalize();
    }
}
