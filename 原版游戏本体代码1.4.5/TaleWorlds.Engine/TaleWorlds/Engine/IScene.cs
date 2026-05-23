using System;
using TaleWorlds.DotNet;
using TaleWorlds.Library;

namespace TaleWorlds.Engine;

[ApplicationInterfaceBase]
internal interface IScene
{
	[EngineMethod("create_new_scene", false, null, false)]
	Scene CreateNewScene(bool initializePhysics, bool enableDecals = true, int atlasGroup = 0, string sceneName = "mono_renderscene");

	[EngineMethod("has_navmesh_face_unshared_edges", false, null, false)]
	bool HasNavmeshFaceUnsharedEdges(UIntPtr scenePointer, in PathFaceRecord faceRecord);

	[EngineMethod("get_navmesh_face_count_between_two_ids", false, null, false)]
	int GetNavmeshFaceCountBetweenTwoIds(UIntPtr scenePointer, int firstId, int secondId);

	[EngineMethod("get_navmesh_face_records_between_two_ids", false, null, false)]
	void GetNavmeshFaceRecordsBetweenTwoIds(UIntPtr scenePointer, int firstId, int secondId, PathFaceRecord[] faceRecords);

	[EngineMethod("get_path_between_ai_face_pointers", false, null, false)]
	bool GetPathBetweenAIFacePointers(UIntPtr scenePointer, UIntPtr startingAiFace, UIntPtr endingAiFace, Vec2 startingPosition, Vec2 endingPosition, float agentRadius, Vec2[] result, ref int pathSize, int[] exclusionGroupIds, int exclusionGroupIdsCount);

	[EngineMethod("get_path_between_ai_face_pointers_with_region_switch_cost", false, null, false)]
	bool GetPathBetweenAIFacePointersWithRegionSwitchCost(UIntPtr scenePointer, UIntPtr startingAiFace, UIntPtr endingAiFace, Vec2 startingPosition, Vec2 endingPosition, float agentRadius, Vec2[] result, ref int pathSize, int[] exclusionGroupIds, int exclusionGroupIdsCount, int regionSwitchCostTo0, int regionSwitchCostTo1);

	[EngineMethod("get_path_between_ai_face_indices", false, null, false)]
	bool GetPathBetweenAIFaceIndices(UIntPtr scenePointer, int startingAiFace, int endingAiFace, Vec2 startingPosition, Vec2 endingPosition, float agentRadius, Vec2[] result, ref int pathSize, int[] exclusionGroupIds, int exclusionGroupIdsCount, float extraCostMultiplier);

	[EngineMethod("get_path_between_ai_face_indices_with_region_switch_cost", false, null, false)]
	bool GetPathBetweenAIFaceIndicesWithRegionSwitchCost(UIntPtr scenePointer, int startingAiFace, int endingAiFace, Vec2 startingPosition, Vec2 endingPosition, float agentRadius, Vec2[] result, ref int pathSize, int[] exclusionGroupIds, int exclusionGroupIdsCount, float extraCostMultiplier, int regionSwitchCostTo0, int regionSwitchCostTo1);

	[EngineMethod("get_path_distance_between_ai_faces", false, null, false)]
	bool GetPathDistanceBetweenAIFaces(UIntPtr scenePointer, int startingAiFace, int endingAiFace, Vec2 startingPosition, Vec2 endingPosition, float agentRadius, float distanceLimit, out float distance, int[] exclusionGroupIds, int exclusionGroupIdsCount, int regionSwitchCostTo0, int regionSwitchCostTo1);

	[EngineMethod("get_nav_mesh_face_index_with_region", false, null, true)]
	void GetNavMeshFaceIndex(UIntPtr scenePointer, ref PathFaceRecord record, Vec2 position, bool isRegion1, bool checkIfDisabled, bool ignoreHeight);

	[EngineMethod("is_default_editor_scene", false, null, false)]
	bool IsDefaultEditorScene(Scene scene);

	[EngineMethod("is_multiplayer_scene", false, null, false)]
	bool IsMultiplayerScene(Scene scene);

	[EngineMethod("take_photo_mode_picture", false, null, false)]
	string TakePhotoModePicture(Scene scene, bool saveAmbientOcclusionPass, bool saveObjectIdPass, bool saveShadowPass);

	[EngineMethod("get_all_color_grade_names", false, null, false)]
	string GetAllColorGradeNames(Scene scene);

	[EngineMethod("get_all_filter_names", false, null, false)]
	string GetAllFilterNames(Scene scene);

	[EngineMethod("get_photo_mode_roll", false, null, false)]
	float GetPhotoModeRoll(Scene scene);

	[EngineMethod("get_photo_mode_fov", false, null, false)]
	float GetPhotoModeFov(Scene scene);

	[EngineMethod("has_decal_renderer", false, null, true)]
	bool HasDecalRenderer(UIntPtr scenePointer);

	[EngineMethod("get_photo_mode_orbit", false, null, false)]
	bool GetPhotoModeOrbit(Scene scene);

	[EngineMethod("get_photo_mode_on", false, null, false)]
	bool GetPhotoModeOn(Scene scene);

	[EngineMethod("get_photo_mode_focus", false, null, false)]
	void GetPhotoModeFocus(Scene scene, ref float focus, ref float focusStart, ref float focusEnd, ref float exposure, ref bool vignetteOn);

	[EngineMethod("get_scene_color_grade_index", false, null, false)]
	int GetSceneColorGradeIndex(Scene scene);

	[EngineMethod("get_scene_filter_index", false, null, false)]
	int GetSceneFilterIndex(Scene scene);

	[EngineMethod("enable_fixed_tick", false, null, false)]
	void EnableFixedTick(Scene scene);

	[EngineMethod("get_loading_state_name", false, null, false)]
	string GetLoadingStateName(Scene scene);

	[EngineMethod("is_loading_finished", false, null, false)]
	bool IsLoadingFinished(Scene scene);

	[EngineMethod("set_photo_mode_roll", false, null, false)]
	void SetPhotoModeRoll(Scene scene, float roll);

	[EngineMethod("set_photo_mode_fov", false, null, false)]
	void SetPhotoModeFov(Scene scene, float verticalFov);

	[EngineMethod("set_photo_mode_orbit", false, null, false)]
	void SetPhotoModeOrbit(Scene scene, bool orbit);

	[EngineMethod("get_fall_density", false, null, false)]
	float GetFallDensity(UIntPtr scenepTR);

	[EngineMethod("set_photo_mode_on", false, null, false)]
	void SetPhotoModeOn(Scene scene, bool on);

	[EngineMethod("set_photo_mode_focus", false, null, false)]
	void SetPhotoModeFocus(Scene scene, float focusStart, float focusEnd, float focus, float exposure);

	[EngineMethod("set_photo_mode_vignette", false, null, false)]
	void SetPhotoModeVignette(Scene scene, bool vignetteOn);

	[EngineMethod("set_scene_color_grade_index", false, null, false)]
	void SetSceneColorGradeIndex(Scene scene, int index);

	[EngineMethod("set_scene_filter_index", false, null, false)]
	int SetSceneFilterIndex(Scene scene, int index);

	[EngineMethod("set_scene_color_grade", false, null, false)]
	void SetSceneColorGrade(Scene scene, string textureName);

	[EngineMethod("get_water_level", false, null, false)]
	float GetWaterLevel(Scene scene);

	[EngineMethod("get_water_level_at_position", false, null, false)]
	float GetWaterLevelAtPosition(Scene scene, Vec2 position, bool useWaterRenderer, bool checkWaterBodyEntities);

	[EngineMethod("get_water_speed_at_position", false, null, true)]
	Vec3 GetWaterSpeedAtPosition(UIntPtr scenePointer, in Vec2 position, bool doChoppinessCorrection);

	[EngineMethod("set_fixed_tick_callback_active", false, null, false)]
	void SetFixedTickCallbackActive(Scene scene, bool isActive);

	[EngineMethod("set_on_collision_filter_callback_active", false, null, false)]
	void SetOnCollisionFilterCallbackActive(Scene scene, bool isActive);

	[EngineMethod("get_interpolation_factor_for_body_world_transform_smoothing", false, null, false)]
	void GetInterpolationFactorForBodyWorldTransformSmoothing(Scene scene, out float interpolationFactor, out float fixedDt);

	[EngineMethod("get_bulk_water_level_at_positions", false, null, false)]
	void GetBulkWaterLevelAtPositions(Scene scene, Vec2[] waterHeightQueryArray, int arraySize, float[] waterHeightsAtVolumes, Vec3[] waterSurfaceNormals);

	[EngineMethod("get_bulk_water_level_at_volumes", false, null, true)]
	void GetBulkWaterLevelAtVolumes(UIntPtr scene, UIntPtr volumeQueryDataArray, int volumeCount, in MatrixFrame entityFrame);

	[EngineMethod("get_water_strength", false, null, false)]
	float GetWaterStrength(Scene scene);

	[EngineMethod("register_ship_visual_to_water_renderer", false, null, false)]
	UIntPtr RegisterShipVisualToWaterRenderer(UIntPtr scenePointer, UIntPtr entityPointer, in Vec3 bb);

	[EngineMethod("deregister_ship_visual", false, null, false)]
	void DeRegisterShipVisual(UIntPtr scenePointer, UIntPtr visualPointer);

	[EngineMethod("set_water_strength", false, null, false)]
	void SetWaterStrength(Scene scene, float newWaterStrength);

	[EngineMethod("add_water_wake_with_capsule", false, null, false)]
	void AddWaterWakeWithCapsule(Scene scene, Vec3 positionA, float radiusA, Vec3 positionB, float radiusB, float wakeVisibility, float foamVisibility);

	[EngineMethod("get_terrain_material_index_at_layer", false, null, false)]
	int GetTerrainPhysicsMaterialIndexAtLayer(Scene scene, int layerIndex);

	[EngineMethod("create_burst_particle", false, null, false)]
	void CreateBurstParticle(Scene scene, int particleId, ref MatrixFrame frame);

	[EngineMethod("get_nav_mesh_face_index3", false, null, false)]
	void GetNavMeshFaceIndex3(UIntPtr scenePointer, ref PathFaceRecord record, Vec3 position, bool checkIfDisabled);

	[EngineMethod("set_upgrade_level", false, null, false)]
	void SetUpgradeLevel(UIntPtr scenePointer, int level);

	[EngineMethod("create_path_mesh", false, null, false)]
	MetaMesh CreatePathMesh(UIntPtr scenePointer, string baseEntityName, bool isWaterPath);

	[EngineMethod("set_active_visibility_levels", false, null, false)]
	void SetActiveVisibilityLevels(UIntPtr scenePointer, string levelsAppended);

	[EngineMethod("set_terrain_dynamic_params", false, null, false)]
	void SetTerrainDynamicParams(UIntPtr scenePointer, Vec3 dynamic_params);

	[EngineMethod("set_do_not_wait_for_loading_states_to_render", false, null, false)]
	void SetDoNotWaitForLoadingStatesToRender(UIntPtr scenePointer, bool value);

	[EngineMethod("set_dynamic_snow_texture", false, null, false)]
	void SetDynamicSnowTexture(UIntPtr scenePointer, UIntPtr texturePointer);

	[EngineMethod("get_flowmap_data", false, null, false)]
	void GetWindFlowMapData(UIntPtr scenePointer, float[] flowmapData);

	[EngineMethod("create_path_mesh2", false, null, false)]
	MetaMesh CreatePathMesh2(UIntPtr scenePointer, UIntPtr[] pathNodes, int pathNodeCount, bool isWaterPath);

	[EngineMethod("clear_all", false, null, false)]
	void ClearAll(UIntPtr scenePointer);

	[EngineMethod("check_resources", false, null, false)]
	void CheckResources(UIntPtr scenePointer, bool checkInvisibleEntities);

	[EngineMethod("force_load_resources", false, null, false)]
	void ForceLoadResources(UIntPtr scenePointer, bool checkInvisibleEntities);

	[EngineMethod("check_path_entities_frame_changed", false, null, false)]
	bool CheckPathEntitiesFrameChanged(UIntPtr scenePointer, string containsName);

	[EngineMethod("tick", false, null, false)]
	void Tick(UIntPtr scenePointer, float deltaTime);

	[EngineMethod("add_entity_with_mesh", false, null, false)]
	void AddEntityWithMesh(UIntPtr scenePointer, UIntPtr meshPointer, ref MatrixFrame frame);

	[EngineMethod("add_entity_with_multi_mesh", false, null, false)]
	void AddEntityWithMultiMesh(UIntPtr scenePointer, UIntPtr multiMeshPointer, ref MatrixFrame frame);

	[EngineMethod("add_item_entity", false, null, false)]
	GameEntity AddItemEntity(UIntPtr scenePointer, ref MatrixFrame frame, UIntPtr meshPointer);

	[EngineMethod("remove_entity", false, null, false)]
	void RemoveEntity(UIntPtr scenePointer, UIntPtr entityId, int removeReason);

	[EngineMethod("attach_entity", false, null, false)]
	bool AttachEntity(UIntPtr scenePointer, UIntPtr entity, bool showWarnings);

	[EngineMethod("get_terrain_height_and_normal", false, null, false)]
	void GetTerrainHeightAndNormal(UIntPtr scenePointer, Vec2 position, out float height, out Vec3 normal);

	[EngineMethod("resume_loading_renderings", false, null, false)]
	void ResumeLoadingRenderings(UIntPtr scenePointer);

	[EngineMethod("get_upgrade_level_mask", false, null, false)]
	uint GetUpgradeLevelMask(UIntPtr scenePointer);

	[EngineMethod("set_upgrade_level_visibility", false, null, false)]
	void SetUpgradeLevelVisibility(UIntPtr scenePointer, string concatLevels);

	[EngineMethod("set_upgrade_level_visibility_with_mask", false, null, false)]
	void SetUpgradeLevelVisibilityWithMask(UIntPtr scenePointer, uint mask);

	[EngineMethod("stall_loading_renderings", false, null, false)]
	void StallLoadingRenderingsUntilFurtherNotice(UIntPtr scenePointer);

	[EngineMethod("get_flora_instance_count", false, null, false)]
	int GetFloraInstanceCount(UIntPtr scenePointer);

	[EngineMethod("get_flora_renderer_texture_usage", false, null, false)]
	int GetFloraRendererTextureUsage(UIntPtr scenePointer);

	[EngineMethod("get_terrain_memory_usage", false, null, false)]
	int GetTerrainMemoryUsage(UIntPtr scenePointer);

	[EngineMethod("set_fetch_crc_info_of_scene", false, null, false)]
	void SetFetchCrcInfoOfScene(UIntPtr scenePointer, bool value);

	[EngineMethod("get_scene_xml_crc", false, null, false)]
	uint GetSceneXMLCRC(UIntPtr scenePointer);

	[EngineMethod("get_navigation_mesh_crc", false, null, false)]
	uint GetNavigationMeshCRC(UIntPtr scenePointer);

	[EngineMethod("get_global_wind_strength_vector", false, null, false)]
	Vec2 GetGlobalWindStrengthVector(UIntPtr scenePointer);

	[EngineMethod("set_global_wind_strength_vector", false, null, false)]
	void SetGlobalWindStrengthVector(UIntPtr scenePointer, in Vec2 strengthVector);

	[EngineMethod("get_global_wind_velocity", false, null, true)]
	Vec2 GetGlobalWindVelocity(UIntPtr scenePointer);

	[EngineMethod("set_global_wind_velocity", false, null, false)]
	void SetGlobalWindVelocity(UIntPtr scenePointer, in Vec2 windVelocity);

	[EngineMethod("get_engine_physics_enabled", false, null, false)]
	bool GetEnginePhysicsEnabled(UIntPtr scenePointer);

	[EngineMethod("clear_nav_mesh", false, null, false)]
	void ClearNavMesh(UIntPtr scenePointer);

	[EngineMethod("get_nav_mesh_face_count", false, null, false)]
	int GetNavMeshFaceCount(UIntPtr scenePointer);

	[EngineMethod("get_nav_mesh_face_center_position", false, null, true)]
	void GetNavMeshFaceCenterPosition(UIntPtr scenePointer, int navMeshFace, ref Vec3 centerPos);

	[EngineMethod("get_nav_mesh_path_face_record", false, null, false)]
	PathFaceRecord GetNavMeshPathFaceRecord(UIntPtr scenePointer, int navMeshFace);

	[EngineMethod("get_path_face_record_from_nav_mesh_face_pointer", false, null, false)]
	PathFaceRecord GetPathFaceRecordFromNavMeshFacePointer(UIntPtr scenePointer, UIntPtr navMeshFacePointer);

	[EngineMethod("get_all_nav_mesh_face_records", false, null, false)]
	void GetAllNavmeshFaceRecords(UIntPtr scenePointer, PathFaceRecord[] faceRecords);

	[EngineMethod("get_id_of_nav_mesh_face", false, null, false)]
	int GetIdOfNavMeshFace(UIntPtr scenePointer, int navMeshFace);

	[EngineMethod("set_cloth_simulation_state", false, null, false)]
	void SetClothSimulationState(UIntPtr scenePointer, bool state);

	[EngineMethod("get_first_entity_with_name", false, null, false)]
	GameEntity GetFirstEntityWithName(UIntPtr scenePointer, string entityName);

	[EngineMethod("get_campaign_entity_with_name", false, null, false)]
	GameEntity GetCampaignEntityWithName(UIntPtr scenePointer, string entityName);

	[EngineMethod("get_all_entities_with_script_component", false, null, false)]
	void GetAllEntitiesWithScriptComponent(UIntPtr scenePointer, string scriptComponentName, UIntPtr output);

	[EngineMethod("get_first_entity_with_script_component", false, null, false)]
	GameEntity GetFirstEntityWithScriptComponent(UIntPtr scenePointer, string scriptComponentName);

	[EngineMethod("get_upgrade_level_mask_of_level_name", false, null, false)]
	uint GetUpgradeLevelMaskOfLevelName(UIntPtr scenePointer, string levelName);

	[EngineMethod("get_level_name_of_level_index", false, null, false)]
	string GetUpgradeLevelNameOfIndex(UIntPtr scenePointer, int index);

	[EngineMethod("get_upgrade_level_count", false, null, false)]
	int GetUpgradeLevelCount(UIntPtr scenePointer);

	[EngineMethod("get_winter_time_factor", false, null, false)]
	float GetWinterTimeFactor(UIntPtr scenePointer);

	[EngineMethod("get_nav_mesh_face_first_vertex_z", false, null, false)]
	float GetNavMeshFaceFirstVertexZ(UIntPtr scenePointer, int navMeshFaceIndex);

	[EngineMethod("set_winter_time_factor", false, null, false)]
	void SetWinterTimeFactor(UIntPtr scenePointer, float winterTimeFactor);

	[EngineMethod("set_dryness_factor", false, null, false)]
	void SetDrynessFactor(UIntPtr scenePointer, float drynessFactor);

	[EngineMethod("get_fog", false, null, false)]
	float GetFog(UIntPtr scenePointer);

	[EngineMethod("set_fog", false, null, false)]
	void SetFog(UIntPtr scenePointer, float fogDensity, ref Vec3 fogColor, float fogFalloff);

	[EngineMethod("set_fog_advanced", false, null, false)]
	void SetFogAdvanced(UIntPtr scenePointer, float fogFalloffOffset, float fogFalloffMinFog, float fogFalloffStartDist);

	[EngineMethod("set_fog_ambient_color", false, null, false)]
	void SetFogAmbientColor(UIntPtr scenePointer, ref Vec3 fogAmbientColor);

	[EngineMethod("set_temperature", false, null, false)]
	void SetTemperature(UIntPtr scenePointer, float temperature);

	[EngineMethod("set_humidity", false, null, false)]
	void SetHumidity(UIntPtr scenePointer, float humidity);

	[EngineMethod("set_dynamic_shadowmap_cascades_radius_multiplier", false, null, false)]
	void SetDynamicShadowmapCascadesRadiusMultiplier(UIntPtr scenePointer, float extraRadius);

	[EngineMethod("set_env_map_multiplier", false, null, false)]
	void SetEnvironmentMultiplier(UIntPtr scenePointer, bool useMultiplier, float multiplier);

	[EngineMethod("set_sky_rotation", false, null, false)]
	void SetSkyRotation(UIntPtr scenePointer, float rotation);

	[EngineMethod("set_sky_brightness", false, null, false)]
	void SetSkyBrightness(UIntPtr scenePointer, float brightness);

	[EngineMethod("set_forced_snow", false, null, false)]
	void SetForcedSnow(UIntPtr scenePointer, bool value);

	[EngineMethod("set_sun", false, null, false)]
	void SetSun(UIntPtr scenePointer, Vec3 color, float altitude, float angle, float intensity);

	[EngineMethod("set_sun_angle_altitude", false, null, false)]
	void SetSunAngleAltitude(UIntPtr scenePointer, float angle, float altitude);

	[EngineMethod("set_sun_light", false, null, false)]
	void SetSunLight(UIntPtr scenePointer, Vec3 color, Vec3 direction);

	[EngineMethod("set_sun_direction", false, null, false)]
	void SetSunDirection(UIntPtr scenePointer, Vec3 direction);

	[EngineMethod("set_sun_size", false, null, false)]
	void SetSunSize(UIntPtr scenePointer, float size);

	[EngineMethod("set_sunshafts_strength", false, null, false)]
	void SetSunShaftStrength(UIntPtr scenePointer, float strength);

	[EngineMethod("get_rain_density", false, null, false)]
	float GetRainDensity(UIntPtr scenePointer);

	[EngineMethod("set_rain_density", false, null, false)]
	void SetRainDensity(UIntPtr scenePointer, float density);

	[EngineMethod("get_snow_density", false, null, false)]
	float GetSnowDensity(UIntPtr scenePointer);

	[EngineMethod("set_snow_density", false, null, false)]
	void SetSnowDensity(UIntPtr scenePointer, float density);

	[EngineMethod("add_decal_instance", false, null, false)]
	void AddDecalInstance(UIntPtr scenePointer, UIntPtr decalMeshPointer, string decalSetID, bool deletable);

	[EngineMethod("remove_decal_instance", false, null, false)]
	void RemoveDecalInstance(UIntPtr scenePointer, UIntPtr decalMeshPointer, string decalSetID);

	[EngineMethod("set_shadow", false, null, false)]
	void SetShadow(UIntPtr scenePointer, bool shadowEnabled);

	[EngineMethod("add_point_light", false, null, false)]
	int AddPointLight(UIntPtr scenePointer, Vec3 position, float radius);

	[EngineMethod("add_directional_light", false, null, false)]
	int AddDirectionalLight(UIntPtr scenePointer, Vec3 position, Vec3 direction, float radius);

	[EngineMethod("set_light_position", false, null, false)]
	void SetLightPosition(UIntPtr scenePointer, int lightIndex, Vec3 position);

	[EngineMethod("set_light_diffuse_color", false, null, false)]
	void SetLightDiffuseColor(UIntPtr scenePointer, int lightIndex, Vec3 diffuseColor);

	[EngineMethod("set_light_direction", false, null, false)]
	void SetLightDirection(UIntPtr scenePointer, int lightIndex, Vec3 direction);

	[EngineMethod("calculate_effective_lighting", false, null, false)]
	bool CalculateEffectiveLighting(UIntPtr scenePointer);

	[EngineMethod("set_rayleigh_constant", false, null, false)]
	void SetMieScatterStrength(UIntPtr scenePointer, float strength);

	[EngineMethod("set_mie_scatter_particle_size", false, null, false)]
	void SetMieScatterFocus(UIntPtr scenePointer, float strength);

	[EngineMethod("set_brightpass_threshold", false, null, false)]
	void SetBrightpassTreshold(UIntPtr scenePointer, float threshold);

	[EngineMethod("set_min_exposure", false, null, false)]
	void SetMinExposure(UIntPtr scenePointer, float minExposure);

	[EngineMethod("set_max_exposure", false, null, false)]
	void SetMaxExposure(UIntPtr scenePointer, float maxExposure);

	[EngineMethod("set_target_exposure", false, null, false)]
	void SetTargetExposure(UIntPtr scenePointer, float targetExposure);

	[EngineMethod("set_middle_gray", false, null, false)]
	void SetMiddleGray(UIntPtr scenePointer, float middleGray);

	[EngineMethod("set_bloom_strength", false, null, false)]
	void SetBloomStrength(UIntPtr scenePointer, float bloomStrength);

	[EngineMethod("set_bloom_amount", false, null, false)]
	void SetBloomAmount(UIntPtr scenePointer, float bloomAmount);

	[EngineMethod("set_grain_amount", false, null, false)]
	void SetGrainAmount(UIntPtr scenePointer, float grainAmount);

	[EngineMethod("set_lens_flare_amount", false, null, false)]
	void SetLensFlareAmount(UIntPtr scenePointer, float lensFlareAmount);

	[EngineMethod("set_lens_flare_threshold", false, null, false)]
	void SetLensFlareThreshold(UIntPtr scenePointer, float lensFlareThreshold);

	[EngineMethod("set_lens_flare_strength", false, null, false)]
	void SetLensFlareStrength(UIntPtr scenePointer, float lensFlareStrength);

	[EngineMethod("set_lens_flare_dirt_weight", false, null, false)]
	void SetLensFlareDirtWeight(UIntPtr scenePointer, float lensFlareDirtWeight);

	[EngineMethod("set_lens_flare_diffraction_weight", false, null, false)]
	void SetLensFlareDiffractionWeight(UIntPtr scenePointer, float lensFlareDiffractionWeight);

	[EngineMethod("set_lens_flare_halo_weight", false, null, false)]
	void SetLensFlareHaloWeight(UIntPtr scenePointer, float lensFlareHaloWeight);

	[EngineMethod("set_lens_flare_ghost_weight", false, null, false)]
	void SetLensFlareGhostWeight(UIntPtr scenePointer, float lensFlareGhostWeight);

	[EngineMethod("set_lens_flare_halo_width", false, null, false)]
	void SetLensFlareHaloWidth(UIntPtr scenePointer, float lensFlareHaloWidth);

	[EngineMethod("set_lens_flare_ghost_samples", false, null, false)]
	void SetLensFlareGhostSamples(UIntPtr scenePointer, int lensFlareGhostSamples);

	[EngineMethod("set_lens_flare_aberration_offset", false, null, false)]
	void SetLensFlareAberrationOffset(UIntPtr scenePointer, float lensFlareAberrationOffset);

	[EngineMethod("set_lens_flare_blur_size", false, null, false)]
	void SetLensFlareBlurSize(UIntPtr scenePointer, int lensFlareBlurSize);

	[EngineMethod("set_lens_flare_blur_sigma", false, null, false)]
	void SetLensFlareBlurSigma(UIntPtr scenePointer, float lensFlareBlurSigma);

	[EngineMethod("set_streak_amount", false, null, false)]
	void SetStreakAmount(UIntPtr scenePointer, float streakAmount);

	[EngineMethod("set_streak_threshold", false, null, false)]
	void SetStreakThreshold(UIntPtr scenePointer, float streakThreshold);

	[EngineMethod("set_streak_strength", false, null, false)]
	void SetStreakStrength(UIntPtr scenePointer, float strengthAmount);

	[EngineMethod("set_streak_stretch", false, null, false)]
	void SetStreakStretch(UIntPtr scenePointer, float stretchAmount);

	[EngineMethod("set_streak_intensity", false, null, false)]
	void SetStreakIntensity(UIntPtr scenePointer, float stretchAmount);

	[EngineMethod("set_streak_tint", false, null, false)]
	void SetStreakTint(UIntPtr scenePointer, ref Vec3 p_streak_tint_color);

	[EngineMethod("set_hexagon_vignette_color", false, null, false)]
	void SetHexagonVignetteColor(UIntPtr scenePointer, ref Vec3 p_hexagon_vignette_color);

	[EngineMethod("set_hexagon_vignette_alpha", false, null, false)]
	void SetHexagonVignetteAlpha(UIntPtr scenePointer, float Alpha);

	[EngineMethod("set_vignette_inner_radius", false, null, false)]
	void SetVignetteInnerRadius(UIntPtr scenePointer, float vignetteInnerRadius);

	[EngineMethod("set_vignette_outer_radius", false, null, false)]
	void SetVignetteOuterRadius(UIntPtr scenePointer, float vignetteOuterRadius);

	[EngineMethod("set_vignette_opacity", false, null, false)]
	void SetVignetteOpacity(UIntPtr scenePointer, float vignetteOpacity);

	[EngineMethod("set_aberration_offset", false, null, false)]
	void SetAberrationOffset(UIntPtr scenePointer, float aberrationOffset);

	[EngineMethod("set_aberration_size", false, null, false)]
	void SetAberrationSize(UIntPtr scenePointer, float aberrationSize);

	[EngineMethod("set_aberration_smooth", false, null, false)]
	void SetAberrationSmooth(UIntPtr scenePointer, float aberrationSmooth);

	[EngineMethod("set_lens_distortion", false, null, false)]
	void SetLensDistortion(UIntPtr scenePointer, float lensDistortion);

	[EngineMethod("get_height_at_point", false, null, false)]
	bool GetHeightAtPoint(UIntPtr scenePointer, Vec2 point, BodyFlags excludeBodyFlags, ref float height);

	[EngineMethod("get_entity_count", false, null, false)]
	int GetEntityCount(UIntPtr scenePointer);

	[EngineMethod("get_entities", false, null, false)]
	void GetEntities(UIntPtr scenePointer, UIntPtr entityObjectsArrayPointer);

	[EngineMethod("get_root_entity_count", false, null, false)]
	int GetRootEntityCount(UIntPtr scenePointer);

	[EngineMethod("get_root_entities", false, null, false)]
	void GetRootEntities(Scene scene, NativeObjectArray output);

	[EngineMethod("get_entity_with_guid", false, null, false)]
	GameEntity GetEntityWithGuid(UIntPtr scenePointer, string guid);

	[EngineMethod("select_entities_in_box_with_script_component", false, null, false)]
	int SelectEntitiesInBoxWithScriptComponent(UIntPtr scenePointer, ref Vec3 boundingBoxMin, ref Vec3 boundingBoxMax, UIntPtr[] entitiesOutput, int maxCount, string scriptComponentName, bool isFixedTick);

	[EngineMethod("ray_cast_excluding_two_entities", false, null, false)]
	bool RayCastExcludingTwoEntities(BodyFlags flags, UIntPtr scenePointer, in Ray ray, UIntPtr entity1, UIntPtr entity2);

	[EngineMethod("select_entities_collided_with", false, null, false)]
	int SelectEntitiesCollidedWith(UIntPtr scenePointer, ref Ray ray, UIntPtr[] entityIds, Intersection[] intersections);

	[EngineMethod("generate_contacts_with_capsule", false, null, false)]
	int GenerateContactsWithCapsule(UIntPtr scenePointer, ref CapsuleData cap, BodyFlags excludeFlags, bool isFixedTick, Intersection[] intersections, UIntPtr[] entityIds);

	[EngineMethod("generate_contacts_with_capsule_against_entity", false, null, false)]
	int GenerateContactsWithCapsuleAgainstEntity(UIntPtr scenePointer, ref CapsuleData cap, BodyFlags excludeFlags, UIntPtr entityId, Intersection[] intersections);

	[EngineMethod("invalidate_terrain_physics_materials", false, null, false)]
	void InvalidateTerrainPhysicsMaterials(UIntPtr scenePointer);

	[EngineMethod("read", false, null, false)]
	void Read(UIntPtr scenePointer, string sceneName, ref SceneInitializationData initData, string forcedAtmoName);

	[EngineMethod("read_in_module", false, null, false)]
	void ReadInModule(UIntPtr scenePointer, string sceneName, string moduleId, ref SceneInitializationData initData, string forcedAtmoName);

	[EngineMethod("read_and_calculate_initial_camera", false, null, false)]
	void ReadAndCalculateInitialCamera(UIntPtr scenePointer, ref MatrixFrame outFrame);

	[EngineMethod("optimize_scene", false, null, false)]
	void OptimizeScene(UIntPtr scenePointer, bool optimizeFlora, bool optimizeOro);

	[EngineMethod("get_terrain_height", false, null, false)]
	float GetTerrainHeight(UIntPtr scenePointer, Vec2 position, bool checkHoles);

	[EngineMethod("get_normal_at", false, null, false)]
	Vec3 GetNormalAt(UIntPtr scenePointer, Vec2 position);

	[EngineMethod("has_terrain_heightmap", false, null, false)]
	bool HasTerrainHeightmap(UIntPtr scenePointer);

	[EngineMethod("contains_terrain", false, null, false)]
	bool ContainsTerrain(UIntPtr scenePointer);

	[EngineMethod("set_dof_focus", false, null, false)]
	void SetDofFocus(UIntPtr scenePointer, float dofFocus);

	[EngineMethod("set_dof_params", false, null, false)]
	void SetDofParams(UIntPtr scenePointer, float dofFocusStart, float dofFocusEnd, bool isVignetteOn);

	[EngineMethod("get_last_final_render_camera_position", false, null, false)]
	Vec3 GetLastFinalRenderCameraPosition(UIntPtr scenePointer);

	[EngineMethod("get_last_final_render_camera_frame", false, null, false)]
	void GetLastFinalRenderCameraFrame(UIntPtr scenePointer, ref MatrixFrame outFrame);

	[EngineMethod("get_time_of_day", false, null, false)]
	float GetTimeOfDay(UIntPtr scenePointer);

	[EngineMethod("set_time_of_day", false, null, false)]
	void SetTimeOfDay(UIntPtr scenePointer, float value);

	[EngineMethod("is_atmosphere_indoor", false, null, false)]
	bool IsAtmosphereIndoor(UIntPtr scenePointer);

	[EngineMethod("set_color_grade_blend", false, null, false)]
	void SetColorGradeBlend(UIntPtr scenePointer, string texture1, string texture2, float alpha);

	[EngineMethod("preload_for_rendering", false, null, false)]
	void PreloadForRendering(UIntPtr scenePointer);

	[EngineMethod("create_dynamic_rain_texture", false, null, false)]
	void CreateDynamicRainTexture(UIntPtr scenePointer, int w, int h);

	[EngineMethod("resume_scene_sounds", false, null, false)]
	void ResumeSceneSounds(UIntPtr scenePointer);

	[EngineMethod("finish_scene_sounds", false, null, false)]
	void FinishSceneSounds(UIntPtr scenePointer);

	[EngineMethod("pause_scene_sounds", false, null, false)]
	void PauseSceneSounds(UIntPtr scenePointer);

	[EngineMethod("get_ground_height_at_position", false, null, false)]
	float GetGroundHeightAtPosition(UIntPtr scenePointer, Vec3 position, uint excludeFlags);

	[EngineMethod("get_ground_height_and_normal_at_position", false, null, false)]
	float GetGroundHeightAndNormalAtPosition(UIntPtr scenePointer, Vec3 position, ref Vec3 normal, uint excludeFlags);

	[EngineMethod("get_ground_height_and_body_flags_at_position", false, null, false)]
	float GetGroundHeightAndBodyFlagsAtPosition(UIntPtr scenePointer, Vec3 position, out BodyFlags contactPointFlags, BodyFlags excludeFlags);

	[EngineMethod("check_point_can_see_point", false, null, false)]
	bool CheckPointCanSeePoint(UIntPtr scenePointer, Vec3 sourcePoint, Vec3 targetPoint, float distanceToCheck);

	[EngineMethod("ray_cast_for_closest_entity_or_terrain", false, null, false)]
	bool RayCastForClosestEntityOrTerrain(UIntPtr scenePointer, in Vec3 sourcePoint, in Vec3 targetPoint, float rayThickness, ref float collisionDistance, ref Vec3 closestPoint, ref UIntPtr entityIndex, BodyFlags bodyExcludeFlags, bool isFixedWorld);

	[EngineMethod("focus_ray_cast_for_fixed_physics", false, null, false)]
	bool FocusRayCastForFixedPhysics(UIntPtr scenePointer, in Vec3 sourcePoint, in Vec3 targetPoint, float rayThickness, ref float collisionDistance, ref Vec3 closestPoint, ref UIntPtr entityIndex, BodyFlags bodyExcludeFlags, bool isFixedWorld);

	[EngineMethod("ray_cast_for_ramming", false, null, false)]
	bool RayCastForRamming(UIntPtr scenePointer, in Vec3 sourcePoint, in Vec3 targetPoint, float rayThickness, ref float collisionDistance, ref Vec3 intersectionPoint, ref UIntPtr intersectedEntityIndex, BodyFlags bodyExcludeFlags, BodyFlags bodyIncludeFlags, UIntPtr ignoredEntityPointer);

	[EngineMethod("ray_cast_for_closest_entity_or_terrain_ignore_entity", false, null, false)]
	bool RayCastForClosestEntityOrTerrainIgnoreEntity(UIntPtr scenePointer, in Vec3 sourcePoint, in Vec3 targetPoint, float rayThickness, ref float collisionDistance, ref Vec3 closestPoint, ref UIntPtr entityIndex, BodyFlags bodyExcludeFlags, UIntPtr ignoredEntityPointer);

	[EngineMethod("box_cast_only_for_camera", false, null, false)]
	bool BoxCastOnlyForCamera(UIntPtr scenePointer, Vec3[] boxPoints, in Vec3 centerPoint, in Vec3 dir, float distance, UIntPtr ignoredEntityPointer, ref float collisionDistance, ref Vec3 closestPoint, ref UIntPtr entityPointer, BodyFlags bodyExcludeFlags);

	[EngineMethod("mark_faces_with_id_as_ladder", false, null, false)]
	void MarkFacesWithIdAsLadder(UIntPtr scenePointer, int faceGroupId, bool isLadder);

	[EngineMethod("box_cast", false, null, false)]
	bool BoxCast(UIntPtr scenePointer, ref Vec3 boxPointBegin, ref Vec3 boxPointEnd, ref Vec3 dir, float distance, ref float collisionDistance, ref Vec3 closestPoint, ref UIntPtr entityIndex, BodyFlags bodyExcludeFlags);

	[EngineMethod("set_ability_of_faces_with_id", false, null, false)]
	int SetAbilityOfFacesWithId(UIntPtr scenePointer, int faceGroupId, bool isEnabled);

	[EngineMethod("set_blocker_direction_for_faces_with_id", false, null, false)]
	void SetBlockerDirectionForFacesWithId(UIntPtr scenePointer, int faceGroupId, float rotation);

	[EngineMethod("swap_face_connections_with_id", false, null, false)]
	bool SwapFaceConnectionsWithId(UIntPtr scenePointer, int hubFaceGroupID, int toBeSeparatedFaceGroupId, int toBeMergedFaceGroupId, bool canFail);

	[EngineMethod("merge_faces_with_id", false, null, false)]
	void MergeFacesWithId(UIntPtr scenePointer, int faceGroupId0, int faceGroupId1, int newFaceGroupId);

	[EngineMethod("separate_faces_with_id", false, null, false)]
	void SeparateFacesWithId(UIntPtr scenePointer, int faceGroupId0, int faceGroupId1);

	[EngineMethod("is_any_face_with_id", false, null, false)]
	bool IsAnyFaceWithId(UIntPtr scenePointer, int faceGroupId);

	[EngineMethod("load_nav_mesh_prefab", false, null, false)]
	void LoadNavMeshPrefab(UIntPtr scenePointer, string navMeshPrefabName, int navMeshGroupIdShift);

	[EngineMethod("load_nav_mesh_prefab_with_frame", false, null, false)]
	void LoadNavMeshPrefabWithFrame(UIntPtr scenePointer, string navMeshPrefabName, MatrixFrame frame);

	[EngineMethod("save_nav_mesh_prefab_with_frame", false, null, false)]
	void SaveNavMeshPrefabWithFrame(UIntPtr scenePointer, string navMeshPrefabName, MatrixFrame frame);

	[EngineMethod("set_nav_mesh_region_map", false, null, false)]
	void SetNavMeshRegionMap(UIntPtr scenePointer, bool[] regionMap, int regionMapSize);

	[EngineMethod("get_navigation_mesh_for_position", false, null, false)]
	UIntPtr GetNavigationMeshForPosition(UIntPtr scenePointer, in Vec3 position, ref int faceGroupId, float heightDifferenceLimit, bool excludeDynamicNavigationMeshes);

	[EngineMethod("get_nearest_navigation_mesh_for_position", false, null, false)]
	UIntPtr GetNearestNavigationMeshForPosition(UIntPtr scenePointer, in Vec3 position, float heightDifferenceLimit, bool excludeDynamicNavigationMeshes);

	[EngineMethod("get_path_distance_between_positions", false, null, false)]
	bool GetPathDistanceBetweenPositions(UIntPtr scenePointer, ref WorldPosition position, ref WorldPosition destination, float agentRadius, ref float pathLength);

	[EngineMethod("is_line_to_point_clear", false, null, false)]
	bool IsLineToPointClear(UIntPtr scenePointer, int startingFace, Vec2 position, Vec2 destination, float agentRadius);

	[EngineMethod("is_line_to_point_clear2", false, null, false)]
	bool IsLineToPointClear2(UIntPtr scenePointer, UIntPtr startingFace, Vec2 position, Vec2 destination, float agentRadius);

	[EngineMethod("get_last_position_on_nav_mesh_face_for_point_and_direction", false, null, false)]
	Vec2 GetLastPositionOnNavMeshFaceForPointAndDirection(UIntPtr scenePointer, in PathFaceRecord record, Vec2 position, Vec2 direction);

	[EngineMethod("get_last_point_on_navigation_mesh_from_position_to_destination", false, null, false)]
	Vec2 GetLastPointOnNavigationMeshFromPositionToDestination(UIntPtr scenePointer, int startingFace, Vec2 position, Vec2 destination, int[] exclusionGroupIds, int exclusionGroupIdsCount);

	[EngineMethod("get_last_point_on_navigation_mesh_from_world_position_to_destination", false, null, false)]
	Vec3 GetLastPointOnNavigationMeshFromWorldPositionToDestination(UIntPtr scenePointer, ref WorldPosition position, Vec2 destination);

	[EngineMethod("does_path_exist_between_positions", false, null, false)]
	bool DoesPathExistBetweenPositions(UIntPtr scenePointer, WorldPosition position, WorldPosition destination);

	[EngineMethod("does_path_exist_between_faces", false, null, false)]
	bool DoesPathExistBetweenFaces(UIntPtr scenePointer, int firstNavMeshFace, int secondNavMeshFace, bool ignoreDisabled);

	[EngineMethod("set_landscape_rain_mask_data", false, null, false)]
	void SetLandscapeRainMaskData(UIntPtr scenePointer, byte[] data);

	[EngineMethod("ensure_postfx_system", false, null, false)]
	void EnsurePostfxSystem(UIntPtr scenePointer);

	[EngineMethod("set_bloom", false, null, false)]
	void SetBloom(UIntPtr scenePointer, bool mode);

	[EngineMethod("set_dof_mode", false, null, false)]
	void SetDofMode(UIntPtr scenePointer, bool mode);

	[EngineMethod("set_occlusion_mode", false, null, false)]
	void SetOcclusionMode(UIntPtr scenePointer, bool mode);

	[EngineMethod("set_external_injection_texture", false, null, false)]
	void SetExternalInjectionTexture(UIntPtr scenePointer, UIntPtr texturePointer);

	[EngineMethod("set_sunshaft_mode", false, null, false)]
	void SetSunshaftMode(UIntPtr scenePointer, bool mode);

	[EngineMethod("get_sun_direction", false, null, false)]
	Vec3 GetSunDirection(UIntPtr scenePointer);

	[EngineMethod("get_north_angle", false, null, false)]
	float GetNorthAngle(UIntPtr scenePointer);

	[EngineMethod("get_terrain_min_max_height", false, null, false)]
	bool GetTerrainMinMaxHeight(Scene scene, ref float min, ref float max);

	[EngineMethod("get_physics_min_max", false, null, false)]
	void GetPhysicsMinMax(UIntPtr scenePointer, ref Vec3 min_max);

	[EngineMethod("is_editor_scene", false, null, false)]
	bool IsEditorScene(UIntPtr scenePointer);

	[EngineMethod("set_motionblur_mode", false, null, false)]
	void SetMotionBlurMode(UIntPtr scenePointer, bool mode);

	[EngineMethod("set_antialiasing_mode", false, null, false)]
	void SetAntialiasingMode(UIntPtr scenePointer, bool mode);

	[EngineMethod("set_dlss_mode", false, null, false)]
	void SetDLSSMode(UIntPtr scenePointer, bool mode);

	[EngineMethod("get_path_with_name", false, null, false)]
	Path GetPathWithName(UIntPtr scenePointer, string name);

	[EngineMethod("get_soft_boundary_vertex_count", false, null, false)]
	int GetSoftBoundaryVertexCount(UIntPtr scenePointer);

	[EngineMethod("delete_path_with_name", false, null, false)]
	void DeletePathWithName(UIntPtr scenePointer, string name);

	[EngineMethod("get_hard_boundary_vertex_count", false, null, false)]
	int GetHardBoundaryVertexCount(UIntPtr scenePointer);

	[EngineMethod("get_hard_boundary_vertex", false, null, false)]
	Vec2 GetHardBoundaryVertex(UIntPtr scenePointer, int index);

	[EngineMethod("add_path", false, null, false)]
	void AddPath(UIntPtr scenePointer, string name);

	[EngineMethod("get_soft_boundary_vertex", false, null, false)]
	Vec2 GetSoftBoundaryVertex(UIntPtr scenePointer, int index);

	[EngineMethod("add_path_point", false, null, false)]
	void AddPathPoint(UIntPtr scenePointer, string name, ref MatrixFrame frame);

	[EngineMethod("get_bounding_box", false, null, false)]
	void GetBoundingBox(UIntPtr scenePointer, ref Vec3 min, ref Vec3 max);

	[EngineMethod("get_scene_limits", false, null, false)]
	void GetSceneLimits(UIntPtr scenePointer, ref Vec3 min, ref Vec3 max);

	[EngineMethod("set_name", false, null, false)]
	void SetName(UIntPtr scenePointer, string name);

	[EngineMethod("get_name", false, null, false)]
	string GetName(UIntPtr scenePointer);

	[EngineMethod("get_module_path", false, null, false)]
	string GetModulePath(UIntPtr scenePointer);

	[EngineMethod("set_time_speed", false, null, false)]
	void SetTimeSpeed(UIntPtr scenePointer, float value);

	[EngineMethod("get_time_speed", false, null, false)]
	float GetTimeSpeed(UIntPtr scenePointer);

	[EngineMethod("set_owner_thread", false, null, false)]
	void SetOwnerThread(UIntPtr scenePointer);

	[EngineMethod("get_number_of_path_with_name_prefix", false, null, false)]
	int GetNumberOfPathsWithNamePrefix(UIntPtr ptr, string prefix);

	[EngineMethod("get_paths_with_name_prefix", false, null, false)]
	void GetPathsWithNamePrefix(UIntPtr ptr, UIntPtr[] points, string prefix);

	[EngineMethod("set_use_constant_time", false, null, false)]
	void SetUseConstantTime(UIntPtr ptr, bool value);

	[EngineMethod("set_play_sound_events_after_render_ready", false, null, false)]
	void SetPlaySoundEventsAfterReadyToRender(UIntPtr ptr, bool value);

	[EngineMethod("disable_static_shadows", false, null, false)]
	void DisableStaticShadows(UIntPtr ptr, bool value);

	[EngineMethod("get_skybox_mesh", false, null, false)]
	Mesh GetSkyboxMesh(UIntPtr ptr);

	[EngineMethod("set_atmosphere_with_name", false, null, false)]
	void SetAtmosphereWithName(UIntPtr ptr, string name);

	[EngineMethod("fill_entity_with_hard_border_physics_barrier", false, null, false)]
	void FillEntityWithHardBorderPhysicsBarrier(UIntPtr scenePointer, UIntPtr entityPointer);

	[EngineMethod("clear_decals", false, null, false)]
	void ClearDecals(UIntPtr scenePointer);

	[EngineMethod("set_photo_atmosphere_via_tod", false, null, false)]
	void SetPhotoAtmosphereViaTod(UIntPtr scenePointer, float tod, bool withStorm);

	[EngineMethod("get_scripted_entity_count", false, null, false)]
	int GetScriptedEntityCount(UIntPtr scenePointer);

	[EngineMethod("get_scripted_entity", false, null, false)]
	GameEntity GetScriptedEntity(UIntPtr scenePointer, int index);

	[EngineMethod("world_position_validate_z", false, null, false)]
	void WorldPositionValidateZ(ref WorldPosition position, int minimumValidityState);

	[EngineMethod("world_position_compute_nearest_nav_mesh", false, null, false)]
	void WorldPositionComputeNearestNavMesh(ref WorldPosition position);

	[EngineMethod("get_node_data_count", false, null, false)]
	int GetNodeDataCount(Scene scene, int xIndex, int yIndex);

	[EngineMethod("fill_terrain_height_data", false, null, false)]
	void FillTerrainHeightData(Scene scene, int xIndex, int yIndex, float[] heightArray);

	[EngineMethod("fill_terrain_physics_material_index_data", false, null, false)]
	void FillTerrainPhysicsMaterialIndexData(Scene scene, int xIndex, int yIndex, short[] materialIndexArray);

	[EngineMethod("get_terrain_data", false, null, false)]
	void GetTerrainData(Scene scene, out Vec2i nodeDimension, out float nodeSize, out int layerCount, out int layerVersion);

	[EngineMethod("get_terrain_node_data", false, null, false)]
	void GetTerrainNodeData(Scene scene, int xIndex, int yIndex, out int vertexCountAlongAxis, out float quadLength, out float minHeight, out float maxHeight);

	[EngineMethod("add_always_rendered_skeleton", false, null, false)]
	void AddAlwaysRenderedSkeleton(UIntPtr scenePointer, UIntPtr skeletonPointer);

	[EngineMethod("remove_always_rendered_skeleton", false, null, false)]
	void RemoveAlwaysRenderedSkeleton(UIntPtr scenePointer, UIntPtr skeletonPointer);

	[EngineMethod("is_position_on_a_dynamic_nav_mesh", false, null, false)]
	bool IsPositionOnADynamicNavMesh(UIntPtr scenePointer, Vec3 position);

	[EngineMethod("wait_water_renderer_cpu_simulation", false, null, false)]
	void WaitWaterRendererCPUSimulation(UIntPtr scenePointer);

	[EngineMethod("enable_inclusive_async_physx", false, null, false)]
	void EnableInclusiveAsyncPhysx(UIntPtr scenePointer);

	[EngineMethod("ensure_water_wake_renderer", false, null, false)]
	void EnsureWaterWakeRenderer(UIntPtr scenePointer);

	[EngineMethod("set_water_wake_world_size", false, null, false)]
	void SetWaterWakeWorldSize(UIntPtr scenePointer, float worldSize, float eraseFactor);

	[EngineMethod("set_water_wake_camera_offset", false, null, false)]
	void SetWaterWakeCameraOffset(UIntPtr scenePointer, float cameraOffset);

	[EngineMethod("delete_water_wake_renderer", false, null, false)]
	void DeleteWaterWakeRenderer(UIntPtr scenePointer);

	[EngineMethod("scene_had_water_wake_renderer", false, null, false)]
	bool SceneHadWaterWakeRenderer(UIntPtr scenePointer);

	[EngineMethod("tick_wake", false, null, false)]
	void TickWake(UIntPtr scenePointer, float dt);

	[EngineMethod("set_do_not_add_entities_to_tick_list", false, null, false)]
	void SetDoNotAddEntitiesToTickList(UIntPtr scenePointer, bool value);

	[EngineMethod("set_dont_load_invisible_entities", false, null, false)]
	void SetDontLoadInvisibleEntities(UIntPtr scenePointer, bool value);

	[EngineMethod("set_uses_delete_later_system", false, null, false)]
	void SetUsesDeleteLaterSystem(UIntPtr scenePointer, bool value);

	[EngineMethod("handle_current_frame_tick_entities", false, null, false)]
	void HandleCurrentFrameTickEntities(UIntPtr scenePointer);

	[EngineMethod("clear_current_frame_tick_entities", false, null, false)]
	void ClearCurrentFrameTickEntities(UIntPtr scenePointer);

	[EngineMethod("set_use_advanced_water_rendering", false, null, false)]
	void SetUseAdvancedWaterRendering(UIntPtr scenePointer, bool value);

	[EngineMethod("find_closest_exit_position_for_position_on_a_boundary_face", false, null, false)]
	Vec2 FindClosestExitPositionForPositionOnABoundaryFace(UIntPtr scenePointer, Vec3 position, UIntPtr boundaryNavMeshFacePointer);
}
