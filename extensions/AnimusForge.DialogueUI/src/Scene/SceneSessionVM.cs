using System;
using System.Collections.Generic;
using System.Linq;
using AnimusForge;
using TaleWorlds.Core;
using TaleWorlds.Core.ImageIdentifiers;
using TaleWorlds.Library;

namespace AnimusForge.DialogueUI.Scene;

// Presentation state for the persistent scene session. Rebuilt only when the host version changes:
// participants are updated in place (rows replaced only on membership change), history only when
// its content differs. Commands are queued and run from the layer's tick, never inside a click.
public sealed class SceneSessionVM : ViewModel
{
    private const int StreamLines = 40;
    private const int DrawerLines = 200;
    private readonly Dictionary<int, SceneParticipantVM> _rows = new();
    private string _inputText = "", _status = "", _addressee = "", _audienceTitle = "", _audienceStats = "", _collapsedText = "";
    private bool _isCollapsed, _isHidden, _isBusy, _isHistoryOpen;
    private int _historySignature;
    private Action _pending;
    private bool _released;
    private readonly Action _openDirectActions;
    [DataSourceProperty] public string DirectActionsButtonText => DialogueUiLocalization.Text("AFD_UI_ManualTags", "Trigger tags manually");
    [DataSourceProperty] public bool IsDirectActionsVisible => DuelSettings.GetSettings()?.EnableDevEditHistory == true;
    private bool _directActionsVisible;
    [DataSourceProperty] public bool CanOpenDirectActions => IsDirectActionsVisible && !_released && !_isBusy && !Trade.IsOpen;
    private int _illustrationVersion = -1;
    [DataSourceProperty] public bool IsIllustrationVisible => ShoutBehavior.IsSceneIllustrationAvailableForExternal;
    [DataSourceProperty] public bool CanIllustrate => IsIllustrationVisible && !ShoutBehavior.IsSceneIllustrationBusyForExternal;
    [DataSourceProperty] public string IllustrationButtonText => ShoutBehavior.IsSceneIllustrationBusyForExternal ? DialogueUiLocalization.Text("AFD_UI_Illustration_Busy", "Capturing…") : DialogueUiLocalization.Text("AFD_UI_Illustration_Button", "Illustrate");
    [DataSourceProperty] public string SessionTitleText => DialogueUiLocalization.Text("AFD_UI_Scene_SessionTitle", "Scene conversation folio");
    [DataSourceProperty] public string CollapseText => DialogueUiLocalization.Text("AFD_UI_Scene_Collapse", "Collapse interface");
    [DataSourceProperty] public string IncludeAllText => DialogueUiLocalization.Text("AFD_UI_Scene_IncludeAll", "Include all");
    [DataSourceProperty] public string KeepLockedText => DialogueUiLocalization.Text("AFD_UI_Scene_KeepLocked", "Keep locked only");
    [DataSourceProperty] public string SceneHistorySeparatorText => DialogueUiLocalization.Text("AFD_UI_Scene_HistorySeparator", "—— Scene conversation record ——");
    [DataSourceProperty] public string CancelGiftText => DialogueUiLocalization.Text("AFD_UI_Scene_CancelGift", "Cancel gift");
    [DataSourceProperty] public string InputPlaceholderText => DialogueUiLocalization.Text("AFD_UI_Scene_InputPlaceholder", "Tell the people around you something…");
    [DataSourceProperty] public string InputHintText => DialogueUiLocalization.Text("AFD_UI_Scene_InputHint", "[Enter] send · [Shift+Enter] new line · distribute to the audience");
    [DataSourceProperty] public string HistoryText => DialogueUiLocalization.Text("AFD_UI_Scene_History", "History");
    [DataSourceProperty] public string GiftText => DialogueUiLocalization.Text("AFD_UI_Scene_Gift", "Gift");
    [DataSourceProperty] public string GalleryText => DialogueUiLocalization.Text("AFD_UI_Scene_Gallery", "Gallery");
    [DataSourceProperty] public string EncyclopediaText => DialogueUiLocalization.Text("AFD_UI_Scene_Encyclopedia", "Encyclopedia");
    [DataSourceProperty] public string LeaveText => DialogueUiLocalization.Text("AFD_UI_Scene_Leave", "Leave");
    [DataSourceProperty] public string InterruptText => DialogueUiLocalization.Text("AFD_UI_Scene_Interrupt", "Interrupt · interject");
    [DataSourceProperty] public string SceneHistoryTitleText => DialogueUiLocalization.Text("AFD_UI_Scene_HistoryTitle", "Scene conversation record");
    [DataSourceProperty] public string CloseText => DialogueUiLocalization.Text("AFD_UI_Scene_Close", "Close");
    [DataSourceProperty] public string ReturnText => DialogueUiLocalization.Text("AFD_UI_Scene_Return", "Back to conversation");
    [DataSourceProperty] public string ExpandFolioText => DialogueUiLocalization.Text("AFD_UI_Scene_ExpandFolio", "Expand\nconversation folio");
    [DataSourceProperty] public string ExpandFolioHintText => DialogueUiLocalization.Text("AFD_UI_Scene_ExpandFolioHint", "(Press T)");
    [DataSourceProperty] public string DocketInstructionsText => DialogueUiLocalization.Text("AFD_UI_Scene_DocketInstructions", "Click a nameplate = address that person · click a seal = participate / hide / lock");
    [DataSourceProperty] public string MoreHistoryText => DialogueUiLocalization.Text("AFD_UI_Scene_MoreHistory", "Scroll for more");
    [DataSourceProperty] public string RollUpDocketText => DialogueUiLocalization.Text("AFD_UI_Scene_RollUpDocket", "Roll up docket");
    public void ExecuteIllustrate() => Queue(() => { if (CanIllustrate) ShoutBehavior.RequestSceneIllustrationForExternal(() => !_released && ShoutBehavior.IsScenePresentationActiveForExternal); });
    public void ExecuteOpenGallery() => Queue(() => { if (!_released && IsIllustrationVisible) ShoutBehavior.OpenSceneIllustrationGalleryForUi(); });
    internal void RefreshIllustration()
    {
        bool visible = IsDirectActionsVisible;
        if (visible != _directActionsVisible) { _directActionsVisible = visible; OnPropertyChanged(nameof(IsDirectActionsVisible)); OnPropertyChanged(nameof(CanOpenDirectActions)); }
        int version = ShoutBehavior.SceneIllustrationVersionForExternal;
        if (version == _illustrationVersion) return;
        _illustrationVersion = version;
        OnPropertyChanged(nameof(IsIllustrationVisible)); OnPropertyChanged(nameof(CanIllustrate)); OnPropertyChanged(nameof(IllustrationButtonText));
    }

    internal int LayoutVersion { get; private set; }

    [DataSourceProperty] public MBBindingList<SceneParticipantVM> Participants { get; } = new();
    [DataSourceProperty] public MBBindingList<SceneHistoryLineVM> HistoryLines { get; } = new();
    [DataSourceProperty] public int HistoryScrollVersion { get; private set; }
    [DataSourceProperty] public bool IsVisible => !_isHidden;
    [DataSourceProperty] public bool IsExpanded => !_isCollapsed;
    [DataSourceProperty] public bool IsCollapsed => _isCollapsed;
    [DataSourceProperty] public bool IsHistoryOpen => _isHistoryOpen && !_isCollapsed;
    [DataSourceProperty] public bool IsBusy => _isBusy;
    [DataSourceProperty] public bool CanSend => !_isBusy;
    [DataSourceProperty] public string SendText => _isBusy ? DialogueUiLocalization.Text("AFD_UI_Scene_Waiting", "Waiting for response…") : DialogueUiLocalization.Text("AFD_UI_Scene_Send", "Send (Enter)");
    [DataSourceProperty] public string StatusText => _status;
    [DataSourceProperty] public string AddresseeText => _addressee;
    [DataSourceProperty] public string AudienceTitle => _audienceTitle;
    [DataSourceProperty] public string AudienceStats => _audienceStats;
    [DataSourceProperty] public string CollapsedText => _collapsedText;
    [DataSourceProperty] public bool IsInputEmpty => string.IsNullOrWhiteSpace(_inputText);

    // Give/show panel shown over the session; the rest of the chrome hides while it is open.
    [DataSourceProperty] public SceneTradeVM Trade { get; }
    [DataSourceProperty] public bool IsTradeOpen => Trade.IsOpen && !_isCollapsed;
    [DataSourceProperty] public bool IsChromeVisible => !Trade.IsOpen;
    // Scroll style: the audience docket can be rolled up into a small tag (local UI state only).
    private bool _isDocketOpen = true;
    [DataSourceProperty] public bool IsDocketOpen => _isDocketOpen;
    [DataSourceProperty] public bool IsDocketRolled => !_isDocketOpen;
    [DataSourceProperty] public string DocketTagText => DialogueUiLocalization.Text("AFD_UI_Scene_Docket", "Expand audience docket · {COUNT} people", "COUNT", Participants.Count.ToString());
    private string _stagedTrade = "";
    [DataSourceProperty] public bool HasStagedTrade => !string.IsNullOrEmpty(_stagedTrade);
    [DataSourceProperty] public string StagedTradeText => HasStagedTrade ? DialogueUiLocalization.Text("AFD_UI_Scene_StagedTrade", "Staged · {TRADE} (delivered with the next line)", "TRADE", _stagedTrade) : "";

    public SceneSessionVM(Action openDirectActions = null) { _openDirectActions = openDirectActions; Trade = new SceneTradeVM(SetStatus, OnTradeVisibilityChanged); }

    // Rows are styled after list changes in either the session or its give panel.
    internal int StyleVersion => LayoutVersion * 4099 + Trade.LayoutVersion;

    [DataSourceProperty]
    public string InputText
    {
        get => _inputText;
        set
        {
            string text = AnimusForgeTextInputSanitizer.SanitizeMultiline(value ?? "", AnimusForgeTextInputSanitizer.MaxNativeConversationChars);
            if (text == _inputText) return;
            bool wasEmpty = IsInputEmpty;
            _inputText = text;
            OnPropertyChangedWithValue(text, nameof(InputText));
            if (wasEmpty != IsInputEmpty) OnPropertyChanged(nameof(IsInputEmpty));
        }
    }

    internal Action TakePending() { Action action = _pending; _pending = null; return action; }
    private void Queue(Action action) => _pending = action;

    public void ExecuteSubmit() => Queue(Submit);
    public void ExecuteToggleHistory() => Queue(() => { _isHistoryOpen = !_isHistoryOpen; OnPropertyChanged(nameof(IsHistoryOpen)); RefreshHistory(force: true); });
    public void ExecuteGift() => Queue(() => OpenTrade("give"));
    public void ExecuteDirectActions()
    {
        Queue(() => { if (CanOpenDirectActions) _openDirectActions?.Invoke(); });
    }
    public void ExecuteCancelStagedTrade() => Queue(() => { ShoutBehavior.CancelScenePresentationTradeForExternal(); SetStatus(DialogueUiLocalization.Text("AFD_UI_Scene_TradeCancelled", "The staged gift was cancelled.")); });
    public void ExecuteToggleDocket() => Queue(() =>
    {
        _isDocketOpen = !_isDocketOpen;
        OnPropertyChanged(nameof(IsDocketOpen));
        OnPropertyChanged(nameof(IsDocketRolled));
    });

    private void OpenTrade(string mode)
    {
        Trade.Open(mode);
        LayoutVersion++;
        OnTradeVisibilityChanged();
    }

    internal void OnTradeVisibilityChanged()
    {
        OnPropertyChanged(nameof(CanOpenDirectActions));
        OnPropertyChanged(nameof(IsTradeOpen));
        OnPropertyChanged(nameof(IsChromeVisible));
    }

    // Esc: close the give panel first, otherwise leave the session. Collapsing is mouse-only.
    internal bool CloseTradeIfOpen()
    {
        if (!Trade.IsOpen) return false;
        Trade.Close();
        OnTradeVisibilityChanged();
        return true;
    }
    public void ExecuteEncyclopedia() => Queue(() => { if (!ShoutBehavior.OpenScenePresentationEncyclopediaForExternal()) SetStatus(DialogueUiLocalization.Text("AFD_UI_Scene_NoEncyclopedia", "The current conversation target has no encyclopedia entry.")); });
    public void ExecuteLeave() => Queue(() => ShoutBehavior.EndScenePresentationForExternal("leave"));
    public void ExecuteCollapse() => Queue(() => ShoutBehavior.SetScenePresentationCollapsedForExternal(true));
    public void ExecuteExpand() => Queue(() => ShoutBehavior.SetScenePresentationCollapsedForExternal(false));
    public void ExecuteIncludeAll() => Queue(() => ShoutBehavior.SetAllScenePresentationParticipantsForExternal(true));
    public void ExecuteKeepLocked() => Queue(() => ShoutBehavior.SetAllScenePresentationParticipantsForExternal(false));
    internal void QueueSelect(int agentIndex) => Queue(() => { if (!ShoutBehavior.SetScenePresentationAddresseeForExternal(agentIndex)) SetStatus(DialogueUiLocalization.Text("AFD_UI_Scene_TargetOutOfRange", "This person has left the area and cannot be addressed.")); });
    internal void QueueCycle(int agentIndex) => Queue(() => ShoutBehavior.CycleScenePresentationParticipantForExternal(agentIndex));

    private void Submit()
    {
        if (IsInputEmpty)
        {
            if (HasStagedTrade) SetStatus(DialogueUiLocalization.Text("AFD_UI_Scene_TradeNeedsText", "The gift will be delivered with your line. Write something to say first."));
            return;
        }
        string text = AnimusForgeTextInputSanitizer.SanitizeSingleLine(_inputText, AnimusForgeTextInputSanitizer.MaxNativeConversationChars);
        if (ShoutBehavior.SubmitScenePresentationTextForExternal(text, out string status))
        {
            InputText = "";
            SetStatus("");
        }
        else if (!string.IsNullOrWhiteSpace(status))
        {
            SetStatus(status);
        }
    }

    internal void SetStatus(string status)
    {
        _status = status ?? "";
        OnPropertyChanged(nameof(StatusText));
    }

    internal void SetHidden(bool hidden)
    {
        if (_isHidden == hidden) return;
        _isHidden = hidden;
        OnPropertyChanged(nameof(IsVisible));
    }

    internal void SetBusy(bool busy, bool canInterrupt)
    {
        if (_canInterrupt != canInterrupt)
        {
            _canInterrupt = canInterrupt;
            OnPropertyChanged(nameof(CanInterrupt));
        }
        if (_isBusy == busy) return;
        _isBusy = busy;
        OnPropertyChanged(nameof(CanOpenDirectActions));
        OnPropertyChanged(nameof(IsBusy));
        OnPropertyChanged(nameof(CanSend));
        OnPropertyChanged(nameof(SendText));
    }

    // 打断: end the running round (NPC replies stop, queued speech retires) so the next line can be sent.
    private bool _canInterrupt;
    [DataSourceProperty] public bool CanInterrupt => _canInterrupt;
    public void ExecuteInterrupt() => Queue(() =>
    {
        if (ShoutBehavior.InterruptScenePresentationForExternal()) SetStatus(DialogueUiLocalization.Text("AFD_UI_Scene_Interrupted", "Interrupted. You can send the next line."));
    });

    internal void Refresh(bool forceHistory = false)
    {
        bool collapsed = ShoutBehavior.IsScenePresentationCollapsedForExternal;
        if (collapsed != _isCollapsed)
        {
            _isCollapsed = collapsed;
            OnPropertyChanged(nameof(IsExpanded));
            OnPropertyChanged(nameof(IsCollapsed));
            OnPropertyChanged(nameof(IsHistoryOpen));
            OnPropertyChanged(nameof(IsTradeOpen));
        }
        // The wheel's give/show choices ask for the give view through the host (consumed once).
        string requested = ShoutBehavior.ConsumeScenePresentationTradeRequestForExternal();
        if (requested != null) OpenTrade(requested);
        string staged = ShoutBehavior.HasScenePresentationStagedTradeForExternal ? ShoutBehavior.GetScenePresentationStagedTradeSummaryForExternal() : "";
        if (staged != _stagedTrade)
        {
            _stagedTrade = staged;
            OnPropertyChanged(nameof(HasStagedTrade));
            OnPropertyChanged(nameof(StagedTradeText));
        }
        OnTradeVisibilityChanged();
        RefreshParticipants();
        OnPropertyChanged(nameof(DocketTagText));
        RefreshHistory(forceHistory);
    }

    private void RefreshParticipants()
    {
        List<ScenePresentationParticipantInfo> infos = ShoutBehavior.GetScenePresentationParticipantsForExternal();
        bool membership = infos.Count != _rows.Count || infos.Any(x => !_rows.ContainsKey(x.AgentIndex));
        if (membership)
        {
            Participants.Clear();
            var keep = new Dictionary<int, SceneParticipantVM>();
            foreach (var info in infos)
            {
                if (!_rows.TryGetValue(info.AgentIndex, out var row)) row = new SceneParticipantVM(info.AgentIndex, info.Character, this);
                keep[info.AgentIndex] = row;
                Participants.Add(row);
            }
            foreach (var old in _rows.Where(x => !keep.ContainsKey(x.Key))) old.Value.OnFinalize();
            _rows.Clear();
            foreach (var pair in keep) _rows[pair.Key] = pair.Value;
            LayoutVersion++;
        }
        int participating = 0, excluded = 0;
        string addressee = "";
        foreach (var info in infos)
        {
            _rows[info.AgentIndex].Update(info);
            if (info.State == ScenePresentationParticipantState.Excluded) excluded++;
            else if (info.IsInRange) participating++;
            if (info.IsAddressee) addressee = info.Name;
        }
        _addressee = string.IsNullOrWhiteSpace(addressee) ? "" : DialogueUiLocalization.Text("AFD_UI_Scene_Address", "To {NAME}:", "NAME", addressee);
        _audienceTitle = DialogueUiLocalization.Text("AFD_UI_Scene_AudienceTitle", "Scene audience docket · {COUNT} people", "COUNT", infos.Count.ToString());
        _audienceStats = DialogueUiLocalization.Text("AFD_UI_Scene_AudienceStats", "{PARTICIPATING} participating · {EXCLUDED} hidden", "PARTICIPATING", participating.ToString(), "EXCLUDED", excluded.ToString());
        _collapsedText = DialogueUiLocalization.Text("AFD_UI_Scene_Collapsed", "Expand scene conversation (T)") + (string.IsNullOrWhiteSpace(addressee) ? "" : DialogueUiLocalization.Text("AFD_UI_Scene_CollapsedTarget", " · {NAME}{MORE}", "NAME", addressee, "MORE", infos.Count > 1 ? DialogueUiLocalization.Text("AFD_UI_Scene_MorePeople", " and {COUNT} others", "COUNT", (infos.Count - 1).ToString()) : ""));
        OnPropertyChanged(nameof(AddresseeText));
        OnPropertyChanged(nameof(AudienceTitle));
        OnPropertyChanged(nameof(AudienceStats));
        OnPropertyChanged(nameof(CollapsedText));
    }

    private void RefreshHistory(bool force)
    {
        List<ScenePresentationHistoryLine> lines = ShoutBehavior.GetScenePresentationHistoryForExternal(IsHistoryOpen ? DrawerLines : StreamLines);
        int signature = lines.Count;
        if (lines.Count > 0) signature = unchecked(signature * 31 + (lines[0].Text?.GetHashCode() ?? 0) * 17 + (lines[lines.Count - 1].Text?.GetHashCode() ?? 0));
        if (!force && signature == _historySignature) return;
        _historySignature = signature;
        HistoryLines.Clear();
        foreach (var line in lines) HistoryLines.Add(new SceneHistoryLineVM(line));
        HistoryScrollVersion++;
        OnPropertyChanged(nameof(HistoryScrollVersion));
    }

    public override void OnFinalize()
    {
        _released = true;
        Trade.OnFinalize();
        foreach (var row in _rows.Values) row.OnFinalize();
        _rows.Clear();
        Participants.Clear();
        HistoryLines.Clear();
        _pending = null;
        base.OnFinalize();
    }
}

public sealed class SceneParticipantVM : ViewModel
{
    private readonly int _agentIndex;
    private readonly SceneSessionVM _owner;
    private readonly ImageIdentifier _image;
    private string _name = "", _detail = "", _stateText = "";
    private bool _isAddressee, _isParticipating, _isExcluded, _isLocked, _isAway;

    internal SceneParticipantVM(int agentIndex, TaleWorlds.CampaignSystem.CharacterObject character, SceneSessionVM owner)
    {
        _agentIndex = agentIndex;
        _owner = owner;
        // Built once per member; membership changes are rare, so no per-refresh identifier work.
        try { _image = character != null ? new CharacterImageIdentifier(CharacterCode.CreateFrom(character)) : null; }
        catch { _image = null; }
    }

    [DataSourceProperty] public string Name => _name;
    [DataSourceProperty] public string Detail => _detail;
    [DataSourceProperty] public string StateText => _stateText;
    [DataSourceProperty] public bool IsAddressee => _isAddressee;
    [DataSourceProperty] public bool IsParticipating => _isParticipating;
    [DataSourceProperty] public bool IsExcluded => _isExcluded;
    [DataSourceProperty] public bool IsLocked => _isLocked;
    [DataSourceProperty] public bool IsDimmed => _isExcluded || _isAway;
    [DataSourceProperty] public string ImageId => _image?.Id ?? "";
    [DataSourceProperty] public string ImageArgs => _image?.AdditionalArgs ?? "";
    [DataSourceProperty] public string ImageProvider => _image?.TextureProviderName ?? "";
    [DataSourceProperty] public bool HasImage => _image != null;

    public void ExecuteSelect() => _owner.QueueSelect(_agentIndex);
    public void ExecuteCycle() => _owner.QueueCycle(_agentIndex);

    internal void Update(ScenePresentationParticipantInfo info)
    {
        string name = (info.IsAddressee ? "★ " : "") + info.Name;
        string detail = string.IsNullOrWhiteSpace(info.Role) ? "" : "【" + info.Role + "】";
        if (info.IsAddressee) detail += " " + DialogueUiLocalization.Text("AFD_UI_Scene_CurrentTarget", "Current target");
        else if (!info.IsInRange) detail += " " + DialogueUiLocalization.Text("AFD_UI_Scene_LeftArea", "Out of range");
        string state = info.State == ScenePresentationParticipantState.Locked ? DialogueUiLocalization.Text("AFD_UI_Scene_StateLocked", "Locked")
            : info.State == ScenePresentationParticipantState.Excluded ? DialogueUiLocalization.Text("AFD_UI_Scene_StateHidden", "Hidden") : DialogueUiLocalization.Text("AFD_UI_Scene_StateParticipating", "Participating");
        Set(ref _name, name, nameof(Name));
        Set(ref _detail, detail.Trim(), nameof(Detail));
        Set(ref _stateText, state, nameof(StateText));
        bool wasDimmed = IsDimmed;
        Set(ref _isAddressee, info.IsAddressee, nameof(IsAddressee));
        Set(ref _isParticipating, info.State == ScenePresentationParticipantState.Participating, nameof(IsParticipating));
        Set(ref _isExcluded, info.State == ScenePresentationParticipantState.Excluded, nameof(IsExcluded));
        Set(ref _isLocked, info.State == ScenePresentationParticipantState.Locked, nameof(IsLocked));
        _isAway = !info.IsInRange;
        if (wasDimmed != IsDimmed) OnPropertyChanged(nameof(IsDimmed));
    }

    private void Set<T>(ref T field, T value, string name)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return;
        field = value;
        OnPropertyChanged(name);
    }
}

public sealed class SceneHistoryLineVM : ViewModel
{
    internal SceneHistoryLineVM(ScenePresentationHistoryLine line)
    {
        Speaker = line?.Speaker ?? "";
        Text = line?.Text ?? "";
        IsPlayer = line?.Kind == "player";
        IsFact = line?.Kind == "fact";
        IsNpc = !IsPlayer && !IsFact;
    }

    [DataSourceProperty] public string Speaker { get; }
    [DataSourceProperty] public string Text { get; }
    [DataSourceProperty] public bool IsPlayer { get; }
    [DataSourceProperty] public bool IsNpc { get; }
    [DataSourceProperty] public bool IsFact { get; }
    [DataSourceProperty] public int FontSize => DialogueUiOptions.BodyFontSize;
}
