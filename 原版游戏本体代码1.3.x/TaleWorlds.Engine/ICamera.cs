using System;
using TaleWorlds.Library;

namespace TaleWorlds.Engine
{
	// Token: 0x02000026 RID: 38
	[ApplicationInterfaceBase]
	internal interface ICamera
	{
		// Token: 0x0600024A RID: 586
		[EngineMethod("release", false, null, false)]
		void Release(UIntPtr cameraPointer);

		// Token: 0x0600024B RID: 587
		[EngineMethod("set_entity", false, null, false)]
		void SetEntity(UIntPtr cameraPointer, UIntPtr entityId);

		// Token: 0x0600024C RID: 588
		[EngineMethod("get_entity", false, null, false)]
		GameEntity GetEntity(UIntPtr cameraPointer);

		// Token: 0x0600024D RID: 589
		[EngineMethod("create_camera", false, null, false)]
		Camera CreateCamera();

		// Token: 0x0600024E RID: 590
		[EngineMethod("release_camera_entity", false, null, false)]
		void ReleaseCameraEntity(UIntPtr cameraPointer);

		// Token: 0x0600024F RID: 591
		[EngineMethod("look_at", false, null, false)]
		void LookAt(UIntPtr cameraPointer, Vec3 position, Vec3 target, Vec3 upVector);

		// Token: 0x06000250 RID: 592
		[EngineMethod("screen_space_ray_projection", false, null, false)]
		void ScreenSpaceRayProjection(UIntPtr cameraPointer, Vec2 screenPosition, ref Vec3 rayBegin, ref Vec3 rayEnd);

		// Token: 0x06000251 RID: 593
		[EngineMethod("check_entity_visibility", false, null, false)]
		bool CheckEntityVisibility(UIntPtr cameraPointer, UIntPtr entityPointer);

		// Token: 0x06000252 RID: 594
		[EngineMethod("set_position", false, null, false)]
		void SetPosition(UIntPtr cameraPointer, Vec3 position);

		// Token: 0x06000253 RID: 595
		[EngineMethod("set_view_volume", false, null, false)]
		void SetViewVolume(UIntPtr cameraPointer, bool perspective, float dLeft, float dRight, float dBottom, float dTop, float dNear, float dFar);

		// Token: 0x06000254 RID: 596
		[EngineMethod("get_near_plane_points_static", false, null, false)]
		void GetNearPlanePointsStatic(ref MatrixFrame cameraFrame, float verticalFov, float aspectRatioXY, float newDNear, float newDFar, Vec3[] nearPlanePoints);

		// Token: 0x06000255 RID: 597
		[EngineMethod("get_near_plane_points", false, null, false)]
		void GetNearPlanePoints(UIntPtr cameraPointer, Vec3[] nearPlanePoints);

		// Token: 0x06000256 RID: 598
		[EngineMethod("set_fov_vertical", false, null, false)]
		void SetFovVertical(UIntPtr cameraPointer, float verticalFov, float aspectRatio, float newDNear, float newDFar);

		// Token: 0x06000257 RID: 599
		[EngineMethod("get_view_proj_matrix", false, null, false)]
		void GetViewProjMatrix(UIntPtr cameraPointer, ref MatrixFrame frame);

		// Token: 0x06000258 RID: 600
		[EngineMethod("set_fov_horizontal", false, null, false)]
		void SetFovHorizontal(UIntPtr cameraPointer, float horizontalFov, float aspectRatio, float newDNear, float newDFar);

		// Token: 0x06000259 RID: 601
		[EngineMethod("get_fov_vertical", false, null, false)]
		float GetFovVertical(UIntPtr cameraPointer);

		// Token: 0x0600025A RID: 602
		[EngineMethod("get_fov_horizontal", false, null, false)]
		float GetFovHorizontal(UIntPtr cameraPointer);

		// Token: 0x0600025B RID: 603
		[EngineMethod("get_aspect_ratio", false, null, false)]
		float GetAspectRatio(UIntPtr cameraPointer);

		// Token: 0x0600025C RID: 604
		[EngineMethod("fill_parameters_from", false, null, false)]
		void FillParametersFrom(UIntPtr cameraPointer, UIntPtr otherCameraPointer);

		// Token: 0x0600025D RID: 605
		[EngineMethod("render_frustrum", false, null, false)]
		void RenderFrustrum(UIntPtr cameraPointer);

		// Token: 0x0600025E RID: 606
		[EngineMethod("set_frame", false, null, false)]
		void SetFrame(UIntPtr cameraPointer, ref MatrixFrame frame);

		// Token: 0x0600025F RID: 607
		[EngineMethod("get_frame", false, null, false)]
		void GetFrame(UIntPtr cameraPointer, ref MatrixFrame outFrame);

		// Token: 0x06000260 RID: 608
		[EngineMethod("get_near", false, null, false)]
		float GetNear(UIntPtr cameraPointer);

		// Token: 0x06000261 RID: 609
		[EngineMethod("get_far", false, null, false)]
		float GetFar(UIntPtr cameraPointer);

		// Token: 0x06000262 RID: 610
		[EngineMethod("get_horizontal_fov", false, null, false)]
		float GetHorizontalFov(UIntPtr cameraPointer);

		// Token: 0x06000263 RID: 611
		[EngineMethod("viewport_point_to_world_ray", false, null, false)]
		void ViewportPointToWorldRay(UIntPtr cameraPointer, ref Vec3 rayBegin, ref Vec3 rayEnd, Vec3 viewportPoint);

		// Token: 0x06000264 RID: 612
		[EngineMethod("world_point_to_viewport_point", false, null, false)]
		Vec3 WorldPointToViewportPoint(UIntPtr cameraPointer, ref Vec3 worldPoint);

		// Token: 0x06000265 RID: 613
		[EngineMethod("encloses_point", false, null, false)]
		bool EnclosesPoint(UIntPtr cameraPointer, Vec3 pointInWorldSpace);

		// Token: 0x06000266 RID: 614
		[EngineMethod("construct_camera_from_position_elevation_bearing", false, null, false)]
		void ConstructCameraFromPositionElevationBearing(Vec3 position, float elevation, float bearing, ref MatrixFrame outFrame);
	}
}
