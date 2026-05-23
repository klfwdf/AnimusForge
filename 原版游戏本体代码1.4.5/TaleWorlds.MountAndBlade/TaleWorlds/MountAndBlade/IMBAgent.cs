using System;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade;

[ScriptingInterfaceBase]
internal interface IMBAgent
{
	[EngineMethod("get_movement_flags", false, null, false)]
	Agent.MovementControlFlag GetMovementFlags(UIntPtr agentPointer);

	[EngineMethod("set_movement_flags", false, null, false)]
	void SetMovementFlags(UIntPtr agentPointer, Agent.MovementControlFlag value);

	[EngineMethod("get_movement_input_vector", false, null, false)]
	Vec2 GetMovementInputVector(UIntPtr agentPointer);

	[EngineMethod("set_movement_input_vector", false, null, false)]
	void SetMovementInputVector(UIntPtr agentPointer, Vec2 value);

	[EngineMethod("get_collision_capsule", false, null, false)]
	void GetCollisionCapsule(UIntPtr agentPointer, ref CapsuleData value);

	[EngineMethod("set_attack_state", false, null, false)]
	void SetAttackState(UIntPtr agentPointer, int attackState);

	[EngineMethod("get_agent_visuals", false, null, false)]
	MBAgentVisuals GetAgentVisuals(UIntPtr agentPointer);

	[EngineMethod("get_event_control_flags", false, null, false)]
	Agent.EventControlFlag GetEventControlFlags(UIntPtr agentPointer);

	[EngineMethod("set_event_control_flags", false, null, false)]
	void SetEventControlFlags(UIntPtr agentPointer, Agent.EventControlFlag eventflag);

	[EngineMethod("get_has_on_ai_input_set_callback", false, null, false)]
	bool GetHasOnAiInputSetCallback(UIntPtr agentPointer);

	[EngineMethod("set_has_on_ai_input_set_callback", false, null, false)]
	void SetHasOnAiInputSetCallback(UIntPtr agentPointer, bool value);

	[EngineMethod("set_average_ping_in_milliseconds", false, null, false)]
	void SetAveragePingInMilliseconds(UIntPtr agentPointer, double averagePingInMilliseconds);

	[EngineMethod("set_look_agent", false, null, false)]
	void SetLookAgent(UIntPtr agentPointer, UIntPtr lookAtAgentPointer);

	[EngineMethod("get_look_agent", false, null, false)]
	Agent GetLookAgent(UIntPtr agentPointer);

	[EngineMethod("get_target_agent", false, null, false)]
	Agent GetTargetAgent(UIntPtr agentPointer);

	[EngineMethod("set_target_agent", false, null, false)]
	void SetTargetAgent(UIntPtr agentPointer, int targetAgentIndex);

	[EngineMethod("set_is_physics_force_closed", false, null, false)]
	void SetIsPhysicsForceClosed(UIntPtr agentPointer, bool isPhysicsForceClosed);

	[EngineMethod("set_interaction_agent", false, null, false)]
	void SetInteractionAgent(UIntPtr agentPointer, UIntPtr interactionAgentPointer);

	[EngineMethod("set_look_to_point_of_interest", false, null, false)]
	void SetLookToPointOfInterest(UIntPtr agentPointer, Vec3 point);

	[EngineMethod("disable_look_to_point_of_interest", false, null, false)]
	void DisableLookToPointOfInterest(UIntPtr agentPointer);

	[EngineMethod("is_enemy", false, null, false)]
	bool IsEnemy(UIntPtr agentPointer1, UIntPtr agentPointer2);

	[EngineMethod("is_friend", false, null, false)]
	bool IsFriend(UIntPtr agentPointer1, UIntPtr agentPointer2);

	[EngineMethod("set_agent_flags", false, null, false)]
	void SetAgentFlags(UIntPtr agentPointer, uint agentFlags);

	[EngineMethod("set_selected_mount_index", false, null, false)]
	void SetSelectedMountIndex(UIntPtr agentPointer, int mount_index);

	[EngineMethod("get_selected_mount_index", false, null, false)]
	int GetSelectedMountIndex(UIntPtr agentPointer);

	[EngineMethod("get_firing_order", false, null, false)]
	int GetFiringOrder(UIntPtr agentPointer);

	[EngineMethod("get_riding_order", false, null, false)]
	int GetRidingOrder(UIntPtr agentPointer);

	[EngineMethod("get_stepped_body_flags", false, null, false)]
	BodyFlags GetSteppedBodyFlags(UIntPtr agentPointer);

	[EngineMethod("get_stepped_entity_id", false, null, true)]
	UIntPtr GetSteppedEntityId(UIntPtr agentPointer0);

	[EngineMethod("get_stepped_root_entity_id", false, null, true)]
	UIntPtr GetSteppedRootEntity(UIntPtr agentPointer0);

	[EngineMethod("set_network_peer", false, null, false)]
	void SetNetworkPeer(UIntPtr agentPointer, int networkPeerIndex);

	[EngineMethod("die", false, null, false)]
	void Die(UIntPtr agentPointer, ref Blow b, sbyte overrideKillInfo);

	[EngineMethod("make_dead", false, null, false)]
	void MakeDead(UIntPtr agentPointer, bool isKilled, int actionIndex, int corpsesToFadeIndex);

	[EngineMethod("set_formation_frame_disabled", false, null, true)]
	void SetFormationFrameDisabled(UIntPtr agentPointer);

	[EngineMethod("set_formation_frame_enabled", false, null, true)]
	bool SetFormationFrameEnabled(UIntPtr agentPointer, WorldPosition position, Vec2 direction, Vec2 positionVelocity, float formationDirectionEnforcingFactor, bool teleportAgents);

	[EngineMethod("set_should_catch_up_with_formation", false, null, false)]
	void SetShouldCatchUpWithFormation(UIntPtr agentPointer, bool value);

	[EngineMethod("set_formation_integrity_data", false, null, true)]
	void SetFormationIntegrityData(UIntPtr agentPointer, in Vec2 position, in Vec2 currentFormationDirection, in Vec2 averageVelocityOfCloseAgents, float averageMaxUnlimitedSpeedOfCloseAgents, float deviationOfPositions, bool shouldKeepWithFormationInsteadOfMovingToAgent);

	[EngineMethod("set_formation_info", false, null, false)]
	void SetFormationInfo(UIntPtr agentPointer, int fileIndex, int rankIndex, int fileCount, int rankCount, int unitCount, Vec2 wallDir, int unitSpacing);

	[EngineMethod("set_retreat_mode", false, null, false)]
	void SetRetreatMode(UIntPtr agentPointer, WorldPosition retreatPos, bool retreat);

	[EngineMethod("is_retreating", false, null, true)]
	bool IsRetreating(UIntPtr agentPointer);

	[EngineMethod("is_fading_out", false, null, true)]
	bool IsFadingOut(UIntPtr agentPointer);

	[EngineMethod("is_wandering", false, null, true)]
	bool IsWandering(UIntPtr agentPointer);

	[EngineMethod("start_fading_out", false, null, false)]
	void StartFadingOut(UIntPtr agentPointer);

	[EngineMethod("set_render_check_enabled", false, null, false)]
	void SetRenderCheckEnabled(UIntPtr agentPointer, bool value);

	[EngineMethod("get_render_check_enabled", false, null, false)]
	bool GetRenderCheckEnabled(UIntPtr agentPointer);

	[EngineMethod("get_retreat_pos", false, null, false)]
	WorldPosition GetRetreatPos(UIntPtr agentPointer);

	[EngineMethod("get_team", false, null, false)]
	int GetTeam(UIntPtr agentPointer);

	[EngineMethod("set_team", false, null, false)]
	void SetTeam(UIntPtr agentPointer, int teamIndex);

	[EngineMethod("set_courage", false, null, false)]
	void SetCourage(UIntPtr agentPointer, float courage);

	[EngineMethod("update_driven_properties", false, null, false)]
	void UpdateDrivenProperties(UIntPtr agentPointer, float[] values);

	[EngineMethod("get_look_direction", false, null, false)]
	Vec3 GetLookDirection(UIntPtr agentPointer);

	[EngineMethod("set_look_direction", false, null, false)]
	void SetLookDirection(UIntPtr agentPointer, Vec3 lookDirection);

	[EngineMethod("get_look_down_limit", false, null, false)]
	float GetLookDownLimit(UIntPtr agentPointer);

	[EngineMethod("get_position", false, null, false)]
	Vec3 GetPosition(UIntPtr agentPointer);

	[EngineMethod("set_position", false, null, false)]
	void SetPosition(UIntPtr agentPointer, ref Vec3 position);

	[EngineMethod("get_rotation_frame", false, null, false)]
	void GetRotationFrame(UIntPtr agentPointer, ref MatrixFrame outFrame);

	[EngineMethod("get_eye_global_height", false, null, false)]
	float GetEyeGlobalHeight(UIntPtr agentPointer);

	[EngineMethod("get_movement_velocity", false, null, false)]
	Vec2 GetMovementVelocity(UIntPtr agentPointer);

	[EngineMethod("get_average_velocity", false, null, false)]
	Vec3 GetAverageVelocity(UIntPtr agentPointer);

	[EngineMethod("set_weapon_guard", false, null, false)]
	void SetWeaponGuard(UIntPtr agentPointer, Agent.UsageDirection direction);

	[EngineMethod("get_is_left_stance", false, null, false)]
	bool GetIsLeftStance(UIntPtr agentPointer);

	[EngineMethod("invalidate_target_agent", false, null, false)]
	void InvalidateTargetAgent(UIntPtr agentPointer);

	[EngineMethod("invalidate_ai_weapon_selections", false, null, false)]
	void InvalidateAIWeaponSelections(UIntPtr agentPointer);

	[EngineMethod("reset_enemy_caches", false, null, false)]
	void ResetEnemyCaches(UIntPtr agentPointer);

	[EngineMethod("get_ai_state_flags", false, null, true)]
	Agent.AIStateFlag GetAIStateFlags(UIntPtr agentPointer);

	[EngineMethod("set_ai_alarm_state", false, null, false)]
	void SetAIAlarmState(UIntPtr agentPointer, Agent.AIStateFlag aiStateFlags);

	[EngineMethod("set_ai_state_flags", false, null, false)]
	void SetAIStateFlags(UIntPtr agentPointer, Agent.AIStateFlag aiStateFlags);

	[EngineMethod("set_automatic_target_agent_selection", false, null, false)]
	void SetAutomaticTargetSelection(UIntPtr agentPointer, bool enable);

	[EngineMethod("start_ragdoll_as_corpse", false, null, false)]
	void StartRagdollAsCorpse(UIntPtr agentPointer);

	[EngineMethod("end_ragdoll_as_corpse", false, null, false)]
	void EndRagdollAsCorpse(UIntPtr agentPointer);

	[EngineMethod("is_added_as_corpse", false, null, false)]
	bool IsAddedAsCorpse(UIntPtr agentPointer);

	[EngineMethod("add_as_corpse", false, null, false)]
	void AddAsCorpse(UIntPtr agentPointer);

	[EngineMethod("set_overriden_strike_and_death_action", false, null, false)]
	void SetOverridenStrikeAndDeathAction(UIntPtr agentPointer, int strikeActionIndex, int deathActionIndex);

	[EngineMethod("apply_force_on_ragdoll", false, null, false)]
	void ApplyForceOnRagdoll(UIntPtr agentPointer, sbyte boneIndex, in Vec3 force);

	[EngineMethod("set_velocity_limits_on_ragdoll", false, null, false)]
	void SetVelocityLimitsOnRagdoll(UIntPtr agentPointer, float linearVelocityLimit, float angularVelocityLimit);

	[EngineMethod("get_state_flags", false, null, false)]
	AgentState GetStateFlags(UIntPtr agentPointer);

	[EngineMethod("set_state_flags", false, null, false)]
	void SetStateFlags(UIntPtr agentPointer, AgentState StateFlags);

	[EngineMethod("get_ai_last_suspicious_position", false, null, false)]
	WorldPosition GetAILastSuspiciousPosition(UIntPtr agentPointer);

	[EngineMethod("set_ai_last_suspicious_position", false, null, false)]
	void SetAILastSuspiciousPosition(UIntPtr agentPointer, in WorldPosition lastSuspiciousPosition);

	[EngineMethod("get_mount_agent", false, null, false)]
	Agent GetMountAgent(UIntPtr agentPointer);

	[EngineMethod("set_mount_agent", false, null, false)]
	void SetMountAgent(UIntPtr agentPointer, int mountAgentIndex);

	[EngineMethod("get_rider_agent", false, null, false)]
	Agent GetRiderAgent(UIntPtr agentPointer);

	[EngineMethod("set_controller", false, null, false)]
	void SetController(UIntPtr agentPointer, AgentControllerType controller);

	[EngineMethod("set_initial_frame", false, null, false)]
	void SetInitialFrame(UIntPtr agentPointer, in Vec3 initialPosition, in Vec2 initialDirection, bool canSpawnOutsideOfMissionBoundary);

	[EngineMethod("weapon_equipped", false, null, false)]
	void WeaponEquipped(UIntPtr agentPointer, int equipmentSlot, in WeaponData weaponData, WeaponStatsData[] weaponStatsData, int weaponStatsDataLength, in WeaponData ammoWeaponData, WeaponStatsData[] ammoWeaponStatsData, int ammoWeaponStatsDataLength, UIntPtr weaponEntity, bool removeOldWeaponFromScene, bool isWieldedOnSpawn);

	[EngineMethod("drop_item", false, null, false)]
	void DropItem(UIntPtr agentPointer, int itemIndex, int pickedUpItemType);

	[EngineMethod("set_weapon_amount_in_slot", false, null, false)]
	void SetWeaponAmountInSlot(UIntPtr agentPointer, int equipmentSlot, short amount, bool enforcePrimaryItem);

	[EngineMethod("clear_equipment", false, null, false)]
	void ClearEquipment(UIntPtr agentPointer);

	[EngineMethod("set_wielded_item_index_as_client", false, null, false)]
	void SetWieldedItemIndexAsClient(UIntPtr agentPointer, int handIndex, int wieldedItemIndex, bool isWieldedInstantly, bool isWieldedOnSpawn, int mainHandCurrentUsageIndex);

	[EngineMethod("set_usage_index_of_weapon_in_slot_as_client", false, null, false)]
	void SetUsageIndexOfWeaponInSlotAsClient(UIntPtr agentPointer, int slotIndex, int usageIndex);

	[EngineMethod("set_weapon_hit_points_in_slot", false, null, false)]
	void SetWeaponHitPointsInSlot(UIntPtr agentPointer, int wieldedItemIndex, short hitPoints);

	[EngineMethod("set_weapon_ammo_as_client", false, null, false)]
	void SetWeaponAmmoAsClient(UIntPtr agentPointer, int equipmentIndex, int ammoEquipmentIndex, short ammo);

	[EngineMethod("set_weapon_reload_phase_as_client", false, null, false)]
	void SetWeaponReloadPhaseAsClient(UIntPtr agentPointer, int wieldedItemIndex, short reloadPhase);

	[EngineMethod("set_reload_ammo_in_slot", false, null, false)]
	void SetReloadAmmoInSlot(UIntPtr agentPointer, int slotIndex, int ammoSlotIndex, short reloadedAmmo);

	[EngineMethod("start_switching_weapon_usage_index_as_client", false, null, false)]
	void StartSwitchingWeaponUsageIndexAsClient(UIntPtr agentPointer, int wieldedItemIndex, int usageIndex, Agent.UsageDirection currentMovementFlagUsageDirection);

	[EngineMethod("try_to_wield_weapon_in_slot", false, null, false)]
	void TryToWieldWeaponInSlot(UIntPtr agentPointer, int equipmentSlot, int type, bool isWieldedOnSpawn);

	[EngineMethod("get_weapon_entity_from_equipment_slot", false, null, false)]
	UIntPtr GetWeaponEntityFromEquipmentSlot(UIntPtr agentPointer, int equipmentSlot);

	[EngineMethod("prepare_weapon_for_drop_in_equipment_slot", false, null, false)]
	void PrepareWeaponForDropInEquipmentSlot(UIntPtr agentPointer, int equipmentSlot, bool dropWithHolster);

	[EngineMethod("try_to_sheath_weapon_in_hand", false, null, false)]
	void TryToSheathWeaponInHand(UIntPtr agentPointer, int handIndex, int type);

	[EngineMethod("update_weapons", false, null, false)]
	void UpdateWeapons(UIntPtr agentPointer);

	[EngineMethod("attach_weapon_to_bone", false, null, false)]
	void AttachWeaponToBone(UIntPtr agentPointer, in WeaponData weaponData, WeaponStatsData[] weaponStatsData, int weaponStatsDataLength, UIntPtr weaponEntity, sbyte boneIndex, ref MatrixFrame attachLocalFrame);

	[EngineMethod("delete_attached_weapon_from_bone", false, null, false)]
	void DeleteAttachedWeaponFromBone(UIntPtr agentPointer, int attachedWeaponIndex);

	[EngineMethod("attach_weapon_to_weapon_in_slot", false, null, false)]
	void AttachWeaponToWeaponInSlot(UIntPtr agentPointer, in WeaponData weaponData, WeaponStatsData[] weaponStatsData, int weaponStatsDataLength, UIntPtr weaponEntity, int slotIndex, ref MatrixFrame attachLocalFrame);

	[EngineMethod("build", false, null, false)]
	void Build(UIntPtr agentPointer, Vec3 eyeOffsetWrtHead);

	[EngineMethod("lock_agent_replication_table_with_current_reliable_sequence_no", false, null, false)]
	void LockAgentReplicationTableDataWithCurrentReliableSequenceNo(UIntPtr agentPointer, int peerIndex);

	[EngineMethod("set_agent_exclude_state_for_face_group_id", false, null, false)]
	void SetAgentExcludeStateForFaceGroupId(UIntPtr agentPointer, int faceGroupId, bool isExcluded);

	[EngineMethod("set_agent_scale", false, null, false)]
	void SetAgentScale(UIntPtr agentPointer, float scale);

	[EngineMethod("initialize_agent_record", false, null, false)]
	void InitializeAgentRecord(UIntPtr agentPointer);

	[EngineMethod("get_current_velocity", false, null, false)]
	Vec2 GetCurrentVelocity(UIntPtr agentPointer);

	[EngineMethod("get_turn_speed", false, null, false)]
	float GetTurnSpeed(UIntPtr agentPointer);

	[EngineMethod("set_movement_direction", false, null, false)]
	void SetMovementDirection(UIntPtr agentPointer, in Vec2 direction);

	[EngineMethod("get_current_speed_limit", false, null, false)]
	float GetCurrentSpeedLimit(UIntPtr agentPointer);

	[EngineMethod("set_maximum_speed_limit", false, null, true)]
	void SetMaximumSpeedLimit(UIntPtr agentPointer, float maximumSpeedLimit, bool isMultiplier);

	[EngineMethod("get_maximum_speed_limit", false, null, false)]
	float GetMaximumSpeedLimit(UIntPtr agentPointer);

	[EngineMethod("get_real_global_velocity", false, null, false)]
	Vec3 GetRealGlobalVelocity(UIntPtr agentPointer);

	[EngineMethod("get_average_real_global_velocity", false, null, false)]
	Vec3 GetAverageRealGlobalVelocity(UIntPtr agentPointer);

	[EngineMethod("fade_out", false, null, false)]
	void FadeOut(UIntPtr agentPointer, bool hideInstantly);

	[EngineMethod("fade_in", false, null, false)]
	void FadeIn(UIntPtr agentPointer);

	[EngineMethod("get_scripted_flags", false, null, false)]
	int GetScriptedFlags(UIntPtr agentPointer);

	[EngineMethod("set_scripted_flags", false, null, false)]
	void SetScriptedFlags(UIntPtr agentPointer, int flags);

	[EngineMethod("get_scripted_combat_flags", false, null, false)]
	int GetScriptedCombatFlags(UIntPtr agentPointer);

	[EngineMethod("set_scripted_combat_flags", false, null, false)]
	void SetScriptedCombatFlags(UIntPtr agentPointer, int flags);

	[EngineMethod("set_scripted_position_and_direction", false, null, false)]
	bool SetScriptedPositionAndDirection(UIntPtr agentPointer, ref WorldPosition targetPosition, float targetDirection, bool addHumanLikeDelay, int additionalFlags);

	[EngineMethod("set_scripted_position", false, null, false)]
	bool SetScriptedPosition(UIntPtr agentPointer, ref WorldPosition targetPosition, bool addHumanLikeDelay, int additionalFlags);

	[EngineMethod("set_scripted_target_entity", false, null, false)]
	void SetScriptedTargetEntity(UIntPtr agentPointer, UIntPtr entityId, int additionalFlags, bool ignoreIfAlreadyAttacking);

	[EngineMethod("disable_scripted_movement", false, null, false)]
	void DisableScriptedMovement(UIntPtr agentPointer);

	[EngineMethod("disable_scripted_combat_movement", false, null, false)]
	void DisableScriptedCombatMovement(UIntPtr agentPointer);

	[EngineMethod("force_ai_behavior_selection", false, null, false)]
	void ForceAiBehaviorSelection(UIntPtr agentPointer);

	[EngineMethod("has_path_through_navigation_face_id_from_direction", false, null, false)]
	bool HasPathThroughNavigationFaceIdFromDirection(UIntPtr agentPointer, int navigationFaceId, ref Vec2 direction);

	[EngineMethod("has_path_through_navigation_faces_id_from_direction", false, null, false)]
	bool HasPathThroughNavigationFacesIDFromDirection(UIntPtr agentPointer, int navigationFaceID_1, int navigationFaceID_2, int navigationFaceID_3, ref Vec2 direction);

	[EngineMethod("can_move_directly_to_position", false, null, false)]
	bool CanMoveDirectlyToPosition(UIntPtr agentPointer, in Vec2 position);

	[EngineMethod("check_path_to_ai_target_agent_passes_through_navigation_face_id_from_direction", false, null, false)]
	bool CheckPathToAITargetAgentPassesThroughNavigationFaceIdFromDirection(UIntPtr agentPointer, int navigationFaceId, in Vec3 direction, float overridenCostForFaceId);

	[EngineMethod("is_target_navigation_face_id_between", false, null, false)]
	bool IsTargetNavigationFaceIdBetween(UIntPtr agentPointer, int navigationFaceIdStart, int navigationFaceIdEnd);

	[EngineMethod("get_path_distance_to_point", false, null, false)]
	float GetPathDistanceToPoint(UIntPtr agentPointer, ref Vec3 direction);

	[EngineMethod("get_current_navigation_face_id", false, null, true)]
	int GetCurrentNavigationFaceId(UIntPtr agentPointer);

	[EngineMethod("get_world_position", false, null, false)]
	WorldPosition GetWorldPosition(UIntPtr agentPointer);

	[EngineMethod("set_agent_facial_animation", false, null, false)]
	void SetAgentFacialAnimation(UIntPtr agentPointer, int channel, string animationName, bool loop);

	[EngineMethod("get_agent_facial_animation", false, null, false)]
	string GetAgentFacialAnimation(UIntPtr agentPointer);

	[EngineMethod("get_agent_voice_definiton", false, null, false)]
	string GetAgentVoiceDefinition(UIntPtr agentPointer);

	[EngineMethod("get_current_animation_flags", false, null, false)]
	ulong GetCurrentAnimationFlags(UIntPtr agentPointer, int channelNo);

	[EngineMethod("get_current_action_type", false, null, true)]
	int GetCurrentActionType(UIntPtr agentPointer, int channelNo);

	[EngineMethod("get_current_action_stage", false, null, true)]
	int GetCurrentActionStage(UIntPtr agentPointer, int channelNo);

	[EngineMethod("get_current_action_direction", false, null, true)]
	int GetCurrentActionDirection(UIntPtr agentPointer, int channelNo);

	[EngineMethod("compute_animation_displacement", false, null, false)]
	Vec3 ComputeAnimationDisplacement(UIntPtr agentPointer, float dt);

	[EngineMethod("get_current_action_priority", false, null, true)]
	int GetCurrentActionPriority(UIntPtr agentPointer, int channelNo);

	[EngineMethod("get_current_action_progress", false, null, true)]
	float GetCurrentActionProgress(UIntPtr agentPointer, int channelNo);

	[EngineMethod("set_current_action_progress", false, null, true)]
	void SetCurrentActionProgress(UIntPtr agentPointer, int channelNo, float progress);

	[EngineMethod("set_action_channel", false, null, true)]
	bool SetActionChannel(UIntPtr agentPointer, int channelNo, int actionNo, ulong additionalFlags, bool ignorePriority, float blendWithNextActionFactor, float actionSpeed, float blendInPeriod, float blendOutPeriodToNoAnim, float startProgress, bool useLinearSmoothing, float blendOutPeriod, bool forceFaceMorphRestart);

	[EngineMethod("set_current_action_speed", false, null, false)]
	void SetCurrentActionSpeed(UIntPtr agentPointer, int channelNo, float actionSpeed);

	[EngineMethod("tick_action_channels", false, null, false)]
	void TickActionChannels(UIntPtr agentPointer, float dt);

	[EngineMethod("get_action_channel_weight", false, null, false)]
	float GetActionChannelWeight(UIntPtr agentPointer, int channelNo);

	[EngineMethod("get_action_channel_current_action_weight", false, null, false)]
	float GetActionChannelCurrentActionWeight(UIntPtr agentPointer, int channelNo);

	[EngineMethod("set_action_set", false, null, false)]
	void SetActionSet(UIntPtr agentPointer, ref AnimationSystemData animationSystemData);

	[EngineMethod("get_action_set_no", false, null, true)]
	int GetActionSetNo(UIntPtr agentPointer);

	[EngineMethod("get_movement_locked_state", false, null, false)]
	AgentMovementLockedState GetMovementLockedState(UIntPtr agentPointer);

	[EngineMethod("get_aiming_timer", false, null, false)]
	float GetAimingTimer(UIntPtr agentPointer);

	[EngineMethod("get_target_position", false, null, false)]
	Vec2 GetTargetPosition(UIntPtr agentPointer);

	[EngineMethod("set_target_position", false, null, false)]
	void SetTargetPosition(UIntPtr agentPointer, ref Vec2 targetPosition);

	[EngineMethod("set_target_z", false, null, false)]
	void SetTargetZ(UIntPtr agentPointer, float targetZ);

	[EngineMethod("clear_target_z", false, null, false)]
	void ClearTargetZ(UIntPtr agentPointer);

	[EngineMethod("set_target_up", false, null, false)]
	void SetTargetUp(UIntPtr agentPointer, in Vec3 targetUp);

	[EngineMethod("get_target_direction", false, null, false)]
	Vec3 GetTargetDirection(UIntPtr agentPointer);

	[EngineMethod("set_target_position_and_direction", false, null, true)]
	void SetTargetPositionAndDirection(UIntPtr agentPointer, in Vec2 targetPosition, in Vec3 targetDirection);

	[EngineMethod("add_acceleration", false, null, false)]
	void AddAcceleration(UIntPtr agentPointer, in Vec3 acceleration);

	[EngineMethod("clear_target_frame", false, null, false)]
	void ClearTargetFrame(UIntPtr agentPointer);

	[EngineMethod("get_is_look_direction_locked", false, null, false)]
	bool GetIsLookDirectionLocked(UIntPtr agentPointer);

	[EngineMethod("set_is_look_direction_locked", false, null, false)]
	void SetIsLookDirectionLocked(UIntPtr agentPointer, bool isLocked);

	[EngineMethod("set_mono_object", false, null, false)]
	void SetMonoObject(UIntPtr agentPointer, Agent monoObject);

	[EngineMethod("get_eye_global_position", false, null, false)]
	Vec3 GetEyeGlobalPosition(UIntPtr agentPointer);

	[EngineMethod("get_chest_global_position", false, null, false)]
	Vec3 GetChestGlobalPosition(UIntPtr agentPointer);

	[EngineMethod("add_mesh_to_bone", false, null, false)]
	void AddMeshToBone(UIntPtr agentPointer, UIntPtr meshPointer, sbyte boneIndex);

	[EngineMethod("remove_mesh_from_bone", false, null, false)]
	void RemoveMeshFromBone(UIntPtr agentPointer, UIntPtr meshPointer, sbyte boneIndex);

	[EngineMethod("add_prefab_to_agent_bone", false, null, false)]
	CompositeComponent AddPrefabToAgentBone(UIntPtr agentPointer, string prefabName, sbyte boneIndex);

	[EngineMethod("wield_next_weapon", false, null, false)]
	void WieldNextWeapon(UIntPtr agentPointer, int handIndex, int wieldActionType);

	[EngineMethod("preload_for_rendering", false, null, false)]
	void PreloadForRendering(UIntPtr agentPointer);

	[EngineMethod("get_ai_move_stop_tolerance", false, null, false)]
	float GetAIMoveStopTolerance(UIntPtr agentPointer);

	[EngineMethod("get_agent_scale", false, null, true)]
	float GetAgentScale(UIntPtr agentPointer);

	[EngineMethod("get_ai_move_destination", false, null, false)]
	WorldPosition GetAIMoveDestination(UIntPtr agentPointer);

	[EngineMethod("find_longest_direct_move_to_position", false, null, false)]
	Vec2 FindLongestDirectMoveToPosition(UIntPtr agentPointer, Vec2 targetPosition, bool checkBoundaries, bool checkFriendlyAgents, out bool isCollidedWithAgent);

	[EngineMethod("get_crouch_mode", false, null, false)]
	bool GetCrouchMode(UIntPtr agentPointer);

	[EngineMethod("get_walk_mode", false, null, false)]
	bool GetWalkMode(UIntPtr agentPointer);

	[EngineMethod("get_visual_position", false, null, false)]
	Vec3 GetVisualPosition(UIntPtr agentPointer);

	[EngineMethod("is_look_rotation_in_slow_motion", false, null, false)]
	bool IsLookRotationInSlowMotion(UIntPtr agentPointer);

	[EngineMethod("get_look_direction_as_angle", false, null, false)]
	float GetLookDirectionAsAngle(UIntPtr agentPointer);

	[EngineMethod("set_look_direction_as_angle", false, null, false)]
	void SetLookDirectionAsAngle(UIntPtr agentPointer, float value);

	[EngineMethod("attack_direction_to_movement_flag", false, null, false)]
	Agent.MovementControlFlag AttackDirectionToMovementFlag(UIntPtr agentPointer, Agent.UsageDirection direction);

	[EngineMethod("defend_direction_to_movement_flag", false, null, false)]
	Agent.MovementControlFlag DefendDirectionToMovementFlag(UIntPtr agentPointer, Agent.UsageDirection direction);

	[EngineMethod("get_head_camera_mode", false, null, false)]
	bool GetHeadCameraMode(UIntPtr agentPointer);

	[EngineMethod("set_head_camera_mode", false, null, false)]
	void SetHeadCameraMode(UIntPtr agentPointer, bool value);

	[EngineMethod("kick_clear", false, null, false)]
	bool KickClear(UIntPtr agentPointer);

	[EngineMethod("reset_guard", false, null, false)]
	void ResetGuard(UIntPtr agentPointer);

	[EngineMethod("get_current_guard_mode", false, null, false)]
	Agent.GuardMode GetCurrentGuardMode(UIntPtr agentPointer);

	[EngineMethod("get_defend_movement_flag", false, null, false)]
	Agent.MovementControlFlag GetDefendMovementFlag(UIntPtr agentPointer);

	[EngineMethod("get_attack_direction", false, null, false)]
	Agent.UsageDirection GetAttackDirection(UIntPtr agentPointer);

	[EngineMethod("player_attack_direction", false, null, false)]
	Agent.UsageDirection PlayerAttackDirection(UIntPtr agentPointer);

	[EngineMethod("get_wielded_weapon_info", false, null, false)]
	bool GetWieldedWeaponInfo(UIntPtr agentPointer, int handIndex, ref bool isMeleeWeapon, ref bool isRangedWeapon);

	[EngineMethod("get_immediate_enemy", false, null, false)]
	Agent GetImmediateEnemy(UIntPtr agentPointer);

	[EngineMethod("try_get_immediate_agent_movement_data", false, null, false)]
	bool TryGetImmediateEnemyAgentMovementData(UIntPtr agentPointer, out float maximumForwardUnlimitedSpeed, out Vec3 position);

	[EngineMethod("get_is_doing_passive_attack", false, null, false)]
	bool GetIsDoingPassiveAttack(UIntPtr agentPointer);

	[EngineMethod("get_is_passive_usage_conditions_are_met", false, null, false)]
	bool GetIsPassiveUsageConditionsAreMet(UIntPtr agentPointer);

	[EngineMethod("get_current_aiming_turbulance", false, null, false)]
	float GetCurrentAimingTurbulance(UIntPtr agentPointer);

	[EngineMethod("get_current_aiming_error", false, null, false)]
	float GetCurrentAimingError(UIntPtr agentPointer);

	[EngineMethod("get_body_rotation_constraint", false, null, false)]
	Vec3 GetBodyRotationConstraint(UIntPtr agentPointer, int channelIndex);

	[EngineMethod("get_total_mass", false, null, false)]
	float GetTotalMass(UIntPtr agentPointer);

	[EngineMethod("get_action_direction", false, null, false)]
	Agent.UsageDirection GetActionDirection(int actionIndex);

	[EngineMethod("get_attack_direction_usage", false, null, false)]
	Agent.UsageDirection GetAttackDirectionUsage(UIntPtr agentPointer);

	[EngineMethod("handle_blow_aux", false, null, false)]
	void HandleBlowAux(UIntPtr agentPointer, ref Blow blow);

	[EngineMethod("get_quick_bone_entitial_frame", false, null, false)]
	void GetBoneEntitialFrame(UIntPtr agentPointer, sbyte boneIndex, bool useBoneMapping, ref MatrixFrame outFrame);

	[EngineMethod("make_voice", false, null, false)]
	void MakeVoice(UIntPtr agentPointer, int voiceType, int predictionType);

	[EngineMethod("yell_after_delay", false, null, false)]
	void YellAfterDelay(UIntPtr agentPointer, float delayTimeInSecond);

	[EngineMethod("set_hand_inverse_kinematics_frame", false, null, false)]
	bool SetHandInverseKinematicsFrame(UIntPtr agentPointer, in MatrixFrame leftGlobalFrame, in MatrixFrame rightGlobalFrame);

	[EngineMethod("set_hand_inverse_kinematics_frame_for_mission_object_usage", false, null, false)]
	bool SetHandInverseKinematicsFrameForMissionObjectUsage(UIntPtr agentPointer, in MatrixFrame localIKFrame, in MatrixFrame boundEntityGlobalFrame, float animationHeightDifference);

	[EngineMethod("clear_hand_inverse_kinematics", false, null, false)]
	void ClearHandInverseKinematics(UIntPtr agentPointer);

	[EngineMethod("debug_more", false, null, false)]
	void DebugMore(UIntPtr agentPointer);

	[EngineMethod("get_agent_parent_entity", false, null, false)]
	GameEntity GetAgentParentEntity(UIntPtr agentPointer);

	[EngineMethod("set_excluded_from_gravity", false, null, false)]
	void SetExcludedFromGravity(UIntPtr agentPointer, bool exclude, bool applyAverageGlobalVelocity);

	[EngineMethod("set_force_attached_entity", false, null, false)]
	void SetForceAttachedEntity(UIntPtr agentPointer, UIntPtr entityPointer);

	[EngineMethod("is_sliding", false, null, false)]
	bool IsSliding(UIntPtr agentPointer);

	[EngineMethod("is_running_away", false, null, false)]
	bool IsRunningAway(UIntPtr agentPointer);

	[EngineMethod("is_crouching_allowed", false, null, false)]
	bool IsCrouchingAllowed(UIntPtr agentPointer);

	[EngineMethod("get_cur_weapon_offset", false, null, false)]
	Vec3 GetCurWeaponOffset(UIntPtr agentPointer);

	[EngineMethod("get_walking_speed_limit_of_mountable", false, null, false)]
	float GetWalkSpeedLimitOfMountable(UIntPtr agentPointer);

	[EngineMethod("create_blood_burst_at_limb", false, null, false)]
	void CreateBloodBurstAtLimb(UIntPtr agentPointer, sbyte realBoneIndex, float scale);

	[EngineMethod("get_native_action_index", false, null, false)]
	int GetNativeActionIndex(string actionName);

	[EngineMethod("set_columnwise_follow_agent", false, null, false)]
	void SetColumnwiseFollowAgent(UIntPtr agentPointer, int followAgentIndex, ref Vec2 followPosition);

	[EngineMethod("get_monster_usage_index", false, null, false)]
	int GetMonsterUsageIndex(string monsterUsage);

	[EngineMethod("get_missile_range_with_height_difference", false, null, false)]
	float GetMissileRangeWithHeightDifference(UIntPtr agentPointer, float targetZ);

	[EngineMethod("set_formation_no", false, null, false)]
	void SetFormationNo(UIntPtr agentPointer, int formationNo);

	[EngineMethod("enforce_shield_usage", false, null, false)]
	void EnforceShieldUsage(UIntPtr agentPointer, Agent.UsageDirection direction);

	[EngineMethod("set_firing_order", false, null, false)]
	void SetFiringOrder(UIntPtr agentPointer, int order);

	[EngineMethod("set_riding_order", false, null, false)]
	void SetRidingOrder(UIntPtr agentPointer, int order);

	[EngineMethod("get_target_formation_index", false, null, false)]
	int GetTargetFormationIndex(UIntPtr agentPointer);

	[EngineMethod("set_target_formation_index", false, null, false)]
	void SetTargetFormationIndex(UIntPtr agentPointer, int targetFormationIndex);

	[EngineMethod("set_direction_change_tendency", false, null, true)]
	void SetDirectionChangeTendency(UIntPtr agentPointer, float tendency);

	[EngineMethod("set_ai_behavior_params", false, null, false)]
	void SetAIBehaviorParams(UIntPtr agentPointer, int behavior, float y1, float x2, float y2, float x3, float y3);

	[EngineMethod("set_all_ai_behavior_params", false, null, false)]
	void SetAllAIBehaviorParams(UIntPtr agentPointer, HumanAIComponent.BehaviorValues[] behaviorParams);

	[EngineMethod("set_body_armor_material_type", false, null, false)]
	void SetBodyArmorMaterialType(UIntPtr agentPointer, ArmorComponent.ArmorMaterialTypes bodyArmorMaterialType);

	[EngineMethod("get_maximum_number_of_agents", false, null, false)]
	int GetMaximumNumberOfAgents();

	[EngineMethod("get_running_simulation_data_until_maximum_speed_reached", false, null, false)]
	void GetRunningSimulationDataUntilMaximumSpeedReached(UIntPtr agentPointer, ref float combatAccelerationTime, ref float maxSpeed, float[] speedValues);

	[EngineMethod("get_last_target_visibility_state", false, null, false)]
	int GetLastTargetVisibilityState(UIntPtr agentPointer);

	[EngineMethod("get_missile_range", false, null, false)]
	float GetMissileRange(UIntPtr agentPointer);

	[EngineMethod("set_agent_idle_animation_status", false, null, false)]
	void SetAgentIdleAnimationStatus(UIntPtr agentPointer, bool idleEnabled);

	[EngineMethod("get_old_wielded_item_info", false, null, false)]
	void GetOldWieldedItemInfo(UIntPtr agentPointer, out int rightHandSlotIndex, out int rightHandUsageIndex, out int leftHandSlotIndex, out int leftHandUsageIndex);

	[EngineMethod("get_ground_material_for_collision_effect", false, null, false)]
	int GetGroundMaterialForCollisionEffect(UIntPtr agentPointer);

	[EngineMethod("get_bone_entitial_frame_at_animation_progress", false, null, true)]
	MatrixFrame GetBoneEntitialFrameAtAnimationProgress(UIntPtr agentPointer, sbyte boneIndex, int animationIndex, float progress);
}
