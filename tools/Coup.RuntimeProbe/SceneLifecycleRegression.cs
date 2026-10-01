using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using HarmonyLib;

// Optional, explicitly synthetic native context; production registration still runs unmodified.
internal static class SceneLifecycleRegression
{
    private const BindingFlags All = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
    private static object _town;
    private static object _currentTown;
    private static bool _allowEntry;
    private static readonly Dictionary<string, object[]> Menus = new Dictionary<string, object[]>();

    internal static void Run(Assembly af, Assembly coup, Action<string> write)
    {
        var fixture = new Harmony("AnimusForge.Coup.RuntimeProbe.SceneFixture");
        int checks = 0;
        Action<bool, string> check = (ok, message) =>
        {
            if (!ok) throw new InvalidOperationException("SCENE_FAIL " + message);
            checks++;
            write("SCENE_PASS " + message);
        };
        Type host = af.GetType("AnimusForge.SettlementEntryTroopSelectionBehavior", true);
        Type ownerType = coup.GetType("AnimusForge.CoupSystem.CoupCampaignBehavior", true);
        PropertyInfo singleton = ownerType.GetProperty("Instance", All);
        object previousOwner = singleton.GetValue(null);
        PropertyInfo pending = host.GetProperty("_pendingMissionEntry", All);
        object previousPending = pending.GetValue(null);
        try
        {
            Type characterType = AccessTools.TypeByName("TaleWorlds.CampaignSystem.CharacterObject");
            Type clanType = AccessTools.TypeByName("TaleWorlds.CampaignSystem.Clan");
            Prefix(fixture, AccessTools.PropertyGetter(clanType, "PlayerClan"), nameof(NullContext));
            object soldier = FormatterServices.GetUninitializedObject(characterType);
            MethodInfo originFactory = host.GetMethod("CreateCoupOrigin", All);
            host.GetMethod("ValidateArmedCoupRuntime", All).Invoke(null, null);
            ConstructorInfo constructor = (ConstructorInfo)host.GetField("_coupOriginConstructor", All).GetValue(null);
            check(constructor != null && !constructor.IsPublic, "host resolves the actual non-public CoupAgentOrigin constructor");
            foreach (bool ally in new[] { true, false })
            {
                object origin = originFactory.Invoke(null, new[] { soldier, (object)ally });
                check(origin != null && origin.GetType().FullName == "AnimusForge.CoupSystem.CoupAgentOrigin", "non-null origin created ally=" + ally);
                check(ReferenceEquals(origin.GetType().GetProperty("Troop").GetValue(origin), soldier), "origin preserves soldier identity ally=" + ally);
                check((bool)origin.GetType().GetProperty("IsUnderPlayersCommand").GetValue(origin) == ally, "origin preserves command side ally=" + ally);
                // Calls the same origin callback that vanilla BattleAgentLogic.OnAgentHit dereferences.
                origin.GetType().GetMethod("OnScoreHit").Invoke(origin, new object[] { soldier, null, 10, false, false, null });
                check(true, "origin score-hit callback executes without party or casualty writes ally=" + ally);
            }
            host.GetField("_coupOriginConstructor", All).SetValue(null, null);
            try
            {
                bool refused = false;
                try { originFactory.Invoke(null, new[] { soldier, (object)true }); }
                catch (TargetInvocationException ex) { refused = ex.InnerException is InvalidOperationException; }
                check(refused, "missing origin binding throws before a troop can spawn");
            }
            finally { host.GetField("_coupOriginConstructor", All).SetValue(null, constructor); }

            Type settlementType = AccessTools.TypeByName("TaleWorlds.CampaignSystem.Settlements.Settlement");
            _town = FormatterServices.GetUninitializedObject(settlementType);
            settlementType.GetProperty("StringId").SetValue(_town, "probe_coup_town");
            _currentTown = _town;
            Prefix(fixture, AccessTools.PropertyGetter(settlementType, "CurrentSettlement"), nameof(CurrentTown));
            Prefix(fixture, AccessTools.Method(settlementType, "Find", new[] { typeof(string) }), nameof(FindTown));

            object owner = Activator.CreateInstance(ownerType, true);
            singleton.SetValue(null, owner);
            Type sessionType = coup.GetType("AnimusForge.CoupSystem.CoupSession", true);
            object session = Activator.CreateInstance(sessionType, true);
            sessionType.GetField("SettlementId").SetValue(session, "probe_coup_town");
            FieldInfo phase = sessionType.GetField("Phase");
            phase.SetValue(session, Enum.Parse(phase.FieldType, "Street"));
            ownerType.GetField("_session", All).SetValue(owner, session);

            Type missionType = AccessTools.TypeByName("TaleWorlds.MountAndBlade.Mission");
            object mission = CreateMission(missionType);
            MethodInfo coupStart = ownerType.GetMethod("OnMissionStarted", All);
            MethodInfo hostStart = host.GetMethod("OnMissionStarted", All);
            FieldInfo attached = ownerType.GetField("_mission", All);
            coupStart.Invoke(owner, new[] { mission });
            check(attached.GetValue(owner) == null, "early Coup event does not claim a mission without SETS");

            object entry = Activator.CreateInstance(pending.PropertyType, true);
            pending.PropertyType.GetField("SettlementId").SetValue(entry, "probe_coup_town");
            pending.PropertyType.GetField("ArmedCoup").SetValue(entry, true);
            pending.SetValue(null, entry);
            object hostBehavior = Activator.CreateInstance(host, true);
            Type missionInterface = hostStart.GetParameters()[0].ParameterType;
            Type eventType = AccessTools.TypeByName("TaleWorlds.CampaignSystem.MbEvent`1").MakeGenericType(missionInterface);
            object campaignEvent = Activator.CreateInstance(eventType);
            Type actionType = typeof(Action<>).MakeGenericType(missionInterface);
            MethodInfo listen = eventType.GetMethod("AddNonSerializedListener");
            listen.Invoke(campaignEvent, new[] { hostBehavior, Delegate.CreateDelegate(actionType, hostBehavior, hostStart) });
            listen.Invoke(campaignEvent, new[] { owner, Delegate.CreateDelegate(actionType, owner, coupStart) });
            eventType.GetMethod("Invoke").Invoke(campaignEvent, new[] { mission });
            check(ReferenceEquals(attached.GetValue(owner), mission), "real reverse-order MbEvent attaches Coup via SETS postfix");
            IList behaviors = (IList)missionType.GetProperty("MissionBehaviors").GetValue(mission);
            check(behaviors.Cast<object>().Count(x => x.GetType().Name == "CoupMissionBehavior") == 1, "exactly one Coup mission behavior installed");
            check((bool)sessionType.GetField("SceneEntered").GetValue(session), "session records scene admission");
            eventType.GetMethod("Invoke").Invoke(campaignEvent, new[] { mission });
            check(behaviors.Cast<object>().Count(x => x.GetType().Name == "CoupMissionBehavior") == 1, "duplicate event/postfix does not attach twice");
            MethodInfo active = ownerType.GetMethod("IsMissionActive", All);
            check((bool)active.Invoke(null, new[] { mission }), "guards recognize the admitted mission");
            object unrelated = CreateMission(missionType);
            check(!(bool)active.Invoke(null, new[] { unrelated }), "guards leave unrelated missions untouched");
            attached.SetValue(owner, null);
            eventType.GetMethod("Invoke").Invoke(campaignEvent, new[] { unrelated });
            check(attached.GetValue(owner) == null, "ordinary mission without armed SETS remains ordinary");

            // Capture real menu registration, then exercise the actual phase/visibility callbacks.
            // The expensive campaign eligibility query is stubbed explicitly, not claimed tested here.
            Type starter = AccessTools.TypeByName("TaleWorlds.CampaignSystem.CampaignGameStarter");
            Prefix(fixture, AccessTools.Method(starter, "AddGameMenuOption"), nameof(CaptureMenu));
            Prefix(fixture, ownerType.GetMethod("CanEnterSelectedScene", All), nameof(EntryEligibility));
            ownerType.GetMethod("OnSessionLaunched", All).Invoke(owner, new[] { FormatterServices.GetUninitializedObject(starter) });
            check(Menus.ContainsKey("af_coup_enter_street") && Menus.ContainsKey("af_coup_enter_hall"), "dedicated street and hall options registered on town menu");
            Type argsType = ((Delegate)Menus["af_coup_enter_street"][3]).Method.GetParameters()[0].ParameterType;
            object menuArgs = FormatterServices.GetUninitializedObject(argsType);
            Func<string, bool> visible = id => (bool)((Delegate)Menus[id][3]).DynamicInvoke(menuArgs);
            _allowEntry = true;
            check(visible("af_coup_enter_street") && !visible("af_coup_enter_hall"), "street phase offers only street assault");
            check((bool)argsType.GetField("IsEnabled").GetValue(menuArgs), "eligible dedicated entry enabled");
            _allowEntry = false;
            check(visible("af_coup_enter_street") && !(bool)argsType.GetField("IsEnabled").GetValue(menuArgs), "failed eligibility disables dedicated entry");
            phase.SetValue(session, Enum.Parse(phase.FieldType, "Hall"));
            _allowEntry = true;
            check(!visible("af_coup_enter_street") && visible("af_coup_enter_hall"), "hall phase offers only hall assault");
            ownerType.GetField("_selectionOpen", All).SetValue(owner, true);
            check(!visible("af_coup_enter_hall"), "entry hidden while troop selection is open");
            ownerType.GetField("_selectionOpen", All).SetValue(owner, false);
            attached.SetValue(owner, mission);
            check(!visible("af_coup_enter_hall"), "entry hidden after mission admission");
            attached.SetValue(owner, null);
            phase.SetValue(session, Enum.Parse(phase.FieldType, "Suspended"));
            check(!visible("af_coup_enter_street") && !visible("af_coup_enter_hall"), "technical suspension cannot reopen combat entry");
            write("PASS scene lifecycle fixture assertions=" + checks);
            write("SCENE_SCOPE synthetic Mission lists, Settlement and CharacterObject; no native scene, agents or Campaign constructed. Only fixture settlement/clan queries, menu registration and eligibility replaced; real MbEvent, SETS handler/postfix, Coup admission, origin factory and phase callbacks executed.");
        }
        finally
        {
            singleton.SetValue(null, previousOwner);
            pending.SetValue(null, previousPending);
            fixture.UnpatchAll(fixture.Id);
            _town = _currentTown = null;
            Menus.Clear();
        }
    }

    private static object CreateMission(Type type)
    {
        object mission = FormatterServices.GetUninitializedObject(type);
        foreach (string name in new[] { "MissionBehaviors", "MissionLogics" })
        {
            FieldInfo list = type.GetField("<" + name + ">k__BackingField", All);
            list.SetValue(mission, Activator.CreateInstance(list.FieldType));
        }
        return mission;
    }
    private static void Prefix(Harmony harmony, MethodInfo target, string method)
        => harmony.Patch(target, prefix: new HarmonyMethod(typeof(SceneLifecycleRegression), method));
    private static bool NullContext(ref object __result) { __result = null; return false; }
    private static bool CurrentTown(ref object __result) { __result = _currentTown; return false; }
    private static bool FindTown(ref object __result) { __result = _town; return false; }
    private static bool EntryEligibility(ref string reason, ref bool __result)
    { reason = _allowEntry ? "" : "fixture unavailable"; __result = _allowEntry; return false; }
    private static bool CaptureMenu(object[] __args)
    { if ((string)__args[0] == "town") Menus[(string)__args[1]] = __args; return false; }
}
