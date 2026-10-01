using System;
using System.Collections;
using System.Reflection;
using System.Runtime.Serialization;
using HarmonyLib;

// Real host capture and JSON schema, detached MyBehavior; only the setting is overridden.
internal static class CoupBulletinRegression
{
    private const BindingFlags All = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
    private static bool _enabled;

    internal static void Run(Assembly af, Assembly coup, Action<string> write)
    {
        var fixture = new Harmony("AnimusForge.Coup.RuntimeProbe.BulletinFixture");
        int checks = 0;
        Action<bool, string> check = (ok, message) =>
        {
            if (!ok) throw new InvalidOperationException("BULLETIN_FAIL " + message);
            checks++;
            write("BULLETIN_PASS " + message);
        };
        Type ownerType = af.GetType("AnimusForge.MyBehavior", true);
        fixture.Patch(ownerType.GetMethod("IsWorldBulletinEnabled", All), prefix: new HarmonyMethod(typeof(CoupBulletinRegression), nameof(Enabled)));
        try
        {
            object owner = FormatterServices.GetUninitializedObject(ownerType);
            MethodInfo capture = ownerType.GetMethod("TryRecordCoupOutcomeForBulletin", All);
            FieldInfo stateField = ownerType.GetField("_worldBulletinState", All);
            Func<string, bool, bool> record = (id, success) => (bool)capture.Invoke(owner, new object[] {
                id, success, "领主在加伦发动政变，经过街战和大厅作战，最终结果已确认。", "发生时间：某日", "vlandia", "player_realm" });
            _enabled = false;
            check(record("disabled", true) && stateField.GetValue(owner) == null, "settings-off handled without creating a bulletin or blocking settlement");
            _enabled = true;
            check(!record(" ", true), "empty coup id rejected");
            check(record("one", true), "actual host captures dedicated coup outcome");
            object state = stateField.GetValue(owner);
            Func<IList> events = () => (IList)stateField.GetValue(owner).GetType().GetField("Events").GetValue(stateField.GetValue(owner));
            object fact = events()[0];
            Type factType = fact.GetType();
            check((string)factType.GetField("Kind").GetValue(fact) == "coup_success" && (int)factType.GetField("Score").GetValue(fact) == 95, "success classified and scored as coup headline");
            check((string)factType.GetField("Key").GetValue(fact) == "coup:one:bulletin" && (string)factType.GetField("Group").GetValue(fact) == "coup:one", "stable identity groups one coup");
            var realms = (IList)factType.GetField("KingdomIds").GetValue(fact);
            check(realms.Contains("vlandia") && realms.Contains("player_realm"), "original and player realm remain visible to regional NPC knowledge");
            check(record("one", true) && events().Count == 1, "immediate retry is accepted without duplicate capture");
            for (int i = 0; i < 100; i++)
            {
                object other = Activator.CreateInstance(factType);
                factType.GetField("Key").SetValue(other, "unrelated:" + i);
                events().Add(other);
            }
            check(record("one", true) && events().Count == 101, "retry beyond generic 64-event tail remains idempotent");
            stateField.SetValue(owner, Roundtrip(state, state.GetType()));
            check(record("one", true) && events().Count == 101, "save JSON roundtrip retains dedup identity");
            check(record("two", false) && events().Count == 102, "another coup gets an independent fact");
            fact = events()[101];
            check((string)factType.GetField("Kind").GetValue(fact) == "coup_failure" && (int)factType.GetField("Score").GetValue(fact) == 80, "failure has explicit classification and trigger score");
            Type receiptType = coup.GetType("AnimusForge.CoupSystem.CoupRebellionBridge", true).GetNestedType("Outcome", All);
            object receipt = Activator.CreateInstance(receiptType, true);
            receiptType.GetField("BulletinText").SetValue(receipt, "已确认且不会随改名改变的快报事实");
            receiptType.GetField("BulletinHandled").SetValue(receipt, true);
            receiptType.GetField("OriginalKingdomId").SetValue(receipt, "vlandia");
            object restored = Roundtrip(receipt, receiptType);
            check((bool)receiptType.GetField("BulletinHandled").GetValue(restored)
                && (string)receiptType.GetField("BulletinText").GetValue(restored) == "已确认且不会随改名改变的快报事实"
                && (string)receiptType.GetField("OriginalKingdomId").GetValue(restored) == "vlandia", "outcome receipt persists frozen facts and completion flag");
            write("PASS coup bulletin host fixture assertions=" + checks);
            write("BULLETIN_SCOPE production host capture with detached MyBehavior, setting override and real Newtonsoft JSON; no live political settlement, game save, LLM or published UI exercised.");
        }
        finally { fixture.UnpatchAll(fixture.Id); }
    }
    private static bool Enabled(ref bool __result) { __result = _enabled; return false; }
    private static object Roundtrip(object value, Type type)
    {
        Type json = AccessTools.TypeByName("Newtonsoft.Json.JsonConvert");
        string text = (string)json.GetMethod("SerializeObject", new[] { typeof(object) }).Invoke(null, new[] { value });
        return json.GetMethod("DeserializeObject", new[] { typeof(string), typeof(Type) }).Invoke(null, new object[] { text, type });
    }
}
