using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Xml.Linq;
using AnimusForge;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;

internal static class Program
{
    private static int _checks;
    private static void Check(bool value, string name)
    {
        _checks++;
        if (!value) throw new Exception("FAILED: " + name);
    }
    private static T Empty<T>() => (T)FormatterServices.GetUninitializedObject(typeof(T));
    private static void Field(object instance, string name, object value) => AccessTools.Field(instance.GetType(), name).SetValue(instance, value);
    private static object Invoke(string name, params object[] args) => AccessTools.Method(typeof(SettlementBalanceRuntime), name).Invoke(null, args);
    private static int Main(string[] args)
    {
        try { Run(args); return 0; }
        catch (Exception ex)
        {
            for (var error = ex; error != null; error = error.InnerException)
            {
                Console.Error.WriteLine(error.GetType().FullName + ": " + error.Message);
                Console.Error.WriteLine(error.StackTrace);
            }
            return 1;
        }
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Run(string[] args)
    {
        if (args.Length != 1) throw new ArgumentException("Repository root required.");
        string root = Path.GetFullPath(args[0]);
        string directory = Path.Combine(root, "artifacts", "settlement-balance-20261003", "runtime-" + Guid.NewGuid().ToString("N"));
        var store = new SettlementBalanceSettingsStore(Path.Combine(directory, SettlementBalanceSettings.FileName));
        AccessTools.Field(typeof(SettlementBalanceSettings), "_store").SetValue(null, store);
        var settings = SettlementBalanceSnapshot.Default;
        foreach (var definition in SettlementBalanceRules.Definitions)
            settings = settings.With(definition.Metric, true, definition.DefaultValue);
        Check(store.TrySave(settings, out _), "workspace-only runtime settings");
        EditorTests(settings);
        NativeAdapterTests(settings, store);
        ModelOrderingTests();
        FoodTranspilerTests(store);
        PopupBindingTests(root);
        Console.WriteLine("PASS: " + _checks + " production-linked runtime/editor checks; native assembly " + typeof(Town).Assembly.GetName().Version + ". No live Campaign or Gauntlet screen.");
    }
    private static void EditorTests(SettlementBalanceSnapshot settings)
    {
        int saves = 0, cancels = 0;
        var vm = new SettlementBalanceEditorVM(settings, () => saves++, () => cancels++);
        Check(vm.Rows.Count == 7, "seven editor rows");
        Check(vm.TitleText == "政策相关数值上限调整", "editor title matches requested policy settings label");
        Check(vm.DescriptionText.Contains("原版＋政策的净变化") && vm.DescriptionText.Contains("负数照常扣除") && vm.DescriptionText.Contains("超额旧存量不削减"), "editor explains net settlement and existing stocks");
        Check(vm.FoodRuleText.Contains("玩家和 NPC") && vm.FoodRuleText.Contains("默认仅开启") && vm.FoodRuleText.Contains("1000") && vm.FoodRuleText.Contains("其余六项默认关闭") && vm.FoodRuleText.Contains("不叠加城堡、建筑加成"), "editor explains scope, defaults and final capacity");
        Check(vm.FoodRuleText.Contains("取消繁荣度耗粮") && vm.FoodRuleText.Contains("独立开关") && vm.FoodRuleText.Contains("默认关闭并保留原版") && vm.FoodRuleText.Contains("与本窗口各项开关无关"), "editor explains independent default-off prosperity food toggle");
        Check(vm.StatusText.Contains("未开启的预填数值不生效") && vm.StatusText.Contains("恢复默认后需保存"), "editor explains disabled values and draft defaults");
        var row = vm.Rows.Single(r => r.Metric == SettlementBalanceMetric.CityProsperity);
        row.ValueInt = 12345;
        Check(row.ValueText == "12345", "slider to input synchronization");
        row.ValueText = "10001";
        Check(row.ValueInt == 10001 && row.IsValid, "integer input to slider synchronization");
        row.ValueText = "10000.5";
        Check(!vm.TryCreateSnapshot(out _, out _) && !row.IsValid, "fractional input blocks save");
        row.ValueText = "50001";
        Check(!row.IsValid && row.ValueInt == 10001, "invalid input does not mutate threshold");
        row.ValueText = "";
        row.Enabled = false;
        Check(row.IsValid && row.ValueText == "10001", "disable discards incomplete input");
        row.ExecuteToggle();
        row.ValueInt = 50000;
        Check(vm.TryCreateSnapshot(out var draft, out _) && draft.Get(row.Metric).Value == 50000, "valid draft");
        Check(SettlementBalanceSettings.Current.Get(row.Metric).Value == 10000, "editing does not apply");
        vm.ExecuteCancel();
        Check(cancels == 1 && saves == 0 && SettlementBalanceSettings.Current.Get(row.Metric).Value == 10000, "cancel callback leaves settings unchanged");
        vm.ExecuteRestoreDefaults();
        Check(vm.TryCreateSnapshot(out draft, out _), "restored defaults valid");
        foreach (var definition in SettlementBalanceRules.Definitions)
            Check(draft.Get(definition.Metric).Value == definition.DefaultValue && draft.Get(definition.Metric).Enabled == definition.DefaultEnabled, definition.Id + " restore default");
        Check(SettlementBalanceSettings.Current.Get(row.Metric).Enabled, "restore defaults remains a draft");
        vm.ExecuteSave();
        Check(saves == 1, "save callback");
        vm.OnFinalize();
    }
    private static T Attach<T>(bool castle = false) where T : SettlementComponent
    {
        var component = Empty<T>();
        var settlement = Empty<Settlement>();
        var party = Empty<PartyBase>();
        Field(party, "<Settlement>k__BackingField", settlement);
        Field(component, "_owner", party);
        Field(settlement, "<SettlementComponent>k__BackingField", component);
        Field(settlement, "<Party>k__BackingField", party);
        if (component is Town town) { Field(town, "_isCastle", castle); Field(settlement, "Town", town); }
        if (component is Village village) Field(settlement, "Village", village);
        return component;
    }
    private static void NativeAdapterTests(SettlementBalanceSnapshot settings, SettlementBalanceSettingsStore store)
    {
        var town = Attach<Town>();
        var other = Attach<Town>();
        SettlementBalanceRuntime.TownDailyPrefix(town, out var incomplete);
        Check(AccessTools.Field(typeof(SettlementBalanceRuntime), "_day").GetValue(null) is SettlementBalanceRuntime.DailyContext disabledContext && disabledContext.Town == null, "incomplete patch installation cannot leak daily context");
        AccessTools.Field(typeof(SettlementBalanceRuntime), "_dailyInstalled").SetValue(null, true);
        town.Prosperity = 9999f;
        town.FoodStocks = 1200.5f;
        float proposed = 10099f;
        SettlementBalanceRuntime.ProsperityWritePrefix(town, ref proposed);
        Check(proposed == 10099f, "initialization/load write untouched outside daily context");
        SettlementBalanceRuntime.TownDailyPrefix(town, out var previous);
        try
        {
            SettlementBalanceRuntime.ProsperityWritePrefix(town, ref proposed);
            Check(proposed == 10000f, "current town positive write capped");
            float unrelated = 10099f;
            SettlementBalanceRuntime.ProsperityWritePrefix(other, ref unrelated);
            Check(unrelated == 10099f, "other town write untouched");
            town.Prosperity = 12000f;
            proposed = 12100f;
            SettlementBalanceRuntime.ProsperityWritePrefix(town, ref proposed);
            Check(proposed == 12000f, "above cap positive write");
            proposed = 11900f;
            SettlementBalanceRuntime.ProsperityWritePrefix(town, ref proposed);
            Check(proposed == 11900f, "above cap negative write");
            float food = 1300.5f;
            SettlementBalanceRuntime.FoodWritePrefix(town, ref food);
            Check(food == 1200.5f, "grandfather food write");
            food = 1100.5f;
            SettlementBalanceRuntime.FoodWritePrefix(town, ref food);
            Check(food == 1100.5f, "grandfather food loss");
            int capacity = 750;
            SettlementBalanceRuntime.FoodCapacityPostfix(town, ref capacity);
            Check(capacity == 1000, "ordinary in-day query uses final capacity for surplus prosperity");
            var capacityHarmony = new Harmony("test.settlementbalance.capacity");
            var capacityMethod = AccessTools.Method(typeof(Town), "FoodStocksUpperLimit");
            capacityHarmony.Patch(capacityMethod, prefix: new HarmonyMethod(typeof(Program), "CapacityStub"), postfix: new HarmonyMethod(typeof(SettlementBalanceRuntime), "FoodCapacityPostfix"));
            try
            {
                Check(town.FoodStocksUpperLimit() == 1000, "actual capacity wrapper ignores building/base total");
                Check((int)Invoke("FoodCapacityForDailyClip", town) == 1201, "native clipping helper grandfathers fractional old stock only");
            }
            finally { capacityHarmony.Unpatch(capacityMethod, HarmonyPatchType.All, capacityHarmony.Id); }
            capacity = 750;
            SettlementBalanceRuntime.FoodCapacityPostfix(other, ref capacity);
            Check(capacity == 1000, "other town queries final capacity");
            var village = Attach<Village>();
            village.Hearth = 1499f;
            SettlementBalanceRuntime.VillageDailyPrefix(village, out var outer);
            proposed = 1599f;
            SettlementBalanceRuntime.HearthWritePrefix(village, ref proposed);
            Check(proposed == 1500f, "village hearth cap");
            var failure = new InvalidOperationException("injected daily failure");
            Check(ReferenceEquals(SettlementBalanceRuntime.DailyFinalizer(outer, failure), failure), "exception not swallowed");
            proposed = 12100f;
            SettlementBalanceRuntime.ProsperityWritePrefix(town, ref proposed);
            Check(proposed == 12000f, "nested daily restores outer target");
            // Direct model replay uses the real ExplainedNumber, not a reimplemented fake.
            var number = new ExplainedNumber(200f, true);
            number.Add(-100f);
            var arguments = new object[] { town, true, number };
            Invoke("ProsperityModelPostfix", arguments);
            number = (ExplainedNumber)arguments[2];
            Check(number.ResultNumber == 0f && number.GetLines().Any(line => line.Item1.Contains("总量瓶颈")), "aggregate net cap and description");
            arguments[2] = new ExplainedNumber(-100f, true);
            Invoke("ProsperityModelPostfix", arguments);
            Check(((ExplainedNumber)arguments[2]).ResultNumber == -100f, "negative model delta untouched");
            object[] guard = { false };
            Invoke("ProsperityFoodPrefix", guard);
            var foodArgs = new object[] { town, true, true, new ExplainedNumber(100f, true) };
            Invoke("FoodModelPostfix", foodArgs);
            Check(((ExplainedNumber)foodArgs[3]).ResultNumber == 100f, "raw food surplus preserved inside prosperity");
            object[] cleanup = { guard[0], failure };
            Check(ReferenceEquals(Invoke("ProsperityFoodFinalizer", cleanup), failure), "prosperity exception preserved");
            Invoke("FoodModelPostfix", foodArgs);
            Check(((ExplainedNumber)foodArgs[3]).ResultNumber == 0f, "food cap restored after exception");
            // The roster setter is not called: verify the argument before native roster generation.
            Field(town.Settlement, "_readyMilitia", 499f);
            proposed = 599f;
            SettlementBalanceRuntime.MilitiaWritePrefix(town.Settlement, ref proposed);
            Check(proposed == 500f, "militia capped before roster write");
            Field(village.Settlement, "_readyMilitia", 99f);
            SettlementBalanceRuntime.VillageDailyPrefix(village, out outer);
            proposed = 199f;
            SettlementBalanceRuntime.MilitiaWritePrefix(village.Settlement, ref proposed);
            Check(proposed == 100f, "village militia separate cap");
            proposed = -10f;
            SettlementBalanceRuntime.HearthWritePrefix(village, ref proposed);
            Check(proposed == -10f, "hearth decline left for native daily minimum");
            SettlementBalanceRuntime.DailyFinalizer(outer, null);
        }
        finally { SettlementBalanceRuntime.DailyFinalizer(previous, null); }
        proposed = 12100f;
        SettlementBalanceRuntime.ProsperityWritePrefix(town, ref proposed);
        Check(proposed == 12100f, "daily context cleanup");
        int finalCapacity = 750;
        SettlementBalanceRuntime.FoodCapacityPostfix(town, ref finalCapacity);
        Check(finalCapacity == 1000 && town.FoodStocks == 1200.5f, "ordinary query final 1000 without truncation");
        var castle = Attach<Town>(true);
        SettlementBalanceRuntime.FoodCapacityPostfix(castle, ref finalCapacity);
        Check(finalCapacity == 1000, "castle shares final food capacity");
        castle.Prosperity = 2999f;
        Field(castle.Settlement, "_readyMilitia", 299f);
        SettlementBalanceRuntime.TownDailyPrefix(castle, out previous);
        proposed = 3099f;
        SettlementBalanceRuntime.ProsperityWritePrefix(castle, ref proposed);
        Check(proposed == 3000f, "castle prosperity separate cap");
        proposed = 399f;
        SettlementBalanceRuntime.MilitiaWritePrefix(castle.Settlement, ref proposed);
        Check(proposed == 300f, "castle militia separate cap");
        SettlementBalanceRuntime.DailyFinalizer(previous, null);
        Check(store.TrySave(settings.With(SettlementBalanceMetric.Food, true, 2000), out _), "raise capacity save");
        SettlementBalanceRuntime.TownDailyPrefix(town, out previous);
        proposed = 1300.5f;
        SettlementBalanceRuntime.FoodWritePrefix(town, ref proposed);
        Check(proposed == 1300.5f, "raised capacity permits storage again");
        SettlementBalanceRuntime.DailyFinalizer(previous, null);
        Check(store.TrySave(settings.With(SettlementBalanceMetric.Food, false, 1000), out _), "disable capacity save");
        finalCapacity = 750;
        SettlementBalanceRuntime.FoodCapacityPostfix(town, ref finalCapacity);
        Check(finalCapacity == 750, "disabled capacity uses original result");
        // Exercise the real Harmony setter wrapper and its original minimum.
        var harmony = new Harmony("test.settlementbalance.setter");
        var setter = AccessTools.PropertySetter(typeof(Town), "Prosperity");
        harmony.Patch(setter, prefix: new HarmonyMethod(typeof(SettlementBalanceRuntime), "ProsperityWritePrefix"));
        try
        {
            // Release JIT can inline direct setters before this method installs the patch.
            setter.Invoke(town, new object[] { 9999f });
            SettlementBalanceRuntime.TownDailyPrefix(town, out previous);
            setter.Invoke(town, new object[] { town.Prosperity + 100f });
            Check(town.Prosperity == 10000f, "real patched prosperity setter crossing");
            setter.Invoke(town, new object[] { -100f });
            Check(town.Prosperity == 0f, "native prosperity minimum retained");
            SettlementBalanceRuntime.DailyFinalizer(previous, null);
        }
        finally { harmony.Unpatch(setter, HarmonyPatchType.All, harmony.Id); }
        foreach (var component in new SettlementComponent[] { town, Attach<Village>() })
        {
            var daily = AccessTools.Method(component.GetType(), "DailyTick");
            var prefix = component is Town ? "TownDailyPrefix" : "VillageDailyPrefix";
            harmony.Patch(daily, prefix: new HarmonyMethod(typeof(SettlementBalanceRuntime), prefix), transpiler: component is Town ? new HarmonyMethod(typeof(SettlementBalanceRuntime), "PreserveExistingDailyFood") : null, finalizer: new HarmonyMethod(typeof(SettlementBalanceRuntime), "DailyFinalizer"));
            bool failed = false;
            try { daily.Invoke(component, null); }
            catch (TargetInvocationException) { failed = true; } // No Campaign: native tick must fail, not get swallowed.
            finally { harmony.Unpatch(daily, HarmonyPatchType.All, harmony.Id); }
            Check(failed, component.GetType().Name + " actual Harmony finalizer preserves native exception");
            Check(AccessTools.Field(typeof(SettlementBalanceRuntime), "_day").GetValue(null) is SettlementBalanceRuntime.DailyContext context && context.Settlement == null, component.GetType().Name + " actual Harmony finalizer clears context");
        }
    }
    private static void FoodTranspilerTests(SettlementBalanceSettingsStore store)
    {
        var target = AccessTools.Method(typeof(DefaultSettlementFoodModel), "CalculateTownFoodChangeInternal", new[] { typeof(Town), typeof(bool), typeof(bool) });
        var original = PatchProcessor.GetOriginalInstructions(target).ToList();
        var untouched = original.Select(code => new CodeInstruction(code)).ToList();
        var transformed = SettlementBalanceRuntime.RemoveProsperityFood(original).ToList();
        Check(transformed.Count == untouched.Count, "native food IL length unchanged");
        int changed = 0;
        for (int i = 0; i < transformed.Count; i++)
            if (transformed[i].opcode != untouched[i].opcode || !Equals(transformed[i].operand, untouched[i].operand)) changed++;
        Check(changed == 1 && transformed.Count(code => code.operand is MethodInfo method && method.Name == "ProsperityForFood") == 1, "only native prosperity input replaced");
        Check(untouched.Where(code => code.operand is MethodInfo method && method.Name != "get_Prosperity").All(code => transformed.Any(candidate => candidate.opcode == code.opcode && Equals(candidate.operand, code.operand))), "garrison supply siege/perk calls unchanged");
        bool rejected = false;
        try { SettlementBalanceRuntime.RemoveProsperityFood(new[] { new CodeInstruction(OpCodes.Ret) }); }
        catch (InvalidOperationException) { rejected = true; }
        Check(rejected, "mismatched native structure fails explicitly");
        var harmony = new Harmony("test.settlementbalance.food");
        harmony.Patch(target, transpiler: new HarmonyMethod(typeof(SettlementBalanceRuntime), "RemoveProsperityFood"));
        Check(Harmony.GetPatchInfo(target).Transpilers.Any(p => p.owner == harmony.Id), "real native food transpiler installs");
        harmony.Unpatch(target, HarmonyPatchType.All, harmony.Id);
        FoodToggleTests(store);
        var daily = PatchProcessor.GetOriginalInstructions(AccessTools.Method(typeof(Town), "DailyTick")).ToList();
        var dayPatched = SettlementBalanceRuntime.PreserveExistingDailyFood(daily).ToList();
        Check(dayPatched.Count(code => code.operand is MethodInfo method && method.Name == "FoodCapacityForDailyClip") == 2, "only daily clipping uses grandfather capacity");
        rejected = false;
        try { SettlementBalanceRuntime.PreserveExistingDailyFood(new[] { new CodeInstruction(OpCodes.Ret) }); }
        catch (InvalidOperationException) { rejected = true; }
        Check(rejected, "mismatched daily clipping structure rejected");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static float ProsperityFoodFixture(DefaultSettlementFoodModel model, Town town)
        => town.Prosperity / model.NumberOfProsperityToEatOneFood;

    private static void FoodToggleTests(SettlementBalanceSettingsStore store)
    {
        var settings = new DuelSettings();
        var property = typeof(DuelSettings).GetProperty(nameof(DuelSettings.DisableProsperityFoodConsumption));
        var toggle = property.GetCustomAttribute<MCM.Abstractions.Attributes.v2.SettingPropertyBoolAttribute>();
        var group = property.GetCustomAttribute<MCM.Abstractions.Attributes.SettingPropertyGroupAttribute>();
        Check(toggle.Name == "取消繁荣度耗粮" && !toggle.RequireRestart && group.Name == "16. 政策系统", "production MCM toggle is independent and requires no restart");
        Check(!settings.DisableProsperityFoodConsumption, "new settings default to vanilla food consumption");
        Check(!Newtonsoft.Json.JsonConvert.DeserializeObject<DuelSettings>("{}").DisableProsperityFoodConsumption, "old settings without the new key retain vanilla consumption");
        settings.DisableProsperityFoodConsumption = true;
        Check(Newtonsoft.Json.JsonConvert.DeserializeObject<DuelSettings>(Newtonsoft.Json.JsonConvert.SerializeObject(settings)).DisableProsperityFoodConsumption, "enabled toggle survives JSON round trip");
        settings.DisableProsperityFoodConsumption = false;
        Check(!Newtonsoft.Json.JsonConvert.DeserializeObject<DuelSettings>(Newtonsoft.Json.JsonConvert.SerializeObject(settings)).DisableProsperityFoodConsumption, "disabled toggle survives JSON round trip");

        var town = Attach<Town>();
        town.Prosperity = 10000f;
        var model = Empty<DefaultSettlementFoodModel>();
        var fixture = AccessTools.Method(typeof(Program), nameof(ProsperityFoodFixture));
        var harmony = new Harmony("test.settlementbalance.food-toggle");
        var previous = store.Current;
        float ReadFood() => (float)fixture.Invoke(null, new object[] { model, town });
        float vanilla = ReadFood();
        Check(vanilla == 250f, "fixture uses native prosperity food divisor");
        harmony.Patch(fixture, transpiler: new HarmonyMethod(typeof(SettlementBalanceRuntime), "RemoveProsperityFood"));
        try
        {
            MCM.Abstractions.Base.Global.GlobalSettings<DuelSettings>.Instance = settings;
            foreach (bool capsEnabled in new[] { false, true })
            {
                var caps = SettlementBalanceSnapshot.Default;
                foreach (var definition in SettlementBalanceRules.Definitions)
                    caps = caps.With(definition.Metric, capsEnabled, definition.DefaultValue);
                Check(store.TrySave(caps, out _), "workspace-only cap snapshot for toggle independence");
                Check(ReadFood() == vanilla, "disabled toggle retains native consumption with caps " + capsEnabled);
                settings.DisableProsperityFoodConsumption = true;
                Check(ReadFood() == 0f, "enabled toggle removes consumption with caps " + capsEnabled);
                settings.DisableProsperityFoodConsumption = false;
                Check(ReadFood() == vanilla, "live disable restores consumption without repatching with caps " + capsEnabled);
                Check(town.Prosperity == 10000f, "toggle never changes actual town prosperity");
            }
            MCM.Abstractions.Base.Global.GlobalSettings<DuelSettings>.Instance = null;
            Check(ReadFood() == vanilla, "missing MCM settings retain native consumption");
            MCM.Abstractions.Base.Global.GlobalSettings<DuelSettings>.ThrowOnRead = true;
            Check(ReadFood() == vanilla, "MCM read failure retains native consumption");
        }
        finally
        {
            MCM.Abstractions.Base.Global.GlobalSettings<DuelSettings>.ThrowOnRead = false;
            MCM.Abstractions.Base.Global.GlobalSettings<DuelSettings>.Instance = null;
            harmony.Unpatch(fixture, HarmonyPatchType.All, harmony.Id);
            store.TrySave(previous, out _);
        }
        Check(ReadFood() == vanilla, "fixture unpatch restores original food input");
    }
    // Fixture for the original capacity calculation only; the production postfix/helper are real.
    private static bool CapacityStub(out int __result) { __result = 750; return false; }
    private static float _policyDelta;
    private static void PolicySumPostfix(ref ExplainedNumber __result) { __result.Add(_policyDelta); }
    private sealed class ModelFixture
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public ExplainedNumber CalculateProsperityChange(Town town, bool descriptions) => new ExplainedNumber(100f, descriptions);
    }
    private static void ModelOrderingTests()
    {
        var town = Attach<Town>();
        var model = new ModelFixture();
        var target = AccessTools.Method(typeof(ModelFixture), "CalculateProsperityChange");
        var policies = new Harmony("com.AnimusForge.custompolicy.settlementmodels");
        var balance = new Harmony("test.settlementbalance.models");
        policies.Patch(target, postfix: new HarmonyMethod(typeof(Program), "PolicySumPostfix"));
        Invoke("InstallModel", balance, model, "CalculateProsperityChange", new[] { typeof(Town), typeof(bool) }, "ProsperityModelPostfix", true);
        try
        {
            town.Prosperity = 9999f;
            _policyDelta = -50f;
            Check(model.CalculateProsperityChange(town, true).ResultNumber == 1f, "real Harmony applies bottleneck after vanilla plus policy net");
            town.Prosperity = 12000f;
            _policyDelta = -200f;
            Check(model.CalculateProsperityChange(town, true).ResultNumber == -100f, "real Harmony net loss above cap unaffected");
            _policyDelta = 200f;
            Check(model.CalculateProsperityChange(town, true).ResultNumber == 0f, "real Harmony net gain above cap stopped");
        }
        finally
        {
            policies.Unpatch(target, HarmonyPatchType.All, policies.Id);
            balance.Unpatch(target, HarmonyPatchType.All, balance.Id);
        }
    }
    private static void PopupBindingTests(string root)
    {
        string mcm = File.ReadAllText(Path.Combine(root, "src/AF.GameAdapter.Bannerlord/Configuration/Mcm/DuelSettings.cs"));
        Check(mcm.Contains("SettingPropertyButton(\"政策相关数值上限调整\"") && mcm.Contains("Content = \"调整上限与容量\""), "MCM button matches requested policy settings label");
        Check(mcm.Contains("SettingPropertyInteger(\"AI评议参考本国最新政策条数\"") && mcm.Contains("SettingPropertyInteger(\"AI评议参考世界相关政策条数\"") && mcm.Contains("不是可生效政策的数量上限"), "MCM distinguishes AI reference counts from policy limits");
        var xml = XDocument.Load(Path.Combine(root, "content/modules/PolicySystem/GUI/Prefabs/SettlementBalancePopup.xml"));
        var slider = xml.Descendants("SliderWidget").Single();
        Check((string)slider.Attribute("ValueInt") == "@ValueInt" && (string)slider.Attribute("DiscreteIncrementInterval") == "1", "integer slider prefab bindings");
        Check(xml.Descendants("EditableTextWidget").Single().Attribute("Text").Value == "@ValueText", "numeric input prefab binding");
        var commands = xml.Descendants().Attributes("Command.Click").Select(a => a.Value).ToArray();
        Check(commands.Contains("ExecuteSave") && commands.Contains("ExecuteCancel") && commands.Contains("ExecuteRestoreDefaults") && commands.Contains("ExecuteToggle"), "popup commands wired");
    }
}

namespace AnimusForge
{
    public partial class DuelSettings
    {
        internal static string GetCustomPromptTextStoreDirectoryForPolicyPrompts() => throw new Exception("Tests must never access player settings.");
    }
    internal static class PolicySystemLog
    {
        internal static void Failure(string area, string code, string message, string detail) { Console.WriteLine("Diagnostic: " + area + "/" + code); }
    }
}

namespace MCM.Abstractions.Attributes
{
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class SettingPropertyGroupAttribute : Attribute
    {
        public SettingPropertyGroupAttribute(string name) { Name = name; }
        public string Name { get; }
        public int GroupOrder { get; set; }
    }
}

namespace MCM.Abstractions.Attributes.v2
{
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class SettingPropertyBoolAttribute : Attribute
    {
        public SettingPropertyBoolAttribute(string name) { Name = name; }
        public string Name { get; }
        public int Order { get; set; }
        public bool RequireRestart { get; set; }
        public string HintText { get; set; }
    }
}

namespace MCM.Abstractions.Base.Global
{
    public static class GlobalSettings<T> where T : class
    {
        private static T _instance;
        internal static bool ThrowOnRead;
        public static T Instance
        {
            get { if (ThrowOnRead) throw new Exception("Fixture MCM provider failure."); return _instance; }
            set { _instance = value; }
        }
    }
}
