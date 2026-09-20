using System.ComponentModel;
using TaleWorlds.Library;

namespace AnimusForge.DialogueUI.Native;

// Presentation only: the AF owner remains responsible for every command and submission.
public sealed class NativeOverlayVM : ViewModel
{
    public AnimusForgeNativeConversationOverlayVM Original { get; }
    private bool _moreVisible;
    private bool _disposed;

    public NativeOverlayVM(AnimusForgeNativeConversationOverlayVM original)
    {
        Original = original;
        original.PropertyChanged += Changed;
        original.PropertyChangedWithValue += ValueChanged;
        original.PropertyChangedWithBoolValue += BoolChanged;
        original.PropertyChangedWithIntValue += IntChanged;
        original.PropertyChangedWithFloatValue += FloatChanged;
    }

    [DataSourceProperty] public string InputText { get => Original.InputText; set { if (!_disposed) Original.InputText = value; } }
    [DataSourceProperty] public string SwitchTitle => Original.SwitchTitle;
    [DataSourceProperty] public string AIChatHistoryButtonText => Original.AIChatHistoryButtonText;
    [DataSourceProperty] public string GiveShowButtonText => Original.GiveShowButtonText;
    [DataSourceProperty] public string PersonaEditButtonText => Original.PersonaEditButtonText;
    [DataSourceProperty] public string TagTestButtonText => Original.TagTestButtonText;
    [DataSourceProperty] public bool IsCustomAnswerVisible => Original.IsCustomAnswerVisible;
    [DataSourceProperty] public bool IsInputEnabled => Original.IsInputEnabled;
    [DataSourceProperty] public bool IsPersonaEditVisible => Original.IsPersonaEditVisible;
    [DataSourceProperty] public bool IsTagTestVisible => Original.IsTagTestVisible;
    [DataSourceProperty] public int InputFocusVersion => Original.InputFocusVersion;
    [DataSourceProperty] public float AIChatboxOffset => Original.AIChatboxOffset;
    [DataSourceProperty] public bool HasMoreActions => IsPersonaEditVisible || IsTagTestVisible;
    [DataSourceProperty] public bool IsMoreVisible => _moreVisible && HasMoreActions;

    public void ExecuteSubmit() { if (!_disposed) Original.ExecuteSubmit(); }
    public void SwitchTalk() { if (_disposed) return; CloseMore(); Original.SwitchTalk(); }
    public void ShowLogView() { if (_disposed) return; CloseMore(); Original.ShowLogView(); }
    public void ShowGiveShowMenu() { if (_disposed) return; CloseMore(); Original.ShowGiveShowMenu(); }
    public void EditPersona() { if (_disposed) return; CloseMore(); Original.EditPersona(); }
    public void OpenTagTest() { if (_disposed) return; CloseMore(); Original.OpenTagTest(); }
    public void StartTyping() { if (!_disposed) Original.StartTyping(); }
    public void StopTyping() { if (!_disposed) Original.StopTyping(); }
    public void ToggleMore() { if (_disposed) return; _moreVisible = !_moreVisible; OnPropertyChanged(nameof(IsMoreVisible)); }
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
