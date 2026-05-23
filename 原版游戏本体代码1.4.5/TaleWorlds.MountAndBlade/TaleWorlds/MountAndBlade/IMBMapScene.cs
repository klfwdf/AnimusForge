using System;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade;

[ScriptingInterfaceBase]
internal interface IMBMapScene
{
	[EngineMethod("get_accessible_point_near_position", false, null, false)]
	Vec3 GetAccessiblePointNearPosition(UIntPtr scenePointer, Vec2 position, bool isRegionMap0, float radius);

	[EngineMethod("get_nearest_nav_mesh_face_center_position_for_position", false, null, false)]
	Vec2 GetNearestFaceCenterPositionForPosition(UIntPtr scenePointer, Vec3 position, bool isRegionMap0, int[] excludedFaceIds, int excludedFaceIdCount);

	[EngineMethod("get_nearest_nav_mesh_face_center_position_between_regions_using_path", false, null, false)]
	Vec2 GetNearestFaceCenterForPositionWithPath(UIntPtr scenePointer, int startFaceIndex, bool targetRegionMap0, float distMax, int[] excludedFaceIds, int excludedFaceIdCount);

	[EngineMethod("remove_zero_corner_bodies", false, null, false)]
	void RemoveZeroCornerBodies(UIntPtr scenePointer);

	[EngineMethod("load_atmosphere_data", false, null, false)]
	void LoadAtmosphereData(UIntPtr scenePointer);

	[EngineMethod("tick_step_sound", false, null, false)]
	void TickStepSound(UIntPtr scenePointer, UIntPtr visualsPointer, int faceIndexTerrainType, TerrainTypeSoundSlot soundType, int partySize);

	[EngineMethod("tick_ambient_sounds", false, null, false)]
	void TickAmbientSounds(UIntPtr scenePointer, int terrainType);

	[EngineMethod("tick_visuals", false, null, false)]
	void TickVisuals(UIntPtr scenePointer, float tod, UIntPtr[] ticked_map_meshes, int tickedMapMeshesCount);

	[EngineMethod("validate_terrain_sound_ids", false, null, false)]
	void ValidateTerrainSoundIds();

	[EngineMethod("set_political_color", false, null, false)]
	void SetPoliticalColor(UIntPtr scenePointer, string value);

	[EngineMethod("set_frame_for_atmosphere", false, null, false)]
	void SetFrameForAtmosphere(UIntPtr scenePointer, float tod, float cameraElevation, bool forceLoadTextures);

	[EngineMethod("get_color_grade_grid_data", false, null, false)]
	void GetColorGradeGridData(UIntPtr scenePointer, byte[] snowData, string textureName);

	[EngineMethod("get_battle_scene_index_map_resolution", false, null, false)]
	void GetBattleSceneIndexMapResolution(UIntPtr scenePointer, ref int width, ref int height);

	[EngineMethod("get_battle_scene_index_map", false, null, false)]
	void GetBattleSceneIndexMap(UIntPtr scenePointer, byte[] indexData);

	[EngineMethod("set_terrain_dynamic_params", false, null, false)]
	void SetTerrainDynamicParams(UIntPtr scenePointer, Vec3 dynamic_params);

	[EngineMethod("set_season_time_factor", false, null, false)]
	void SetSeasonTimeFactor(UIntPtr scenePointer, float seasonTimeFactor);

	[EngineMethod("get_season_time_factor", false, null, false)]
	float GetSeasonTimeFactor(UIntPtr scenePointer);

	[EngineMethod("get_mouse_visible", false, null, false)]
	bool GetMouseVisible();

	[EngineMethod("send_mouse_key_down_event", false, null, false)]
	void SendMouseKeyEvent(int keyId, bool isDown);

	[EngineMethod("set_mouse_visible", false, null, false)]
	void SetMouseVisible(bool value);

	[EngineMethod("set_mouse_pos", false, null, false)]
	void SetMousePos(int posX, int posY);
}
