using System;
using TaleWorlds.Core;
using TaleWorlds.DotNet;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade;

[ScriptingInterfaceBase]
internal interface IMBMission
{
	[EngineMethod("clear_resources", false, null, false)]
	void ClearResources(UIntPtr missionPointer);

	[EngineMethod("create_mission", false, null, false)]
	UIntPtr CreateMission(Mission mission);

	[EngineMethod("set_close_proximity_wave_sounds_enabled", false, null, false)]
	void SetCloseProximityWaveSoundsEnabled(UIntPtr missionPointer, bool value);

	[EngineMethod("force_disable_occlusion", false, null, false)]
	void ForceDisableOcclusion(UIntPtr missionPointer, bool value);

	[EngineMethod("tick_agents_and_teams_async", false, null, false)]
	void TickAgentsAndTeamsAsync(UIntPtr missionPointer, float dt);

	[EngineMethod("get_tick_debug_paused", false, null, false)]
	bool GetTickDebugPaused(UIntPtr missionPointer);

	[EngineMethod("clear_agent_actions", false, null, false)]
	void ClearAgentActions(UIntPtr missionPointer);

	[EngineMethod("clear_missiles", false, null, false)]
	void ClearMissiles(UIntPtr missionPointer);

	[EngineMethod("clear_corpses", false, null, false)]
	void ClearCorpses(UIntPtr missionPointer, bool isMissionReset);

	[EngineMethod("get_pause_ai_tick", false, null, false)]
	bool GetPauseAITick(UIntPtr missionPointer);

	[EngineMethod("set_pause_ai_tick", false, null, false)]
	void SetPauseAITick(UIntPtr missionPointer, bool value);

	[EngineMethod("get_clear_scene_timer_elapsed_time", false, null, false)]
	float GetClearSceneTimerElapsedTime(UIntPtr missionPointer);

	[EngineMethod("reset_first_third_person_view", false, null, false)]
	void ResetFirstThirdPersonView(UIntPtr missionPointer);

	[EngineMethod("set_camera_is_first_person", false, null, false)]
	void SetCameraIsFirstPerson(bool value);

	[EngineMethod("set_camera_frame", false, null, false)]
	void SetCameraFrame(UIntPtr missionPointer, ref MatrixFrame cameraFrame, float zoomFactor, ref Vec3 attenuationPosition);

	[EngineMethod("get_camera_frame", false, null, false)]
	MatrixFrame GetCameraFrame(UIntPtr missionPointer);

	[EngineMethod("get_is_loading_finished", false, null, false)]
	bool GetIsLoadingFinished(UIntPtr missionPointer);

	[EngineMethod("clear_scene", false, null, false)]
	void ClearScene(UIntPtr missionPointer);

	[EngineMethod("initialize_mission", false, null, false)]
	void InitializeMission(UIntPtr missionPointer, ref MissionInitializerRecord rec);

	[EngineMethod("finalize_mission", false, null, false)]
	void FinalizeMission(UIntPtr missionPointer);

	[EngineMethod("get_time", false, null, false)]
	float GetTime(UIntPtr missionPointer);

	[EngineMethod("get_average_fps", false, null, false)]
	float GetAverageFps(UIntPtr missionPointer);

	[EngineMethod("get_combat_type", false, null, false)]
	int GetCombatType(UIntPtr missionPointer);

	[EngineMethod("set_combat_type", false, null, false)]
	void SetCombatType(UIntPtr missionPointer, int combatType);

	[EngineMethod("ray_cast_for_closest_agent", false, null, false)]
	Agent RayCastForClosestAgent(UIntPtr missionPointer, Vec3 sourcePoint, Vec3 rayFinishPoint, int excludeAgentIndex, float rayThickness, out float collisionDistance);

	[EngineMethod("ray_cast_for_closest_agents_limbs", false, null, false)]
	Agent RayCastForClosestAgentsLimbs(UIntPtr missionPointer, Vec3 sourcePoint, Vec3 rayFinishPoint, int excludeAgentIndex, float rayThickness, out float collisionDistance, out sbyte boneIndex);

	[EngineMethod("ray_cast_for_given_agents_limbs", false, null, false)]
	bool RayCastForGivenAgentsLimbs(UIntPtr missionPointer, Vec3 sourcePoint, Vec3 rayFinishPoint, int givenAgentIndex, float rayThickness, out float collisionDistance, out sbyte boneIndex);

	[EngineMethod("get_number_of_teams", false, null, false)]
	int GetNumberOfTeams(UIntPtr missionPointer);

	[EngineMethod("reset_teams", false, null, false)]
	void ResetTeams(UIntPtr missionPointer);

	[EngineMethod("add_team", false, null, false)]
	int AddTeam(UIntPtr missionPointer);

	[EngineMethod("restart_record", false, null, false)]
	void RestartRecord(UIntPtr missionPointer);

	[EngineMethod("is_position_inside_boundaries", false, null, false)]
	bool IsPositionInsideBoundaries(UIntPtr missionPointer, Vec2 position);

	[EngineMethod("is_position_inside_hard_boundaries", false, null, false)]
	bool IsPositionInsideHardBoundaries(UIntPtr missionPointer, Vec2 position);

	[EngineMethod("is_position_inside_any_blocker_nav_mesh_face_2d", false, null, false)]
	bool IsPositionInsideAnyBlockerNavMeshFace2D(UIntPtr missionPointer, Vec2 position);

	[EngineMethod("is_position_on_any_blocker_nav_mesh_face", false, null, false)]
	bool IsPositionOnAnyBlockerNavMeshFace(UIntPtr missionPointer, Vec3 position);

	[EngineMethod("get_alternate_position_for_navmeshless_or_out_of_bounds_position", false, null, false)]
	WorldPosition GetAlternatePositionForNavmeshlessOrOutOfBoundsPosition(UIntPtr ptr, ref Vec2 directionTowards, ref WorldPosition originalPosition, ref float positionPenalty);

	[EngineMethod("add_missile", false, null, false)]
	int AddMissile(UIntPtr missionPointer, bool isPrediction, int shooterAgentIndex, in WeaponData weaponData, WeaponStatsData[] weaponStatsData, int weaponStatsDataLength, float damageBonus, ref Vec3 position, ref Vec3 direction, ref Mat3 orientation, float baseSpeed, float speed, bool addRigidBody, UIntPtr entityPointer, int forcedMissileIndex, bool isPrimaryWeaponShot, out UIntPtr missileEntity);

	[EngineMethod("add_missile_single_usage", false, null, false)]
	int AddMissileSingleUsage(UIntPtr missionPointer, bool isPrediction, int shooterAgentIndex, in WeaponData weaponData, in WeaponStatsData weaponStatsData, float damageBonus, ref Vec3 position, ref Vec3 direction, ref Mat3 orientation, float baseSpeed, float speed, bool addRigidBody, UIntPtr entityPointer, int forcedMissileIndex, bool isPrimaryWeaponShot, out UIntPtr missileEntity);

	[EngineMethod("get_missile_collision_point", false, null, false)]
	Vec3 GetMissileCollisionPoint(UIntPtr missionPointer, Vec3 missileStartingPosition, Vec3 missileDirection, float missileStartingSpeed, in WeaponData weaponData);

	[EngineMethod("remove_missile", false, null, false)]
	void RemoveMissile(UIntPtr missionPointer, int missileIndex);

	[EngineMethod("get_missile_vertical_aim_correction", false, null, false)]
	float GetMissileVerticalAimCorrection(Vec3 vecToTarget, float missileStartingSpeed, ref WeaponStatsData weaponStatsData, float airFrictionConstant);

	[EngineMethod("get_missile_range", false, null, false)]
	float GetMissileRange(float missileStartingSpeed, float heightDifference);

	[EngineMethod("compute_exact_missile_range_at_height_difference", false, null, false)]
	float ComputeExactMissileRangeAtHeightDifference(float targetHeightDifference, float initialSpeed, float airFrictionConstant, float maxDuration);

	[EngineMethod("prepare_missile_weapon_for_drop", false, null, false)]
	void PrepareMissileWeaponForDrop(UIntPtr missionPointer, int missileIndex);

	[EngineMethod("add_particle_system_burst_by_name", false, null, false)]
	void AddParticleSystemBurstByName(UIntPtr missionPointer, string particleSystem, ref MatrixFrame frame, bool synchThroughNetwork);

	[EngineMethod("tick", false, null, false)]
	void Tick(UIntPtr missionPointer, float dt);

	[EngineMethod("idle_tick", false, null, false)]
	void IdleTick(UIntPtr missionPointer, float dt);

	[EngineMethod("make_sound", false, null, false)]
	void MakeSound(UIntPtr pointer, int nativeSoundCode, Vec3 position, bool soundCanBePredicted, bool isReliable, int relatedAgent1, int relatedAgent2);

	[EngineMethod("make_sound_with_parameter", false, null, false)]
	void MakeSoundWithParameter(UIntPtr pointer, int nativeSoundCode, Vec3 position, bool soundCanBePredicted, bool isReliable, int relatedAgent1, int relatedAgent2, SoundEventParameter parameter);

	[EngineMethod("make_sound_only_on_related_peer", false, null, false)]
	void MakeSoundOnlyOnRelatedPeer(UIntPtr pointer, int nativeSoundCode, Vec3 position, int relatedAgent);

	[EngineMethod("create_agent", false, null, false)]
	Mission.AgentCreationResult CreateAgent(UIntPtr missionPointer, ulong monsterFlag, int forcedAgentIndex, bool isFemale, ref AgentSpawnData spawnData, ref CapsuleData bodyCapsule, ref CapsuleData crouchedBodyCapsule, ref AnimationSystemData animationSystemData, int instanceNo);

	[EngineMethod("get_position_of_missile", false, null, false)]
	Vec3 GetPositionOfMissile(UIntPtr missionPointer, int index);

	[EngineMethod("get_old_position_of_missile", false, null, false)]
	Vec3 GetOldPositionOfMissile(UIntPtr missionPointer, int index);

	[EngineMethod("get_velocity_of_missile", false, null, false)]
	Vec3 GetVelocityOfMissile(UIntPtr missionPointer, int index);

	[EngineMethod("set_velocity_of_missile", false, null, false)]
	void SetVelocityOfMissile(UIntPtr missionPointer, int index, in Vec3 velocity);

	[EngineMethod("get_missile_has_rigid_body", false, null, false)]
	bool GetMissileHasRigidBody(UIntPtr missionPointer, int index);

	[EngineMethod("add_boundary", false, null, false)]
	bool AddBoundary(UIntPtr missionPointer, string name, Vec2[] boundaryPoints, int boundaryPointCount, bool isAllowanceInside);

	[EngineMethod("remove_boundary", false, null, false)]
	bool RemoveBoundary(UIntPtr missionPointer, string name);

	[EngineMethod("get_boundary_points", false, null, false)]
	void GetBoundaryPoints(UIntPtr missionPointer, string name, int boundaryPointOffset, Vec2[] boundaryPoints, int boundaryPointsSize, ref int retrievedPointCount);

	[EngineMethod("get_boundary_count", false, null, false)]
	int GetBoundaryCount(UIntPtr missionPointer);

	[EngineMethod("get_boundary_radius", false, null, false)]
	float GetBoundaryRadius(UIntPtr missionPointer, string name);

	[EngineMethod("get_boundary_name", false, null, false)]
	string GetBoundaryName(UIntPtr missionPointer, int boundaryIndex);

	[EngineMethod("get_closest_boundary_position", false, null, false)]
	Vec2 GetClosestBoundaryPosition(UIntPtr missionPointer, Vec2 position);

	[EngineMethod("get_navigation_points", false, null, false)]
	bool GetNavigationPoints(UIntPtr missionPointer, ref NavigationData navigationData);

	[EngineMethod("set_navigation_face_cost_with_id_around_position", false, null, false)]
	void SetNavigationFaceCostWithIdAroundPosition(UIntPtr missionPointer, int navigationFaceId, Vec3 position, float cost);

	[EngineMethod("pause_mission_scene_sounds", false, null, false)]
	void PauseMissionSceneSounds(UIntPtr missionPointer);

	[EngineMethod("resume_mission_scene_sounds", false, null, false)]
	void ResumeMissionSceneSounds(UIntPtr missionPointer);

	[EngineMethod("process_record_until_time", false, null, false)]
	void ProcessRecordUntilTime(UIntPtr missionPointer, float time);

	[EngineMethod("end_of_record", false, null, false)]
	bool EndOfRecord(UIntPtr missionPointer);

	[EngineMethod("record_current_state", false, null, false)]
	void RecordCurrentState(UIntPtr missionPointer);

	[EngineMethod("start_recording", false, null, false)]
	void StartRecording();

	[EngineMethod("backup_record_to_file", false, null, false)]
	void BackupRecordToFile(UIntPtr missionPointer, string fileName, string gameType, string sceneLevels);

	[EngineMethod("restore_record_from_file", false, null, false)]
	void RestoreRecordFromFile(UIntPtr missionPointer, string fileName);

	[EngineMethod("clear_record_buffers", false, null, false)]
	void ClearRecordBuffers(UIntPtr missionPointer);

	[EngineMethod("get_scene_name_for_replay", false, null, false)]
	string GetSceneNameForReplay(PlatformFilePath replayName);

	[EngineMethod("get_game_type_for_replay", false, null, false)]
	string GetGameTypeForReplay(PlatformFilePath replayName);

	[EngineMethod("get_scene_levels_for_replay", false, null, false)]
	string GetSceneLevelsForReplay(PlatformFilePath replayName);

	[EngineMethod("get_atmosphere_name_for_replay", false, null, false)]
	string GetAtmosphereNameForReplay(PlatformFilePath replayName);

	[EngineMethod("get_atmosphere_season_for_replay", false, null, false)]
	int GetAtmosphereSeasonForReplay(PlatformFilePath replayName);

	[EngineMethod("get_closest_enemy", false, null, false)]
	Agent GetClosestEnemy(UIntPtr missionPointer, int teamIndex, Vec3 position, float radius);

	[EngineMethod("get_closest_ally", false, null, false)]
	Agent GetClosestAlly(UIntPtr missionPointer, int teamIndex, Vec3 position, float radius);

	[EngineMethod("is_agent_in_proximity_map", false, null, false)]
	bool IsAgentInProximityMap(UIntPtr missionPointer, int agentIndex);

	[EngineMethod("has_any_agents_of_team_around", false, null, false)]
	bool HasAnyAgentsOfTeamAround(UIntPtr missionPointer, Vec3 origin, float radius, int teamNo);

	[EngineMethod("get_agent_count_around_position", false, null, false)]
	void GetAgentCountAroundPosition(UIntPtr missionPointer, int teamIndex, Vec2 position, float radius, ref int allyCount, ref int enemyCount);

	[EngineMethod("find_agent_with_index", false, null, false)]
	Agent FindAgentWithIndex(UIntPtr missionPointer, int index);

	[EngineMethod("set_random_decide_time_of_agents", false, null, false)]
	void SetRandomDecideTimeOfAgents(UIntPtr missionPointer, int agentCount, int[] agentIndices, float minAIReactionTime, float maxAIReactionTime);

	[EngineMethod("get_average_morale_of_agents", false, null, false)]
	float GetAverageMoraleOfAgents(UIntPtr missionPointer, int agentCount, int[] agentIndices);

	[EngineMethod("get_best_slope_towards_direction", false, null, false)]
	WorldPosition GetBestSlopeTowardsDirection(UIntPtr missionPointer, ref WorldPosition centerPosition, float halfsize, ref WorldPosition referencePosition);

	[EngineMethod("get_best_slope_angle_height_pos_for_defending", false, null, false)]
	WorldPosition GetBestSlopeAngleHeightPosForDefending(UIntPtr missionPointer, WorldPosition enemyPosition, WorldPosition defendingPosition, int sampleSize, float distanceRatioAllowedFromDefendedPos, float distanceSqrdAllowedFromBoundary, float cosinusOfBestSlope, float cosinusOfMaxAcceptedSlope, float minSlopeScore, float maxSlopeScore, float excessiveSlopePenalty, float nearConeCenterRatio, float nearConeCenterBonus, float heightDifferenceCeiling, float maxDisplacementPenalty);

	[EngineMethod("get_nearby_agents_aux", false, null, false)]
	void GetNearbyAgentsAux(UIntPtr missionPointer, Vec2 center, float radius, int teamIndex, int friendOrEnemyOrAll, int agentsArrayOffset, ref EngineStackArray.StackArray40Int agentIds, ref int retrievedAgentCount);

	[EngineMethod("get_weighted_point_of_enemies", false, null, false)]
	Vec2 GetWeightedPointOfEnemies(UIntPtr missionPointer, int agentIndex, Vec2 basePoint);

	[EngineMethod("is_formation_unit_position_available", false, null, false)]
	bool IsFormationUnitPositionAvailable(UIntPtr missionPointer, ref WorldPosition orderPosition, ref WorldPosition unitPosition, ref WorldPosition nearestAvailableUnitPosition, float manhattanDistance);

	[EngineMethod("get_straight_path_to_target", false, null, false)]
	WorldPosition GetStraightPathToTarget(UIntPtr scenePointer, Vec2 targetPosition, WorldPosition startingPosition, float samplingDistance, bool stopAtObstacle);

	[EngineMethod("set_bow_missile_speed_modifier", false, null, false)]
	void SetBowMissileSpeedModifier(UIntPtr missionPointer, float modifier);

	[EngineMethod("set_crossbow_missile_speed_modifier", false, null, false)]
	void SetCrossbowMissileSpeedModifier(UIntPtr missionPointer, float modifier);

	[EngineMethod("set_throwing_missile_speed_modifier", false, null, false)]
	void SetThrowingMissileSpeedModifier(UIntPtr missionPointer, float modifier);

	[EngineMethod("set_missile_range_modifier", false, null, false)]
	void SetMissileRangeModifier(UIntPtr missionPointer, float modifier);

	[EngineMethod("set_last_movement_key_pressed", false, null, false)]
	void SetLastMovementKeyPressed(UIntPtr missionPointer, Agent.MovementControlFlag lastMovementKeyPressed);

	[EngineMethod("skip_forward_mission_replay", false, null, false)]
	void SkipForwardMissionReplay(UIntPtr missionPointer, float startTime, float endTime);

	[EngineMethod("get_debug_agent", false, null, false)]
	int GetDebugAgent(UIntPtr missionPointer);

	[EngineMethod("set_debug_agent", false, null, false)]
	void SetDebugAgent(UIntPtr missionPointer, int index);

	[EngineMethod("add_ai_debug_text", false, null, false)]
	void AddAiDebugText(UIntPtr missionPointer, string text);

	[EngineMethod("agent_proximity_map_begin_search", false, null, false)]
	AgentProximityMap.ProximityMapSearchStructInternal ProximityMapBeginSearch(UIntPtr missionPointer, Vec2 searchPos, float searchRadius);

	[EngineMethod("agent_proximity_map_find_next", false, null, false)]
	void ProximityMapFindNext(UIntPtr missionPointer, ref AgentProximityMap.ProximityMapSearchStructInternal searchStruct);

	[EngineMethod("agent_proximity_map_get_max_search_radius", false, null, false)]
	float ProximityMapMaxSearchRadius(UIntPtr missionPointer);

	[EngineMethod("set_override_corpse_count", false, null, false)]
	void SetOverrideCorpseCount(UIntPtr missionPointer, int overrideCorpseCount);

	[EngineMethod("get_biggest_agent_collision_padding", false, null, false)]
	float GetBiggestAgentCollisionPadding(UIntPtr missionPointer);

	[EngineMethod("set_mission_corpse_fade_out_time_in_seconds", false, null, false)]
	void SetMissionCorpseFadeOutTimeInSeconds(UIntPtr missionPointer, float corpseFadeOutTimeInSeconds);

	[EngineMethod("set_report_stuck_agents_mode", false, null, false)]
	void SetReportStuckAgentsMode(UIntPtr missionPointer, bool value);

	[EngineMethod("batch_formation_unit_positions", false, null, false)]
	void BatchFormationUnitPositions(UIntPtr missionPointer, Vec2i[] orderedPositionIndices, Vec2[] orderedLocalPositions, int[] availabilityTable, WorldPosition[] globalPositionTable, WorldPosition orderPosition, Vec2 direction, int fileCount, int rankCount, bool fastCheckWithSameFaceGroupIdDigit);

	[EngineMethod("get_fall_avoid_system_active", false, null, false)]
	bool GetFallAvoidSystemActive(UIntPtr missionPointer);

	[EngineMethod("set_fall_avoid_system_active", false, null, false)]
	void SetFallAvoidSystemActive(UIntPtr missionPointer, bool fallAvoidActive);

	[EngineMethod("get_water_level_at_position", false, null, false)]
	float GetWaterLevelAtPosition(UIntPtr missionPointer, Vec2 position, bool useWaterRenderer);

	[EngineMethod("find_convex_hull", false, null, false)]
	void FindConvexHull(Vec2[] boundaryPoints, int boundaryPointCount, ref int convexPointCount);

	[EngineMethod("on_fast_forward_state_changed", false, null, false)]
	void OnFastForwardStateChanged(UIntPtr missionPointer, bool state);

	[EngineMethod("get_current_volume_generator_version", false, null, false)]
	int GetCurrentVolumeGeneratorVersion();
}
