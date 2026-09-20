using System;
using System.ComponentModel;
using TaleWorlds.Library;

namespace AnimusForge.DialogueUI.Native;

// Presentation only: the AF owner remains responsible for every command and submission.
public sealed class NativeOverlayVM : ViewModel
{
    public AnimusForgeNativeConversationOverlayVM Original { get; }
    private bool _moreVisible;
    private bool _disposed;
    private bool _leavePending;
    private readonly Func<bool> _requestLeave;
    private readonly Action _manualModeSelection;

    public NativeOverlayVM(AnimusForgeNativeConversationOverlayVM original, Func<bool> requestLeave = null,
        Action manualModeSelection = null)
    {
        Original = original;
        _requestLeave = requestLeave;
        _manualModeSelection = manualModeSelection;
        original.PropertyChanged += Changed;
        original.PropertyChangedWithValue += ValueChanged;
        original.PropertyChangedWithBoolValue += BoolChanged;
        original.PropertyChangedWithIntValue += IntChanged;
        original.PropertyChangedWithFloatValue += FloatChanged;
    }

    [DataSourceProperty] public string InputText { get => Original.InputText; set { if (CanInteract) Original.InputText = value; } }
    [DataSourceProperty] public string SwitchTitle => Original.SwitchTitle;
    [DataSourceProperty] public string AIChatHistoryButtonText => Original.AIChatHistoryButtonText;
    [DataSourceProperty] public string GiveShowButtonText => Original.GiveShowButtonText;
    [DataSourceProperty] public string PersonaEditButtonText => Original.PersonaEditButtonText;
    [DataSourceProperty] public string TagTestButtonText => Original.TagTestButtonText;
    [DataSourceProperty] public bool IsCustomAnswerVisible => Original.IsCustomAnswerVisible;
    [DataSourceProperty] public bool IsInputEnabled => CanInteract && Original.IsInputEnabled;
    [DataSourceProperty] public bool IsInteractionEnabled => CanInteract;
    [DataSourceProperty] public bool CanLeave => CanInteract && _requestLeave != null;
    [DataSourceProperty] public bool IsPersonaEditVisible => Original.IsPersonaEditVisible;
    [DataSourceProperty] public bool IsTagTestVisible => Original.IsTagTestVisible;
    [DataSourceProperty] public int InputFocusVersion => Original.InputFocusVersion;
    [DataSourceProperty] public float AIChatboxOffset => Original.AIChatboxOffset;
    [DataSourceProperty] public bool HasMoreActions => IsPersonaEditVisible || IsTagTestVisible;
    [DataSourceProperty] public bool IsMoreVisible => _moreVisible && HasMoreActions;

    private bool CanInteract => !_disposed && !_leavePending;
    public void ExecuteSubmit() { if (CanInteract) Original.ExecuteSubmit(); }
    public void SwitchTalk() { if (!CanInteract) return; _manualModeSelection?.Invoke(); CloseMore(); Original.SwitchTalk(); }
    public void ShowLogView() { if (!CanInteract) return; CloseMore(); Original.ShowLogView(); }
    public void ShowGiveShowMenu() { if (!CanInteract) return; CloseMore(); Original.ShowGiveShowMenu(); }
    public void EditPersona() { if (!CanInteract) return; CloseMore(); Original.EditPersona(); }
    public void OpenTagTest() { if (!CanInteract) return; CloseMore(); Original.OpenTagTest(); }
    public void StartTyping() { if (CanInteract) Original.StartTyping(); }
    public void StopTyping() { if (CanInteract) Original.StopTyping(); }
    public void ToggleMore() { if (!CanInteract) return; _moreVisible = !_moreVisible; OnPropertyChanged(nameof(IsMoreVisible)); }
    public void LeaveConversation()
    {
        if (!CanLeave || _requestLeave?.Invoke() != true) return;
        _leavePending = true;
        CloseMore();
        NotifyLeaveState();
    }
    internal void ClearLeavePending()
    {
        if (_disposed || !_leavePending) return;
        _leavePending = false;
        NotifyLeaveState();
    }
    private void NotifyLeaveState()
    {
        OnPropertyChanged(nameof(IsInputEnabled));
        OnPropertyChanged(nameof(IsInteractionEnabled));
        OnPropertyChanged(nameof(CanLeave));
    }
    private void CloseMore() { _moreVisible = false; OnPropertyChanged(nameof(IsMoreVisible)); }
    private void Forward(string name)
    {
        if (_disposed) return;
        OnPropertyChanged(name);
        if (name == nameof(IsPersonaEditVisible) || name == nameof(IsTagTestVisible))
        {
            OnPropertyChanged(nameof(HasMoreActions));
            OnPropertyChanged(nameof(IsMoreVisible));
        }
    }
    private void Changed(object sender, PropertyChangedEventArgs e) => Forward(e.PropertyName);
    private void ValueChanged(object sender, PropertyChangedWithValueEventArgs e) => Forward(e.PropertyName);
    private void BoolChanged(object sender, PropertyChangedWithBoolValueEventArgs e) => Forward(e.PropertyName);
    private void IntChanged(object sender, PropertyChangedWithIntValueEventArgs e) => Forward(e.PropertyName);
    private void FloatChanged(object sender, PropertyChangedWithFloatValueEventArgs e) => Forward(e.PropertyName);
    public override void OnFinalize()
    {
        if (_disposed) return;
        _disposed = true;
        Original.PropertyChanged -= Changed;
        Original.PropertyChangedWithValue -= ValueChanged;
        Original.PropertyChangedWithBoolValue -= BoolChanged;
        Original.PropertyChangedWithIntValue -= IntChanged;
        Original.PropertyChangedWithFloatValue -= FloatChanged;
        base.OnFinalize();
    }
}
