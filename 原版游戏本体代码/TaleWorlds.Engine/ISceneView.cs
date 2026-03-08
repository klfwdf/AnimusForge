using System;
using TaleWorlds.Library;

namespace TaleWorlds.Engine
{
	// Token: 0x0200002F RID: 47
	[ApplicationInterfaceBase]
	internal interface ISceneView
	{
		// Token: 0x06000505 RID: 1285
		[EngineMethod("create_scene_view", false, null, false)]
		SceneView CreateSceneView();

		// Token: 0x06000506 RID: 1286
		[EngineMethod("set_scene", false, null, false)]
		void SetScene(UIntPtr ptr, UIntPtr scenePtr);

		// Token: 0x06000507 RID: 1287
		[EngineMethod("set_accept_global_debug_render_objects", false, null, false)]
		void SetAcceptGlobalDebugRenderObjects(UIntPtr ptr, bool value);

		// Token: 0x06000508 RID: 1288
		[EngineMethod("set_render_with_postfx", false, null, false)]
		void SetRenderWithPostfx(UIntPtr ptr, bool value);

		// Token: 0x06000509 RID: 1289
		[EngineMethod("set_force_shader_compilation", false, null, false)]
		void SetForceShaderCompilation(UIntPtr ptr, bool value);

		// Token: 0x0600050A RID: 1290
		[EngineMethod("check_scene_ready_to_render", false, null, false)]
		bool CheckSceneReadyToRender(UIntPtr ptr);

		// Token: 0x0600050B RID: 1291
		[EngineMethod("set_do_quick_exposure", false, null, false)]
		void SetDoQuickExposure(UIntPtr ptr, bool value);

		// Token: 0x0600050C RID: 1292
		[EngineMethod("set_postfx_config_params", false, null, false)]
		void SetPostfxConfigParams(UIntPtr ptr, int value);

		// Token: 0x0600050D RID: 1293
		[EngineMethod("set_camera", false, null, false)]
		void SetCamera(UIntPtr ptr, UIntPtr cameraPtr);

		// Token: 0x0600050E RID: 1294
		[EngineMethod("set_resolution_scaling", false, null, false)]
		void SetResolutionScaling(UIntPtr ptr, bool value);

		// Token: 0x0600050F RID: 1295
		[EngineMethod("set_postfx_from_config", false, null, false)]
		void SetPostfxFromConfig(UIntPtr ptr);

		// Token: 0x06000510 RID: 1296
		[EngineMethod("world_point_to_screen_point", false, null, false)]
		Vec2 WorldPointToScreenPoint(UIntPtr ptr, Vec3 position);

		// Token: 0x06000511 RID: 1297
		[EngineMethod("screen_point_to_viewport_point", false, null, false)]
		Vec2 ScreenPointToViewportPoint(UIntPtr ptr, float position_x, float position_y);

		// Token: 0x06000512 RID: 1298
		[EngineMethod("projected_mouse_position_on_ground", false, null, false)]
		bool ProjectedMousePositionOnGround(UIntPtr pointer, out Vec3 groundPosition, out Vec3 groundNormal, bool mouseVisible, BodyFlags excludeBodyOwnerFlags, bool checkOccludedSurface);

		// Token: 0x06000513 RID: 1299
		[EngineMethod("projected_mouse_position_on_water", false, null, false)]
		bool ProjectedMousePositionOnWater(UIntPtr pointer, out Vec3 groundPosition, bool mouseVisible);

		// Token: 0x06000514 RID: 1300
		[EngineMethod("translate_mouse", false, null, false)]
		void TranslateMouse(UIntPtr pointer, ref Vec3 worldMouseNear, ref Vec3 worldMouseFar, float maxDistance);

		// Token: 0x06000515 RID: 1301
		[EngineMethod("set_scene_uses_skybox", false, null, false)]
		void SetSceneUsesSkybox(UIntPtr pointer, bool value);

		// Token: 0x06000516 RID: 1302
		[EngineMethod("set_scene_uses_shadows", false, null, false)]
		void SetSceneUsesShadows(UIntPtr pointer, bool value);

		// Token: 0x06000517 RID: 1303
		[EngineMethod("set_scene_uses_contour", false, null, false)]
		void SetSceneUsesContour(UIntPtr pointer, bool value);

		// Token: 0x06000518 RID: 1304
		[EngineMethod("do_not_clear", false, null, false)]
		void DoNotClear(UIntPtr pointer, bool value);

		// Token: 0x06000519 RID: 1305
		[EngineMethod("add_clear_task", false, null, false)]
		void AddClearTask(UIntPtr ptr, bool clearOnlySceneview);

		// Token: 0x0600051A RID: 1306
		[EngineMethod("ready_to_render", false, null, false)]
		bool ReadyToRender(UIntPtr pointer);

		// Token: 0x0600051B RID: 1307
		[EngineMethod("set_clear_and_disable_after_succesfull_render", false, null, false)]
		void SetClearAndDisableAfterSucessfullRender(UIntPtr pointer, bool value);

		// Token: 0x0600051C RID: 1308
		[EngineMethod("set_clear_gbuffer", false, null, false)]
		void SetClearGbuffer(UIntPtr pointer, bool value);

		// Token: 0x0600051D RID: 1309
		[EngineMethod("set_shadowmap_resolution_multiplier", false, null, false)]
		void SetShadowmapResolutionMultiplier(UIntPtr pointer, float value);

		// Token: 0x0600051E RID: 1310
		[EngineMethod("set_pointlight_resolution_multiplier", false, null, false)]
		void SetPointlightResolutionMultiplier(UIntPtr pointer, float value);

		// Token: 0x0600051F RID: 1311
		[EngineMethod("set_clean_screen_until_loading_done", false, null, false)]
		void SetCleanScreenUntilLoadingDone(UIntPtr pointer, bool value);

		// Token: 0x06000520 RID: 1312
		[EngineMethod("clear_all", false, null, false)]
		void ClearAll(UIntPtr pointer, bool clear_scene, bool remove_terrain);

		// Token: 0x06000521 RID: 1313
		[EngineMethod("set_focused_shadowmap", false, null, false)]
		void SetFocusedShadowmap(UIntPtr ptr, bool enable, ref Vec3 center, float radius);

		// Token: 0x06000522 RID: 1314
		[EngineMethod("get_scene", false, null, false)]
		Scene GetScene(UIntPtr ptr);

		// Token: 0x06000523 RID: 1315
		[EngineMethod("ray_cast_for_closest_entity_or_terrain", false, null, false)]
		bool RayCastForClosestEntityOrTerrain(UIntPtr ptr, ref Vec3 sourcePoint, ref Vec3 targetPoint, float rayThickness, ref float collisionDistance, ref Vec3 closestPoint, ref UIntPtr entityIndex, BodyFlags bodyExcludeFlags);
	}
}
