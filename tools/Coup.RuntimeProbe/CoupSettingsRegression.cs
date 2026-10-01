using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using HarmonyLib;

// Production MCM metadata, JSON, adapter, entry preparation and reserve-wave scheduling.
// Native spawning, time, eligibility and world lookups are explicit synthetic boundaries.
internal static class CoupSettingsRegression
{
    private const BindingFlags All = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;
    private static float _time;
    private static int _activeWaves, _spawnCalls, _lastSpawnCount;
    private static object _soldier;

    internal static void Run(Assembly af, Assembly coup, Action<string> write)
    {
        var fixture = new Harmony("AnimusForge.Coup.RuntimeProbe.SettingsFixture");
        Type host = af.GetType("AnimusForge.SettlementEntryTroopSelectionBehavior", true);
        Type adapter = coup.GetType("AnimusForge.CoupSystem.SettlementEntryTroopSelectionBehavior", true);
        Type settingsType = coup.GetType("AnimusForge.CoupSystem.CoupSettings", true);
        Type sessionType = coup.GetType("AnimusForge.CoupSystem.CoupSession", true);
        Type logicType = host.GetNestedType("SettlementEntryTroopSelectionMissionLogic", All);
        PropertyInfo pendingProperty = host.GetProperty("_pendingMissionEntry", All);
        object previousPending = pendingProperty.GetValue(null);
        int checks = 0;
        Action<bool, string> check = (ok, message) =>
        {
            if (!ok) throw new InvalidOperationException("SETTINGS_FAIL " + message);
            checks++;
            write("SETTINGS_PASS " + message);
        };
        try
        {
            object settings = Activator.CreateInstance(settingsType);
            string[] names = { "StreetAllyLimit", "HallAllyLimit", "GateGuardLimit", "HallGuardLimit",
                "DefenderWaveSize", "DefenderWaveIntervalSeconds", "MaxActiveDefenderWaves" };
            int[] defaults = { 60, 20, 10, 20, 30, 30, 4 }, maxima = { 120, 40, 30, 40, 60, 120, 4 };
            for (int i = 0; i < names.Length; i++)
            {
                PropertyInfo property = settingsType.GetProperty(names[i]);
                check((int)property.GetValue(settings) == defaults[i], "MCM default " + names[i]);
                object attribute = property.GetCustomAttributes(true).Single(a => a.GetType().Name == "SettingPropertyIntegerAttribute");
                check(!(bool)attribute.GetType().GetProperty("RequireRestart").GetValue(attribute), "MCM no restart " + names[i]);
                var metadata = property.GetCustomAttributesData().Single(a => a.AttributeType.Name == "SettingPropertyIntegerAttribute");
                check(Convert.ToInt32(metadata.ConstructorArguments[1].Value) == (i == 5 ? 5 : 1)
                    && Convert.ToInt32(metadata.ConstructorArguments[2].Value) == maxima[i], "MCM actual slider range " + names[i]);
                property.SetValue(settings, maxima[i]);
            }
            object options = settingsType.GetMethod("CaptureBattleOptions", All).Invoke(settings, null);
            settingsType.GetProperty("StreetAllyLimit").SetValue(settings, 1);
            check((int)Field(options, "StreetAllyLimit") == 120, "captured options detached from later MCM changes");
            object session = Activator.CreateInstance(sessionType);
            Set(session, "BattleOptions", options);
            Type json = AccessTools.TypeByName("Newtonsoft.Json.JsonConvert");
            var serialize = json.GetMethod("SerializeObject", new[] { typeof(object) });
            var deserialize = json.GetMethod("DeserializeObject", new[] { typeof(string), typeof(Type) });
            string saved = (string)serialize.Invoke(null, new[] { session });
            object restored = deserialize.Invoke(null, new object[] { saved, sessionType });
            check((int)Field(Field(restored, "BattleOptions"), "StreetAllyLimit") == 120, "actual Newtonsoft retains expanded snapshot");
            object legacy = deserialize.Invoke(null, new object[] { "{}", sessionType });
            check((int)Field(Field(legacy, "BattleOptions"), "StreetAllyLimit") == 60, "actual Newtonsoft missing snapshot restores legacy defaults");
            foreach (string bad in new[] { "{\"BattleOptions\":null}", "{\"BattleOptions\":{\"StreetAllyLimit\":60}}" })
            {
                object badOptions = Field(deserialize.Invoke(null, new object[] { bad, sessionType }), "BattleOptions");
                check(badOptions == null || !(bool)badOptions.GetType().GetMethod("IsValid", All).Invoke(badOptions, null), "actual Newtonsoft preserves malformed snapshot for rejection");
            }

            Type characterType = AccessTools.TypeByName("TaleWorlds.CampaignSystem.CharacterObject");
            _soldier = FormatterServices.GetUninitializedObject(characterType);
            characterType.GetProperty("StringId").SetValue(_soldier, "settings_soldier");
            Patch(fixture, characterType.GetMethod("Find", All, null, new[] { typeof(string) }, null), nameof(FindSoldier));
            Type rosterType = AccessTools.TypeByName("TaleWorlds.CampaignSystem.Roster.TroopRoster");
            object roster = rosterType.GetMethod("CreateDummyTroopRoster", All).Invoke(null, null);
            rosterType.GetMethod("AddToCounts", All).Invoke(roster, new object[] { _soldier, 121, false, 0, 0, true, -1 });
            Type townType = AccessTools.TypeByName("TaleWorlds.CampaignSystem.Settlements.Settlement");
            object town = FormatterServices.GetUninitializedObject(townType);
            townType.GetProperty("StringId").SetValue(town, "settings_town");
            Type locationType = AccessTools.TypeByName("TaleWorlds.CampaignSystem.Settlements.Locations.Location");
            object location = FormatterServices.GetUninitializedObject(locationType);
            Patch(fixture, host.GetMethod("ShouldPrepareSettlementEntry", All), nameof(Yes));
            Patch(fixture, host.GetMethod("EnsureProfileRosters", All), nameof(Skip));
            MethodInfo prepare = host.GetMethod("TryPrepareSettlementEntryMission", All);
            object sceneKind = Enum.Parse(prepare.GetParameters()[3].ParameterType, "Town");
            object streetLogic = null;
            foreach (string locationId in new[] { "center", "lordshall" })
            {
                locationType.GetProperty("StringId", All).SetValue(location, locationId);
                adapter.GetMethod("QueueArmedCoup", All).Invoke(null, new[] { "settings_town", locationId, roster, null, options });
                prepare.Invoke(null, new[] { town, location, locationId, sceneKind });
                object entry = pendingProperty.GetValue(null);
                int expected = locationId == "center" ? 120 : 40;
                check(entry != null && (int)Field(entry, "Limit") == expected, "real adapter and prepare retain expanded cap " + locationId);
                check((int)Field(entry, "SelectedRoster").GetType().GetProperty("TotalManCount").GetValue(Field(entry, "SelectedRoster")) == expected,
                    "real entry roster no longer truncated to legacy cap " + locationId);
                object logic = Activator.CreateInstance(logicType, All, null, new[] { entry }, null);
                check((int)Field(logic, "_defenderWaveSize") == 60 && (float)Field(logic, "_defenderWaveIntervalSeconds") == 120f
                    && (int)Field(logic, "_maxActiveDefenderWaves") == 4, "mission constructor receives options " + locationId);
                if (locationId == "center") streetLogic = logic;
            }
            host.GetMethod("QueueArmedCoupEntry", All).Invoke(null, new[] { "settings_town", "center", roster, null });
            check((int)host.GetField("_armedCoupEntryLimit", All).GetValue(null) == 60
                && (int)host.GetField("_armedCoupWaveSize", All).GetValue(null) == 30, "old four-argument entry retains defaults");
            object ordinaryEntry = Activator.CreateInstance(pendingProperty.PropertyType);
            Set(ordinaryEntry, "IsOwnSettlement", true);
            Set(ordinaryEntry, "SceneKind", sceneKind);
            Set(ordinaryEntry, "CoupWaveSize", 60);
            Set(ordinaryEntry, "CoupWaveIntervalSeconds", 120);
            Set(ordinaryEntry, "CoupMaxActiveWaves", 1);
            object ordinary = Activator.CreateInstance(logicType, All, null, new[] { ordinaryEntry }, null);
            check((int)Field(ordinary, "_defenderWaveSize") == 30 && (float)Field(ordinary, "_defenderWaveIntervalSeconds") == 30f
                && (int)Field(ordinary, "_maxActiveDefenderWaves") == 4, "ordinary SETS ignores all coup overrides");

            Type missionType = AccessTools.TypeByName("TaleWorlds.MountAndBlade.Mission");
            object mission = FormatterServices.GetUninitializedObject(missionType);
            logicType.GetProperty("Mission", All).SetValue(streetLogic, mission);
            Patch(fixture, missionType.GetProperty("CurrentTime", All).GetMethod, nameof(Time));
            foreach (string method in new[] { "EnsureEnemyTeam", "RefreshSetsUsableProtectionState", "ResetDefenderReserveProgress", "RefreshEnemyNativeCombatOrders" })
                Patch(fixture, logicType.GetMethod(method, All), nameof(Skip));
            Patch(fixture, logicType.GetMethod("SpawnAgentsNearPlayer", All), nameof(Spawn));
            Patch(fixture, logicType.GetMethod("CountActiveDefenderReserveWaves", All), nameof(ActiveWaves));
            Type infoType = AccessTools.TypeByName("TaleWorlds.Library.InformationManager");
            foreach (var method in infoType.GetMethods(All).Where(m => m.Name == "DisplayMessage")) Patch(fixture, method, nameof(Skip));
            IList reserve = (IList)Field(streetLogic, "_remainingDefenderReserve");
            Type reserveType = reserve.GetType().GetGenericArguments()[0];
            for (int i = 0; i < 65; i++)
            {
                object soldier = Activator.CreateInstance(reserveType, true);
                Set(soldier, "Character", _soldier);
                Set(soldier, "SourceKind", "garrison");
                reserve.Add(soldier);
            }
            _time = 10;
            _activeWaves = _spawnCalls = 0;
            logicType.GetMethod("SpawnInitialDefenderReserveWave", All).Invoke(streetLogic, null);
            check(_spawnCalls == 1 && _lastSpawnCount == 60 && reserve.Count == 5, "initial wave uses configured size and real remaining reserve");
            check((float)Field(streetLogic, "_nextDefenderReserveWaveTime") == 130f, "initial wave schedules configured interval");
            logicType.GetMethod("SpawnInitialDefenderReserveWave", All).Invoke(streetLogic, null);
            check(_spawnCalls == 1, "initial wave cannot spawn twice");
            MethodInfo timed = logicType.GetMethod("TrySpawnTimedDefenderReserveWave", All);
            _time = 129;
            timed.Invoke(streetLogic, null);
            check(_spawnCalls == 1, "no timed reinforcement before interval");
            _time = 130;
            _activeWaves = 4;
            timed.Invoke(streetLogic, null);
            check(_spawnCalls == 1 && reserve.Count == 5, "active wave cap delays reinforcements");
            _activeWaves = 3;
            timed.Invoke(streetLogic, null);
            check(_spawnCalls == 2 && _lastSpawnCount == 5 && reserve.Count == 0, "vacated wave slot spawns only remaining real troops");
            _time = 1000;
            timed.Invoke(streetLogic, null);
            check(_spawnCalls == 2, "exhausted reserve never creates new troops");
            bool rejected = false;
            try { host.GetMethod("QueueArmedCoupEntryWithOptions", All).Invoke(null, new[] { "settings_town", "center", roster, null, (object)121, 60, 120, 4 }); }
            catch (TargetInvocationException ex) { rejected = ex.InnerException is ArgumentOutOfRangeException; }
            check(rejected && host.GetField("_armedCoupSettlementId", All).GetValue(null) == null, "invalid entry values rejected and stale queue cleared");
            CheckDefenderAllocation(coup, town, roster, options, check);
            write("PASS coup settings fixture assertions=" + checks);
            write("SETTINGS_SCOPE real settings metadata/Newtonsoft/bridge/entry preparation/constructor/scheduling; synthetic world, time, active-wave count and native spawn boundaries; no live mission or MCM rendering.");
        }
        finally
        {
            host.GetMethod("ClearArmedCoupEntry", All).Invoke(null, null);
            pendingProperty.SetValue(null, previousPending);
            fixture.UnpatchAll(fixture.Id);
            _soldier = null;
        }
    }

    private static object _defenderParty, _defenderRoster, _townComponent, _emptyParties, _playerClan;
    private static void CheckDefenderAllocation(Assembly coup, object town, object roster, object options, Action<bool, string> check)
    {
        var fixture = new Harmony("AnimusForge.Coup.RuntimeProbe.DefenderAllocationFixture");
        Type ownerType = coup.GetType("AnimusForge.CoupSystem.CoupCampaignBehavior", true);
        Type partyType = AccessTools.TypeByName("TaleWorlds.CampaignSystem.Party.MobileParty");
        Type clanType = AccessTools.TypeByName("TaleWorlds.CampaignSystem.Clan");
        Type townType = town.GetType();
        try
        {
            _defenderParty = FormatterServices.GetUninitializedObject(partyType);
            (partyType.GetProperty("StringId", All) ?? throw new MissingMemberException(partyType.FullName, "StringId")).SetValue(_defenderParty, "settings_garrison");
            _playerClan = FormatterServices.GetUninitializedObject(clanType);
            FieldInfo townField = townType.GetField("Town", All) ?? throw new MissingMemberException(townType.FullName, "Town");
            _townComponent = FormatterServices.GetUninitializedObject(townField.FieldType);
            townField.SetValue(town, _townComponent);
            Type partiesType = (townType.GetProperty("Parties", All) ?? throw new MissingMemberException(townType.FullName, "Parties")).PropertyType;
            // MBReadOnlyList derives from List in the tested game; use its normal empty constructor.
            _emptyParties = Activator.CreateInstance(partiesType);
            Patch(fixture, ownerType.GetMethod("FindKing", All), nameof(Null));
            Patch(fixture, partyType.GetProperty("MainParty", All).GetMethod, nameof(Null));
            Patch(fixture, partyType.GetProperty("ActualClan", All).GetMethod, nameof(Null));
            Patch(fixture, partyType.GetProperty("MemberRoster", All).GetMethod, nameof(DefenderRoster));
            Patch(fixture, clanType.GetProperty("PlayerClan", All).GetMethod, nameof(PlayerClan));
            if (townType.GetProperty("MilitiaPartyComponent", All) is PropertyInfo militia)
                Patch(fixture, militia.GetMethod, nameof(Null));
            Patch(fixture, townType.GetProperty("Parties", All).GetMethod, nameof(EmptyParties));
            Patch(fixture, _townComponent.GetType().GetProperty("GarrisonParty", All).GetMethod, nameof(DefenderParty));
            Patch(fixture, _soldier.GetType().GetProperty("Tier", All).GetMethod, nameof(Tier));
            object owner = Activator.CreateInstance(ownerType, true);
            Type sessionType = coup.GetType("AnimusForge.CoupSystem.CoupSession", true);
            foreach (int healthy in new[] { 0, 3, 45, 75 })
            {
                _defenderRoster = roster.GetType().GetMethod("CreateDummyTroopRoster", All).Invoke(null, null);
                roster.GetType().GetMethod("AddToCounts", All).Invoke(_defenderRoster, new object[] { _soldier, healthy + 2, false, 2, 0, true, -1 });
                object session = Activator.CreateInstance(sessionType);
                Set(session, "BattleOptions", options);
                Set(owner, "_session", session);
                ownerType.GetMethod("BuildDefenders", All).Invoke(owner, new[] { town });
                var troops = ((IList)Field(session, "Troops")).Cast<object>().ToList();
                check(troops.Count == healthy, "real defender allocation excludes wounded and creates no extra troops: healthy=" + healthy);
                int hall = Math.Min(healthy, 40), gate = Math.Min(Math.Max(0, healthy - hall), 30);
                check(troops.Count(t => Field(t, "Role").ToString() == "HallGuard") == hall
                    && troops.Count(t => Field(t, "Role").ToString() == "GateGuard") == gate
                    && troops.Count(t => Field(t, "Role").ToString() == "StreetDefender") == healthy - hall - gate,
                    "real defender allocation applies hall/gate caps with shortages: healthy=" + healthy);
            }
        }
        finally
        {
            fixture.UnpatchAll(fixture.Id);
            _defenderParty = _defenderRoster = _townComponent = _emptyParties = _playerClan = null;
        }
    }
    private static object Field(object value, string name) => value.GetType().GetField(name, All).GetValue(value);
    private static void Set(object value, string name, object field) => value.GetType().GetField(name, All).SetValue(value, field);
    private static void Patch(Harmony fixture, MethodInfo target, string method) => fixture.Patch(
        AccessTools.DeclaredMethod(target.DeclaringType, target.Name, target.GetParameters().Select(p => p.ParameterType).ToArray()),
        prefix: new HarmonyMethod(typeof(CoupSettingsRegression), method));
    private static bool Skip() => false;
    private static bool Null(ref object __result) { __result = null; return false; }
    private static bool DefenderParty(ref object __result) { __result = _defenderParty; return false; }
    private static bool DefenderRoster(ref object __result) { __result = _defenderRoster; return false; }
    private static bool EmptyParties(ref object __result) { __result = _emptyParties; return false; }
    private static bool PlayerClan(ref object __result) { __result = _playerClan; return false; }
    private static bool Tier(ref int __result) { __result = 3; return false; }
    private static bool Yes(ref bool __result) { __result = true; return false; }
    private static bool FindSoldier(ref object __result) { __result = _soldier; return false; }
    private static bool Time(ref float __result) { __result = _time; return false; }
    private static bool ActiveWaves(ref int __result) { __result = _activeWaves; return false; }
    private static bool Spawn(object[] __args, ref int __result)
    {
        IList requested = (IList)__args[5], spawned = (IList)__args[7];
        foreach (object value in requested) spawned.Add(value);
        _lastSpawnCount = __result = requested.Count;
        _spawnCalls++;
        return false;
    }
}
