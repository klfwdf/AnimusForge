using System;
using System.IO;
using AnimusForge;

internal static class Program
{
    private static int _checks;
    private static void Check(bool condition, string name)
    {
        _checks++;
        if (!condition) throw new Exception("FAILED: " + name);
    }

    private static void Main()
    {
        Check(SettlementBalanceRules.LimitDelta(9999f, 100f, true, 10000) == 1f, "positive crossing");
        Check(SettlementBalanceRules.LimitDelta(10000f, 100f, true, 10000) == 0f, "at cap");
        Check(SettlementBalanceRules.LimitDelta(12000f, 100f, true, 10000) == 0f, "grandfather positive");
        Check(SettlementBalanceRules.LimitDelta(12000f, -100f, true, 10000) == -100f, "grandfather negative");
        Check(SettlementBalanceRules.LimitDelta(9999f, -100f, true, 10000) == -100f, "negative unchanged");
        Check(SettlementBalanceRules.LimitDelta(9999f, 100f, false, 10000) == 100f, "disabled unchanged");
        Check(SettlementBalanceRules.LimitDelta(10000f, 0f, true, 10000) == 0f, "zero unchanged");
        Check(SettlementBalanceRules.LimitDelta(0f, 100f, true, 0) == 0f, "zero cap enabled");
        Check(SettlementBalanceRules.LimitDelta(9999.5f, 0.75f, true, 10000) == 0.5f, "fractional stock");
        Check(SettlementBalanceRules.LimitDelta(9999f, 110f - 109f, true, 10000) == 1f, "aggregate net");
        Check(SettlementBalanceRules.LimitWrite(12000f, 12100f, true, 10000) == 12000f, "write positive guard");
        Check(SettlementBalanceRules.LimitWrite(12000f, 11900f, true, 10000) == 11900f, "write negative guard");
        Check(SettlementBalanceRules.LimitWrite(9999f, 10099f, true, 10000) == 10000f, "write crossing");
        Check(SettlementBalanceRules.DailyFoodCapacity(1000, 1200f, true) == 1200, "native food clamp grandfather");
        Check(SettlementBalanceRules.DailyFoodCapacity(1000, 1200.5f, true) == 1201, "fractional grandfather capacity");
        Check(SettlementBalanceRules.DailyFoodCapacity(1000, 1200f, false) == 1000, "disabled native clamp");
        Check(SettlementBalanceRules.DailyFoodCapacity(1000, 999f, true) == 1000, "normal capacity");
        Check(float.IsNaN(SettlementBalanceRules.LimitDelta(0f, float.NaN, true, 1000)), "nonfinite delta retained");
        var defaults = SettlementBalanceSnapshot.Default;
        Check(SettlementBalanceRules.Definitions.Count == 7, "exact seven metrics");
        foreach (var definition in SettlementBalanceRules.Definitions)
        {
            var limit = defaults.Get(definition.Metric);
            Check(limit.Enabled == (definition.Metric == SettlementBalanceMetric.Food), definition.Id + " default toggle");
            Check(limit.Value == definition.DefaultValue, definition.Id + " default value");
        }
        Check(defaults.Get(SettlementBalanceMetric.Food).Value == 1000, "final food 1000");
        var edited = defaults.With(SettlementBalanceMetric.CityProsperity, true, 10000);
        Check(!defaults.Get(SettlementBalanceMetric.CityProsperity).Enabled, "immutable editing");
        Check(edited.Get(SettlementBalanceMetric.CityProsperity).Enabled, "new snapshot editing");
        Check(SettlementBalanceSettings.TryDecode(SettlementBalanceSettings.Encode(edited), out var decoded, out _), "configuration decode");
        Check(decoded.Get(SettlementBalanceMetric.CityProsperity).Enabled, "roundtrip enabled");
        Check(decoded.Get(SettlementBalanceMetric.Food).Value == 1000, "roundtrip food");
        Check(!SettlementBalanceSettings.TryDecode("{\"Version\":99}", out _, out _), "unknown version rejected");
        Check(!SettlementBalanceSettings.TryDecode("{", out _, out _), "corrupt JSON rejected");
        var invalid = SettlementBalanceSettings.Encode(edited).Replace("10000", "50001");
        Check(!SettlementBalanceSettings.TryDecode(invalid, out _, out _), "out of range rejected");
        Check(!SettlementBalanceSettings.TryDecode("{\"Version\":1,\"Limits\":{\"Food\":{\"Enabled\":true,\"Value\":1.5}}}", out _, out _), "fractional threshold rejected");
        var directory = Path.Combine(Environment.CurrentDirectory, "artifacts", "settlement-balance-20261003", "settings-test-" + Guid.NewGuid().ToString("N"));
        var path = Path.Combine(directory, SettlementBalanceSettings.FileName);
        var store = new SettlementBalanceSettingsStore(path);
        Check(store.Current.Get(SettlementBalanceMetric.Food).Value == 1000, "missing configuration defaults");
        Check(store.TrySave(edited, out _), "atomic save");
        Check(new SettlementBalanceSettingsStore(path).Current.Get(SettlementBalanceMetric.CityProsperity).Enabled, "reload saved settings");
        var before = File.ReadAllText(path);
        Check(!store.TrySave(edited.With(SettlementBalanceMetric.Food, true, 0), out _), "invalid save rejected");
        Check(File.ReadAllText(path) == before, "failed save preserves file");
        Check(store.Current.Get(SettlementBalanceMetric.Food).Value == 1000, "failed save preserves cache");
        Check(store.TrySave(edited.With(SettlementBalanceMetric.Food, true, 2000), out _), "atomic replacement of existing configuration");
        Check(new SettlementBalanceSettingsStore(path).Current.Get(SettlementBalanceMetric.Food).Value == 2000, "replacement reload");
        Check(Directory.GetFiles(directory, "*.tmp-*").Length == 0, "atomic temporary file cleanup");
        var corruptPath = Path.Combine(directory, "corrupt.json");
        File.WriteAllText(corruptPath, "{");
        Check(new SettlementBalanceSettingsStore(corruptPath).Current.Get(SettlementBalanceMetric.Food).Value == 1000, "corrupt configuration uses defaults");
        Check(File.ReadAllText(corruptPath) == "{", "corrupt user file retained");
        var blockedParent = Path.Combine(directory, "blocked-parent");
        File.WriteAllText(blockedParent, "fixture");
        var blocked = new SettlementBalanceSettingsStore(Path.Combine(blockedParent, "settings.json"));
        Check(!blocked.TrySave(edited, out _), "IO failure does not report success");
        Check(!blocked.Current.Get(SettlementBalanceMetric.CityProsperity).Enabled, "IO failure leaves cached defaults");
        Check(SettlementBalanceSettings.Current.Get(SettlementBalanceMetric.Food).Value == 1000 && DuelSettings.PathRequests == 0, "hot settings getter does not read files");
        Check(!SettlementBalanceSettings.TrySave(edited, out _), "path resolver failure isolated");
        Console.WriteLine("PASS: " + _checks + " settlement balance checks (production-linked rules/settings).");
    }
}

namespace AnimusForge
{
    internal static class DuelSettings
    {
        internal static int PathRequests;
        internal static string GetCustomPromptTextStoreDirectoryForPolicyPrompts() { PathRequests++; throw new Exception("Tests must use an explicit workspace store path."); }
    }
    internal static class PolicySystemLog
    {
        internal static void Failure(string area, string code, string message, string detail) { }
    }
}
