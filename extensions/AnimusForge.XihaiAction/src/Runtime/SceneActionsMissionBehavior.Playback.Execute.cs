using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using AnimusForge.SceneActions.Core;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace AnimusForge.XihaiAction
{
    internal sealed partial class SceneActionsMissionBehavior
    {
        private void Execute(PlannedTarget plan, double now)
        {
            if (plan == null || !_trackers.ContainsKey(plan.RequestId))
            {
                return;
            }
            if (plan.OwnerToken != Guid.Empty &&
                _cancelledTrustedOwners.Contains(plan.OwnerToken))
            {
                FinishPlan(
                    plan,
                    ExecutionResultCode.Cancelled,
                    "Trusted playback owner was cancelled before execution.");
                return;
            }
            if (now > plan.ExpiresAtMissionTime)
            {
                FinishPlan(plan, ExecutionResultCode.Expired, "Target due time exceeded TTL.");
                return;
            }
            if (plan.ProgramExecution != null)
            {
                ExecuteProgramStep(plan, now);
                return;
            }
            bool allowOwnedChannelZero = plan.Handle?.Agent != null &&
                                         ((_ownedStates.TryGetValue(
                                               plan.Handle.Agent.Index,
                                               out OwnedActionState ownedAtValidation) &&
                                           ownedAtValidation.Channel == 0) ||
                                          (_ownedLoops.TryGetValue(
                                               plan.Handle.Agent.Index,
                                               out OwnedLoopState loopAtValidation) &&
                                           loopAtValidation.Channel == 0));
            if (!TryValidateAgent(
                plan.Handle,
                out Agent agent,
                out ExecutionResultCode failure,
                allowOwnedChannelZero))
            {
                FinishPlan(plan, failure, "Agent validation failed at execution time.");
                return;
            }
            if (plan.OwnerToken != Guid.Empty &&
                IsTrustedPlaybackBlocked(plan, agent, out string trustedBlockReason))
            {
                FinishPlan(
                    plan,
                    ExecutionResultCode.Interrupted,
                    trustedBlockReason);
                return;
            }
            if (plan.Intent.Kind != IntentKind.ReleaseOwnedAction &&
                plan.Intent.Kind != IntentKind.ExitOwnedState &&
                !TryPrepareForPlayback(
                    agent,
                    plan.SelectedAction == null ||
                    plan.SelectedAction.Definition.Mode != ActionMode.Stateful,
                    out string prepareReason))
            {
                FinishPlan(
                    plan,
                    ExecutionResultCode.PreviousActionNotReleased,
                    prepareReason);
                return;
            }
            if (plan.Intent.Kind == IntentKind.ExitOwnedState)
            {
                ExecuteExitOwnedState(plan, agent, now);
                return;
            }
            if (plan.Intent.Kind == IntentKind.ReleaseOwnedAction ||
                plan.Intent.Kind == IntentKind.DrawWeapon ||
                plan.Intent.Kind == IntentKind.SheatheWeapon)
            {
                ExecuteRuntimeControl(plan, agent);
                return;
            }

            if (IsCooldownActive(agent, plan.SelectedAction.Definition.Key, now))
            {
                FinishPlan(plan, ExecutionResultCode.Interrupted, "Per-agent cooldown is active.");
                return;
            }
            if (plan.SelectedAction.Definition.Mode == ActionMode.Stateful)
            {
                ExecuteStatefulEnter(plan, agent, now);
            }
            else
            {
                ExecuteOneShot(plan, agent, now);
            }
        }
        private void ExecuteOneShot(PlannedTarget plan, Agent agent, double now)
        {
            SelectedAction selected = plan.SelectedAction;
            if (!_providerSession.TryResolve(
                selected.Definition.ProviderId,
                plan.FrozenActionId,
                out ActionIndexCache action,
                out ExecutionResultCode failure,
                out string reason))
            {
                FinishPlan(plan, failure, reason);
                return;
            }
            if (!TrySetAction(agent, selected.Variant, action, out string setReason))
            {
                FinishPlan(plan, ExecutionResultCode.SetActionRejected, setReason);
                return;
            }
            SetCooldown(agent, selected, now);
            RegisterOwnedPlayback(
                plan.Handle,
                plan.Intent.Key,
                selected,
                action,
                selected.Variant.Channel,
                now,
                plan.OwnerToken);
            FinishPlan(
                plan,
                ExecutionResultCode.AcceptedByEngine,
                "SetActionChannel accepted; visual completion was not asserted.");
        }
        private void ExecuteRuntimeControl(PlannedTarget plan, Agent agent)
        {
            if (plan.Intent.Kind == IntentKind.ReleaseOwnedAction)
            {
                if (!_ownedLoops.TryGetValue(agent.Index, out OwnedLoopState owned) ||
                    !ReferenceEquals(owned.Handle.Agent, agent))
                {
                    FinishPlan(
                        plan,
                        ExecutionResultCode.NoOwnedAction,
                        "No SceneActions-owned playback exists on this agent.");
                    return;
                }
                bool released = ReleaseOwnedLoopForAgent(agent, true);
                FinishPlan(
                    plan,
                    released
                        ? ExecutionResultCode.CompletedObserved
                        : ExecutionResultCode.Interrupted,
                    released
                        ? "SceneActions-owned playback channel was released."
                        : "Owned playback changed before it could be released safely.");
                return;
            }

            if (plan.Intent.Kind == IntentKind.DrawWeapon)
            {
                EquipmentIndex wielded = agent.GetPrimaryWieldedItemIndex();
                if (wielded >= EquipmentIndex.WeaponItemBeginSlot &&
                    wielded < EquipmentIndex.NumAllWeaponSlots)
                {
                    FinishPlan(
                        plan,
                        ExecutionResultCode.AlreadyWielded,
                        "Agent already has a primary weapon wielded.");
                    return;
                }
                if (!TryFindMeleeWeaponSlot(agent, out EquipmentIndex slot))
                {
                    FinishPlan(
                        plan,
                        ExecutionResultCode.NoUsableWeapon,
                        "Agent equipment contains no usable non-consumable melee weapon.");
                    return;
                }
                try
                {
                    agent.TryToWieldWeaponInSlot(
                        slot,
                        Agent.WeaponWieldActionType.WithAnimation,
                        false);
                    FinishPlan(
                        plan,
                        ExecutionResultCode.AcceptedByEngine,
                        "TryToWieldWeaponInSlot requested slot " + slot +
                        "; visual completion was not asserted.");
                }
                catch (Exception ex)
                {
                    FinishPlan(
                        plan,
                        ExecutionResultCode.ExecutorException,
                        ex.GetType().Name + ": " + ex.Message);
                }
                return;
            }

            EquipmentIndex primary = agent.GetPrimaryWieldedItemIndex();
            EquipmentIndex offhand = agent.GetOffhandWieldedItemIndex();
            bool hasPrimary = primary >= EquipmentIndex.WeaponItemBeginSlot &&
                              primary < EquipmentIndex.NumAllWeaponSlots;
            bool hasOffhand = offhand >= EquipmentIndex.WeaponItemBeginSlot &&
                              offhand < EquipmentIndex.NumAllWeaponSlots;
            if (!hasPrimary && !hasOffhand)
            {
                FinishPlan(
                    plan,
                    ExecutionResultCode.AlreadySheathed,
                    "Agent has no wielded weapon to sheathe.");
                return;
            }
            try
            {
                if (hasOffhand)
                {
                    agent.TryToSheathWeaponInHand(
                        Agent.HandIndex.OffHand,
                        Agent.WeaponWieldActionType.WithAnimation);
                }
                if (hasPrimary)
                {
                    agent.TryToSheathWeaponInHand(
                        Agent.HandIndex.MainHand,
                        Agent.WeaponWieldActionType.WithAnimation);
                }
                FinishPlan(
                    plan,
                    ExecutionResultCode.AcceptedByEngine,
                    "TryToSheathWeaponInHand was requested; visual completion was not asserted.");
            }
            catch (Exception ex)
            {
                FinishPlan(
                    plan,
                    ExecutionResultCode.ExecutorException,
                    ex.GetType().Name + ": " + ex.Message);
            }
        }
        private static bool TryFindMeleeWeaponSlot(
            Agent agent,
            out EquipmentIndex slot)
        {
            slot = EquipmentIndex.None;
            MissionEquipment equipment = agent?.Equipment;
            if (equipment == null)
            {
                return false;
            }
            for (EquipmentIndex candidate = EquipmentIndex.WeaponItemBeginSlot;
                 candidate < EquipmentIndex.NumPrimaryWeaponSlots;
                 candidate++)
            {
                MissionWeapon weapon = equipment[candidate];
                WeaponComponentData usage = weapon.CurrentUsageItem;
                if (!weapon.IsEmpty &&
                    usage != null &&
                    usage.IsMeleeWeapon &&
                    !usage.IsShield &&
                    !usage.IsConsumable)
                {
                    slot = candidate;
                    return true;
                }
            }
            return false;
        }
    }
}
