using System;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using HarmonyLib;

// Real settings, native rosters and production selection callback. World eligibility,
// PartyScreen rendering and defender collection are explicitly replaced in this fixture.
internal static class CoupAdmissionRegression
{
    private const BindingFlags All = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;
    private static object _settings, _clan, _party, _roster, _soldier, _hero, _owner;
    private static int _tier;
    private static float _influence;
    private static bool _markHero;
    private static object[] _selection;

    internal static void Run(Assembly af, Assembly coup, Action<string> write)
    {
        var fixture = new Harmony("AnimusForge.Coup.RuntimeProbe.AdmissionFixture");
        Type ownerType = coup.GetType("AnimusForge.CoupSystem.CoupCampaignBehavior", true);
        Type settingsType = coup.GetType("AnimusForge.CoupSystem.CoupSettings", true);
        Type sessionType = coup.GetType("AnimusForge.CoupSystem.CoupSession", true);
        Type host = af.GetType("AnimusForge.SettlementEntryTroopSelectionBehavior", true);
        PropertyInfo singleton = ownerType.GetProperty("Instance", All);
        object previousOwner = singleton.GetValue(null);
        int checks = 0;
        Action<bool, string> check = (ok, message) =>
        {
            if (!ok) throw new InvalidOperationException("ADMISSION_FAIL " + message);
            checks++;
            write("ADMISSION_PASS " + message);
        };
        try
        {
            _settings = Activator.CreateInstance(settingsType);
            string[] names = { "MinimumClanTier", "MinimumInfluence", "MinimumTroops" };
            int[] defaults = { 4, 300, 60 }, minima = { 0, 0, 1 }, maxima = { 6, 5000, 120 };
            for (int i = 0; i < names.Length; i++)
            {
                PropertyInfo property = settingsType.GetProperty(names[i]);
                check((int)property.GetValue(_settings) == defaults[i], "MCM default " + names[i]);
                var attribute = property.GetCustomAttributesData().Single(a => a.AttributeType.Name == "SettingPropertyIntegerAttribute");
                check(Convert.ToInt32(attribute.ConstructorArguments[1].Value) == minima[i]
                    && Convert.ToInt32(attribute.ConstructorArguments[2].Value) == maxima[i], "MCM bounds " + names[i]);
                object rendered = property.GetCustomAttributes(true).Single(a => a.GetType().Name == "SettingPropertyIntegerAttribute");
                check(!(bool)rendered.GetType().GetProperty("RequireRestart").GetValue(rendered), "MCM no restart " + names[i]);
            }
            MethodInfo newGate = settingsType.GetMethod("CheckNewCoupRequirements", All);
            Func<int, float, int, bool> canBegin = (tier, influence, count) =>
                (bool)newGate.Invoke(null, new object[] { tier, influence, count, null });
            check(!canBegin(3, 300, 61) && !canBegin(4, 299, 61) && !canBegin(4, 300, 60), "real MCM gate rejects all three below-threshold cases");
            check(canBegin(4, 300, 61), "real MCM gate accepts tier4 influence300 healthy61");

            Type clanType = AccessTools.TypeByName("TaleWorlds.CampaignSystem.Clan");
            Type partyType = AccessTools.TypeByName("TaleWorlds.CampaignSystem.Party.MobileParty");
            Type characterType = AccessTools.TypeByName("TaleWorlds.CampaignSystem.CharacterObject");
            Type rosterType = AccessTools.TypeByName("TaleWorlds.CampaignSystem.Roster.TroopRoster");
            _clan = FormatterServices.GetUninitializedObject(clanType);
            _party = FormatterServices.GetUninitializedObject(partyType);
            partyType.GetProperty("StringId").SetValue(_party, "admission_party");
            _soldier = FormatterServices.GetUninitializedObject(characterType);
            _hero = FormatterServices.GetUninitializedObject(characterType);
            characterType.GetProperty("StringId").SetValue(_soldier, "admission_soldier");
            Patch(fixture, AccessTools.PropertyGetter(clanType, "PlayerClan"), nameof(PlayerClan));
            Patch(fixture, AccessTools.PropertyGetter(clanType, "Tier"), nameof(Tier));
            Patch(fixture, AccessTools.PropertyGetter(clanType, "Influence"), nameof(Influence));
            Patch(fixture, AccessTools.PropertyGetter(partyType, "MainParty"), nameof(MainParty));
            Patch(fixture, AccessTools.PropertyGetter(partyType, "MemberRoster"), nameof(Roster));
            Patch(fixture, AccessTools.PropertyGetter(characterType, "IsHero"), nameof(IsHero));
            Patch(fixture, AccessTools.Method(characterType, "Find", new[] { typeof(string) }), nameof(FindSoldier));
            Patch(fixture, AccessTools.Method(AccessTools.TypeByName("TaleWorlds.CampaignSystem.Settlements.Settlement"), "Find", new[] { typeof(string) }), nameof(Null));
            Patch(fixture, AccessTools.PropertyGetter(AccessTools.TypeByName("TaleWorlds.CampaignSystem.Campaign"), "Current"), nameof(Null));
            Type info = AccessTools.TypeByName("TaleWorlds.Library.InformationManager");
            foreach (MethodInfo display in info.GetMethods(All).Where(m => m.Name == "DisplayMessage")) Patch(fixture, display, nameof(Skip));
            _owner = Activator.CreateInstance(ownerType, true);
            singleton.SetValue(null, _owner);
            _tier = 4; _influence = 300;
            _roster = NewRoster(rosterType, 63, 2);
            _markHero = false;
            rosterType.GetMethod("AddToCounts", All).Invoke(_roster, new object[] { _hero, 1, false, 0, 0, true, -1 });
            _markHero = true;
            MethodInfo countHealthy = ownerType.GetMethod("HealthyRegulars", All);
            check((int)countHealthy.Invoke(null, new[] { _roster }) == 61, "real roster count excludes two wounded and one hero");

            object captured = settingsType.GetMethod("CaptureEntryRequirements", All).Invoke(_settings, null);
            settingsType.GetProperty("MinimumTroops").SetValue(_settings, 1);
            settingsType.GetProperty("MinimumClanTier").SetValue(_settings, 0);
            settingsType.GetProperty("MinimumInfluence").SetValue(_settings, 0);
            object session = Activator.CreateInstance(sessionType);
            Set(session, "EntryRequirements", captured);
            Set(_owner, "_session", session);
            MethodInfo savedGate = ownerType.GetMethod("CheckSessionAdmission", All);
            Func<bool> checkSaved = () => (bool)savedGate.Invoke(_owner, new object[] { null });
            check(checkSaved(), "captured requirements accept original qualified roster");
            _influence = 299;
            check(!checkSaved(), "selection-time recheck uses frozen influence requirement despite changed source settings object");
            _influence = 300; _tier = 3;
            check(!checkSaved(), "selection-time recheck rejects downgraded clan");
            _tier = 4; _roster = NewRoster(rosterType, 60, 0);
            check(!checkSaved(), "selection-time recheck retains rear guard requirement");
            Set(session, "Started", true);
            _tier = 0; _influence = 0; _roster = NewRoster(rosterType, 1, 0);
            check(checkSaved(), "registered coup bypasses admission after losses or political changes");

            // Keep the actual callback, troop validation, registration and host queue;
            // bypass unrelated world context and native UI only.
            Patch(fixture, ownerType.GetMethod("ValidatePreparedContext", All), nameof(AdmissionOnly));
            Patch(fixture, ownerType.GetMethod("BuildDefenders", All), nameof(Skip));
            Patch(fixture, coup.GetType("AnimusForge.CoupSystem.CoupTroopSelection").GetMethod("Open", All), nameof(CaptureSelection));
            foreach (string scenario in new[] { "under", "influence", "roster", "wounded", "success" })
            {
                _tier = 4; _influence = 300; _roster = NewRoster(rosterType, 61, 0);
                session = Activator.CreateInstance(sessionType);
                Set(session, "EntryRequirements", captured);
                Set(session, "SettlementId", "admission_town");
                Set(_owner, "_session", session);
                _selection = null;
                ownerType.GetMethod("OpenStreetSelection", All).Invoke(_owner, new[] { Field(session, "Id") });
                check(_selection != null && (int)_selection[1] == 60 && (int)_selection[5] == 60, "actual street picker receives frozen 60..60 range: " + scenario);
                if (scenario == "influence") _influence = 299;
                if (scenario == "roster") _roster = NewRoster(rosterType, 60, 0);
                object selected = NewRoster(rosterType, scenario == "under" ? 59 : 60, scenario == "wounded" ? 1 : 0);
                ((Delegate)_selection[3]).DynamicInvoke(selected);
                if (scenario == "success")
                {
                    check((bool)Field(session, "Started") && Field(session, "Phase").ToString() == "Street",
                        "actual callback registers assault only after all gates pass");
                    object queued = host.GetField("_armedCoupRoster", All).GetValue(null);
                    check((int)queued.GetType().GetProperty("TotalManCount").GetValue(queued) == 60,
                        "actual callback queues precisely 60 soldiers");
                    check(_influence == 300 && (int)countHealthy.Invoke(null, new[] { _roster }) == 61,
                        "admission does not spend influence or transfer real roster members");
                }
                else check(Field(_owner, "_session") == null && !(bool)Field(session, "Started"), "actual callback rejects stale or invalid selection: " + scenario);
            }
            write("PASS coup admission fixture assertions=" + checks);
            write("ADMISSION_SCOPE real MCM, native dummy rosters, saved/new admission methods and street-selection callback/queue; clan/world values, eligibility context, native PartyScreen and defender collection are stubs. No game rendering or live political outcome tested.");
        }
        finally
        {
            host.GetMethod("ClearArmedCoupEntry", All).Invoke(null, null);
            singleton.SetValue(null, previousOwner);
            fixture.UnpatchAll(fixture.Id);
            _settings = _clan = _party = _roster = _soldier = _hero = _owner = null;
        }
    }
    private static object NewRoster(Type type, int count, int wounded)
    {
        object roster = type.GetMethod("CreateDummyTroopRoster", All).Invoke(null, null);
        type.GetMethod("AddToCounts", All).Invoke(roster, new object[] { _soldier, count, false, wounded, 0, true, -1 });
        return roster;
    }
    private static object Field(object value, string name) => value.GetType().GetField(name, All).GetValue(value);
    private static void Set(object value, string name, object field) => value.GetType().GetField(name, All).SetValue(value, field);
    private static void Patch(Harmony fixture, MethodInfo target, string method) => fixture.Patch(
        AccessTools.DeclaredMethod(target.DeclaringType, target.Name, target.GetParameters().Select(p => p.ParameterType).ToArray()),
        prefix: new HarmonyMethod(typeof(CoupAdmissionRegression), method));
    private static bool PlayerClan(ref object __result) { __result = _clan; return false; }
    private static bool MainParty(ref object __result) { __result = _party; return false; }
    private static bool Roster(ref object __result) { __result = _roster; return false; }
    private static bool Tier(ref int __result) { __result = _tier; return false; }
    private static bool Influence(ref float __result) { __result = _influence; return false; }
    private static bool IsHero(object __instance, ref bool __result) { __result = _markHero && ReferenceEquals(__instance, _hero); return false; }
    private static bool FindSoldier(ref object __result) { __result = _soldier; return false; }
    private static bool Null(ref object __result) { __result = null; return false; }
    private static bool Skip() => false;
    private static bool CaptureSelection(object[] __args) { _selection = __args; return false; }
    private static bool AdmissionOnly(ref string reason, ref bool __result)
    {
        object[] args = { null };
        __result = (bool)_owner.GetType().GetMethod("CheckSessionAdmission", All).Invoke(_owner, args);
        reason = (string)args[0];
        return false;
    }
}
