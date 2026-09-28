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
    private static readonly List<FieldInfo> PendingObjects = new List<FieldInfo>();
    private static readonly List<FieldInfo> ActiveFlags = new List<FieldInfo>();
    private static FieldInfo _pendingRebellion;
    private static MethodInfo _queueCoup, _clearCoup;
    private static Type _setsLogicType;
    internal static bool IsAvailable { get; private set; }

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
            foreach (string name in new[] { "_pendingProfileSelection", "_pendingMissionEntry", "_pendingVictoryMenuEntry",
                "_pendingVillageVictoryRewardEntry", "_pendingVillageAftermathEncounterExit", "_pendingSettlementCivilianGatherRequest" })
                PendingObjects.Add(RequiredField(host, name));
            ActiveFlags.Add(RequiredField(host, "_setsEntryMissionActive"));
            ActiveFlags.Add(RequiredField(host, "_setsActiveUsableProtection"));
            _pendingRebellion = RequiredField(host, "_pendingSameKingdomVassalRebellionKingdomId");
            _queueCoup = AccessTools.Method(host, "QueueArmedCoupEntry", new[] { typeof(string), typeof(TroopRoster) }) ?? throw new MissingMethodException(host.FullName, "QueueArmedCoupEntry");
            _clearCoup = AccessTools.Method(host, "ClearArmedCoupEntry") ?? throw new MissingMethodException(host.FullName, "ClearArmedCoupEntry");
            _setsLogicType = host.GetNestedType("SettlementEntryTroopSelectionMissionLogic", BindingFlags.NonPublic) ?? throw new MissingMemberException(host.FullName, "SettlementEntryTroopSelectionMissionLogic");
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

    private static FieldInfo RequiredField(Type owner, string name) => AccessTools.Field(owner, name)
        ?? throw new MissingFieldException(owner.FullName, name);

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

    internal static void QueueArmedCoup(string settlementId, TroopRoster roster)
    {
        if (!IsAvailable) throw new InvalidOperationException("AF SETS兼容桥未就绪。");
        _queueCoup.Invoke(null, new object[] { settlementId, roster });
    }

    internal static void ClearArmedCoup() => _clearCoup?.Invoke(null, null);

    private static MissionBehavior FindSetsLogic(Mission mission)
    {
        if (mission == null || _setsLogicType == null) return null;
        return mission.MissionBehaviors.FirstOrDefault(behavior => behavior != null && _setsLogicType.IsInstanceOfType(behavior));
    }

    internal static bool HasSetsLogic(Mission mission) => FindSetsLogic(mission) != null;

    internal static Agent SpawnCoupKing(Mission mission, CharacterObject character, MatrixFrame frame)
    {
        object logic = FindSetsLogic(mission) ?? throw new InvalidOperationException("SETS 进城逻辑不在当前场景。");
        MethodInfo method = AccessTools.Method(_setsLogicType, "SpawnArmedCoupKing");
        return method?.Invoke(logic, new object[] { character, frame }) as Agent;
    }

    internal static int CountCoupRole(Mission mission, string role)
    {
        object logic = FindSetsLogic(mission);
        MethodInfo method = logic == null ? null : AccessTools.Method(_setsLogicType, "CountArmedCoupRole", new[] { typeof(string) });
        return method?.Invoke(logic, new object[] { role }) is int count ? count : 0;
    }

    internal static TroopRoster BuildCoupSelectableRoster(TroopRoster source)
    {
        if (!IsAvailable) throw new InvalidOperationException("AF SETS兼容桥未就绪。");
        return _buildSelectable(source);
    }

    internal static bool HasPendingFlowForCoup()
    {
        if (!IsAvailable) return true;
        try
        {
            foreach (FieldInfo field in PendingObjects) if (field.GetValue(null) != null) return true;
            foreach (FieldInfo field in ActiveFlags) if ((bool)field.GetValue(null)) return true;
            return !string.IsNullOrEmpty(_pendingRebellion.GetValue(null) as string);
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
