// Test hosts only. The production settings class is source-linked unchanged.
using System.ComponentModel;

namespace MCM.Abstractions.Base.Global
{
    public abstract class AttributeGlobalSettings<T>
    {
        public abstract string Id { get; }
        public abstract string DisplayName { get; }
        public abstract string FolderName { get; }
        public abstract string FormatType { get; }
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        public void NotifySave() => OnPropertyChanged("SaveTriggered");
    }
    public static class GlobalSettings<T> { public static T Instance { get; set; } }
}
namespace MCM.Abstractions
{
    public sealed class BaseSettingsProvider
    {
        public static BaseSettingsProvider Instance { get; } = new();
        public int Saves;
        public void SaveSettings(object value) => Saves++;
    }
}
namespace MCM.Abstractions.Attributes
{
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class SettingPropertyGroupAttribute(string group) : Attribute { }
}
namespace MCM.Abstractions.Attributes.v2
{
    public abstract class SettingAttribute : Attribute
    {
        public int Order { get; set; }
        public bool RequireRestart { get; set; }
        public string HintText { get; set; }
        public string Content { get; set; }
    }
    public sealed class SettingPropertyBoolAttribute(string name) : SettingAttribute { }
    public sealed class SettingPropertyDropdownAttribute(string name) : SettingAttribute { }
    public sealed class SettingPropertyIntegerAttribute(string name, int min, int max, string format) : SettingAttribute { }
    public sealed class SettingPropertyFloatingIntegerAttribute(string name, float min, float max, string format) : SettingAttribute { }
    public sealed class SettingPropertyTextAttribute(string name, int order, bool restart, string hint) : SettingAttribute { }
    public sealed class SettingPropertyButtonAttribute(string name, int order, bool restart, string hint) : SettingAttribute { }
}
namespace MCM.Common
{
    public sealed class Dropdown<T>(IEnumerable<T> options, int selectedIndex) : List<T>(options), INotifyPropertyChanged
    {
        private int _selectedIndex = selectedIndex;
        public event PropertyChangedEventHandler PropertyChanged;
        public int SelectedIndex
        {
            get => _selectedIndex;
            set
            {
                if (_selectedIndex == value) return;
                _selectedIndex = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedIndex)));
            }
        }
    }
}
namespace TaleWorlds.Localization
{
    public sealed class TextObject(string text)
    {
        public override string ToString() => text.StartsWith("{=") ? text[(text.IndexOf('}') + 1)..] : text;
    }
}
namespace TaleWorlds.Library
{
    public sealed class InformationMessage(string text) { }
    public static class InformationManager { public static void DisplayMessage(InformationMessage message) { } }
}
namespace AnimusForge
{
    public sealed class DuelSettings
    {
        public static DuelSettings Current;
        public static int SaveCount;
        public static DuelSettings GetSettings() { SpeechSettings.Synchronize(Current); return Current; }
        public static void SaveCurrentSettings() => SaveCount++;
        public bool EnableTtsSpeech { get; set; }
        public bool TtsUseGptSoVits { get; set; }
        public bool TtsVolcDedicatedEnabled { get; set; }
        public float TtsVolcDedicatedVolume { get; set; } = 1;
        public bool TtsSceneUseWinmmAudible { get; set; } = true;
        public float TtsLipSyncSoundEventVolume { get; set; }
        public string TtsGptSoVitsApiUrl { get; set; } = "http://127.0.0.1:9880/tts";
        public string TtsGptSoVitsManifestPath { get; set; } = "";
        public float TtsGptSoVitsSpeed { get; set; } = 1;
        public int TtsGptSoVitsMood { get; set; }
        public string TtsVolcDedicatedApiUrl { get; set; } = "https://fixture.invalid/v1";
        public string TtsVolcDedicatedApiKey { get; set; } = "";
        public string TtsVolcDedicatedAppKey { get; set; } = "";
        public string TtsVolcDedicatedResourceId { get; set; } = "";
        public string TtsVolcDedicatedSpeaker { get; set; } = "";
        public string TtsVolcDedicatedAdditionsJson { get; set; } = "{}";
        public string TtsVolcDedicatedAudioFormat { get; set; } = "wav";
        public int TtsVolcDedicatedSampleRate { get; set; } = 24000;
        public float TtsVolcDedicatedSpeed { get; set; } = 1;
        public int LocalTests;
        public Action TestTtsGptSoVitsVoice => () => LocalTests++;
        public Action TestTtsVolcDedicatedVoice => () => throw new Exception("Paid endpoint must not be invoked by tests.");
    }
}
