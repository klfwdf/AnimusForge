using System;
using TaleWorlds.Library;

namespace TaleWorlds.Engine;

[ApplicationInterfaceBase]
internal interface ISceneView
{
	[EngineMethod("create_scene_view", false, null, false)]
	SceneView CreateSceneView();

	[EngineMethod("set_scene", false, null, false)]
	void SetScene(UIntPtr ptr, UIntPtr scenePtr);

	[EngineMethod("set_accept_global_debug_render_objects", false, null, false)]
	void SetAcceptGlobalDebugRenderObjects(UIntPtr ptr, bool value);

	[EngineMethod("set_render_with_postfx", false, null, false)]
	void SetRenderWithPostfx(UIntPtr ptr, bool value);

	[EngineMethod("set_force_shader_compilation", false, null, false)]
	void SetForceShaderCompilation(UIntPtr ptr, bool value);

	[EngineMethod("check_scene_ready_to_render", false, null, false)]
	bool CheckSceneReadyToRender(UIntPtr ptr);

	[EngineMethod("set_do_quick_exposure", false, null, false)]
	void SetDoQuickExposure(UIntPtr ptr, bool value);

	[EngineMethod("set_postfx_config_params", false, null, false)]
	void SetPostfxConfigParams(UIntPtr ptr, int value);

	[EngineMethod("set_camera", false, null, false)]
	void SetCamera(UIntPtr ptr, UIntPtr cameraPtr);

	[EngineMethod("set_resolution_scaling", false, null, false)]
	void SetResolutionScaling(UIntPtr ptr, bool value);

	[EngineMethod("set_postfx_from_config", false, null, false)]
	void SetPostfxFromConfig(UIntPtr ptr);

	[EngineMethod("world_point_to_screen_point", false, null, false)]
	Vec2 WorldPointToScreenPoint(UIntPtr ptr, Vec3 position);

	[EngineMethod("screen_point_to_viewport_point", false, null, false)]
	Vec2 ScreenPointToViewportPoint(UIntPtr ptr, float position_x, float position_y);

	[EngineMethod("projected_mouse_position_on_ground", false, null, false)]
	bool ProjectedMousePositionOnGround(UIntPtr pointer, out Vec3 groundPosition, out Vec3 groundNormal, bool mouseVisible, BodyFlags excludeBodyOwnerFlags, bool checkOccludedSurface);

	[EngineMethod("projected_mouse_position_on_water", false, null, false)]
	bool ProjectedMousePositionOnWater(UIntPtr pointer, out Vec3 groundPosition, bool mouseVisible);

	[EngineMethod("translate_mouse", false, null, false)]
	void TranslateMouse(UIntPtr pointer, ref Vec3 worldMouseNear, ref Vec3 worldMouseFar, float maxDistance);

	[EngineMethod("set_scene_uses_skybox", false, null, false)]
	void SetSceneUsesSkybox(UIntPtr pointer, bool value);

	[EngineMethod("set_scene_uses_shadows", false, null, false)]
	void SetSceneUsesShadows(UIntPtr pointer, bool value);

	[EngineMethod("set_scene_uses_contour", false, null, false)]
	void SetSceneUsesContour(UIntPtr pointer, bool value);

	[EngineMethod("do_not_clear", false, null, false)]
	void DoNotClear(UIntPtr pointer, bool value);

	[EngineMethod("add_clear_task", false, null, false)]
	void AddClearTask(UIntPtr ptr, bool clearOnlySceneview);

	[EngineMethod("ready_to_render", false, null, false)]
	bool ReadyToRender(UIntPtr pointer);

	[EngineMethod("set_clear_and_disable_after_succesfull_render", false, null, false)]
	void SetClearAndDisableAfterSucessfullRender(UIntPtr pointer, bool value);

	[EngineMethod("set_clear_gbuffer", false, null, false)]
	void SetClearGbuffer(UIntPtr pointer, bool value);

	[EngineMethod("set_shadowmap_resolution_multiplier", false, null, false)]
	void SetShadowmapResolutionMultiplier(UIntPtr pointer, float value);

	[EngineMethod("set_pointlight_resolution_multiplier", false, null, false)]
	void SetPointlightResolutionMultiplier(UIntPtr pointer, float value);

	[EngineMethod("set_clean_screen_until_loading_done", false, null, false)]
	void SetCleanScreenUntilLoadingDone(UIntPtr pointer, bool value);

	[EngineMethod("clear_all", false, null, false)]
	void ClearAll(UIntPtr pointer, bool clear_scene, bool remove_terrain);

	[EngineMethod("set_focused_shadowmap", false, null, false)]
	void SetFocusedShadowmap(UIntPtr ptr, bool enable, ref Vec3 center, float radius);

	[EngineMethod("get_scene", false, null, false)]
	Scene GetScene(UIntPtr ptr);

	[EngineMethod("ray_cast_for_closest_entity_or_terrain", false, null, false)]
	bool RayCastForClosestEntityOrTerrain(UIntPtr ptr, ref Vec3 sourcePoint, ref Vec3 targetPoint, float rayThickness, ref float collisionDistance, ref Vec3 closestPoint, ref UIntPtr entityIndex, BodyFlags bodyExcludeFlags);
}
