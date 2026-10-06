using System;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade;

[ScriptingInterfaceBase]
internal interface IMBEditor
{
	[EngineMethod("is_edit_mode", false, null, false)]
	bool IsEditMode();

	[EngineMethod("is_edit_mode_enabled", false, null, false)]
	bool IsEditModeEnabled();

	[EngineMethod("update_scene_tree", false, null, false)]
	void UpdateSceneTree(bool do_next_frame);

	[EngineMethod("is_entity_selected", false, null, false)]
	bool IsEntitySelected(UIntPtr entityId);

	[EngineMethod("add_editor_warning", false, null, false)]
	void AddEditorWarning(string msg);

	[EngineMethod("render_editor_mesh", false, null, false)]
	void RenderEditorMesh(UIntPtr metaMeshId, ref MatrixFrame frame);

	[EngineMethod("apply_delta_to_editor_camera", false, null, false)]
	void ApplyDeltaToEditorCamera(in Vec3 delta);

	[EngineMethod("enter_edit_mode", false, null, false)]
	void EnterEditMode(UIntPtr sceneWidgetPointer, ref MatrixFrame initialCameraFrame, float initialCameraElevation, float initialCameraBearing);

	[EngineMethod("tick_edit_mode", false, null, false)]
	void TickEditMode(float dt);

	[EngineMethod("leave_edit_mode", false, null, false)]
	void LeaveEditMode();

	[EngineMethod("enter_edit_mission_mode", false, null, false)]
	void EnterEditMissionMode(UIntPtr missionPointer);

	[EngineMethod("leave_edit_mission_mode", false, null, false)]
	void LeaveEditMissionMode();

	[EngineMethod("activate_scene_editor_presentation", false, null, false)]
	void ActivateSceneEditorPresentation();

	[EngineMethod("deactivate_scene_editor_presentation", false, null, false)]
	void DeactivateSceneEditorPresentation();

	[EngineMethod("tick_scene_editor_presentation", false, null, false)]
	void TickSceneEditorPresentation(float dt);

	[EngineMethod("get_editor_scene_view", false, null, false)]
	SceneView GetEditorSceneView();

	[EngineMethod("helpers_enabled", false, null, false)]
	bool HelpersEnabled();

	[EngineMethod("border_helpers_enabled", false, null, false)]
	bool BorderHelpersEnabled();

	[EngineMethod("zoom_to_position", false, null, false)]
	void ZoomToPosition(Vec3 pos);

	[EngineMethod("add_entity_warning", false, null, false)]
	void AddEntityWarning(UIntPtr entityId, string msg);

	[EngineMethod("add_nav_mesh_warning", false, null, false)]
	void AddNavMeshWarning(UIntPtr sceneId, in PathFaceRecord record, string msg);

	[EngineMethod("get_all_prefabs_and_child_with_tag", false, null, false)]
	string GetAllPrefabsAndChildWithTag(string tag);

	[EngineMethod("set_upgrade_level_visibility", false, null, false)]
	void SetUpgradeLevelVisibility(string cumulated_string);

	[EngineMethod("set_level_visibility", false, null, false)]
	void SetLevelVisibility(string cumulated_string);

	[EngineMethod("toggle_enable_editor_physics", false, null, false)]
	void ToggleEnableEditorPhysics();

	[EngineMethod("exit_edit_mode", false, null, false)]
	void ExitEditMode();

	[EngineMethod("is_replay_manager_recording", false, null, false)]
	bool IsReplayManagerRecording();

	[EngineMethod("is_replay_manager_rendering", false, null, false)]
	bool IsReplayManagerRendering();

	[EngineMethod("is_replay_manager_replaying", false, null, false)]
	bool IsReplayManagerReplaying();
}
