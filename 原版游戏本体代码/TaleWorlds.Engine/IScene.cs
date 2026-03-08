using System;
using TaleWorlds.DotNet;
using TaleWorlds.Library;

namespace TaleWorlds.Engine
{
	// Token: 0x0200002A RID: 42
	[ApplicationInterfaceBase]
	internal interface IScene
	{
		// Token: 0x060003B7 RID: 951
		[EngineMethod("create_new_scene", false, null, false)]
		Scene CreateNewScene(bool initializePhysics, bool enableDecals = true, int atlasGroup = 0, string sceneName = "mono_renderscene");

		// Token: 0x060003B8 RID: 952
		[EngineMethod("has_navmesh_face_unshared_edges", false, null, false)]
		bool HasNavmeshFaceUnsharedEdges(UIntPtr scenePointer, in PathFaceRecord faceRecord);

		// Token: 0x060003B9 RID: 953
		[EngineMethod("get_navmesh_face_count_between_two_ids", false, null, false)]
		int GetNavmeshFaceCountBetweenTwoIds(UIntPtr scenePointer, int firstId, int secondId);

		// Token: 0x060003BA RID: 954
		[EngineMethod("get_navmesh_face_records_between_two_ids", false, null, false)]
		void GetNavmeshFaceRecordsBetweenTwoIds(UIntPtr scenePointer, int firstId, int secondId, PathFaceRecord[] faceRecords);

		// Token: 0x060003BB RID: 955
		[EngineMethod("get_path_between_ai_face_pointers", false, null, false)]
		bool GetPathBetweenAIFacePointers(UIntPtr scenePointer, UIntPtr startingAiFace, UIntPtr endingAiFace, Vec2 startingPosition, Vec2 endingPosition, float agentRadius, Vec2[] result, ref int pathSize, int[] exclusionGroupIds, int exclusionGroupIdsCount);

		// Token: 0x060003BC RID: 956
		[EngineMethod("get_path_between_ai_face_pointers_with_region_switch_cost", false, null, false)]
		bool GetPathBetweenAIFacePointersWithRegionSwitchCost(UIntPtr scenePointer, UIntPtr startingAiFace, UIntPtr endingAiFace, Vec2 startingPosition, Vec2 endingPosition, float agentRadius, Vec2[] result, ref int pathSize, int[] exclusionGroupIds, int exclusionGroupIdsCount, int regionSwitchCostTo0, int regionSwitchCostTo1);

		// Token: 0x060003BD RID: 957
		[EngineMethod("get_path_between_ai_face_indices", false, null, false)]
		bool GetPathBetweenAIFaceIndices(UIntPtr scenePointer, int startingAiFace, int endingAiFace, Vec2 startingPosition, Vec2 endingPosition, float agentRadius, Vec2[] result, ref int pathSize, int[] exclusionGroupIds, int exclusionGroupIdsCount, float extraCostMultiplier);

		// Token: 0x060003BE RID: 958
		[EngineMethod("get_path_between_ai_face_indices_with_region_switch_cost", false, null, false)]
		bool GetPathBetweenAIFaceIndicesWithRegionSwitchCost(UIntPtr scenePointer, int startingAiFace, int endingAiFace, Vec2 startingPosition, Vec2 endingPosition, float agentRadius, Vec2[] result, ref int pathSize, int[] exclusionGroupIds, int exclusionGroupIdsCount, float extraCostMultiplier, int regionSwitchCostTo0, int regionSwitchCostTo1);

		// Token: 0x060003BF RID: 959
		[EngineMethod("get_path_distance_between_ai_faces", false, null, false)]
		bool GetPathDistanceBetweenAIFaces(UIntPtr scenePointer, int startingAiFace, int endingAiFace, Vec2 startingPosition, Vec2 endingPosition, float agentRadius, float distanceLimit, out float distance, int[] exclusionGroupIds, int exclusionGroupIdsCount, int regionSwitchCostTo0, int regionSwitchCostTo1);

		// Token: 0x060003C0 RID: 960
		[EngineMethod("get_nav_mesh_face_index_with_region", false, null, true)]
		void GetNavMeshFaceIndex(UIntPtr scenePointer, ref PathFaceRecord record, Vec2 position, bool isRegion1, bool checkIfDisabled, bool ignoreHeight);

		// Token: 0x060003C1 RID: 961
		[EngineMethod("is_default_editor_scene", false, null, false)]
		bool IsDefaultEditorScene(Scene scene);

		// Token: 0x060003C2 RID: 962
		[EngineMethod("is_multiplayer_scene", false, null, false)]
		bool IsMultiplayerScene(Scene scene);

		// Token: 0x060003C3 RID: 963
		[EngineMethod("take_photo_mode_picture", false, null, false)]
		string TakePhotoModePicture(Scene scene, bool saveAmbientOcclusionPass, bool saveObjectIdPass, bool saveShadowPass);

		// Token: 0x060003C4 RID: 964
		[EngineMethod("get_all_color_grade_names", false, null, false)]
		string GetAllColorGradeNames(Scene scene);

		// Token: 0x060003C5 RID: 965
		[EngineMethod("get_all_filter_names", false, null, false)]
		string GetAllFilterNames(Scene scene);

		// Token: 0x060003C6 RID: 966
		[EngineMethod("get_photo_mode_roll", false, null, false)]
		float GetPhotoModeRoll(Scene scene);

		// Token: 0x060003C7 RID: 967
		[EngineMethod("get_photo_mode_fov", false, null, false)]
		float GetPhotoModeFov(Scene scene);

		// Token: 0x060003C8 RID: 968
		[EngineMethod("has_decal_renderer", false, null, true)]
		bool HasDecalRenderer(UIntPtr scenePointer);

		// Token: 0x060003C9 RID: 969
		[EngineMethod("get_photo_mode_orbit", false, null, false)]
		bool GetPhotoModeOrbit(Scene scene);

		// Token: 0x060003CA RID: 970
		[EngineMethod("get_photo_mode_on", false, null, false)]
		bool GetPhotoModeOn(Scene scene);

		// Token: 0x060003CB RID: 971
		[EngineMethod("get_photo_mode_focus", false, null, false)]
		void GetPhotoModeFocus(Scene scene, ref float focus, ref float focusStart, ref float focusEnd, ref float exposure, ref bool vignetteOn);

		// Token: 0x060003CC RID: 972
		[EngineMethod("get_scene_color_grade_index", false, null, false)]
		int GetSceneColorGradeIndex(Scene scene);

		// Token: 0x060003CD RID: 973
		[EngineMethod("get_scene_filter_index", false, null, false)]
		int GetSceneFilterIndex(Scene scene);

		// Token: 0x060003CE RID: 974
		[EngineMethod("enable_fixed_tick", false, null, false)]
		void EnableFixedTick(Scene scene);

		// Token: 0x060003CF RID: 975
		[EngineMethod("get_loading_state_name", false, null, false)]
		string GetLoadingStateName(Scene scene);

		// Token: 0x060003D0 RID: 976
		[EngineMethod("is_loading_finished", false, null, false)]
		bool IsLoadingFinished(Scene scene);

		// Token: 0x060003D1 RID: 977
		[EngineMethod("set_photo_mode_roll", false, null, false)]
		void SetPhotoModeRoll(Scene scene, float roll);

		// Token: 0x060003D2 RID: 978
		[EngineMethod("set_photo_mode_fov", false, null, false)]
		void SetPhotoModeFov(Scene scene, float verticalFov);

		// Token: 0x060003D3 RID: 979
		[EngineMethod("set_photo_mode_orbit", false, null, false)]
		void SetPhotoModeOrbit(Scene scene, bool orbit);

		// Token: 0x060003D4 RID: 980
		[EngineMethod("get_fall_density", false, null, false)]
		float GetFallDensity(UIntPtr scenepTR);

		// Token: 0x060003D5 RID: 981
		[EngineMethod("set_photo_mode_on", false, null, false)]
		void SetPhotoModeOn(Scene scene, bool on);

		// Token: 0x060003D6 RID: 982
		[EngineMethod("set_photo_mode_focus", false, null, false)]
		void SetPhotoModeFocus(Scene scene, float focusStart, float focusEnd, float focus, float exposure);

		// Token: 0x060003D7 RID: 983
		[EngineMethod("set_photo_mode_vignette", false, null, false)]
		void SetPhotoModeVignette(Scene scene, bool vignetteOn);

		// Token: 0x060003D8 RID: 984
		[EngineMethod("set_scene_color_grade_index", false, null, false)]
		void SetSceneColorGradeIndex(Scene scene, int index);

		// Token: 0x060003D9 RID: 985
		[EngineMethod("set_scene_filter_index", false, null, false)]
		int SetSceneFilterIndex(Scene scene, int index);

		// Token: 0x060003DA RID: 986
		[EngineMethod("set_scene_color_grade", false, null, false)]
		void SetSceneColorGrade(Scene scene, string textureName);

		// Token: 0x060003DB RID: 987
		[EngineMethod("get_water_level", false, null, false)]
		float GetWaterLevel(Scene scene);

		// Token: 0x060003DC RID: 988
		[EngineMethod("get_water_level_at_position", false, null, false)]
		float GetWaterLevelAtPosition(Scene scene, Vec2 position, bool useWaterRenderer, bool checkWaterBodyEntities);

		// Token: 0x060003DD RID: 989
		[EngineMethod("get_water_speed_at_position", false, null, true)]
		Vec3 GetWaterSpeedAtPosition(UIntPtr scenePointer, in Vec2 position, bool doChoppinessCorrection);

		// Token: 0x060003DE RID: 990
		[EngineMethod("set_fixed_tick_callback_active", false, null, false)]
		void SetFixedTickCallbackActive(Scene scene, bool isActive);

		// Token: 0x060003DF RID: 991
		[EngineMethod("set_on_collision_filter_callback_active", false, null, false)]
		void SetOnCollisionFilterCallbackActive(Scene scene, bool isActive);

		// Token: 0x060003E0 RID: 992
		[EngineMethod("get_interpolation_factor_for_body_world_transform_smoothing", false, null, false)]
		void GetInterpolationFactorForBodyWorldTransformSmoothing(Scene scene, out float interpolationFactor, out float fixedDt);

		// Token: 0x060003E1 RID: 993
		[EngineMethod("get_bulk_water_level_at_positions", false, null, false)]
		void GetBulkWaterLevelAtPositions(Scene scene, Vec2[] waterHeightQueryArray, int arraySize, float[] waterHeightsAtVolumes, Vec3[] waterSurfaceNormals);

		// Token: 0x060003E2 RID: 994
		[EngineMethod("get_bulk_water_level_at_volumes", false, null, true)]
		void GetBulkWaterLevelAtVolumes(UIntPtr scene, UIntPtr volumeQueryDataArray, int volumeCount, in MatrixFrame entityFrame);

		// Token: 0x060003E3 RID: 995
		[EngineMethod("get_water_strength", false, null, false)]
		float GetWaterStrength(Scene scene);

		// Token: 0x060003E4 RID: 996
		[EngineMethod("register_ship_visual_to_water_renderer", false, null, false)]
		UIntPtr RegisterShipVisualToWaterRenderer(UIntPtr scenePointer, UIntPtr entityPointer, in Vec3 bb);

		// Token: 0x060003E5 RID: 997
		[EngineMethod("deregister_ship_visual", false, null, false)]
		void DeRegisterShipVisual(UIntPtr scenePointer, UIntPtr visualPointer);

		// Token: 0x060003E6 RID: 998
		[EngineMethod("set_water_strength", false, null, false)]
		void SetWaterStrength(Scene scene, float newWaterStrength);

		// Token: 0x060003E7 RID: 999
		[EngineMethod("add_water_wake_with_capsule", false, null, false)]
		void AddWaterWakeWithCapsule(Scene scene, Vec3 positionA, float radiusA, Vec3 positionB, float radiusB, float wakeVisibility, float foamVisibility);

		// Token: 0x060003E8 RID: 1000
		[EngineMethod("get_terrain_material_index_at_layer", false, null, false)]
		int GetTerrainPhysicsMaterialIndexAtLayer(Scene scene, int layerIndex);

		// Token: 0x060003E9 RID: 1001
		[EngineMethod("create_burst_particle", false, null, false)]
		void CreateBurstParticle(Scene scene, int particleId, ref MatrixFrame frame);

		// Token: 0x060003EA RID: 1002
		[EngineMethod("get_nav_mesh_face_index3", false, null, false)]
		void GetNavMeshFaceIndex3(UIntPtr scenePointer, ref PathFaceRecord record, Vec3 position, bool checkIfDisabled);

		// Token: 0x060003EB RID: 1003
		[EngineMethod("set_upgrade_level", false, null, false)]
		void SetUpgradeLevel(UIntPtr scenePointer, int level);

		// Token: 0x060003EC RID: 1004
		[EngineMethod("create_path_mesh", false, null, false)]
		MetaMesh CreatePathMesh(UIntPtr scenePointer, string baseEntityName, bool isWaterPath);

		// Token: 0x060003ED RID: 1005
		[EngineMethod("set_active_visibility_levels", false, null, false)]
		void SetActiveVisibilityLevels(UIntPtr scenePointer, string levelsAppended);

		// Token: 0x060003EE RID: 1006
		[EngineMethod("set_terrain_dynamic_params", false, null, false)]
		void SetTerrainDynamicParams(UIntPtr scenePointer, Vec3 dynamic_params);

		// Token: 0x060003EF RID: 1007
		[EngineMethod("set_do_not_wait_for_loading_states_to_render", false, null, false)]
		void SetDoNotWaitForLoadingStatesToRender(UIntPtr scenePointer, bool value);

		// Token: 0x060003F0 RID: 1008
		[EngineMethod("set_dynamic_snow_texture", false, null, false)]
		void SetDynamicSnowTexture(UIntPtr scenePointer, UIntPtr texturePointer);

		// Token: 0x060003F1 RID: 1009
		[EngineMethod("get_flowmap_data", false, null, false)]
		void GetWindFlowMapData(UIntPtr scenePointer, float[] flowmapData);

		// Token: 0x060003F2 RID: 1010
		[EngineMethod("create_path_mesh2", false, null, false)]
		MetaMesh CreatePathMesh2(UIntPtr scenePointer, UIntPtr[] pathNodes, int pathNodeCount, bool isWaterPath);

		// Token: 0x060003F3 RID: 1011
		[EngineMethod("clear_all", false, null, false)]
		void ClearAll(UIntPtr scenePointer);

		// Token: 0x060003F4 RID: 1012
		[EngineMethod("check_resources", false, null, false)]
		void CheckResources(UIntPtr scenePointer, bool checkInvisibleEntities);

		// Token: 0x060003F5 RID: 1013
		[EngineMethod("force_load_resources", false, null, false)]
		void ForceLoadResources(UIntPtr scenePointer, bool checkInvisibleEntities);

		// Token: 0x060003F6 RID: 1014
		[EngineMethod("check_path_entities_frame_changed", false, null, false)]
		bool CheckPathEntitiesFrameChanged(UIntPtr scenePointer, string containsName);

		// Token: 0x060003F7 RID: 1015
		[EngineMethod("tick", false, null, false)]
		void Tick(UIntPtr scenePointer, float deltaTime);

		// Token: 0x060003F8 RID: 1016
		[EngineMethod("add_entity_with_mesh", false, null, false)]
		void AddEntityWithMesh(UIntPtr scenePointer, UIntPtr meshPointer, ref MatrixFrame frame);

		// Token: 0x060003F9 RID: 1017
		[EngineMethod("add_entity_with_multi_mesh", false, null, false)]
		void AddEntityWithMultiMesh(UIntPtr scenePointer, UIntPtr multiMeshPointer, ref MatrixFrame frame);

		// Token: 0x060003FA RID: 1018
		[EngineMethod("add_item_entity", false, null, false)]
		GameEntity AddItemEntity(UIntPtr scenePointer, ref MatrixFrame frame, UIntPtr meshPointer);

		// Token: 0x060003FB RID: 1019
		[EngineMethod("remove_entity", false, null, false)]
		void RemoveEntity(UIntPtr scenePointer, UIntPtr entityId, int removeReason);

		// Token: 0x060003FC RID: 1020
		[EngineMethod("attach_entity", false, null, false)]
		bool AttachEntity(UIntPtr scenePointer, UIntPtr entity, bool showWarnings);

		// Token: 0x060003FD RID: 1021
		[EngineMethod("get_terrain_height_and_normal", false, null, false)]
		void GetTerrainHeightAndNormal(UIntPtr scenePointer, Vec2 position, out float height, out Vec3 normal);

		// Token: 0x060003FE RID: 1022
		[EngineMethod("resume_loading_renderings", false, null, false)]
		void ResumeLoadingRenderings(UIntPtr scenePointer);

		// Token: 0x060003FF RID: 1023
		[EngineMethod("get_upgrade_level_mask", false, null, false)]
		uint GetUpgradeLevelMask(UIntPtr scenePointer);

		// Token: 0x06000400 RID: 1024
		[EngineMethod("set_upgrade_level_visibility", false, null, false)]
		void SetUpgradeLevelVisibility(UIntPtr scenePointer, string concatLevels);

		// Token: 0x06000401 RID: 1025
		[EngineMethod("set_upgrade_level_visibility_with_mask", false, null, false)]
		void SetUpgradeLevelVisibilityWithMask(UIntPtr scenePointer, uint mask);

		// Token: 0x06000402 RID: 1026
		[EngineMethod("stall_loading_renderings", false, null, false)]
		void StallLoadingRenderingsUntilFurtherNotice(UIntPtr scenePointer);

		// Token: 0x06000403 RID: 1027
		[EngineMethod("get_flora_instance_count", false, null, false)]
		int GetFloraInstanceCount(UIntPtr scenePointer);

		// Token: 0x06000404 RID: 1028
		[EngineMethod("get_flora_renderer_texture_usage", false, null, false)]
		int GetFloraRendererTextureUsage(UIntPtr scenePointer);

		// Token: 0x06000405 RID: 1029
		[EngineMethod("get_terrain_memory_usage", false, null, false)]
		int GetTerrainMemoryUsage(UIntPtr scenePointer);

		// Token: 0x06000406 RID: 1030
		[EngineMethod("set_fetch_crc_info_of_scene", false, null, false)]
		void SetFetchCrcInfoOfScene(UIntPtr scenePointer, bool value);

		// Token: 0x06000407 RID: 1031
		[EngineMethod("get_scene_xml_crc", false, null, false)]
		uint GetSceneXMLCRC(UIntPtr scenePointer);

		// Token: 0x06000408 RID: 1032
		[EngineMethod("get_navigation_mesh_crc", false, null, false)]
		uint GetNavigationMeshCRC(UIntPtr scenePointer);

		// Token: 0x06000409 RID: 1033
		[EngineMethod("get_global_wind_strength_vector", false, null, false)]
		Vec2 GetGlobalWindStrengthVector(UIntPtr scenePointer);

		// Token: 0x0600040A RID: 1034
		[EngineMethod("set_global_wind_strength_vector", false, null, false)]
		void SetGlobalWindStrengthVector(UIntPtr scenePointer, in Vec2 strengthVector);

		// Token: 0x0600040B RID: 1035
		[EngineMethod("get_global_wind_velocity", false, null, true)]
		Vec2 GetGlobalWindVelocity(UIntPtr scenePointer);

		// Token: 0x0600040C RID: 1036
		[EngineMethod("set_global_wind_velocity", false, null, false)]
		void SetGlobalWindVelocity(UIntPtr scenePointer, in Vec2 windVelocity);

		// Token: 0x0600040D RID: 1037
		[EngineMethod("get_engine_physics_enabled", false, null, false)]
		bool GetEnginePhysicsEnabled(UIntPtr scenePointer);

		// Token: 0x0600040E RID: 1038
		[EngineMethod("clear_nav_mesh", false, null, false)]
		void ClearNavMesh(UIntPtr scenePointer);

		// Token: 0x0600040F RID: 1039
		[EngineMethod("get_nav_mesh_face_count", false, null, false)]
		int GetNavMeshFaceCount(UIntPtr scenePointer);

		// Token: 0x06000410 RID: 1040
		[EngineMethod("get_nav_mesh_face_center_position", false, null, true)]
		void GetNavMeshFaceCenterPosition(UIntPtr scenePointer, int navMeshFace, ref Vec3 centerPos);

		// Token: 0x06000411 RID: 1041
		[EngineMethod("get_nav_mesh_path_face_record", false, null, false)]
		PathFaceRecord GetNavMeshPathFaceRecord(UIntPtr scenePointer, int navMeshFace);

		// Token: 0x06000412 RID: 1042
		[EngineMethod("get_path_face_record_from_nav_mesh_face_pointer", false, null, false)]
		PathFaceRecord GetPathFaceRecordFromNavMeshFacePointer(UIntPtr scenePointer, UIntPtr navMeshFacePointer);

		// Token: 0x06000413 RID: 1043
		[EngineMethod("get_all_nav_mesh_face_records", false, null, false)]
		void GetAllNavmeshFaceRecords(UIntPtr scenePointer, PathFaceRecord[] faceRecords);

		// Token: 0x06000414 RID: 1044
		[EngineMethod("get_id_of_nav_mesh_face", false, null, false)]
		int GetIdOfNavMeshFace(UIntPtr scenePointer, int navMeshFace);

		// Token: 0x06000415 RID: 1045
		[EngineMethod("set_cloth_simulation_state", false, null, false)]
		void SetClothSimulationState(UIntPtr scenePointer, bool state);

		// Token: 0x06000416 RID: 1046
		[EngineMethod("get_first_entity_with_name", false, null, false)]
		GameEntity GetFirstEntityWithName(UIntPtr scenePointer, string entityName);

		// Token: 0x06000417 RID: 1047
		[EngineMethod("get_campaign_entity_with_name", false, null, false)]
		GameEntity GetCampaignEntityWithName(UIntPtr scenePointer, string entityName);

		// Token: 0x06000418 RID: 1048
		[EngineMethod("get_all_entities_with_script_component", false, null, false)]
		void GetAllEntitiesWithScriptComponent(UIntPtr scenePointer, string scriptComponentName, UIntPtr output);

		// Token: 0x06000419 RID: 1049
		[EngineMethod("get_first_entity_with_script_component", false, null, false)]
		GameEntity GetFirstEntityWithScriptComponent(UIntPtr scenePointer, string scriptComponentName);

		// Token: 0x0600041A RID: 1050
		[EngineMethod("get_upgrade_level_mask_of_level_name", false, null, false)]
		uint GetUpgradeLevelMaskOfLevelName(UIntPtr scenePointer, string levelName);

		// Token: 0x0600041B RID: 1051
		[EngineMethod("get_level_name_of_level_index", false, null, false)]
		string GetUpgradeLevelNameOfIndex(UIntPtr scenePointer, int index);

		// Token: 0x0600041C RID: 1052
		[EngineMethod("get_upgrade_level_count", false, null, false)]
		int GetUpgradeLevelCount(UIntPtr scenePointer);

		// Token: 0x0600041D RID: 1053
		[EngineMethod("get_winter_time_factor", false, null, false)]
		float GetWinterTimeFactor(UIntPtr scenePointer);

		// Token: 0x0600041E RID: 1054
		[EngineMethod("get_nav_mesh_face_first_vertex_z", false, null, false)]
		float GetNavMeshFaceFirstVertexZ(UIntPtr scenePointer, int navMeshFaceIndex);

		// Token: 0x0600041F RID: 1055
		[EngineMethod("set_winter_time_factor", false, null, false)]
		void SetWinterTimeFactor(UIntPtr scenePointer, float winterTimeFactor);

		// Token: 0x06000420 RID: 1056
		[EngineMethod("set_dryness_factor", false, null, false)]
		void SetDrynessFactor(UIntPtr scenePointer, float drynessFactor);

		// Token: 0x06000421 RID: 1057
		[EngineMethod("get_fog", false, null, false)]
		float GetFog(UIntPtr scenePointer);

		// Token: 0x06000422 RID: 1058
		[EngineMethod("set_fog", false, null, false)]
		void SetFog(UIntPtr scenePointer, float fogDensity, ref Vec3 fogColor, float fogFalloff);

		// Token: 0x06000423 RID: 1059
		[EngineMethod("set_fog_advanced", false, null, false)]
		void SetFogAdvanced(UIntPtr scenePointer, float fogFalloffOffset, float fogFalloffMinFog, float fogFalloffStartDist);

		// Token: 0x06000424 RID: 1060
		[EngineMethod("set_fog_ambient_color", false, null, false)]
		void SetFogAmbientColor(UIntPtr scenePointer, ref Vec3 fogAmbientColor);

		// Token: 0x06000425 RID: 1061
		[EngineMethod("set_temperature", false, null, false)]
		void SetTemperature(UIntPtr scenePointer, float temperature);

		// Token: 0x06000426 RID: 1062
		[EngineMethod("set_humidity", false, null, false)]
		void SetHumidity(UIntPtr scenePointer, float humidity);

		// Token: 0x06000427 RID: 1063
		[EngineMethod("set_dynamic_shadowmap_cascades_radius_multiplier", false, null, false)]
		void SetDynamicShadowmapCascadesRadiusMultiplier(UIntPtr scenePointer, float extraRadius);

		// Token: 0x06000428 RID: 1064
		[EngineMethod("set_env_map_multiplier", false, null, false)]
		void SetEnvironmentMultiplier(UIntPtr scenePointer, bool useMultiplier, float multiplier);

		// Token: 0x06000429 RID: 1065
		[EngineMethod("set_sky_rotation", false, null, false)]
		void SetSkyRotation(UIntPtr scenePointer, float rotation);

		// Token: 0x0600042A RID: 1066
		[EngineMethod("set_sky_brightness", false, null, false)]
		void SetSkyBrightness(UIntPtr scenePointer, float brightness);

		// Token: 0x0600042B RID: 1067
		[EngineMethod("set_forced_snow", false, null, false)]
		void SetForcedSnow(UIntPtr scenePointer, bool value);

		// Token: 0x0600042C RID: 1068
		[EngineMethod("set_sun", false, null, false)]
		void SetSun(UIntPtr scenePointer, Vec3 color, float altitude, float angle, float intensity);

		// Token: 0x0600042D RID: 1069
		[EngineMethod("set_sun_angle_altitude", false, null, false)]
		void SetSunAngleAltitude(UIntPtr scenePointer, float angle, float altitude);

		// Token: 0x0600042E RID: 1070
		[EngineMethod("set_sun_light", false, null, false)]
		void SetSunLight(UIntPtr scenePointer, Vec3 color, Vec3 direction);

		// Token: 0x0600042F RID: 1071
		[EngineMethod("set_sun_direction", false, null, false)]
		void SetSunDirection(UIntPtr scenePointer, Vec3 direction);

		// Token: 0x06000430 RID: 1072
		[EngineMethod("set_sun_size", false, null, false)]
		void SetSunSize(UIntPtr scenePointer, float size);

		// Token: 0x06000431 RID: 1073
		[EngineMethod("set_sunshafts_strength", false, null, false)]
		void SetSunShaftStrength(UIntPtr scenePointer, float strength);

		// Token: 0x06000432 RID: 1074
		[EngineMethod("get_rain_density", false, null, false)]
		float GetRainDensity(UIntPtr scenePointer);

		// Token: 0x06000433 RID: 1075
		[EngineMethod("set_rain_density", false, null, false)]
		void SetRainDensity(UIntPtr scenePointer, float density);

		// Token: 0x06000434 RID: 1076
		[EngineMethod("get_snow_density", false, null, false)]
		float GetSnowDensity(UIntPtr scenePointer);

		// Token: 0x06000435 RID: 1077
		[EngineMethod("set_snow_density", false, null, false)]
		void SetSnowDensity(UIntPtr scenePointer, float density);

		// Token: 0x06000436 RID: 1078
		[EngineMethod("add_decal_instance", false, null, false)]
		void AddDecalInstance(UIntPtr scenePointer, UIntPtr decalMeshPointer, string decalSetID, bool deletable);

		// Token: 0x06000437 RID: 1079
		[EngineMethod("remove_decal_instance", false, null, false)]
		void RemoveDecalInstance(UIntPtr scenePointer, UIntPtr decalMeshPointer, string decalSetID);

		// Token: 0x06000438 RID: 1080
		[EngineMethod("set_shadow", false, null, false)]
		void SetShadow(UIntPtr scenePointer, bool shadowEnabled);

		// Token: 0x06000439 RID: 1081
		[EngineMethod("add_point_light", false, null, false)]
		int AddPointLight(UIntPtr scenePointer, Vec3 position, float radius);

		// Token: 0x0600043A RID: 1082
		[EngineMethod("add_directional_light", false, null, false)]
		int AddDirectionalLight(UIntPtr scenePointer, Vec3 position, Vec3 direction, float radius);

		// Token: 0x0600043B RID: 1083
		[EngineMethod("set_light_position", false, null, false)]
		void SetLightPosition(UIntPtr scenePointer, int lightIndex, Vec3 position);

		// Token: 0x0600043C RID: 1084
		[EngineMethod("set_light_diffuse_color", false, null, false)]
		void SetLightDiffuseColor(UIntPtr scenePointer, int lightIndex, Vec3 diffuseColor);

		// Token: 0x0600043D RID: 1085
		[EngineMethod("set_light_direction", false, null, false)]
		void SetLightDirection(UIntPtr scenePointer, int lightIndex, Vec3 direction);

		// Token: 0x0600043E RID: 1086
		[EngineMethod("calculate_effective_lighting", false, null, false)]
		bool CalculateEffectiveLighting(UIntPtr scenePointer);

		// Token: 0x0600043F RID: 1087
		[EngineMethod("set_rayleigh_constant", false, null, false)]
		void SetMieScatterStrength(UIntPtr scenePointer, float strength);

		// Token: 0x06000440 RID: 1088
		[EngineMethod("set_mie_scatter_particle_size", false, null, false)]
		void SetMieScatterFocus(UIntPtr scenePointer, float strength);

		// Token: 0x06000441 RID: 1089
		[EngineMethod("set_brightpass_threshold", false, null, false)]
		void SetBrightpassTreshold(UIntPtr scenePointer, float threshold);

		// Token: 0x06000442 RID: 1090
		[EngineMethod("set_min_exposure", false, null, false)]
		void SetMinExposure(UIntPtr scenePointer, float minExposure);

		// Token: 0x06000443 RID: 1091
		[EngineMethod("set_max_exposure", false, null, false)]
		void SetMaxExposure(UIntPtr scenePointer, float maxExposure);

		// Token: 0x06000444 RID: 1092
		[EngineMethod("set_target_exposure", false, null, false)]
		void SetTargetExposure(UIntPtr scenePointer, float targetExposure);

		// Token: 0x06000445 RID: 1093
		[EngineMethod("set_middle_gray", false, null, false)]
		void SetMiddleGray(UIntPtr scenePointer, float middleGray);

		// Token: 0x06000446 RID: 1094
		[EngineMethod("set_bloom_strength", false, null, false)]
		void SetBloomStrength(UIntPtr scenePointer, float bloomStrength);

		// Token: 0x06000447 RID: 1095
		[EngineMethod("set_bloom_amount", false, null, false)]
		void SetBloomAmount(UIntPtr scenePointer, float bloomAmount);

		// Token: 0x06000448 RID: 1096
		[EngineMethod("set_grain_amount", false, null, false)]
		void SetGrainAmount(UIntPtr scenePointer, float grainAmount);

		// Token: 0x06000449 RID: 1097
		[EngineMethod("set_lens_flare_amount", false, null, false)]
		void SetLensFlareAmount(UIntPtr scenePointer, float lensFlareAmount);

		// Token: 0x0600044A RID: 1098
		[EngineMethod("set_lens_flare_threshold", false, null, false)]
		void SetLensFlareThreshold(UIntPtr scenePointer, float lensFlareThreshold);

		// Token: 0x0600044B RID: 1099
		[EngineMethod("set_lens_flare_strength", false, null, false)]
		void SetLensFlareStrength(UIntPtr scenePointer, float lensFlareStrength);

		// Token: 0x0600044C RID: 1100
		[EngineMethod("set_lens_flare_dirt_weight", false, null, false)]
		void SetLensFlareDirtWeight(UIntPtr scenePointer, float lensFlareDirtWeight);

		// Token: 0x0600044D RID: 1101
		[EngineMethod("set_lens_flare_diffraction_weight", false, null, false)]
		void SetLensFlareDiffractionWeight(UIntPtr scenePointer, float lensFlareDiffractionWeight);

		// Token: 0x0600044E RID: 1102
		[EngineMethod("set_lens_flare_halo_weight", false, null, false)]
		void SetLensFlareHaloWeight(UIntPtr scenePointer, float lensFlareHaloWeight);

		// Token: 0x0600044F RID: 1103
		[EngineMethod("set_lens_flare_ghost_weight", false, null, false)]
		void SetLensFlareGhostWeight(UIntPtr scenePointer, float lensFlareGhostWeight);

		// Token: 0x06000450 RID: 1104
		[EngineMethod("set_lens_flare_halo_width", false, null, false)]
		void SetLensFlareHaloWidth(UIntPtr scenePointer, float lensFlareHaloWidth);

		// Token: 0x06000451 RID: 1105
		[EngineMethod("set_lens_flare_ghost_samples", false, null, false)]
		void SetLensFlareGhostSamples(UIntPtr scenePointer, int lensFlareGhostSamples);

		// Token: 0x06000452 RID: 1106
		[EngineMethod("set_lens_flare_aberration_offset", false, null, false)]
		void SetLensFlareAberrationOffset(UIntPtr scenePointer, float lensFlareAberrationOffset);

		// Token: 0x06000453 RID: 1107
		[EngineMethod("set_lens_flare_blur_size", false, null, false)]
		void SetLensFlareBlurSize(UIntPtr scenePointer, int lensFlareBlurSize);

		// Token: 0x06000454 RID: 1108
		[EngineMethod("set_lens_flare_blur_sigma", false, null, false)]
		void SetLensFlareBlurSigma(UIntPtr scenePointer, float lensFlareBlurSigma);

		// Token: 0x06000455 RID: 1109
		[EngineMethod("set_streak_amount", false, null, false)]
		void SetStreakAmount(UIntPtr scenePointer, float streakAmount);

		// Token: 0x06000456 RID: 1110
		[EngineMethod("set_streak_threshold", false, null, false)]
		void SetStreakThreshold(UIntPtr scenePointer, float streakThreshold);

		// Token: 0x06000457 RID: 1111
		[EngineMethod("set_streak_strength", false, null, false)]
		void SetStreakStrength(UIntPtr scenePointer, float strengthAmount);

		// Token: 0x06000458 RID: 1112
		[EngineMethod("set_streak_stretch", false, null, false)]
		void SetStreakStretch(UIntPtr scenePointer, float stretchAmount);

		// Token: 0x06000459 RID: 1113
		[EngineMethod("set_streak_intensity", false, null, false)]
		void SetStreakIntensity(UIntPtr scenePointer, float stretchAmount);

		// Token: 0x0600045A RID: 1114
		[EngineMethod("set_streak_tint", false, null, false)]
		void SetStreakTint(UIntPtr scenePointer, ref Vec3 p_streak_tint_color);

		// Token: 0x0600045B RID: 1115
		[EngineMethod("set_hexagon_vignette_color", false, null, false)]
		void SetHexagonVignetteColor(UIntPtr scenePointer, ref Vec3 p_hexagon_vignette_color);

		// Token: 0x0600045C RID: 1116
		[EngineMethod("set_hexagon_vignette_alpha", false, null, false)]
		void SetHexagonVignetteAlpha(UIntPtr scenePointer, float Alpha);

		// Token: 0x0600045D RID: 1117
		[EngineMethod("set_vignette_inner_radius", false, null, false)]
		void SetVignetteInnerRadius(UIntPtr scenePointer, float vignetteInnerRadius);

		// Token: 0x0600045E RID: 1118
		[EngineMethod("set_vignette_outer_radius", false, null, false)]
		void SetVignetteOuterRadius(UIntPtr scenePointer, float vignetteOuterRadius);

		// Token: 0x0600045F RID: 1119
		[EngineMethod("set_vignette_opacity", false, null, false)]
		void SetVignetteOpacity(UIntPtr scenePointer, float vignetteOpacity);

		// Token: 0x06000460 RID: 1120
		[EngineMethod("set_aberration_offset", false, null, false)]
		void SetAberrationOffset(UIntPtr scenePointer, float aberrationOffset);

		// Token: 0x06000461 RID: 1121
		[EngineMethod("set_aberration_size", false, null, false)]
		void SetAberrationSize(UIntPtr scenePointer, float aberrationSize);

		// Token: 0x06000462 RID: 1122
		[EngineMethod("set_aberration_smooth", false, null, false)]
		void SetAberrationSmooth(UIntPtr scenePointer, float aberrationSmooth);

		// Token: 0x06000463 RID: 1123
		[EngineMethod("set_lens_distortion", false, null, false)]
		void SetLensDistortion(UIntPtr scenePointer, float lensDistortion);

		// Token: 0x06000464 RID: 1124
		[EngineMethod("get_height_at_point", false, null, false)]
		bool GetHeightAtPoint(UIntPtr scenePointer, Vec2 point, BodyFlags excludeBodyFlags, ref float height);

		// Token: 0x06000465 RID: 1125
		[EngineMethod("get_entity_count", false, null, false)]
		int GetEntityCount(UIntPtr scenePointer);

		// Token: 0x06000466 RID: 1126
		[EngineMethod("get_entities", false, null, false)]
		void GetEntities(UIntPtr scenePointer, UIntPtr entityObjectsArrayPointer);

		// Token: 0x06000467 RID: 1127
		[EngineMethod("get_root_entity_count", false, null, false)]
		int GetRootEntityCount(UIntPtr scenePointer);

		// Token: 0x06000468 RID: 1128
		[EngineMethod("get_root_entities", false, null, false)]
		void GetRootEntities(Scene scene, NativeObjectArray output);

		// Token: 0x06000469 RID: 1129
		[EngineMethod("get_entity_with_guid", false, null, false)]
		GameEntity GetEntityWithGuid(UIntPtr scenePointer, string guid);

		// Token: 0x0600046A RID: 1130
		[EngineMethod("select_entities_in_box_with_script_component", false, null, false)]
		int SelectEntitiesInBoxWithScriptComponent(UIntPtr scenePointer, ref Vec3 boundingBoxMin, ref Vec3 boundingBoxMax, UIntPtr[] entitiesOutput, int maxCount, string scriptComponentName);

		// Token: 0x0600046B RID: 1131
		[EngineMethod("ray_cast_excluding_two_entities", false, null, false)]
		bool RayCastExcludingTwoEntities(BodyFlags flags, UIntPtr scenePointer, in Ray ray, UIntPtr entity1, UIntPtr entity2);

		// Token: 0x0600046C RID: 1132
		[EngineMethod("select_entities_collided_with", false, null, false)]
		int SelectEntitiesCollidedWith(UIntPtr scenePointer, ref Ray ray, UIntPtr[] entityIds, Intersection[] intersections);

		// Token: 0x0600046D RID: 1133
		[EngineMethod("generate_contacts_with_capsule", false, null, false)]
		int GenerateContactsWithCapsule(UIntPtr scenePointer, ref CapsuleData cap, BodyFlags excludeFlags, bool isFixedTick, Intersection[] intersections, UIntPtr[] entityIds);

		// Token: 0x0600046E RID: 1134
		[EngineMethod("generate_contacts_with_capsule_against_entity", false, null, false)]
		int GenerateContactsWithCapsuleAgainstEntity(UIntPtr scenePointer, ref CapsuleData cap, BodyFlags excludeFlags, UIntPtr entityId, Intersection[] intersections);

		// Token: 0x0600046F RID: 1135
		[EngineMethod("invalidate_terrain_physics_materials", false, null, false)]
		void InvalidateTerrainPhysicsMaterials(UIntPtr scenePointer);

		// Token: 0x06000470 RID: 1136
		[EngineMethod("read", false, null, false)]
		void Read(UIntPtr scenePointer, string sceneName, ref SceneInitializationData initData, string forcedAtmoName);

		// Token: 0x06000471 RID: 1137
		[EngineMethod("read_in_module", false, null, false)]
		void ReadInModule(UIntPtr scenePointer, string sceneName, string moduleId, ref SceneInitializationData initData, string forcedAtmoName);

		// Token: 0x06000472 RID: 1138
		[EngineMethod("read_and_calculate_initial_camera", false, null, false)]
		void ReadAndCalculateInitialCamera(UIntPtr scenePointer, ref MatrixFrame outFrame);

		// Token: 0x06000473 RID: 1139
		[EngineMethod("optimize_scene", false, null, false)]
		void OptimizeScene(UIntPtr scenePointer, bool optimizeFlora, bool optimizeOro);

		// Token: 0x06000474 RID: 1140
		[EngineMethod("get_terrain_height", false, null, false)]
		float GetTerrainHeight(UIntPtr scenePointer, Vec2 position, bool checkHoles);

		// Token: 0x06000475 RID: 1141
		[EngineMethod("get_normal_at", false, null, false)]
		Vec3 GetNormalAt(UIntPtr scenePointer, Vec2 position);

		// Token: 0x06000476 RID: 1142
		[EngineMethod("has_terrain_heightmap", false, null, false)]
		bool HasTerrainHeightmap(UIntPtr scenePointer);

		// Token: 0x06000477 RID: 1143
		[EngineMethod("contains_terrain", false, null, false)]
		bool ContainsTerrain(UIntPtr scenePointer);

		// Token: 0x06000478 RID: 1144
		[EngineMethod("set_dof_focus", false, null, false)]
		void SetDofFocus(UIntPtr scenePointer, float dofFocus);

		// Token: 0x06000479 RID: 1145
		[EngineMethod("set_dof_params", false, null, false)]
		void SetDofParams(UIntPtr scenePointer, float dofFocusStart, float dofFocusEnd, bool isVignetteOn);

		// Token: 0x0600047A RID: 1146
		[EngineMethod("get_last_final_render_camera_position", false, null, false)]
		Vec3 GetLastFinalRenderCameraPosition(UIntPtr scenePointer);

		// Token: 0x0600047B RID: 1147
		[EngineMethod("get_last_final_render_camera_frame", false, null, false)]
		void GetLastFinalRenderCameraFrame(UIntPtr scenePointer, ref MatrixFrame outFrame);

		// Token: 0x0600047C RID: 1148
		[EngineMethod("get_time_of_day", false, null, false)]
		float GetTimeOfDay(UIntPtr scenePointer);

		// Token: 0x0600047D RID: 1149
		[EngineMethod("set_time_of_day", false, null, false)]
		void SetTimeOfDay(UIntPtr scenePointer, float value);

		// Token: 0x0600047E RID: 1150
		[EngineMethod("is_atmosphere_indoor", false, null, false)]
		bool IsAtmosphereIndoor(UIntPtr scenePointer);

		// Token: 0x0600047F RID: 1151
		[EngineMethod("set_color_grade_blend", false, null, false)]
		void SetColorGradeBlend(UIntPtr scenePointer, string texture1, string texture2, float alpha);

		// Token: 0x06000480 RID: 1152
		[EngineMethod("preload_for_rendering", false, null, false)]
		void PreloadForRendering(UIntPtr scenePointer);

		// Token: 0x06000481 RID: 1153
		[EngineMethod("create_dynamic_rain_texture", false, null, false)]
		void CreateDynamicRainTexture(UIntPtr scenePointer, int w, int h);

		// Token: 0x06000482 RID: 1154
		[EngineMethod("resume_scene_sounds", false, null, false)]
		void ResumeSceneSounds(UIntPtr scenePointer);

		// Token: 0x06000483 RID: 1155
		[EngineMethod("finish_scene_sounds", false, null, false)]
		void FinishSceneSounds(UIntPtr scenePointer);

		// Token: 0x06000484 RID: 1156
		[EngineMethod("pause_scene_sounds", false, null, false)]
		void PauseSceneSounds(UIntPtr scenePointer);

		// Token: 0x06000485 RID: 1157
		[EngineMethod("get_ground_height_at_position", false, null, false)]
		float GetGroundHeightAtPosition(UIntPtr scenePointer, Vec3 position, uint excludeFlags);

		// Token: 0x06000486 RID: 1158
		[EngineMethod("get_ground_height_and_normal_at_position", false, null, false)]
		float GetGroundHeightAndNormalAtPosition(UIntPtr scenePointer, Vec3 position, ref Vec3 normal, uint excludeFlags);

		// Token: 0x06000487 RID: 1159
		[EngineMethod("get_ground_height_and_body_flags_at_position", false, null, false)]
		float GetGroundHeightAndBodyFlagsAtPosition(UIntPtr scenePointer, Vec3 position, out BodyFlags contactPointFlags, BodyFlags excludeFlags);

		// Token: 0x06000488 RID: 1160
		[EngineMethod("check_point_can_see_point", false, null, false)]
		bool CheckPointCanSeePoint(UIntPtr scenePointer, Vec3 sourcePoint, Vec3 targetPoint, float distanceToCheck);

		// Token: 0x06000489 RID: 1161
		[EngineMethod("ray_cast_for_closest_entity_or_terrain", false, null, false)]
		bool RayCastForClosestEntityOrTerrain(UIntPtr scenePointer, in Vec3 sourcePoint, in Vec3 targetPoint, float rayThickness, ref float collisionDistance, ref Vec3 closestPoint, ref UIntPtr entityIndex, BodyFlags bodyExcludeFlags, bool isFixedWorld);

		// Token: 0x0600048A RID: 1162
		[EngineMethod("focus_ray_cast_for_fixed_physics", false, null, false)]
		bool FocusRayCastForFixedPhysics(UIntPtr scenePointer, in Vec3 sourcePoint, in Vec3 targetPoint, float rayThickness, ref float collisionDistance, ref Vec3 closestPoint, ref UIntPtr entityIndex, BodyFlags bodyExcludeFlags, bool isFixedWorld);

		// Token: 0x0600048B RID: 1163
		[EngineMethod("ray_cast_for_ramming", false, null, false)]
		bool RayCastForRamming(UIntPtr scenePointer, in Vec3 sourcePoint, in Vec3 targetPoint, float rayThickness, ref float collisionDistance, ref Vec3 intersectionPoint, ref UIntPtr intersectedEntityIndex, BodyFlags bodyExcludeFlags, BodyFlags bodyIncludeFlags, UIntPtr ignoredEntityPointer);

		// Token: 0x0600048C RID: 1164
		[EngineMethod("ray_cast_for_closest_entity_or_terrain_ignore_entity", false, null, false)]
		bool RayCastForClosestEntityOrTerrainIgnoreEntity(UIntPtr scenePointer, in Vec3 sourcePoint, in Vec3 targetPoint, float rayThickness, ref float collisionDistance, ref Vec3 closestPoint, ref UIntPtr entityIndex, BodyFlags bodyExcludeFlags, UIntPtr ignoredEntityPointer);

		// Token: 0x0600048D RID: 1165
		[EngineMethod("box_cast_only_for_camera", false, null, false)]
		bool BoxCastOnlyForCamera(UIntPtr scenePointer, Vec3[] boxPoints, in Vec3 centerPoint, in Vec3 dir, float distance, UIntPtr ignoredEntityPointer, ref float collisionDistance, ref Vec3 closestPoint, ref UIntPtr entityPointer, BodyFlags bodyExcludeFlags);

		// Token: 0x0600048E RID: 1166
		[EngineMethod("mark_faces_with_id_as_ladder", false, null, false)]
		void MarkFacesWithIdAsLadder(UIntPtr scenePointer, int faceGroupId, bool isLadder);

		// Token: 0x0600048F RID: 1167
		[EngineMethod("box_cast", false, null, false)]
		bool BoxCast(UIntPtr scenePointer, ref Vec3 boxPointBegin, ref Vec3 boxPointEnd, ref Vec3 dir, float distance, ref float collisionDistance, ref Vec3 closestPoint, ref UIntPtr entityIndex, BodyFlags bodyExcludeFlags);

		// Token: 0x06000490 RID: 1168
		[EngineMethod("set_ability_of_faces_with_id", false, null, false)]
		int SetAbilityOfFacesWithId(UIntPtr scenePointer, int faceGroupId, bool isEnabled);

		// Token: 0x06000491 RID: 1169
		[EngineMethod("swap_face_connections_with_id", false, null, false)]
		bool SwapFaceConnectionsWithId(UIntPtr scenePointer, int hubFaceGroupID, int toBeSeparatedFaceGroupId, int toBeMergedFaceGroupId, bool canFail);

		// Token: 0x06000492 RID: 1170
		[EngineMethod("merge_faces_with_id", false, null, false)]
		void MergeFacesWithId(UIntPtr scenePointer, int faceGroupId0, int faceGroupId1, int newFaceGroupId);

		// Token: 0x06000493 RID: 1171
		[EngineMethod("separate_faces_with_id", false, null, false)]
		void SeparateFacesWithId(UIntPtr scenePointer, int faceGroupId0, int faceGroupId1);

		// Token: 0x06000494 RID: 1172
		[EngineMethod("is_any_face_with_id", false, null, false)]
		bool IsAnyFaceWithId(UIntPtr scenePointer, int faceGroupId);

		// Token: 0x06000495 RID: 1173
		[EngineMethod("load_nav_mesh_prefab", false, null, false)]
		void LoadNavMeshPrefab(UIntPtr scenePointer, string navMeshPrefabName, int navMeshGroupIdShift);

		// Token: 0x06000496 RID: 1174
		[EngineMethod("load_nav_mesh_prefab_with_frame", false, null, false)]
		void LoadNavMeshPrefabWithFrame(UIntPtr scenePointer, string navMeshPrefabName, MatrixFrame frame);

		// Token: 0x06000497 RID: 1175
		[EngineMethod("save_nav_mesh_prefab_with_frame", false, null, false)]
		void SaveNavMeshPrefabWithFrame(UIntPtr scenePointer, string navMeshPrefabName, MatrixFrame frame);

		// Token: 0x06000498 RID: 1176
		[EngineMethod("set_nav_mesh_region_map", false, null, false)]
		void SetNavMeshRegionMap(UIntPtr scenePointer, bool[] regionMap, int regionMapSize);

		// Token: 0x06000499 RID: 1177
		[EngineMethod("get_navigation_mesh_for_position", false, null, false)]
		UIntPtr GetNavigationMeshForPosition(UIntPtr scenePointer, in Vec3 position, ref int faceGroupId, float heightDifferenceLimit, bool excludeDynamicNavigationMeshes);

		// Token: 0x0600049A RID: 1178
		[EngineMethod("get_nearest_navigation_mesh_for_position", false, null, false)]
		UIntPtr GetNearestNavigationMeshForPosition(UIntPtr scenePointer, in Vec3 position, float heightDifferenceLimit, bool excludeDynamicNavigationMeshes);

		// Token: 0x0600049B RID: 1179
		[EngineMethod("get_path_distance_between_positions", false, null, false)]
		bool GetPathDistanceBetweenPositions(UIntPtr scenePointer, ref WorldPosition position, ref WorldPosition destination, float agentRadius, ref float pathLength);

		// Token: 0x0600049C RID: 1180
		[EngineMethod("is_line_to_point_clear", false, null, false)]
		bool IsLineToPointClear(UIntPtr scenePointer, int startingFace, Vec2 position, Vec2 destination, float agentRadius);

		// Token: 0x0600049D RID: 1181
		[EngineMethod("is_line_to_point_clear2", false, null, false)]
		bool IsLineToPointClear2(UIntPtr scenePointer, UIntPtr startingFace, Vec2 position, Vec2 destination, float agentRadius);

		// Token: 0x0600049E RID: 1182
		[EngineMethod("get_last_position_on_nav_mesh_face_for_point_and_direction", false, null, false)]
		Vec2 GetLastPositionOnNavMeshFaceForPointAndDirection(UIntPtr scenePointer, in PathFaceRecord record, Vec2 position, Vec2 direction);

		// Token: 0x0600049F RID: 1183
		[EngineMethod("get_last_point_on_navigation_mesh_from_position_to_destination", false, null, false)]
		Vec2 GetLastPointOnNavigationMeshFromPositionToDestination(UIntPtr scenePointer, int startingFace, Vec2 position, Vec2 destination, int[] exclusionGroupIds, int exclusionGroupIdsCount);

		// Token: 0x060004A0 RID: 1184
		[EngineMethod("get_last_point_on_navigation_mesh_from_world_position_to_destination", false, null, false)]
		Vec3 GetLastPointOnNavigationMeshFromWorldPositionToDestination(UIntPtr scenePointer, ref WorldPosition position, Vec2 destination);

		// Token: 0x060004A1 RID: 1185
		[EngineMethod("does_path_exist_between_positions", false, null, false)]
		bool DoesPathExistBetweenPositions(UIntPtr scenePointer, WorldPosition position, WorldPosition destination);

		// Token: 0x060004A2 RID: 1186
		[EngineMethod("does_path_exist_between_faces", false, null, false)]
		bool DoesPathExistBetweenFaces(UIntPtr scenePointer, int firstNavMeshFace, int secondNavMeshFace, bool ignoreDisabled);

		// Token: 0x060004A3 RID: 1187
		[EngineMethod("set_landscape_rain_mask_data", false, null, false)]
		void SetLandscapeRainMaskData(UIntPtr scenePointer, byte[] data);

		// Token: 0x060004A4 RID: 1188
		[EngineMethod("ensure_postfx_system", false, null, false)]
		void EnsurePostfxSystem(UIntPtr scenePointer);

		// Token: 0x060004A5 RID: 1189
		[EngineMethod("set_bloom", false, null, false)]
		void SetBloom(UIntPtr scenePointer, bool mode);

		// Token: 0x060004A6 RID: 1190
		[EngineMethod("set_dof_mode", false, null, false)]
		void SetDofMode(UIntPtr scenePointer, bool mode);

		// Token: 0x060004A7 RID: 1191
		[EngineMethod("set_occlusion_mode", false, null, false)]
		void SetOcclusionMode(UIntPtr scenePointer, bool mode);

		// Token: 0x060004A8 RID: 1192
		[EngineMethod("set_external_injection_texture", false, null, false)]
		void SetExternalInjectionTexture(UIntPtr scenePointer, UIntPtr texturePointer);

		// Token: 0x060004A9 RID: 1193
		[EngineMethod("set_sunshaft_mode", false, null, false)]
		void SetSunshaftMode(UIntPtr scenePointer, bool mode);

		// Token: 0x060004AA RID: 1194
		[EngineMethod("get_sun_direction", false, null, false)]
		Vec3 GetSunDirection(UIntPtr scenePointer);

		// Token: 0x060004AB RID: 1195
		[EngineMethod("get_north_angle", false, null, false)]
		float GetNorthAngle(UIntPtr scenePointer);

		// Token: 0x060004AC RID: 1196
		[EngineMethod("get_terrain_min_max_height", false, null, false)]
		bool GetTerrainMinMaxHeight(Scene scene, ref float min, ref float max);

		// Token: 0x060004AD RID: 1197
		[EngineMethod("get_physics_min_max", false, null, false)]
		void GetPhysicsMinMax(UIntPtr scenePointer, ref Vec3 min_max);

		// Token: 0x060004AE RID: 1198
		[EngineMethod("is_editor_scene", false, null, false)]
		bool IsEditorScene(UIntPtr scenePointer);

		// Token: 0x060004AF RID: 1199
		[EngineMethod("set_motionblur_mode", false, null, false)]
		void SetMotionBlurMode(UIntPtr scenePointer, bool mode);

		// Token: 0x060004B0 RID: 1200
		[EngineMethod("set_antialiasing_mode", false, null, false)]
		void SetAntialiasingMode(UIntPtr scenePointer, bool mode);

		// Token: 0x060004B1 RID: 1201
		[EngineMethod("set_dlss_mode", false, null, false)]
		void SetDLSSMode(UIntPtr scenePointer, bool mode);

		// Token: 0x060004B2 RID: 1202
		[EngineMethod("get_path_with_name", false, null, false)]
		Path GetPathWithName(UIntPtr scenePointer, string name);

		// Token: 0x060004B3 RID: 1203
		[EngineMethod("get_soft_boundary_vertex_count", false, null, false)]
		int GetSoftBoundaryVertexCount(UIntPtr scenePointer);

		// Token: 0x060004B4 RID: 1204
		[EngineMethod("delete_path_with_name", false, null, false)]
		void DeletePathWithName(UIntPtr scenePointer, string name);

		// Token: 0x060004B5 RID: 1205
		[EngineMethod("get_hard_boundary_vertex_count", false, null, false)]
		int GetHardBoundaryVertexCount(UIntPtr scenePointer);

		// Token: 0x060004B6 RID: 1206
		[EngineMethod("get_hard_boundary_vertex", false, null, false)]
		Vec2 GetHardBoundaryVertex(UIntPtr scenePointer, int index);

		// Token: 0x060004B7 RID: 1207
		[EngineMethod("add_path", false, null, false)]
		void AddPath(UIntPtr scenePointer, string name);

		// Token: 0x060004B8 RID: 1208
		[EngineMethod("get_soft_boundary_vertex", false, null, false)]
		Vec2 GetSoftBoundaryVertex(UIntPtr scenePointer, int index);

		// Token: 0x060004B9 RID: 1209
		[EngineMethod("add_path_point", false, null, false)]
		void AddPathPoint(UIntPtr scenePointer, string name, ref MatrixFrame frame);

		// Token: 0x060004BA RID: 1210
		[EngineMethod("get_bounding_box", false, null, false)]
		void GetBoundingBox(UIntPtr scenePointer, ref Vec3 min, ref Vec3 max);

		// Token: 0x060004BB RID: 1211
		[EngineMethod("get_scene_limits", false, null, false)]
		void GetSceneLimits(UIntPtr scenePointer, ref Vec3 min, ref Vec3 max);

		// Token: 0x060004BC RID: 1212
		[EngineMethod("set_name", false, null, false)]
		void SetName(UIntPtr scenePointer, string name);

		// Token: 0x060004BD RID: 1213
		[EngineMethod("get_name", false, null, false)]
		string GetName(UIntPtr scenePointer);

		// Token: 0x060004BE RID: 1214
		[EngineMethod("get_module_path", false, null, false)]
		string GetModulePath(UIntPtr scenePointer);

		// Token: 0x060004BF RID: 1215
		[EngineMethod("set_time_speed", false, null, false)]
		void SetTimeSpeed(UIntPtr scenePointer, float value);

		// Token: 0x060004C0 RID: 1216
		[EngineMethod("get_time_speed", false, null, false)]
		float GetTimeSpeed(UIntPtr scenePointer);

		// Token: 0x060004C1 RID: 1217
		[EngineMethod("set_owner_thread", false, null, false)]
		void SetOwnerThread(UIntPtr scenePointer);

		// Token: 0x060004C2 RID: 1218
		[EngineMethod("get_number_of_path_with_name_prefix", false, null, false)]
		int GetNumberOfPathsWithNamePrefix(UIntPtr ptr, string prefix);

		// Token: 0x060004C3 RID: 1219
		[EngineMethod("get_paths_with_name_prefix", false, null, false)]
		void GetPathsWithNamePrefix(UIntPtr ptr, UIntPtr[] points, string prefix);

		// Token: 0x060004C4 RID: 1220
		[EngineMethod("set_use_constant_time", false, null, false)]
		void SetUseConstantTime(UIntPtr ptr, bool value);

		// Token: 0x060004C5 RID: 1221
		[EngineMethod("set_play_sound_events_after_render_ready", false, null, false)]
		void SetPlaySoundEventsAfterReadyToRender(UIntPtr ptr, bool value);

		// Token: 0x060004C6 RID: 1222
		[EngineMethod("disable_static_shadows", false, null, false)]
		void DisableStaticShadows(UIntPtr ptr, bool value);

		// Token: 0x060004C7 RID: 1223
		[EngineMethod("get_skybox_mesh", false, null, false)]
		Mesh GetSkyboxMesh(UIntPtr ptr);

		// Token: 0x060004C8 RID: 1224
		[EngineMethod("set_atmosphere_with_name", false, null, false)]
		void SetAtmosphereWithName(UIntPtr ptr, string name);

		// Token: 0x060004C9 RID: 1225
		[EngineMethod("fill_entity_with_hard_border_physics_barrier", false, null, false)]
		void FillEntityWithHardBorderPhysicsBarrier(UIntPtr scenePointer, UIntPtr entityPointer);

		// Token: 0x060004CA RID: 1226
		[EngineMethod("clear_decals", false, null, false)]
		void ClearDecals(UIntPtr scenePointer);

		// Token: 0x060004CB RID: 1227
		[EngineMethod("set_photo_atmosphere_via_tod", false, null, false)]
		void SetPhotoAtmosphereViaTod(UIntPtr scenePointer, float tod, bool withStorm);

		// Token: 0x060004CC RID: 1228
		[EngineMethod("get_scripted_entity_count", false, null, false)]
		int GetScriptedEntityCount(UIntPtr scenePointer);

		// Token: 0x060004CD RID: 1229
		[EngineMethod("get_scripted_entity", false, null, false)]
		GameEntity GetScriptedEntity(UIntPtr scenePointer, int index);

		// Token: 0x060004CE RID: 1230
		[EngineMethod("world_position_validate_z", false, null, false)]
		void WorldPositionValidateZ(ref WorldPosition position, int minimumValidityState);

		// Token: 0x060004CF RID: 1231
		[EngineMethod("world_position_compute_nearest_nav_mesh", false, null, false)]
		void WorldPositionComputeNearestNavMesh(ref WorldPosition position);

		// Token: 0x060004D0 RID: 1232
		[EngineMethod("get_node_data_count", false, null, false)]
		int GetNodeDataCount(Scene scene, int xIndex, int yIndex);

		// Token: 0x060004D1 RID: 1233
		[EngineMethod("fill_terrain_height_data", false, null, false)]
		void FillTerrainHeightData(Scene scene, int xIndex, int yIndex, float[] heightArray);

		// Token: 0x060004D2 RID: 1234
		[EngineMethod("fill_terrain_physics_material_index_data", false, null, false)]
		void FillTerrainPhysicsMaterialIndexData(Scene scene, int xIndex, int yIndex, short[] materialIndexArray);

		// Token: 0x060004D3 RID: 1235
		[EngineMethod("get_terrain_data", false, null, false)]
		void GetTerrainData(Scene scene, out Vec2i nodeDimension, out float nodeSize, out int layerCount, out int layerVersion);

		// Token: 0x060004D4 RID: 1236
		[EngineMethod("get_terrain_node_data", false, null, false)]
		void GetTerrainNodeData(Scene scene, int xIndex, int yIndex, out int vertexCountAlongAxis, out float quadLength, out float minHeight, out float maxHeight);

		// Token: 0x060004D5 RID: 1237
		[EngineMethod("add_always_rendered_skeleton", false, null, false)]
		void AddAlwaysRenderedSkeleton(UIntPtr scenePointer, UIntPtr skeletonPointer);

		// Token: 0x060004D6 RID: 1238
		[EngineMethod("remove_always_rendered_skeleton", false, null, false)]
		void RemoveAlwaysRenderedSkeleton(UIntPtr scenePointer, UIntPtr skeletonPointer);

		// Token: 0x060004D7 RID: 1239
		[EngineMethod("is_position_on_a_dynamic_nav_mesh", false, null, false)]
		bool IsPositionOnADynamicNavMesh(UIntPtr scenePointer, Vec3 position);

		// Token: 0x060004D8 RID: 1240
		[EngineMethod("wait_water_renderer_cpu_simulation", false, null, false)]
		void WaitWaterRendererCPUSimulation(UIntPtr scenePointer);

		// Token: 0x060004D9 RID: 1241
		[EngineMethod("enable_inclusive_async_physx", false, null, false)]
		void EnableInclusiveAsyncPhysx(UIntPtr scenePointer);

		// Token: 0x060004DA RID: 1242
		[EngineMethod("ensure_water_wake_renderer", false, null, false)]
		void EnsureWaterWakeRenderer(UIntPtr scenePointer);

		// Token: 0x060004DB RID: 1243
		[EngineMethod("set_water_wake_world_size", false, null, false)]
		void SetWaterWakeWorldSize(UIntPtr scenePointer, float worldSize, float eraseFactor);

		// Token: 0x060004DC RID: 1244
		[EngineMethod("set_water_wake_camera_offset", false, null, false)]
		void SetWaterWakeCameraOffset(UIntPtr scenePointer, float cameraOffset);

		// Token: 0x060004DD RID: 1245
		[EngineMethod("delete_water_wake_renderer", false, null, false)]
		void DeleteWaterWakeRenderer(UIntPtr scenePointer);

		// Token: 0x060004DE RID: 1246
		[EngineMethod("scene_had_water_wake_renderer", false, null, false)]
		bool SceneHadWaterWakeRenderer(UIntPtr scenePointer);

		// Token: 0x060004DF RID: 1247
		[EngineMethod("tick_wake", false, null, false)]
		void TickWake(UIntPtr scenePointer, float dt);

		// Token: 0x060004E0 RID: 1248
		[EngineMethod("set_do_not_add_entities_to_tick_list", false, null, false)]
		void SetDoNotAddEntitiesToTickList(UIntPtr scenePointer, bool value);

		// Token: 0x060004E1 RID: 1249
		[EngineMethod("set_dont_load_invisible_entities", false, null, false)]
		void SetDontLoadInvisibleEntities(UIntPtr scenePointer, bool value);

		// Token: 0x060004E2 RID: 1250
		[EngineMethod("set_uses_delete_later_system", false, null, false)]
		void SetUsesDeleteLaterSystem(UIntPtr scenePointer, bool value);

		// Token: 0x060004E3 RID: 1251
		[EngineMethod("clear_current_frame_tick_entities", false, null, false)]
		void ClearCurrentFrameTickEntities(UIntPtr scenePointer);

		// Token: 0x060004E4 RID: 1252
		[EngineMethod("find_closest_exit_position_for_position_on_a_boundary_face", false, null, false)]
		Vec2 FindClosestExitPositionForPositionOnABoundaryFace(UIntPtr scenePointer, Vec3 position, UIntPtr boundaryNavMeshFacePointer);
	}
}
