// Boundary fakes only: production settings and economy are linked unchanged.
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
namespace MCM.Abstractions.Attributes
{
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class SettingPropertyGroupAttribute(string group) : Attribute { public int GroupOrder { get; set; } }
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
    public sealed class SettingPropertyIntegerAttribute(string name, int min, int max, string format) : SettingAttribute { }
    public sealed class SettingPropertyButtonAttribute(string name, int order, bool restart, string hint) : SettingAttribute { }
}
namespace Newtonsoft.Json { public sealed class JsonIgnoreAttribute : Attribute { } }
namespace TaleWorlds.Localization
{
    public sealed class TextObject(string text) { public override string ToString() => text; }
}
namespace AnimusForge
{
    public sealed class DuelSettings
    {
        public static DuelSettings Current = new();
        public static int SaveCount;
        public static DuelSettings GetSettings() => Current;
        public static void SaveCurrentSettings() => SaveCount++;
        public bool GcczNpcResponseUnlimited { get; set; }
        public int GcczNpcResponseLimit { get; set; } = 10;
        public int GcczVillageInitialAssistance { get; set; }
        public bool EnableGcczDiagnosticLog { get; set; } = true;
        public bool EnableGcczVerboseDiagnosticLog { get; set; }
        public int GcczDiagnosticLogMaxSizeMegabytes { get; set; } = 16;
        public bool EnableSetsDiagnosticLog { get; set; } = true;
        public bool EnableSetsVerboseDiagnosticLog { get; set; }
        public int SetsDiagnosticLogMaxSizeMegabytes { get; set; } = 16;
        public int SetsOtherSettlementFollowerLimit { get; set; } = 10;
        public Action ExportGcczDebugLog { get; set; }
        public Action ClearGcczDebugLog { get; set; }
        public Action ExportSetsDiagnosticLog { get; set; }
        public Action ClearSetsDiagnosticLog { get; set; }
        public Action ExportNoblePrisonerEscortLog { get; set; }
        public Action ClearNoblePrisonerEscortLog { get; set; }
    }
    public static class GcczTownManualInquiryPresenter { public static void Open() { } }
    public static class GcczVillageCastleTutorialPresenter { public static void Show(bool castle) { } }
}
