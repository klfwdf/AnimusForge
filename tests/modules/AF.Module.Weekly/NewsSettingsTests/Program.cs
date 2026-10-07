using System;
using System.IO;
using MCM.Common;
using Newtonsoft.Json.Linq;
using AnimusForge;

// Real production settings partial and MCM Dropdown/attributes. Only the host/load-path
// facade is substituted; this does not claim a rendered MCM or full provider round trip.
namespace AnimusForge
{
    public class SettingsHost { public virtual void OnPropertyChanged(string name = null) {} }
    public partial class DuelSettings : SettingsHost
    {
        public bool AutoGenerateWeeklyReports { get; set; } = true;
        public string Id => "test";
    }
    internal static class Logger { public static void Log(string source, string text) {} }
}
namespace TaleWorlds.Library
{
    public enum PlatformFileType { User }
    public struct PlatformDirectoryPath { public PlatformDirectoryPath(PlatformFileType type, string path) {} }
    public struct PlatformFilePath
    {
        public PlatformFilePath(PlatformDirectoryPath folder, string name) {}
        internal static string TestPath = "__no_test_settings_file__.json";
        public string FileFullPath => TestPath;
    }
}
internal static class Program
{
    private static void Check(bool value, string label)
    { if (!value) throw new Exception(label); Console.WriteLine("PASS " + label); }
    static void Main()
    {
        var legacy = new DuelSettings { AutoGenerateWeeklyReports = false };
        legacy.ApplyLegacyNewsMode(JObject.Parse("{\"UseWorldBulletin\":false}"));
        legacy.OnPropertyChanged("LOADING_COMPLETE");
        Check(!legacy.UseWorldBulletin && !legacy.AutoGenerateWorldBulletins, "old weekly mode and disabled publishing preserved");
        legacy.AutoGenerateWeeklyReports = true;
        Check(!legacy.AutoGenerateWorldBulletins, "weekly toggle no longer controls migrated bulletin setting");
        legacy.NewsMode.SelectedIndex = 1;
        Check(legacy.UseWorldBulletin, "MCM in-place dropdown mutation drives runtime mode");
        legacy.AutoGenerateWorldBulletins = true;
        legacy.AutoGenerateWeeklyReports = false;
        Check(legacy.AutoGenerateWorldBulletins, "new bulletin and weekly switches independent");
        var modern = new DuelSettings { NewsMode = new Dropdown<string>(new[] { "周报", "快报" }, 0), AutoGenerateWorldBulletins = true, AutoGenerateWeeklyReports = false };
        modern.ApplyLegacyNewsMode(JObject.Parse("{\"UseWorldBulletin\":true}"));
        modern.OnPropertyChanged("LOADING_COMPLETE");
        Check(!modern.UseWorldBulletin && modern.AutoGenerateWorldBulletins, "explicit new configuration wins over legacy keys and defaults");
        modern.NewsMode = new Dropdown<string>(new[] { "bad" }, 99);
        Check(modern.NewsMode.SelectedIndex == 1 && modern.NewsMode.Count == 2, "invalid persisted dropdown index repaired");
        Check(new DuelSettings().UseWorldBulletin && new DuelSettings().AutoGenerateWorldBulletins, "fresh defaults remain bulletin enabled");
        string folder = Path.Combine("artifacts", "bulletin-settings-news-20261007", "settings-files-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        TaleWorlds.Library.PlatformFilePath.TestPath = Path.Combine(folder, "test.json");
        File.WriteAllText(TaleWorlds.Library.PlatformFilePath.TestPath, "{\"UseWorldBulletin\":false,\"Unrelated\":42}");
        var fromFile = new DuelSettings { AutoGenerateWeeklyReports = false };
        fromFile.OnPropertyChanged("LOADING_COMPLETE");
        Check(!fromFile.UseWorldBulletin && !fromFile.AutoGenerateWorldBulletins, "load boundary reads old on-disk mode");
        fromFile.NewsMode.SelectedIndex = 1;
        fromFile.OnPropertyChanged("SAVE_TRIGGERED");
        var saved = JObject.Parse(File.ReadAllText(TaleWorlds.Library.PlatformFilePath.TestPath));
        Check(saved.Value<bool>("UseWorldBulletin") && saved.Value<int>("Unrelated") == 42,
            "save boundary updates old alias while preserving other JSON fields");
        Check(Directory.GetFiles(folder, "*.tmp").Length == 0, "atomic alias save leaves no temporary file");
        var roundTrip = new DuelSettings();
        roundTrip.OnPropertyChanged("LOADING_COMPLETE");
        Check(roundTrip.UseWorldBulletin, "saved legacy alias loads again");
        File.WriteAllText(TaleWorlds.Library.PlatformFilePath.TestPath, "broken JSON");
        roundTrip.OnPropertyChanged("SAVE_TRIGGERED");
        Check(File.ReadAllText(TaleWorlds.Library.PlatformFilePath.TestPath) == "broken JSON", "corrupt settings evidence is not replaced");
        Console.WriteLine("ALL PASS");
    }
}
