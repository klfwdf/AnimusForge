using System;
using TaleWorlds.Library;

namespace TaleWorlds.Engine
{
	// Token: 0x02000019 RID: 25
	[ApplicationInterfaceBase]
	internal interface IPath
	{
		// Token: 0x0600011B RID: 283
		[EngineMethod("get_number_of_points", false, null, false)]
		int GetNumberOfPoints(UIntPtr ptr);

		// Token: 0x0600011C RID: 284
		[EngineMethod("get_hermite_frame_wrt_dt", false, null, false)]
		void GetHermiteFrameForDt(UIntPtr ptr, ref MatrixFrame frame, float phase, int firstPoint);

		// Token: 0x0600011D RID: 285
		[EngineMethod("get_hermite_frame_wrt_distance", false, null, false)]
		void GetHermiteFrameForDistance(UIntPtr ptr, ref MatrixFrame frame, float distance);

		// Token: 0x0600011E RID: 286
		[EngineMethod("get_nearest_hermite_frame_with_valid_alpha_wrt_distance", false, null, false)]
		void GetNearestHermiteFrameWithValidAlphaForDistance(UIntPtr ptr, ref MatrixFrame frame, float distance, bool searchForward, float alphaThreshold);

		// Token: 0x0600011F RID: 287
		[EngineMethod("get_hermite_frame_and_color_wrt_distance", false, null, false)]
		void GetHermiteFrameAndColorForDistance(UIntPtr ptr, out MatrixFrame frame, out Vec3 color, float distance);

		// Token: 0x06000120 RID: 288
		[EngineMethod("get_arc_length", false, null, false)]
		float GetArcLength(UIntPtr ptr, int firstPoint);

		// Token: 0x06000121 RID: 289
		[EngineMethod("get_points", false, null, false)]
		void GetPoints(UIntPtr ptr, MatrixFrame[] points);

		// Token: 0x06000122 RID: 290
		[EngineMethod("get_path_length", false, null, false)]
		float GetTotalLength(UIntPtr ptr);

		// Token: 0x06000123 RID: 291
		[EngineMethod("get_path_version", false, null, false)]
		int GetVersion(UIntPtr ptr);

		// Token: 0x06000124 RID: 292
		[EngineMethod("set_frame_of_point", false, null, false)]
		void SetFrameOfPoint(UIntPtr ptr, int pointIndex, ref MatrixFrame frame);

		// Token: 0x06000125 RID: 293
		[EngineMethod("set_tangent_position_of_point", false, null, false)]
		void SetTangentPositionOfPoint(UIntPtr ptr, int pointIndex, int tangentIndex, ref Vec3 position);

		// Token: 0x06000126 RID: 294
		[EngineMethod("add_path_point", false, null, false)]
		int AddPathPoint(UIntPtr ptr, int newNodeIndex);

		// Token: 0x06000127 RID: 295
		[EngineMethod("delete_path_point", false, null, false)]
		void DeletePathPoint(UIntPtr ptr, int newNodeIndex);

		// Token: 0x06000128 RID: 296
		[EngineMethod("has_valid_alpha_at_path_point", false, null, false)]
		bool HasValidAlphaAtPathPoint(UIntPtr ptr, int nodeIndex, float alphaThreshold);

		// Token: 0x06000129 RID: 297
		[EngineMethod("get_name", false, null, false)]
		string GetName(UIntPtr ptr);
	}
}
