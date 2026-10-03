using System;
using System.ComponentModel;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;

namespace AnimusForge.DialogueUI.Native;

public sealed class NativeOverlayVM : ViewModel
{
    public AnimusForgeNativeConversationOverlayVM Original { get; }
    [DataSourceProperty] public DialogueAuxiliaryVM Auxiliary { get; }
    private bool _disposed;
    private bool _moreVisible;
    private string _inputText;

    public NativeOverlayVM(AnimusForgeNativeConversationOverlayVM original)
    {
        Original = original ?? throw new ArgumentNullException(nameof(original));
        _inputText = original.InputText ?? string.Empty;
        Auxiliary = new DialogueAuxiliaryVM(this);
        original.PropertyChanged += Changed;
        original.PropertyChangedWithValue += ValueChanged;
        original.PropertyChangedWithBoolValue += BoolChanged;
        original.PropertyChangedWithIntValue += IntChanged;
        original.PropertyChangedWithFloatValue += FloatChanged;
    }

    [DataSourceProperty]
    public string InputText
    {
        get => _inputText;
        set
        {
            if (_disposed) return;
            string text = AnimusForgeTextInputSanitizer.SanitizeMultiline(value, AnimusForgeTextInputSanitizer.MaxNativeConversationChars);
            if (text == _inputText) return;
            bool wasEmpty = string.IsNullOrWhiteSpace(_inputText);
            _inputText = text;
            OnPropertyChangedWithValue(text, nameof(InputText));
            if (wasEmpty != string.IsNullOrWhiteSpace(text)) OnPropertyChanged(nameof(IsInputEmpty));
        }
    }
    [DataSourceProperty] public bool IsInputEmpty => string.IsNullOrWhiteSpace(_inputText);
    internal bool AutoEnterAiMode { get; } = DialogueUiOptions.AutoEnterAiMode
        && (!DialogueUiOptions.AutoEnterAiModeHeroOnly
            || Campaign.Current?.ConversationManager?.OneToOneConversationCharacter?.IsHero == true);
    [DataSourceProperty] public string SwitchTitle => Original.SwitchTitle;
    [DataSourceProperty] public string AIChatHistoryButtonText => Original.AIChatHistoryButtonText;
    [DataSourceProperty] public string GiveShowButtonText => Original.GiveShowButtonText;
    [DataSourceProperty] public string PersonaEditButtonText => Original.PersonaEditButtonText;
    [DataSourceProperty] public string TagTestButtonText => Original.TagTestButtonText;
    [DataSourceProperty] public bool IsCustomAnswerVisible => Original.IsCustomAnswerVisible && !Auxiliary.IsOpen;
    [DataSourceProperty] public bool IsOrdinaryMode => !Original.IsCustomAnswerVisible && !Auxiliary.IsOpen;
    [DataSourceProperty] public bool IsToolbarVisible => !Auxiliary.IsOpen;
    // Ordinary mode: the native answer list fills the right frame, so the toolbar moves onto the
    // center frame's top border with its right edge at the answer frame's left gold edge (console
    // x=1087, measured from the console art). AI mode keeps the Pen position (x=1105).
    [DataSourceProperty] public float ToolbarOffsetX => IsOrdinaryMode ? -323f : 0f;
    // The center text sheet starts higher than the AI input editor (text y=64 vs editor y=84), so the
    // 28px toolbar at y=46 overlapped the first line; lift it onto the frame border (y=32..60).
    [DataSourceProperty] public float ToolbarOffsetY => IsOrdinaryMode ? -14f : 0f;
    [DataSourceProperty] public bool IsInputEnabled => Original.IsInputEnabled;
    [DataSourceProperty] public bool IsInteractionEnabled => !_disposed && !Auxiliary.IsOpen;
    [DataSourceProperty] public bool CanSwitchTalk => IsInteractionEnabled && Original.CanSwitchTalk;
    [DataSourceProperty] public bool CanLeave => !_disposed;
    [DataSourceProperty] public bool IsPersonaEditVisible => Original.IsPersonaEditVisible;
    [DataSourceProperty] public bool IsTagTestVisible => Original.IsTagTestVisible;
    [DataSourceProperty] public bool IsIllustrationAvailable => IllustratorBridge.IsAvailable();
    [DataSourceProperty] public int InputFocusVersion => Original.InputFocusVersion;
    [DataSourceProperty] public float AIChatboxOffset => Original.AIChatboxOffset;
    [DataSourceProperty] public bool HasMoreActions => IsPersonaEditVisible || IsTagTestVisible;
    [DataSourceProperty] public bool IsMoreVisible => _moreVisible && HasMoreActions;

    public void ExecuteSubmit()
    {
        if (_disposed || Auxiliary.IsOpen) return;
        string multiline = AnimusForgeTextInputSanitizer.SanitizeMultiline(_inputText, AnimusForgeTextInputSanitizer.MaxNativeConversationChars);
        Original.InputText = AnimusForgeTextInputSanitizer.SanitizeSingleLine(multiline, AnimusForgeTextInputSanitizer.MaxNativeConversationChars);
        Original.ExecuteSubmit();
    }
    public void ShowIllustration() { if (!_disposed) IllustratorBridge.Invoke(); }
    public void LeaveConversation()
    {
        if (_disposed) return;
        try
        {
            AnimusForgeNativeConversationOverlay.CloseActive();
            Campaign.Current?.ConversationManager?.EndConversation();
        }
        catch (Exception ex) { AnimusForge.DialogueUI.DialogueUiRuntime.Log("Leave conversation failed: " + ex.Message); }
    }
    public void SwitchTalk() { if (!CanSwitchTalk) return; CloseMore(); Original.SwitchTalk(); ModeChanged(); }
    private void ModeChanged() { OnPropertyChanged(nameof(IsOrdinaryMode)); OnPropertyChanged(nameof(ToolbarOffsetX)); OnPropertyChanged(nameof(ToolbarOffsetY)); }
    public void ShowLogView() { if (!_disposed) { CloseMore(); Auxiliary.Open(true); } }
    public void ShowGiveShowMenu() { if (!_disposed) { CloseMore(); Auxiliary.Open(false); } }
    public void EditPersona() { if (!_disposed) { CloseMore(); Original.EditPersona(); } }
    public void OpenTagTest() { if (!_disposed) { CloseMore(); Original.OpenTagTest(); } }
    public void StartTyping() { if (!_disposed) Original.StartTyping(); }
    public void StopTyping() { if (!_disposed) Original.StopTyping(); }
    public void ToggleMore() { if (!_disposed) { _moreVisible = !_moreVisible; OnPropertyChanged(nameof(IsMoreVisible)); } }

    private void CloseMore() { _moreVisible = false; OnPropertyChanged(nameof(IsMoreVisible)); }
    internal void RefreshAfterSystemUi()
    {
        if (_disposed) return;
        // Root.Show alone does not refresh the replacement prefab's derived bindings.
        OnPropertyChanged(nameof(IsInputEnabled));
        OnPropertyChanged(nameof(CanSwitchTalk));
        OnPropertyChanged(nameof(CanLeave));
        OnPropertyChanged(nameof(IsIllustrationAvailable));
        AuxiliaryStateChanged();
    }
    internal void AuxiliaryStateChanged()
    {
        if (_disposed) return;
        OnPropertyChanged(nameof(IsToolbarVisible));
        OnPropertyChanged(nameof(IsCustomAnswerVisible));
        ModeChanged();
        OnPropertyChanged(nameof(IsInteractionEnabled));
        OnPropertyChanged(nameof(CanSwitchTalk));
        NativeUiAdapter.AuxiliaryStateChanged(this);
        if (!Auxiliary.IsOpen && !_disposed) Original.RequestInputFocus();
    }
    private void Forward(string name)
    {
        if (_disposed) return;
        if (name == nameof(InputText))
        {
            bool wasEmpty = string.IsNullOrWhiteSpace(_inputText);
            _inputText = Original.InputText ?? string.Empty;
            OnPropertyChangedWithValue(_inputText, nameof(InputText));
            if (wasEmpty != string.IsNullOrWhiteSpace(_inputText)) OnPropertyChanged(nameof(IsInputEmpty));
            return;
        }
        OnPropertyChanged(name);
        if (name == nameof(IsInputEnabled)) Auxiliary.RefreshInteraction();
        if (name == nameof(IsCustomAnswerVisible)) ModeChanged();
        if (name == nameof(IsPersonaEditVisible) || name == nameof(IsTagTestVisible))
        { OnPropertyChanged(nameof(HasMoreActions)); OnPropertyChanged(nameof(IsMoreVisible)); }
    }
    private void Changed(object sender, PropertyChangedEventArgs e) => Forward(e.PropertyName);
    private void ValueChanged(object sender, PropertyChangedWithValueEventArgs e) => Forward(e.PropertyName);
    private void BoolChanged(object sender, PropertyChangedWithBoolValueEventArgs e) => Forward(e.PropertyName);
    private void IntChanged(object sender, PropertyChangedWithIntValueEventArgs e) => Forward(e.PropertyName);
    private void FloatChanged(object sender, PropertyChangedWithFloatValueEventArgs e) => Forward(e.PropertyName);

    public override void OnFinalize()
    {
        if (_disposed) return;
        Original.StopTyping();
        _disposed = true;
        Auxiliary.OnFinalize();
        Original.PropertyChanged -= Changed;
        Original.PropertyChangedWithValue -= ValueChanged;
        Original.PropertyChangedWithBoolValue -= BoolChanged;
        Original.PropertyChangedWithIntValue -= IntChanged;
        Original.PropertyChangedWithFloatValue -= FloatChanged;
        base.OnFinalize();
    }
}
