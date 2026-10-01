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
    private static object _oldKing, _newKing, _clans, _town, _playerClan, _ownerClan, _party, _encounter, _game, _manager, _map, _soldier;
    private static readonly Dictionary<object, object> Leaders = new Dictionary<object, object>();
    private static readonly Dictionary<object, int[]> Relations = new Dictionary<object, int[]>();
    private static readonly HashSet<object> Eligible = new HashSet<object>();
    private static bool _enabled, _protected, _menuSucceeds;
    private static int _menuCalls, _survivors;
    private static bool _transfer;
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

            // Real Coup owner -> cached adapter -> SETS gate. Only native menu opening
            // is replaced, so the ownership-transfer argument is checked at the boundary.
            Type townType = AccessTools.TypeByName("TaleWorlds.CampaignSystem.Settlements.Settlement");
            Type partyType = AccessTools.TypeByName("TaleWorlds.CampaignSystem.Party.MobileParty");
            Type encounterType = AccessTools.TypeByName("TaleWorlds.CampaignSystem.Encounters.LocationEncounter");
            Type playerEncounter = AccessTools.TypeByName("TaleWorlds.CampaignSystem.Encounters.PlayerEncounter");
            Type gameType = AccessTools.TypeByName("TaleWorlds.Core.Game");
            Type stateManager = AccessTools.TypeByName("TaleWorlds.Core.GameStateManager");
            Type characterType = AccessTools.TypeByName("TaleWorlds.CampaignSystem.CharacterObject");
            _town = Bare(townType); townType.GetProperty("StringId").SetValue(_town, "probe_aftermath_town");
            _playerClan = _ownerClan = Bare(clanType); _party = Bare(partyType); _encounter = Bare(encounterType);
            _game = Bare(gameType); _manager = Bare(stateManager); _map = Bare(AccessTools.TypeByName("TaleWorlds.CampaignSystem.GameState.MapState"));
            _soldier = Bare(characterType); characterType.GetProperty("StringId").SetValue(_soldier, "probe_survivor");
            Patch(fixture, townType.GetMethod("Find", All, null, new[] { typeof(string) }, null), nameof(Town));
            Patch(fixture, AccessTools.PropertyGetter(townType, "IsTown"), nameof(Yes));
            Patch(fixture, AccessTools.PropertyGetter(townType, "OwnerClan"), nameof(OwnerClan));
            Patch(fixture, AccessTools.PropertyGetter(partyType, "MainParty"), nameof(Party));
            Patch(fixture, AccessTools.PropertyGetter(partyType, "CurrentSettlement"), nameof(Town));
            Patch(fixture, AccessTools.PropertyGetter(playerEncounter, "LocationEncounter"), nameof(Encounter));
            Patch(fixture, AccessTools.PropertyGetter(encounterType, "Settlement"), nameof(Town));
            Patch(fixture, AccessTools.PropertyGetter(gameType, "Current"), nameof(Game));
            Patch(fixture, AccessTools.PropertyGetter(gameType, "GameStateManager"), nameof(Manager));
            Patch(fixture, AccessTools.PropertyGetter(stateManager, "ActiveState"), nameof(Map));
            Patch(fixture, characterType.GetMethod("Find", All, null, new[] { typeof(string) }, null), nameof(Soldier));
            Patch(fixture, af.GetType("AnimusForge.PlayerEncounterCompat", true).GetMethod("HasEncounterBattleContext", All), nameof(No));
            Type info = AccessTools.TypeByName("TaleWorlds.Library.InformationManager");
            Patch(fixture, info.GetMethod("IsAnyInquiryActive", All), nameof(No));
            foreach (MethodInfo display in info.GetMethods(All).Where(m => m.Name == "DisplayMessage")) Patch(fixture, display, nameof(Skip));
            Patch(fixture, af.GetType("AnimusForge.SiegeAiInterventionBehavior", true).GetMethod("TryOpenSettlementEntryVictoryMenu", All), nameof(OpenMenu));
            Type sessionType = coup.GetType("AnimusForge.CoupSystem.CoupSession", true);
            Type ownerType = coup.GetType("AnimusForge.CoupSystem.CoupCampaignBehavior", true);
            object owner = Activator.CreateInstance(ownerType, true);
            object session = Activator.CreateInstance(sessionType);
            foreach (string field in new[] { "KingdomId", "KingId", "OriginalRulingClanId", "OriginalOwnerClanId" }) Set(session, field, field);
            Set(session, "SettlementId", "probe_aftermath_town");
            Set(session, "Phase", Enum.Parse(sessionType.GetField("Phase").FieldType, "Completed"));
            Set(session, "Disposition", Enum.Parse(sessionType.GetField("Disposition").FieldType, "Release"));
            foreach (string field in new[] { "Started", "KingSubdued", "CasualtiesCommitted", "RulingClanCommitted", "TownCommitted", "CustodyCommitted", "FactsCommitted", "RebellionQueued", "AftermathPending" }) Set(session, field, true);
            IList troops = (IList)Field(session, "Troops");
            Type recordType = troops.GetType().GetGenericArguments()[0];
            foreach (bool removed in new[] { false, true })
            {
                object record = Activator.CreateInstance(recordType);
                Set(record, "Id", removed.ToString()); Set(record, "CharacterId", "probe_survivor"); Set(record, "SourcePartyId", "player_party");
                Set(record, "Removed", removed); Set(record, "Wounded", removed); troops.Add(record);
            }
            Set(owner, "_session", session);
            MethodInfo open = ownerType.GetMethod("TryOpenAftermath", All);
            _menuCalls = 0; _menuSucceeds = false;
            open.Invoke(owner, null);
            check(_menuCalls == 1 && (bool)Field(session, "AftermathPending") && !(bool)Field(session, "AftermathOpened"), "unavailable native menu leaves a retryable completed coup");
            check(Field(session, "Phase").ToString() == "Completed" && (bool)Field(session, "TownCommitted"), "failed menu does not roll back or restart political commit");
            _menuSucceeds = true;
            open.Invoke(owner, null);
            check(_menuCalls == 2 && !_transfer && _survivors == 1, "real bridge opens menu without ownership transfer and excludes casualties");
            check((bool)Field(session, "AftermathOpened") && !(bool)Field(session, "AftermathPending"), "successful open persists receipt and consumes pending work");
            open.Invoke(owner, null);
            check(_menuCalls == 2, "duplicate callback cannot reopen disposition");
            Set(owner, "_session", Roundtrip(session)); open.Invoke(owner, null);
            check(_menuCalls == 2, "opened receipt survives reload without replay");
            Set(session, "AftermathOpened", false); Set(session, "AftermathPending", true); Set(owner, "_session", session);
            _ownerClan = Bare(clanType); open.Invoke(owner, null);
            check(_menuCalls == 2 && !(bool)Field(session, "AftermathPending"), "changed town ownership cancels stale aftermath without recapture");
            write("PASS coup loyalist and aftermath fixture assertions=" + checks);
            write("LOYALIST_AFTERMATH_SCOPE real selector/old-new relation policy and completed owner-adapter-host menu path; real ObjectManager/JSON, synthetic world/relations and physical eligibility; native menu opening intercepted. No LLM, actual rebellion, native menu rendering or disposition rewards executed.");
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
    private static bool Town(ref object __result) { __result = _town; return false; }
    private static bool OwnerClan(ref object __result) { __result = _ownerClan; return false; }
    private static bool PlayerClan(ref object __result) { __result = _playerClan; return false; }
    private static bool RestorationHome(ref object __result) { __result = _restorationHome; return false; }
    private static bool Prisoner(object __instance, ref bool __result) { __result = _oldKingCaptured && ReferenceEquals(__instance, _oldKing); return false; }
    private static bool Party(ref object __result) { __result = _party; return false; }
    private static bool Encounter(ref object __result) { __result = _encounter; return false; }
    private static bool Game(ref object __result) { __result = _game; return false; }
    private static bool Manager(ref object __result) { __result = _manager; return false; }
    private static bool Map(ref object __result) { __result = _map; return false; }
    private static bool Soldier(ref object __result) { __result = _soldier; return false; }
    private static bool Yes(ref bool __result) { __result = true; return false; }
    private static bool No(ref bool __result) { __result = false; return false; }
    private static bool Skip() => false;
    private static bool OpenMenu(object[] __args, ref bool __result)
    {
        _menuCalls++; _transfer = (bool)__args[3];
        _survivors = (int)__args[1].GetType().GetProperty("TotalManCount").GetValue(__args[1]);
        __result = _menuSucceeds; return false;
    }
}
