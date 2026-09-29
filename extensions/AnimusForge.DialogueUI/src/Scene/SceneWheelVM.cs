using System;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace AnimusForge.DialogueUI.Scene;

// Built once from the host menu when the wheel opens. Sectors and drawer items mirror the host's
// own entries, so what is enabled (speech, tag test, ...) stays a host decision.
public sealed class SceneWheelVM : ViewModel
{
    private readonly Action<InquiryElement> _choose;
    private readonly Action _cancel;
    private readonly InquiryElement _talk;
    private readonly InquiryElement _give;
    private bool _isDrawerOpen;

    internal int LayoutVersion { get; private set; }

    internal SceneWheelVM(MultiSelectionInquiryData data, Action<InquiryElement> choose, Action cancel)
    {
        _choose = choose;
        _cancel = cancel;
        TargetName = string.IsNullOrWhiteSpace(data?.TitleText) ? "附近的人" : data.TitleText.Trim();
        foreach (var element in data?.InquiryElements ?? new System.Collections.Generic.List<InquiryElement>())
        {
            string id = element?.Identifier as string;
            if (id == null) continue;
            if (id == "normal") { _talk = element; continue; }
            if (id == "give") _give = element;
            DrawerItems.Add(new SceneWheelItemVM(element, LabelFor(id, element.Title), Choose));
        }
        DrawerHeader = "动作分支 · " + DrawerItems.Count + " 项";
    }

    [DataSourceProperty] public string TargetName { get; }
    [DataSourceProperty] public string DrawerHeader { get; }
    [DataSourceProperty] public MBBindingList<SceneWheelItemVM> DrawerItems { get; } = new();
    [DataSourceProperty] public bool CanTalk => _talk?.IsEnabled == true;
    [DataSourceProperty] public bool CanGive => _give?.IsEnabled == true;
    [DataSourceProperty] public bool HasActions => DrawerItems.Count > 0;
    [DataSourceProperty] public bool IsDrawerOpen => _isDrawerOpen;
    [DataSourceProperty] public string ActionsText => _isDrawerOpen ? "收起动作" : "动 作";

    // Sector under the cursor, set by the layer from geometry; only enabled sectors highlight.
    private string _hovered;
    [DataSourceProperty] public bool IsHoverTalk => _hovered == "talk" && CanTalk;
    [DataSourceProperty] public bool IsHoverActions => _hovered == "actions" && HasActions;
    [DataSourceProperty] public bool IsHoverGive => _hovered == "give" && CanGive;
    [DataSourceProperty] public bool IsHoverLeave => _hovered == "leave";
    internal string Hovered => _hovered;

    internal void SetHovered(string sector)
    {
        if (sector == _hovered) return;
        _hovered = sector;
        OnPropertyChanged(nameof(IsHoverTalk));
        OnPropertyChanged(nameof(IsHoverActions));
        OnPropertyChanged(nameof(IsHoverGive));
        OnPropertyChanged(nameof(IsHoverLeave));
    }

    // The whole ring is one hit area; a click acts on the sector the cursor is in.
    private bool _sectorClicked;
    public void ExecuteSector() => _sectorClicked = true;
    internal bool TakeSectorClick() { bool clicked = _sectorClicked; _sectorClicked = false; return clicked; }

    internal void Activate(string sector)
    {
        switch (sector)
        {
            case "talk": if (CanTalk) Choose(_talk); break;
            case "give": if (CanGive) Choose(_give); break;
            case "actions": if (HasActions) ExecuteActions(); break;
            case "leave": _cancel(); break;
        }
    }

    public void ExecuteLeave() => _cancel();

    public void ExecuteActions()
    {
        _isDrawerOpen = !_isDrawerOpen;
        LayoutVersion++;
        OnPropertyChanged(nameof(IsDrawerOpen));
        OnPropertyChanged(nameof(ActionsText));
    }

    private void Choose(InquiryElement element)
    {
        if (element?.IsEnabled == true) _choose(element);
    }

    // Pen drawer labels for known host entries; anything new falls back to the host's own title.
    private static string LabelFor(string id, string hostTitle)
    {
        switch (id)
        {
            case "give": return "给予物品";
            case "show": return "展示物品";
            case "give_troops": return "给予部队";
            case "give_prisoners": return "给予俘虏";
            case "give_settlements": return "转移资产";
            case "battle_speech_player": return "当众演讲";
            case "battle_speech_npc": return "他人演讲";
            case "tag_test": return "开发测试 · 标签";
            default: return string.IsNullOrWhiteSpace(hostTitle) ? id : hostTitle;
        }
    }
}

public sealed class SceneWheelItemVM : ViewModel
{
    private readonly InquiryElement _element;
    private readonly Action<InquiryElement> _choose;

    internal SceneWheelItemVM(InquiryElement element, string label, Action<InquiryElement> choose)
    {
        _element = element;
        _choose = choose;
        Label = label ?? "";
        Hint = element?.Hint ?? "";
    }

    [DataSourceProperty] public string Label { get; }
    [DataSourceProperty] public string Hint { get; }
    [DataSourceProperty] public bool IsEnabled => _element?.IsEnabled == true;

    public void ExecuteSelect() => _choose(_element);
}
