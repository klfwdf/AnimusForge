using System;
using System.ComponentModel;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;

namespace AnimusForge.DialogueUI.Native;

public sealed class NativeOverlayVM : ViewModel
{
    private const int OptionCount = 6;
    public AnimusForgeNativeConversationOverlayVM Original { get; }
    private bool _disposed;
    private bool _moreVisible;
    private string _dialogueText = string.Empty;
    private string _speakerName = string.Empty;
    private readonly string[] _optionText = new string[OptionCount];
    private readonly bool[] _optionVisible = new bool[OptionCount];

    public NativeOverlayVM(AnimusForgeNativeConversationOverlayVM original)
    {
        Original = original ?? throw new ArgumentNullException(nameof(original));
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
    [DataSourceProperty] public bool IsOrdinaryMode => !Original.IsCustomAnswerVisible;
    [DataSourceProperty] public bool IsInputEnabled => Original.IsInputEnabled;
    [DataSourceProperty] public bool IsInteractionEnabled => !_disposed;
    [DataSourceProperty] public bool CanLeave => !_disposed;
    [DataSourceProperty] public bool IsPersonaEditVisible => Original.IsPersonaEditVisible;
    [DataSourceProperty] public bool IsTagTestVisible => Original.IsTagTestVisible;
    [DataSourceProperty] public int InputFocusVersion => Original.InputFocusVersion;
    [DataSourceProperty] public float AIChatboxOffset => Original.AIChatboxOffset;
    [DataSourceProperty] public bool HasMoreActions => IsPersonaEditVisible || IsTagTestVisible;
    [DataSourceProperty] public bool IsMoreVisible => _moreVisible && HasMoreActions;
    [DataSourceProperty] public string DialogueText { get => _dialogueText; private set { if (value != _dialogueText) { _dialogueText = value; OnPropertyChangedWithValue(value, nameof(DialogueText)); } } }
    [DataSourceProperty] public string SpeakerName { get => _speakerName; private set { if (value != _speakerName) { _speakerName = value; OnPropertyChangedWithValue(value, nameof(SpeakerName)); } } }
    public string Option0Text => _optionText[0]; public bool Option0Visible => _optionVisible[0];
    public string Option1Text => _optionText[1]; public bool Option1Visible => _optionVisible[1];
    public string Option2Text => _optionText[2]; public bool Option2Visible => _optionVisible[2];
    public string Option3Text => _optionText[3]; public bool Option3Visible => _optionVisible[3];
    public string Option4Text => _optionText[4]; public bool Option4Visible => _optionVisible[4];
    public string Option5Text => _optionText[5]; public bool Option5Visible => _optionVisible[5];

    public void RefreshConversation()
    {
        if (_disposed) return;
        var manager = Campaign.Current?.ConversationManager;
        DialogueText = manager?.CurrentSentenceText ?? string.Empty;
        SpeakerName = manager?.SpeakerAgent?.Character?.Name?.ToString()
            ?? manager?.OneToOneConversationCharacter?.Name?.ToString() ?? string.Empty;
        var options = manager?.CurOptions;
        for (int i = 0; i < OptionCount; i++)
        {
            bool visible = options != null && i < options.Count;
            string text = visible ? options[i].Text?.ToString() ?? string.Empty : string.Empty;
            if (visible != _optionVisible[i]) { _optionVisible[i] = visible; OnPropertyChanged($"Option{i}Visible"); }
            if (text != _optionText[i]) { _optionText[i] = text; OnPropertyChanged($"Option{i}Text"); }
        }
    }

    public void ExecuteSubmit() { if (!_disposed) Original.ExecuteSubmit(); }
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
    public void SwitchTalk() { if (_disposed) return; CloseMore(); Original.SwitchTalk(); OnPropertyChanged(nameof(IsOrdinaryMode)); }
    public void ShowLogView() { if (!_disposed) { CloseMore(); Original.ShowLogView(); } }
    public void ShowGiveShowMenu() { if (!_disposed) { CloseMore(); Original.ShowGiveShowMenu(); } }
    public void EditPersona() { if (!_disposed) { CloseMore(); Original.EditPersona(); } }
    public void OpenTagTest() { if (!_disposed) { CloseMore(); Original.OpenTagTest(); } }
    public void StartTyping() { if (!_disposed) Original.StartTyping(); }
    public void StopTyping() { if (!_disposed) Original.StopTyping(); }
    public void ToggleMore() { if (!_disposed) { _moreVisible = !_moreVisible; OnPropertyChanged(nameof(IsMoreVisible)); } }
    public void ExecuteOption0() => ExecuteOption(0);
    public void ExecuteOption1() => ExecuteOption(1);
    public void ExecuteOption2() => ExecuteOption(2);
    public void ExecuteOption3() => ExecuteOption(3);
    public void ExecuteOption4() => ExecuteOption(4);
    public void ExecuteOption5() => ExecuteOption(5);

    private void ExecuteOption(int index)
    {
        if (_disposed || !_optionVisible[index]) return;
        try
        {
            var manager = Campaign.Current?.ConversationManager;
            var options = manager?.CurOptions;
            if (manager != null && options != null && index < options.Count) manager.ProcessSentence(options[index]);
        }
        catch (Exception ex) { AnimusForge.DialogueUI.DialogueUiRuntime.Log("Conversation option failed: " + ex.Message); }
    }

    private void CloseMore() { _moreVisible = false; OnPropertyChanged(nameof(IsMoreVisible)); }
    private void Forward(string name)
    {
        if (_disposed) return;
        OnPropertyChanged(name);
        if (name == nameof(IsCustomAnswerVisible)) OnPropertyChanged(nameof(IsOrdinaryMode));
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
        _disposed = true;
        Original.PropertyChanged -= Changed;
        Original.PropertyChangedWithValue -= ValueChanged;
        Original.PropertyChangedWithBoolValue -= BoolChanged;
        Original.PropertyChangedWithIntValue -= IntChanged;
        Original.PropertyChangedWithFloatValue -= FloatChanged;
        base.OnFinalize();
    }
}
