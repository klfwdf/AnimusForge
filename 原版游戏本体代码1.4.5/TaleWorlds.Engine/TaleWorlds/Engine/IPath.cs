using System;
using TaleWorlds.Library;

namespace TaleWorlds.Engine;

[ApplicationInterfaceBase]
internal interface IPath
{
	[EngineMethod("get_number_of_points", false, null, false)]
	int GetNumberOfPoints(UIntPtr ptr);

	[EngineMethod("get_hermite_frame_wrt_dt", false, null, false)]
	void GetHermiteFrameForDt(UIntPtr ptr, ref MatrixFrame frame, float phase, int firstPoint);

	[EngineMethod("get_hermite_frame_wrt_distance", false, null, false)]
	void GetHermiteFrameForDistance(UIntPtr ptr, ref MatrixFrame frame, float distance);

	[EngineMethod("get_nearest_hermite_frame_with_valid_alpha_wrt_distance", false, null, false)]
	void GetNearestHermiteFrameWithValidAlphaForDistance(UIntPtr ptr, ref MatrixFrame frame, float distance, bool searchForward, float alphaThreshold);

	[EngineMethod("get_hermite_frame_and_color_wrt_distance", false, null, false)]
	void GetHermiteFrameAndColorForDistance(UIntPtr ptr, out MatrixFrame frame, out Vec3 color, float distance);

	[EngineMethod("get_arc_length", false, null, false)]
	float GetArcLength(UIntPtr ptr, int firstPoint);

	[EngineMethod("get_points", false, null, false)]
	void GetPoints(UIntPtr ptr, MatrixFrame[] points);

	[EngineMethod("get_path_length", false, null, false)]
	float GetTotalLength(UIntPtr ptr);

	[EngineMethod("get_path_version", false, null, false)]
	int GetVersion(UIntPtr ptr);

	[EngineMethod("set_frame_of_point", false, null, false)]
	void SetFrameOfPoint(UIntPtr ptr, int pointIndex, ref MatrixFrame frame);

	[EngineMethod("set_tangent_position_of_point", false, null, false)]
	void SetTangentPositionOfPoint(UIntPtr ptr, int pointIndex, int tangentIndex, ref Vec3 position);

	[EngineMethod("add_path_point", false, null, false)]
	int AddPathPoint(UIntPtr ptr, int newNodeIndex);

	[EngineMethod("delete_path_point", false, null, false)]
	void DeletePathPoint(UIntPtr ptr, int newNodeIndex);

	[EngineMethod("has_valid_alpha_at_path_point", false, null, false)]
	bool HasValidAlphaAtPathPoint(UIntPtr ptr, int nodeIndex, float alphaThreshold);

	[EngineMethod("get_name", false, null, false)]
	string GetName(UIntPtr ptr);
}
