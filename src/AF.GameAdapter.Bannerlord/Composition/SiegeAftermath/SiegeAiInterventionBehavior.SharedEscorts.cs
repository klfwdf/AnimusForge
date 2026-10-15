using System.Collections.Generic;
using System.Runtime.CompilerServices;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace AnimusForge;

public partial class SiegeAiInterventionBehavior
{
    private static Mission _villageEscortMission;
    private static readonly Dictionary<int, int> VillageEscortOriginIds = new Dictionary<int, int>();

    // The same native construction for town and village escorts. Selection and
    // batched spawning remain with the entry owner; no second troop spawn loop.
    internal static AgentBuildData BuildInterventionEscortData(CharacterObject troop, IAgentOriginBase origin,
        Team team, Vec3 position, Vec3 direction, Formation formation, int count, int index)
    {
        var data = new AgentBuildData(troop).TroopOrigin(origin)
            .Monster(TaleWorlds.Core.FaceGen.GetMonsterWithSuffix(troop.Race, "_settlement")).Team(team)
            .InitialPosition(in position).InitialDirection(direction.AsVec2.Normalized())
            .Controller(AgentControllerType.AI).CivilianEquipment(false).NoHorses(true);
        if (formation != null)
            data = data.Formation(formation).FormationTroopSpawnCount(count).FormationTroopSpawnIndex(index)
                .SpawnsIntoOwnFormation(true).SpawnsUsingOwnTroopClass(false);
        return data;
    }

    private static bool IsInterventionEscortScene(Mission mission)
        => mission != null && ReferenceEquals(mission, Mission.Current) && !mission.IsMissionEnding
            && (IsActiveInCurrentMission() || VillageAftermathBehavior.IsActiveForMission(mission));

    internal static void PrepareVillageEscortForExternal(Agent agent)
    {
        if (agent == null || !VillageAftermathBehavior.IsActiveForMission(agent.Mission)
            || !agent.IsActive() || !agent.IsHuman || agent.IsMainAgent) return;
        if (!ReferenceEquals(_villageEscortMission, agent.Mission))
        {
            ReleaseVillageEscortsForExternal(_villageEscortMission);
            _villageEscortMission = agent.Mission;
        }
        VillageEscortOriginIds[agent.Index] = agent.Origin != null ? RuntimeHelpers.GetHashCode(agent.Origin) : 0;
        AlliedAgentIndexes.Add(agent.Index);
        MarkAgentOriginUnderPlayerCommand(agent);
        // Reuse town friendliness/mortality and disable competing companion AI once.
        RestoreAlliedSoldierFriendlyState(agent, 0f, "gccz_village_escort", forceFollow: false);
        DisableCompanionStyleFollow(agent);
    }

    internal static bool FollowVillageEscortFormationForExternal(Mission mission)
        => VillageAftermathBehavior.IsActiveForMission(mission)
            && TrySetPlayerFormationFollowOrder(FormationClass.Infantry, "gccz_village_escort_follow");

    internal static void ReleaseVillageEscortsForExternal(Mission mission)
    {
        if (mission == null || !ReferenceEquals(_villageEscortMission, mission)) return;
        foreach (var pair in VillageEscortOriginIds)
        {
            AlliedAgentIndexes.Remove(pair.Key);
            BannerBearerAgentIndexes.Remove(pair.Key);
            MassacreReadySoldierAgentIndexes.Remove(pair.Key);
            MassacreCombatPreparedAgentIndexes.Remove(pair.Key);
            if (pair.Value != 0) CommandableOriginRuntimeIds.Remove(pair.Value);
        }
        VillageEscortOriginIds.Clear();
        _villageEscortMission = null;
    }
}
