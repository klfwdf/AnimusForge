using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using HarmonyLib;

// Uses the patched real PassageUsePoint.OnUse. Native scene closure and the party UI
// are bounded fixtures; no live engine, save file, map world or renderer is started.
internal static class PassageTransitionRegression
{
    private const BindingFlags All = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;
    private static int _nativeDoorCalls, _endCalls, _messages;
    private static object _owner, _logic, _soldier;
    private static object[] _selection;

    internal static void Run(Assembly af, Assembly coup, object owner, object session, object mission, Action<string> write)
    {
        var fixture = new Harmony("AnimusForge.Coup.RuntimeProbe.PassageFixture");
        Type ownerType = owner.GetType(), sessionType = session.GetType(), missionType = mission.GetType();
        Type campaignType = AccessTools.TypeByName("TaleWorlds.CampaignSystem.Campaign");
        PropertyInfo current = campaignType.GetProperty("Current", All);
        object previousCampaign = current.GetValue(null);
        FieldInfo moduleTypes = AccessTools.TypeByName("TaleWorlds.DotNet.Managed").GetField("_moduleTypes", All);
        object previousModuleTypes = moduleTypes.GetValue(null);
        _owner = owner;
        int checks = 0;
        Action<bool, string> check = (ok, message) =>
        {
            if (!ok) throw new InvalidOperationException("PASSAGE_FAIL " + message);
            checks++;
            write("PASSAGE_PASS " + message);
        };
        try
        {
            Type menuManagerType = AccessTools.TypeByName("TaleWorlds.CampaignSystem.GameMenus.GameMenuManager");
            object menuManager = FormatterServices.GetUninitializedObject(menuManagerType);
            object campaign = FormatterServices.GetUninitializedObject(campaignType);
            campaignType.GetProperty("GameMenuManager", All).SetValue(campaign, menuManager);
            current.SetValue(null, campaign);
            ownerType.GetField("_campaign", All).SetValue(owner, campaign);
            FieldInfo phase = sessionType.GetField("Phase");
            phase.SetValue(session, Enum.Parse(phase.FieldType, "Street"));
            ownerType.GetField("_mission", All).SetValue(owner, mission);
            IList behaviors = (IList)missionType.GetProperty("MissionBehaviors").GetValue(mission);
            _logic = behaviors.Cast<object>().Single(b => b.GetType().Name == "CoupMissionBehavior");
            _logic.GetType().GetField("_initialized", All).SetValue(_logic, true);
            _logic.GetType().GetField("_exiting", All).SetValue(_logic, false);

            Type agentType = AccessTools.TypeByName("TaleWorlds.MountAndBlade.Agent");
            object player = FormatterServices.GetUninitializedObject(agentType);
            agentType.GetProperty("Mission", All).SetValue(player, mission);
            _logic.GetType().GetField("_player", All).SetValue(_logic, player);
            Patch(fixture, agentType.GetMethod("IsActive", All), nameof(ActiveAgent));
            Patch(fixture, _logic.GetType().GetMethod("SaveHealth", All), nameof(Skip));
            Patch(fixture, missionType.GetMethod("EndMission", All, null, Type.EmptyTypes, null), nameof(EndMission));
            Type information = AccessTools.TypeByName("TaleWorlds.Library.InformationManager");
            foreach (MethodInfo display in information.GetMethods(All).Where(m => m.Name == "DisplayMessage")) Patch(fixture, display, nameof(Message));

            Type locationType = AccessTools.TypeByName("TaleWorlds.CampaignSystem.Settlements.Locations.Location");
            object hall = FormatterServices.GetUninitializedObject(locationType);
            locationType.GetProperty("StringId", All).SetValue(hall, "lordshall");
            Type passageType = AccessTools.TypeByName("SandBox.Objects.PassageUsePoint");
            // ScriptComponentBehavior's static editable-field cache expects engine startup
            // to have supplied this dictionary. An empty fixture avoids starting native modules.
            if (previousModuleTypes == null) moduleTypes.SetValue(null, Activator.CreateInstance(moduleTypes.FieldType));
            object door = FormatterServices.GetUninitializedObject(passageType);
            passageType.GetField("_initialized", All).SetValue(door, true);
            passageType.GetField("_toLocation", All).SetValue(door, hall);
            MethodInfo onUse = passageType.GetMethod("OnUse", All);
            // Lowest-priority sentinel only executes if production allowed vanilla OnUse;
            // it prevents the actual engine transition in this fixture.
            fixture.Patch(onUse, prefix: new HarmonyMethod(typeof(PassageTransitionRegression), nameof(NativeDoor)) { priority = Priority.Last });
            Action<object> use = agent => onUse.Invoke(door, new[] { agent, (object)(sbyte)-1 });
            Type recordType = coup.GetType("AnimusForge.CoupSystem.CoupTroopRecord", true);
            IList troops = (IList)sessionType.GetField("Troops").GetValue(session);
            troops.Clear();
            object guard = Activator.CreateInstance(recordType);
            recordType.GetField("Role").SetValue(guard, Enum.Parse(recordType.GetField("Role").FieldType, "GateGuard"));
            troops.Add(guard);
            use(player);
            check(_nativeDoorCalls == 0 && _endCalls == 0 && phase.GetValue(session).ToString() == "Street", "uncleared gate blocks native transition and keeps Street");
            check(_messages > 0, "blocked gate gives feedback");
            recordType.GetField("Removed").SetValue(guard, true);
            object sets = behaviors.Cast<object>().Single(b => b.GetType().Name == "SettlementEntryTroopSelectionMissionLogic");
            IList reserve = (IList)sets.GetType().GetField("_remainingDefenderReserve", All).GetValue(sets);
            object reservedGuard = Activator.CreateInstance(reserve.GetType().GetGenericArguments()[0], true);
            reservedGuard.GetType().GetField("ArmedCoupRole", All).SetValue(reservedGuard, "GateGuard");
            reserve.Add(reservedGuard);
            use(player);
            check(_endCalls == 0 && _nativeDoorCalls == 0, "remaining SETS guard reserve also blocks passage");
            reserve.Clear();
            object npc = FormatterServices.GetUninitializedObject(agentType);
            agentType.GetProperty("Mission", All).SetValue(npc, mission);
            use(npc);
            check(_endCalls == 0 && _nativeDoorCalls == 0, "NPC cannot open a native passage in the owned mission");
            locationType.GetProperty("StringId", All).SetValue(hall, "tavern");
            use(player);
            check(_endCalls == 0 && _nativeDoorCalls == 0, "other location doors cannot bypass the coup");
            locationType.GetProperty("StringId", All).SetValue(hall, "lordshall");
            // Poison native pending travel: production EndScene must clear both fields.
            menuManagerType.GetField("NextLocation").SetValue(menuManager, hall);
            menuManagerType.GetField("PreviousLocation").SetValue(menuManager, hall);
            use(player);
            check(_endCalls == 1 && _nativeDoorCalls == 0, "cleared hall door closes through Coup only");
            check(phase.GetValue(session).ToString() == "HallSelection", "mission close preserves HallSelection instead of marking retreat");
            check(menuManagerType.GetField("NextLocation").GetValue(menuManager) == null
                && menuManagerType.GetField("PreviousLocation").GetValue(menuManager) == null, "native NextLocation and PreviousLocation cleared before map selection");
            use(player);
            check(_endCalls == 1 && phase.GetValue(session).ToString() == "HallSelection", "repeated door use cannot close or transition twice");

            // Complete the real HallSelection callback using a mocked PartyScreen boundary.
            Type characterType = AccessTools.TypeByName("TaleWorlds.CampaignSystem.CharacterObject");
            _soldier = FormatterServices.GetUninitializedObject(characterType);
            characterType.GetProperty("StringId").SetValue(_soldier, "probe_coup_survivor");
            Patch(fixture, characterType.GetMethod("Find", All, null, new[] { typeof(string) }, null), nameof(FindSoldier));
            object ally = Activator.CreateInstance(recordType);
            recordType.GetField("CharacterId").SetValue(ally, "probe_coup_survivor");
            troops.Add(ally);
            Type selectionType = coup.GetType("AnimusForge.CoupSystem.CoupTroopSelection", true);
            Patch(fixture, selectionType.GetMethod("Open", All), nameof(CaptureSelection));
            ownerType.GetMethod("OpenHallSelection", All).Invoke(owner, null);
            check(_selection != null && (int)_selection[1] == 1, "HallSelection opens bounded survivor picker");
            ((Delegate)_selection[3]).DynamicInvoke(_selection[0]);
            check(phase.GetValue(session).ToString() == "Hall" && (bool)recordType.GetField("HallSelected").GetValue(ally), "confirming survivors advances to Hall and marks selected troop");
            Type host = af.GetType("AnimusForge.SettlementEntryTroopSelectionBehavior", true);
            check((string)host.GetField("_armedCoupLocationId", All).GetValue(null) == "lordshall", "hall selection queues armed lordshall rather than ordinary visit");
            object selected = host.GetField("_armedCoupRoster", All).GetValue(null);
            check((int)selected.GetType().GetProperty("TotalManCount").GetValue(selected) == 1, "hall queue carries selected reinforcements");
            host.GetMethod("ClearArmedCoupEntry", All).Invoke(null, null);

            // Outside this exact mission the original passage route must remain available.
            ownerType.GetField("_mission", All).SetValue(owner, null);
            ownerType.GetField("_endingMission", All).SetValue(owner, null);
            use(player);
            check(_nativeDoorCalls == 1, "non-coup mission keeps native passage behavior");
            write("PASS passage transition fixture assertions=" + checks);
            write("PASSAGE_SCOPE real patched OnUse, real Coup objective/phase/closure and hall-selection callback; synthetic Campaign/menu/Mission/Agent state, health/native EndMission/PartyScreen/display boundaries stubbed. No native F input, rendering, combat or save exercised.");
        }
        finally
        {
            current.SetValue(null, previousCampaign);
            moduleTypes.SetValue(null, previousModuleTypes);
            fixture.UnpatchAll(fixture.Id);
            _owner = _logic = _soldier = null;
            _selection = null;
            _nativeDoorCalls = _endCalls = _messages = 0;
        }
    }
    private static void Patch(Harmony fixture, MethodInfo target, string prefix)
        => fixture.Patch(target, prefix: new HarmonyMethod(typeof(PassageTransitionRegression), prefix));
    private static bool Skip() => false;
    private static bool ActiveAgent(ref bool __result) { __result = true; return false; }
    private static bool Message() { _messages++; return false; }
    private static bool NativeDoor() { _nativeDoorCalls++; return false; }
    private static bool FindSoldier(ref object __result) { __result = _soldier; return false; }
    private static bool CaptureSelection(object[] __args) { _selection = __args; return false; }
    private static bool EndMission(object __instance)
    {
        _endCalls++;
        _owner.GetType().GetMethod("OnMissionEnded", All).Invoke(_owner, new[] { __instance });
        _logic.GetType().GetMethod("OnEndMission", All).Invoke(_logic, null);
        return false;
    }
}
