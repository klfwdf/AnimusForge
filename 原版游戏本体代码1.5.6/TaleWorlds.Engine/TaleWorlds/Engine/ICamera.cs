using System;
using TaleWorlds.Library;

namespace TaleWorlds.Engine;

[ApplicationInterfaceBase]
internal interface ICamera
{
	[EngineMethod("release", false, null, false)]
	void Release(UIntPtr cameraPointer);

	[EngineMethod("set_entity", false, null, false)]
	void SetEntity(UIntPtr cameraPointer, UIntPtr entityId);

	[EngineMethod("get_entity", false, null, false)]
	GameEntity GetEntity(UIntPtr cameraPointer);

	[EngineMethod("create_camera", false, null, false)]
	Camera CreateCamera();

	[EngineMethod("release_camera_entity", false, null, false)]
	void ReleaseCameraEntity(UIntPtr cameraPointer);

	[EngineMethod("look_at", false, null, false)]
	void LookAt(UIntPtr cameraPointer, Vec3 position, Vec3 target, Vec3 upVector);

	[EngineMethod("screen_space_ray_projection", false, null, false)]
	void ScreenSpaceRayProjection(UIntPtr cameraPointer, Vec2 screenPosition, ref Vec3 rayBegin, ref Vec3 rayEnd);

	[EngineMethod("check_entity_visibility", false, null, false)]
	bool CheckEntityVisibility(UIntPtr cameraPointer, UIntPtr entityPointer);

	[EngineMethod("set_position", false, null, false)]
	void SetPosition(UIntPtr cameraPointer, Vec3 position);

	[EngineMethod("set_view_volume", false, null, false)]
	void SetViewVolume(UIntPtr cameraPointer, bool perspective, float dLeft, float dRight, float dBottom, float dTop, float dNear, float dFar);

	[EngineMethod("get_near_plane_points_static", false, null, false)]
	void GetNearPlanePointsStatic(ref MatrixFrame cameraFrame, float verticalFov, float aspectRatioXY, float newDNear, float newDFar, Vec3[] nearPlanePoints);

	[EngineMethod("get_near_plane_points", false, null, false)]
	void GetNearPlanePoints(UIntPtr cameraPointer, Vec3[] nearPlanePoints);

	[EngineMethod("set_fov_vertical", false, null, false)]
	void SetFovVertical(UIntPtr cameraPointer, float verticalFov, float aspectRatio, float newDNear, float newDFar);

	[EngineMethod("get_view_proj_matrix", false, null, false)]
	void GetViewProjMatrix(UIntPtr cameraPointer, ref MatrixFrame frame);

	[EngineMethod("set_fov_horizontal", false, null, false)]
	void SetFovHorizontal(UIntPtr cameraPointer, float horizontalFov, float aspectRatio, float newDNear, float newDFar);

	[EngineMethod("get_fov_vertical", false, null, false)]
	float GetFovVertical(UIntPtr cameraPointer);

	[EngineMethod("get_fov_horizontal", false, null, false)]
	float GetFovHorizontal(UIntPtr cameraPointer);

	[EngineMethod("get_aspect_ratio", false, null, false)]
	float GetAspectRatio(UIntPtr cameraPointer);

	[EngineMethod("fill_parameters_from", false, null, false)]
	void FillParametersFrom(UIntPtr cameraPointer, UIntPtr otherCameraPointer);

	[EngineMethod("render_frustrum", false, null, false)]
	void RenderFrustrum(UIntPtr cameraPointer);

	[EngineMethod("set_frame", false, null, false)]
	void SetFrame(UIntPtr cameraPointer, ref MatrixFrame frame);

	[EngineMethod("get_frame", false, null, false)]
	void GetFrame(UIntPtr cameraPointer, ref MatrixFrame outFrame);

	[EngineMethod("get_near", false, null, false)]
	float GetNear(UIntPtr cameraPointer);

	[EngineMethod("get_far", false, null, false)]
	float GetFar(UIntPtr cameraPointer);

	[EngineMethod("get_horizontal_fov", false, null, false)]
	float GetHorizontalFov(UIntPtr cameraPointer);

	[EngineMethod("viewport_point_to_world_ray", false, null, false)]
	void ViewportPointToWorldRay(UIntPtr cameraPointer, ref Vec3 rayBegin, ref Vec3 rayEnd, Vec3 viewportPoint);

	[EngineMethod("world_point_to_viewport_point", false, null, false)]
	Vec3 WorldPointToViewportPoint(UIntPtr cameraPointer, ref Vec3 worldPoint);

	[EngineMethod("encloses_point", false, null, false)]
	bool EnclosesPoint(UIntPtr cameraPointer, Vec3 pointInWorldSpace);

	[EngineMethod("construct_camera_from_position_elevation_bearing", false, null, false)]
	void ConstructCameraFromPositionElevationBearing(Vec3 position, float elevation, float bearing, ref MatrixFrame outFrame);
}
