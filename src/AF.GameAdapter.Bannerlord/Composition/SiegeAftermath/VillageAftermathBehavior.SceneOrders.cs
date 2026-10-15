using System.Collections.Generic;
using System.Linq;
using AnimusForge.SiegeAftermathIntervention;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.MountAndBlade;

namespace AnimusForge;

public sealed partial class VillageAftermathBehavior
{
    private sealed class VillageQuestionOrder
    {
        internal Agent Soldier;
        internal Agent Resident;
        internal float ExpiresAt;
        internal bool Issued;
        internal float NextAttemptAt;
    }
    private static readonly List<VillageQuestionOrder> VillageQuestionOrders = new List<VillageQuestionOrder>();
    private static bool _foreignBannersAttempted;

    internal static bool IsHostileDispositionForMission(Mission mission)
        => _hostileDispositionActive && IsActiveForMission(mission);

    internal static bool CanGatherInDispositionScene() => IsActive() && _villageVictimTeam == null;

    internal static bool TryGatherDispositionVillagers(int speakerAgentIndex)
        => CanGatherInDispositionScene()
            && _activeMission.GetMissionBehavior<VillageDispositionSceneMissionBehavior>()?.QueueGather(speakerAgentIndex) == true;

    private static void ClearForeignSceneOrders()
    {
        VillageQuestionOrders.Clear(); _foreignBannersAttempted = false;
    }

    private static void TickForeignBanners(Mission mission)
    {
        if (!_foreignBannersAttempted && mission.CurrentTime >= 1f && mission.MainAgent?.IsActive() == true)
        {
            _foreignBannersAttempted = true;
            int count = SiegeAiInterventionBehavior.EnsureInterventionBannerBearersForExternal(mission, "gccz_village_escorts",
                mission.GetMissionBehavior<VillageDispositionSceneMissionBehavior>()?.SelectedRoster);
            GcczDiagnosticLog.Log("VillageFollowers", "banner bearers spawned=" + count + " village=" + _activeVillageId);
        }
        // At most two temporary orders; no all-scene search or per-frame reissue.
        for (int i = VillageQuestionOrders.Count - 1; i >= 0; i--)
        {
            var order = VillageQuestionOrders[i];
            if (!IsLiveHostileSpeaker(order.Soldier) || !IsLiveHostileSpeaker(order.Resident) || mission.CurrentTime >= order.ExpiresAt)
            {
                if (IsLiveHostileSpeaker(order.Soldier) && mission.MainAgent != null)
                    SiegeAiInterventionBehavior.TryMoveForeignVillageAgent(order.Soldier, mission.MainAgent.Position, "village_question_return");
                VillageQuestionOrders.RemoveAt(i);
                continue;
            }
            if (!order.Issued && mission.CurrentTime >= order.NextAttemptAt)
            {
                order.NextAttemptAt = mission.CurrentTime + 0.5f;
                order.Issued = SiegeAiInterventionBehavior.TryMoveForeignVillageAgent(order.Soldier, order.Resident.Position, "village_symbolic_question");
            }
        }
    }

    private static void QueueForeignQuestioning()
    {
        if (!IsActiveForMission(Mission.Current) || VillageQuestionOrders.Count > 0) return;
        var soldiers = _activeMission.Agents.Where(agent => IsLiveHostileSpeaker(agent)
            && agent.Character is CharacterObject character && !character.IsHero && character.IsSoldier
            && agent.Origin?.BattleCombatant == PartyBase.MainParty).Take(2).ToList();
        var residents = _activeMission.Agents.Where(agent => IsLiveHostileSpeaker(agent)
            && ResolveHostileSpeakerRole(agent.Index, out _) is VillageDispositionSpeakerRole.Villager or VillageDispositionSpeakerRole.VillageNotable)
            .Take(2).ToList();
        for (int i = 0; i < soldiers.Count && i < residents.Count; i++)
            VillageQuestionOrders.Add(new VillageQuestionOrder { Soldier = soldiers[i], Resident = residents[i],
                ExpiresAt = _activeMission.CurrentTime + SiegePlunderInteractionProfile.TalkSeconds + 6f });
    }
}
