using System;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace AnimusForge;

public sealed partial class FieldCampMissionLogic
{
    // Mission display equipment only: never touches a campaign roster or character equipment.
    private static bool EquipCampPracticeBow(CampAgent entry)
    {
        Agent agent = entry.Agent;
        // Always use matched blunt practice equipment on these four display-only stations.
        ItemObject bow = TaleWorlds.ObjectSystem.MBObjectManager.Instance.GetObject<ItemObject>("training_bow");
        ItemObject arrows = TaleWorlds.ObjectSystem.MBObjectManager.Instance.GetObject<ItemObject>("blunt_arrows");
        if (bow == null || arrows == null) return false;
        entry.PreviousPracticeWeapon = agent.Equipment[EquipmentIndex.Weapon2];
        entry.PreviousPracticeAmmo = agent.Equipment[EquipmentIndex.Weapon3];
        entry.PracticeEquipmentHeld = true;
        var weapon = new MissionWeapon(bow, null, null);
        var ammo = new MissionWeapon(arrows, null, null);
        agent.EquipWeaponWithNewEntity(EquipmentIndex.Weapon2, ref weapon);
        agent.EquipWeaponWithNewEntity(EquipmentIndex.Weapon3, ref ammo);
        agent.TryToWieldWeaponInSlot(EquipmentIndex.Weapon2, Agent.WeaponWieldActionType.Instant, false);
        return true;
    }

    private static void RestoreCampPracticeEquipment(CampAgent entry)
    {
        if (!entry.PracticeEquipmentHeld) return;
        // Capture/restore only mission slots, including their actual remaining ammunition.
        entry.Agent.EquipWeaponWithNewEntity(EquipmentIndex.Weapon2, ref entry.PreviousPracticeWeapon);
        entry.Agent.EquipWeaponWithNewEntity(EquipmentIndex.Weapon3, ref entry.PreviousPracticeAmmo);
        entry.PracticeEquipmentHeld = false;
    }

    private bool TryStartCampArchery(CampAgent entry)
    {
        if (_assembled || entry.Returning || IsSceneConversationHeld(entry.Agent) || entry.FollowingPlayer) return false;
        GameEntity target = entry.TrainingTarget;
        if (target == null)
        {
            float nearest = 16f * 16f;
            foreach (GameEntity candidate in FieldCampProps.ArcheryTargets)
            {
                float distance = candidate.GlobalPosition.AsVec2.DistanceSquared(entry.Home.AsVec2);
                if (distance < nearest) { nearest = distance; target = candidate; }
            }
        }
        if (target == null) return false;
        if (!IsCampShootingLaneClear(entry, target)) return false;
        ReleasePose(entry);
        if (!EquipCampPracticeBow(entry)) return false;
        StopCampNavigation(entry.Agent);
        entry.TrainingTarget = target;
        entry.Training = true;
        entry.Seated = true;
        entry.Agent.SetIsAIPaused(false);
        // Native tournament uses the same entity-targeted shooting owner, not fake shoot clips.
        entry.Agent.SetMaximumSpeedLimit(0f, isMultiplier: false);
        entry.Agent.SetScriptedTargetEntity(target.WeakEntity, Agent.AISpecialCombatModeFlags.None, false);
        entry.NextTrainingTime = _elapsed + 25f + entry.Agent.Index % 13;
        entry.NextWanderTime = entry.NextTrainingTime;
        Logger.Log(LogSource, "archery started agent=" + entry.Agent.Index);
        return true;
    }

    // Four shooting lanes at most, checked in the existing 20-resident upkeep batch.
    // Native spatial index and one reused list; no full mission enumeration.
    private bool IsCampShootingLaneClear(CampAgent entry, GameEntity target)
    {
        Vec3 lane = target.GlobalPosition - entry.Agent.Position;
        float length = lane.AsVec2.Length;
        Mission.GetNearbyAgents((target.GlobalPosition.AsVec2 + entry.Agent.Position.AsVec2) * 0.5f, length * 0.5f + 1.5f, _campNearbyAgents);
        foreach (Agent other in _campNearbyAgents)
        {
            if (other == entry.Agent || !other.IsActive()) continue;
            Vec2 delta = other.Position.AsVec2 - entry.Agent.Position.AsVec2;
            float projection = Vec2.DotProduct(delta, lane.AsVec2) / Math.Max(0.01f, lane.AsVec2.LengthSquared);
            if (projection > 0f && projection < 1.1f && (delta - lane.AsVec2 * projection).LengthSquared < 2.25f) return false;
        }
        return true;
    }

    private void UpkeepCampArchery(CampAgent entry)
    {
        if (entry.TrainingTarget == null || !IsCampShootingLaneClear(entry, entry.TrainingTarget))
        {
            ReleasePose(entry);
            entry.Agent.SetIsAIPaused(true);
            PlayLoop(entry, LookoutActions);
            entry.NextWanderTime = _elapsed + 2f;
            return;
        }
        if (_elapsed < entry.NextTrainingTime) return;
        // Pause between volleys, replenish only mission-local ammo, then let the normal
        // work/stroll/return state machine bring the resident back to the shooting line.
        ReleasePose(entry);
        entry.Returning = true;
        if (TryNearbyStroll(entry, out Vec3 visit)) MoveTo(entry, visit, entry.HomeFacing, run: false);
        else
        {
            entry.Returning = false;
            TryStartCampArchery(entry);
        }
    }
}
