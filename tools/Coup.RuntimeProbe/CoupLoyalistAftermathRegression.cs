using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using HarmonyLib;

internal static class CoupLoyalistAftermathRegression
{
    private const BindingFlags All = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;
    private static object _oldKing, _newKing, _clans, _playerClan;
    private static readonly Dictionary<object, object> Leaders = new Dictionary<object, object>();
    private static readonly Dictionary<object, int[]> Relations = new Dictionary<object, int[]>();
    private static readonly HashSet<object> Eligible = new HashSet<object>();
    private static bool _enabled, _protected;
    private static object _restorationHome;
    private static bool _oldKingCaptured;

    internal static void Run(Assembly af, Assembly coup, Action<string> write)
    {
        var fixture = new Harmony("AnimusForge.Coup.RuntimeProbe.LoyalistAftermathFixture");
        Type managerType = AccessTools.TypeByName("TaleWorlds.ObjectSystem.MBObjectManager");
        PropertyInfo managerInstance = managerType.GetProperty("Instance", All);
        object previousManager = managerInstance.GetValue(null);
        int checks = 0;
        Action<bool, string> check = (ok, message) =>
        {
            if (!ok) throw new InvalidOperationException("LOYALIST_AFTERMATH_FAIL " + message);
            checks++; write("LOYALIST_AFTERMATH_PASS " + message);
        };
        try
        {
            Type heroType = AccessTools.TypeByName("TaleWorlds.CampaignSystem.Hero");
            Type clanType = AccessTools.TypeByName("TaleWorlds.CampaignSystem.Clan");
            Type kingdomType = AccessTools.TypeByName("TaleWorlds.CampaignSystem.Kingdom");
            object objects = managerType.GetMethod("Init").Invoke(null, null);
            managerType.GetMethod("RegisterType").MakeGenericMethod(heroType).Invoke(objects, new object[] { "Hero", "Heroes", (uint)1, false, false });
            _oldKing = Bare(heroType); _newKing = Bare(heroType);
            heroType.GetProperty("StringId").SetValue(_oldKing, "probe_old_king");
            managerType.GetMethod("RegisterObject").MakeGenericMethod(heroType).Invoke(objects, new[] { _oldKing });
            object kingdom = Bare(kingdomType);
            _clans = Activator.CreateInstance(kingdomType.GetProperty("Clans").PropertyType);
            object royal = NewClan(clanType, "royal", 100, 90);
            object supporter = NewClan(clanType, "supporter", 80, 40);
            NewClan(clanType, "neutral", 40, 40);
            NewClan(clanType, "new_loyalist", 20, 80);
            Patch(fixture, AccessTools.PropertyGetter(kingdomType, "Clans"), nameof(Clans));
            Patch(fixture, AccessTools.PropertyGetter(kingdomType, "Leader"), nameof(NewKing));
            Patch(fixture, AccessTools.PropertyGetter(clanType, "Leader"), nameof(Leader));
            Patch(fixture, AccessTools.PropertyGetter(clanType, "Tier"), nameof(Tier));
            Patch(fixture, heroType.GetMethod("GetRelation", new[] { heroType }), nameof(Relation));
            Type hostOwner = af.GetType("AnimusForge.MyBehavior", true);
            Patch(fixture, hostOwner.GetMethod("TryValidateClanForKingdomRebellion", All), nameof(PhysicalEligibility));
            Type settings = af.GetType("AnimusForge.DuelSettings", true);
            Patch(fixture, settings.GetMethod("IsKingdomStabilityAndRebellionEnabled", All), nameof(Enabled));
            Patch(fixture, af.GetType("AnimusForge.PlayerKingdomRebellionImmunity", true).GetMethod("ShouldProtectKingdom", All), nameof(Protected));
            _enabled = true; _protected = false;
            Type bridgeType = coup.GetType("AnimusForge.CoupSystem.CoupRebellionBridge", true);
            object bridge = Bare(bridgeType), host = Bare(hostOwner);
            MethodInfo select = bridgeType.GetMethod("SelectLoyalists", All);
            Func<IList> selectClans = () => (IList)select.Invoke(bridge, new[] { host, kingdom, "probe_old_king", "royal", (object)true, null });
            IList selected = selectClans();
            check(selected.Count == 2 && (string)Field(selected[0], "ClanId") == "royal", "real selector prioritizes old royal family and includes a supporter friendly to new king");
            check((string)Field(selected[1], "ClanId") == "supporter", "ties and new-king supporters do not rebel");
            IList captured = (IList)select.Invoke(bridge, new[] { host, kingdom, "probe_old_king", "royal", (object)false, null });
            check(captured.Count == 1 && (string)Field(captured[0], "ClanId") == "supporter", "captured old king's family excluded even when otherwise physically eligible");
            Eligible.Remove(royal);
            selected = selectClans();
            check(selected.Count == 1 && (string)Field(selected[0], "ClanId") == "supporter", "unavailable royal leader does not prevent eligible supporter leading");
            Eligible.Remove(supporter);
            check(selectClans().Count == 0, "no physical supporters produces no fabricated rebellion");
            Eligible.Add(supporter); _protected = true;
            check(selectClans().Count == 0, "player kingdom immunity remains respected");
            _protected = false; _enabled = false;
            check(selectClans().Count == 0, "master rebellion switch remains respected");
            _enabled = true;
            Type requestType = bridgeType.GetNestedType("Request", All);
            object request = Activator.CreateInstance(requestType, true);
            Set(request, "LoyalistSelection", true); Set(request, "FormerKingId", "probe_old_king"); Set(request, "FormerRulingClanId", "royal");
            MethodInfo supports = bridgeType.GetMethod("StillSupportsFormerKing", All);
            check((bool)supports.Invoke(null, new[] { request, supporter, kingdom }), "pending supporter revalidation accepts positive relation to new king");
            Relations[Leaders[supporter]] = new[] { 30, 80 };
            check(!(bool)supports.Invoke(null, new[] { request, supporter, kingdom }), "pending supporter revalidation rejects changed allegiance");
            object restored = Roundtrip(request);
            check((bool)Field(restored, "LoyalistSelection") && (string)Field(restored, "FormerKingId") == "probe_old_king", "loyalist policy and former king survive real JSON roundtrip");
            check(!(bool)Field(Activator.CreateInstance(requestType, true), "LoyalistSelection"), "old requests retain their original selection policy");

            managerType.GetMethod("RegisterType").MakeGenericMethod(clanType).Invoke(objects, new object[] { "Clan", "Clans", (uint)2, false, false });
            object royalLeader = Leaders[royal];
            Leaders.Remove(royal); // Native registration changes MBObjectBase's identity hash.
            managerType.GetMethod("RegisterObject").MakeGenericMethod(clanType).Invoke(objects, new[] { royal });
            Leaders[royal] = royalLeader;
            _restorationHome = kingdom; _oldKingCaptured = false;
            _playerClan = Bare(clanType);
            Patch(fixture, AccessTools.PropertyGetter(clanType, "PlayerClan"), nameof(PlayerClan));
            Patch(fixture, AccessTools.PropertyGetter(clanType, "Kingdom"), nameof(RestorationHome));
            Patch(fixture, AccessTools.PropertyGetter(heroType, "IsAlive"), nameof(Yes));
            Patch(fixture, AccessTools.PropertyGetter(heroType, "IsChild"), nameof(No));
            Patch(fixture, AccessTools.PropertyGetter(heroType, "IsPrisoner"), nameof(Prisoner));
            MethodInfo canJoin = bridgeType.GetMethod("CanJoinRestoration", All);
            object[] joinArgs = { request, kingdom, null };
            check((bool)canJoin.Invoke(null, joinArgs) && ReferenceEquals(joinArgs[2], royal), "released royal family can join supporter-led restoration without a land requirement");
            _oldKingCaptured = true;
            check(!(bool)canJoin.Invoke(null, new object[] { request, kingdom, null }), "prisoner old king cannot join restoration through family transfer");
            _oldKingCaptured = false;
            Set(request, "Id", "restoration_probe"); Set(request, "TrackCivilWar", true); Set(request, "RestoreDynasty", true);
            Set(request, "OriginalName", "Original realm"); Set(request, "OriginalShortName", "Original");
            Set(request, "RebelKingdomId", "created_rebels"); Set(request, "RegistrationBlocked", true);
            Set(request, "State", Enum.Parse(requestType.GetField("State").FieldType, "WarCreated"));
            restored = Roundtrip(request);
            check((bool)Field(restored, "RegistrationBlocked") && Field(restored, "State").ToString() == "WarCreated"
                && (string)Field(restored, "OriginalName") == "Original realm", "created-war checkpoint and original name persist without recreating kingdom");
            FieldInfo requests = bridgeType.GetField("_requests", All);
            var pending = (IDictionary)Activator.CreateInstance(requests.FieldType); pending.Add("restoration_probe", restored); requests.SetValue(bridge, pending);
            bridgeType.GetMethod("RetryPendingWarRegistration", All).Invoke(bridge, null);
            check(!(bool)Field(restored, "RegistrationBlocked") && Field(restored, "State").ToString() == "WarCreated"
                && (bool)Field(bridge, "_hasWork"), "manual registration retry resumes only created-war stage");

            // The old siege-aftermath flow was intentionally retired. Validate the
            // current saved feedback contract; native callbacks are exercised by
            // Coup.VictoryFlowTests, not by resurrecting a deleted menu here.
            Type sessionType = coup.GetType("AnimusForge.CoupSystem.CoupSession", true);
            Type ownerType = coup.GetType("AnimusForge.CoupSystem.CoupCampaignBehavior", true);
            check(ownerType.GetMethod("TryOpenAftermath", All) == null
                && ownerType.GetMethod("PresentVictoryFeedback", All) != null,
                "current coup uses victory feedback instead of the retired siege aftermath menu");
            object session = Activator.CreateInstance(sessionType);
            Set(session, "Phase", Enum.Parse(sessionType.GetField("Phase").FieldType, "Completed"));
            Set(session, "Disposition", Enum.Parse(sessionType.GetField("Disposition").FieldType, "Release"));
            foreach (string field in new[] { "Started", "KingSubdued", "CasualtiesCommitted", "RulingClanCommitted", "TownCommitted", "CustodyCommitted", "FactsCommitted", "RebellionQueued" }) Set(session, field, true);
            Set(session, "VictoryReportAcknowledged", false); Set(session, "CoronationRequested", false);
            check((bool)sessionType.GetProperty("HasConfirmedVictory").GetValue(session), "feedback requires confirmed political and fact commits");
            check((bool)sessionType.GetProperty("NeedsVictoryFeedback").GetValue(session), "new completed victory requires feedback");
            object restoredSession = Roundtrip(session);
            check((bool)sessionType.GetProperty("NeedsVictoryFeedback").GetValue(restoredSession), "pending feedback survives JSON reload");
            Set(restoredSession, "VictoryReportAcknowledged", true);
            check(!(bool)sessionType.GetProperty("NeedsVictoryFeedback").GetValue(restoredSession), "acknowledged victory cannot replay feedback");
            Set(session, "VictoryReportAcknowledged", null);
            check(!(bool)sessionType.GetProperty("NeedsVictoryFeedback").GetValue(session), "historical completed save does not invent new feedback");
            Set(session, "FactsCommitted", false);
            check(!(bool)sessionType.GetProperty("HasConfirmedVictory").GetValue(session), "uncommitted facts cannot confirm victory");
            write("PASS coup loyalist and victory-feedback fixture assertions=" + checks);
            write("LOYALIST_AFTERMATH_SCOPE real loyalist selector/recovery checkpoint and current victory-feedback JSON contract; synthetic relations and physical eligibility. Native victory callbacks are covered separately by Coup.VictoryFlowTests; no LLM/native politics/save/UI acceptance.");
        }
        finally
        {
            fixture.UnpatchAll(fixture.Id); managerInstance.SetValue(null, previousManager);
            Leaders.Clear(); Relations.Clear(); Eligible.Clear();
        }
    }
    private static object NewClan(Type clanType, string id, int oldRelation, int newRelation)
    {
        object clan = Bare(clanType), hero = Bare(_oldKing.GetType());
        clanType.GetProperty("StringId").SetValue(clan, id);
        Leaders[clan] = hero; Relations[hero] = new[] { oldRelation, newRelation }; Eligible.Add(clan);
        _clans.GetType().GetMethod("Add").Invoke(_clans, new[] { clan }); return clan;
    }
    private static object Bare(Type type) => FormatterServices.GetUninitializedObject(type);
    private static object Field(object value, string name) => value.GetType().GetField(name, All).GetValue(value);
    private static void Set(object value, string name, object field) => value.GetType().GetField(name, All).SetValue(value, field);
    private static object Roundtrip(object value)
    {
        Type json = AccessTools.TypeByName("Newtonsoft.Json.JsonConvert");
        string text = (string)json.GetMethod("SerializeObject", new[] { typeof(object) }).Invoke(null, new[] { value });
        return json.GetMethod("DeserializeObject", new[] { typeof(string), typeof(Type) }).Invoke(null, new[] { (object)text, value.GetType() });
    }
    private static void Patch(Harmony h, MethodInfo target, string prefix) => h.Patch(AccessTools.DeclaredMethod(target.DeclaringType, target.Name,
        target.GetParameters().Select(p => p.ParameterType).ToArray()), prefix: new HarmonyMethod(typeof(CoupLoyalistAftermathRegression), prefix));
    private static bool PhysicalEligibility(object[] __args, ref bool __result)
    {
        if (!(bool)__args[2]) throw new InvalidOperationException("Coup must bypass ordinary negative-relation threshold");
        __result = Eligible.Contains(__args[0]); __args[3] = __result ? "" : "fixture physical restriction (prisoner or no land)";
        __args[4] = Relations[Leaders[__args[0]]][1]; __args[5] = 1; __args[6] = 0; return false;
    }
    private static bool Clans(ref object __result) { __result = _clans; return false; }
    private static bool NewKing(ref object __result) { __result = _newKing; return false; }
    private static bool Leader(object __instance, ref object __result) { Leaders.TryGetValue(__instance, out __result); return false; }
    private static bool Tier(ref int __result) { __result = 4; return false; }
    private static bool Relation(object __instance, object[] __args, ref int __result) { __result = Relations[__instance][ReferenceEquals(__args[0], _oldKing) ? 0 : 1]; return false; }
    private static bool Enabled(ref bool __result) { __result = _enabled; return false; }
    private static bool Protected(ref bool __result) { __result = _protected; return false; }
    private static bool PlayerClan(ref object __result) { __result = _playerClan; return false; }
    private static bool RestorationHome(ref object __result) { __result = _restorationHome; return false; }
    private static bool Yes(ref bool __result) { __result = true; return false; }
    private static bool No(ref bool __result) { __result = false; return false; }
    private static bool Prisoner(object __instance, ref bool __result) { __result = _oldKingCaptured && ReferenceEquals(__instance, _oldKing); return false; }
}
