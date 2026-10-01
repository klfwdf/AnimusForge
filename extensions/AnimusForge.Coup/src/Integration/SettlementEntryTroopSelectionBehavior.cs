using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace AnimusForge.CoupSystem;

// Compatibility adapter for the already installed AF implementation. This DLL never
// attaches AF's SETS incident owner, mutates its saved profiles, or replaces AF.dll.
internal static class SettlementEntryTroopSelectionBehavior
{
    private static bool _registered;
    private static Func<TroopRoster, TroopRoster> _buildSelectable;
    private static Func<CharacterObject, FormationClass> _formationClass;
    private static Action<Agent, Team, FormationClass> _assignFormation;
    private static Action<Team, Agent> _bindOrderController;
    private static Action<Formation, Agent> _markCommandable;
    private static Action<int, string> _interruptSpeech, _cancelSpeech;
    private static Func<bool> _hasBlockingFlow;
    private static MethodInfo _queueCoup, _clearCoup, _spawnKing, _countRole, _isArmedLogic;
    private static Type _setsLogicType;
    internal static bool IsAvailable { get; private set; }

    internal static void Reset()
    {
        _registered = false;
        IsAvailable = false;
        _buildSelectable = null;
        _formationClass = null;
        _assignFormation = null;
        _bindOrderController = null;
        _markCommandable = null;
        _interruptSpeech = _cancelSpeech = null;
        _hasBlockingFlow = null;
        _queueCoup = _clearCoup = _spawnKing = _countRole = _isArmedLogic = null;
        _setsLogicType = null;
    }

    internal static void Register(Harmony harmony)
    {
        if (_registered || harmony == null) return;
        _registered = true;
        try
        {
            Type host = AccessTools.TypeByName("AnimusForge.SettlementEntryTroopSelectionBehavior")
                ?? throw new MissingMemberException("AF SETS owner is unavailable");
            _buildSelectable = Bind<Func<TroopRoster, TroopRoster>>(host, "BuildConfigSelectableRoster");
            _formationClass = Bind<Func<CharacterObject, FormationClass>>(host, "ResolveSetsFollowerFormationClass");
            _assignFormation = Bind<Action<Agent, Team, FormationClass>>(host, "AssignSetsAgentToPlayerFormation");
            _bindOrderController = Bind<Action<Team, Agent>>(host, "BindSetsPlayerOrderController");
            _markCommandable = Bind<Action<Formation, Agent>>(host, "MarkFormationPlayerCommandable");
            Type speech = AccessTools.TypeByName("AnimusForge.ShoutBehavior");
            if (speech != null)
            {
                _interruptSpeech = OptionalSpeechCallback(speech, "InterruptAgentSpeechForCombatExternal");
                _cancelSpeech = OptionalSpeechCallback(speech, "CancelAgentSpeechForRemovalExternal");
            }
            _hasBlockingFlow = Bind<Func<bool>>(host, "HasBlockingFlowForCoup");
            _queueCoup = AccessTools.Method(host, "QueueArmedCoupEntryWithOptions", new[] { typeof(string), typeof(string), typeof(TroopRoster), typeof(List<string[]>), typeof(int), typeof(int), typeof(int), typeof(int) }) ?? throw new MissingMethodException(host.FullName, "QueueArmedCoupEntryWithOptions");
            _clearCoup = AccessTools.Method(host, "ClearArmedCoupEntry") ?? throw new MissingMethodException(host.FullName, "ClearArmedCoupEntry");
            _setsLogicType = host.GetNestedType("SettlementEntryTroopSelectionMissionLogic", BindingFlags.NonPublic) ?? throw new MissingMemberException(host.FullName, "SettlementEntryTroopSelectionMissionLogic");
            // Resolved once here; the mission tick must not look methods up per call.
            _spawnKing = AccessTools.Method(_setsLogicType, "SpawnArmedCoupKing", new[] { typeof(CharacterObject), typeof(MatrixFrame) }) ?? throw new MissingMethodException(_setsLogicType.FullName, "SpawnArmedCoupKing");
            _countRole = AccessTools.Method(_setsLogicType, "CountArmedCoupRole", new[] { typeof(string) }) ?? throw new MissingMethodException(_setsLogicType.FullName, "CountArmedCoupRole");
            _isArmedLogic = AccessTools.PropertyGetter(_setsLogicType, "IsArmedCoup") ?? throw new MissingMethodException(_setsLogicType.FullName, "IsArmedCoup");
            Bind<Action>(host, "ValidateArmedCoupRuntime")();
            // Campaign events invoke the last registered listener first. Attach only after
            // SETS has added its exact mission logic, independently of registration order.
            MethodInfo missionStarted = AccessTools.Method(host, "OnMissionStarted", new[] { typeof(IMission) })
                ?? throw new MissingMethodException(host.FullName, "OnMissionStarted");
            harmony.Patch(missionStarted, postfix: new HarmonyMethod(
                AccessTools.Method(typeof(SettlementEntryTroopSelectionBehavior), nameof(SetsMissionStartedPostfix))));
            Patch(harmony, host, "IsSetsCommandMissionCandidate", nameof(CommandCandidatePrefix));
            Patch(harmony, host, "ResolveSetsPlayerCommandTeamForExternal", nameof(CommandTeamPrefix));
            Patch(harmony, host, "EnsureSetsCommandUiReadyForExternal", nameof(CommandReadyPrefix));
            Patch(harmony, host, "SetsPlayerHasCommandableAgentsForExternal", nameof(CommandablePrefix));
            Patch(harmony, host, "TryResolveSetsNativeOrderControllerForExternal", nameof(OrderControllerPrefix));
            Patch(harmony, host, "NativeOrderControllerHasSelectedFormationsForSetsExternal", nameof(SelectedFormationPrefix));
            IsAvailable = true;
            Logger.Log("Coup", "Installed AF SETS bridge ready: " + host.Assembly.FullName);
        }
        catch (Exception ex)
        {
            // Partial patches are inert without a coup session. New coups are disabled by IsAvailable.
            IsAvailable = false;
            Logger.Log("Coup", "Installed AF SETS bridge unavailable; new coups disabled: " + ex);
        }
    }

    private static T Bind<T>(Type owner, string name) where T : class
    {
        MethodInfo method = AccessTools.Method(owner, name) ?? throw new MissingMethodException(owner.FullName, name);
        return Delegate.CreateDelegate(typeof(T), method) as T ?? throw new InvalidOperationException("Incompatible AF delegate: " + name);
    }

    private static void SetsMissionStartedPostfix(IMission mission)
    {
        CoupCampaignBehavior.NotifySetsMissionReady(mission);
    }

    private static Action<int, string> OptionalSpeechCallback(Type owner, string name)
    {
        MethodInfo method = AccessTools.Method(owner, name, new[] { typeof(int), typeof(string) });
        return method == null ? null : (Action<int, string>)Delegate.CreateDelegate(typeof(Action<int, string>), method);
    }

    internal static void InterruptAgentSpeech(int agentIndex)
    {
        try { _interruptSpeech?.Invoke(agentIndex, "coup_hit"); }
        catch (Exception ex)
        {
            _interruptSpeech = null;
            Logger.Log("Coup", "AF combat speech callback disabled after failure: " + ex.Message);
        }
    }

    internal static void CancelAgentSpeech(int agentIndex)
    {
        try { _cancelSpeech?.Invoke(agentIndex, "coup_removed"); }
        catch (Exception ex)
        {
            _cancelSpeech = null;
            Logger.Log("Coup", "AF removal speech callback disabled after failure: " + ex.Message);
        }
    }

    private static void Patch(Harmony harmony, Type owner, string method, string prefix)
    {
        MethodInfo target = AccessTools.Method(owner, method) ?? throw new MissingMethodException(owner.FullName, method);
        harmony.Patch(target, prefix: new HarmonyMethod(AccessTools.Method(typeof(SettlementEntryTroopSelectionBehavior), prefix))
        { priority = Priority.First });
    }

    // Defenders are the coup session's own records; the host spawns exactly these and
    // reports casualties back by record id, so roles and source parties never diverge.
    internal static void QueueArmedCoup(string settlementId, string locationId, TroopRoster roster, IEnumerable<CoupTroopRecord> defenders, CoupBattleOptions options)
    {
        if (!IsAvailable) throw new InvalidOperationException("AF SETS兼容桥未就绪。");
        if (options == null || !options.IsValid()) throw new InvalidOperationException("政变战斗参数损坏。");
        var records = new List<string[]>();
        if (defenders != null)
            foreach (CoupTroopRecord record in defenders)
                records.Add(new[] { record.Id, record.CharacterId, record.SourcePartyId, record.Role.ToString() });
        _queueCoup.Invoke(null, new object[] { settlementId, locationId, roster, records,
            locationId == "lordshall" ? options.HallAllyLimit : options.StreetAllyLimit,
            options.DefenderWaveSize, options.DefenderWaveIntervalSeconds, options.MaxActiveDefenderWaves });
    }

    internal static void ClearArmedCoup() => _clearCoup?.Invoke(null, null);

    private static MissionBehavior FindSetsLogic(Mission mission)
    {
        if (mission == null || _setsLogicType == null) return null;
        return mission.MissionBehaviors.FirstOrDefault(behavior => behavior != null && _setsLogicType.IsInstanceOfType(behavior));
    }

    internal static bool HasSetsLogic(Mission mission) => FindSetsLogic(mission) != null;

    // True only when the host logic was built from the queued armed-coup entry for this exact location.
    internal static bool HasArmedSetsLogic(Mission mission)
    {
        object logic = FindSetsLogic(mission);
        return logic != null && _isArmedLogic?.Invoke(logic, null) is bool armed && armed;
    }

    internal static Agent SpawnCoupKing(Mission mission, CharacterObject character, MatrixFrame frame)
    {
        object logic = FindSetsLogic(mission) ?? throw new InvalidOperationException("SETS 进城逻辑不在当前场景。");
        return _spawnKing?.Invoke(logic, new object[] { character, frame }) as Agent;
    }

    // Live agents of a role; callers combine this with the session's unremoved records.
    internal static int CountCoupRole(Mission mission, string role)
    {
        object logic = FindSetsLogic(mission);
        return logic != null && _countRole?.Invoke(logic, new object[] { role }) is int count ? count : 0;
    }

    internal static TroopRoster BuildCoupSelectableRoster(TroopRoster source)
    {
        if (!IsAvailable) throw new InvalidOperationException("AF SETS兼容桥未就绪。");
        return _buildSelectable(source);
    }

    internal static bool HasPendingFlowForCoup()
    {
        if (!IsAvailable || _hasBlockingFlow == null) return true;
        try
        {
            return _hasBlockingFlow();
        }
        catch (Exception ex)
        {
            IsAvailable = false;
            Logger.Log("Coup", "AF SETS pending-flow lookup failed; coups disabled: " + ex.Message);
            return true;
        }
    }

    private static bool CommandCandidatePrefix(Mission mission, ref bool __result)
    {
        if (!CoupCampaignBehavior.IsMissionActive(mission)) return true;
        __result = !mission.IsMissionEnding;
        return false;
    }

    private static bool CommandTeamPrefix(Mission mission, ref Team __result)
    {
        mission ??= Mission.Current;
        if (!CoupCampaignBehavior.IsMissionActive(mission)) return true;
        __result = mission.GetMissionBehavior<CoupMissionBehavior>()?.CommandTeam ?? mission.PlayerTeam;
        return false;
    }

    private static bool CommandReadyPrefix(Mission mission, ref bool __result)
    {
        mission ??= Mission.Current;
        if (!CoupCampaignBehavior.IsMissionActive(mission)) return true;
        __result = mission.GetMissionBehavior<CoupMissionBehavior>()?.EnsureCommandReady() == true;
        return false;
    }

    private static bool CommandablePrefix(Mission mission, ref bool __result)
    {
        mission ??= Mission.Current;
        if (!CoupCampaignBehavior.IsMissionActive(mission)) return true;
        __result = mission.GetMissionBehavior<CoupMissionBehavior>()?.HasCommandableTroops == true;
        return false;
    }

    private static bool OrderControllerPrefix(Mission mission, ref OrderController __result)
    {
        mission ??= Mission.Current;
        if (!CoupCampaignBehavior.IsMissionActive(mission)) return true;
        Team team = mission.GetMissionBehavior<CoupMissionBehavior>()?.CommandTeam ?? mission.PlayerTeam;
        __result = team?.PlayerOrderController ?? team?.MasterOrderController;
        return false;
    }

    private static bool SelectedFormationPrefix(Mission mission, ref bool __result)
    {
        mission ??= Mission.Current;
        if (!CoupCampaignBehavior.IsMissionActive(mission)) return true;
        Team team = mission.GetMissionBehavior<CoupMissionBehavior>()?.CommandTeam ?? mission.PlayerTeam;
        __result = mission.GetMissionBehavior<CoupMissionBehavior>()?.HasCommandableTroops == true
            && team?.PlayerOrderController?.SelectedFormations?.Count > 0;
        return false;
    }
}
