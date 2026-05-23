using System;
using TaleWorlds.DotNet;
using TaleWorlds.Library;

namespace TaleWorlds.Engine
{
	// Token: 0x02000097 RID: 151
	[EngineStruct("rglTwo_dimension_mesh_draw_data", false, null)]
	public struct TwoDimensionMeshDrawData
	{
		// Token: 0x040001D5 RID: 469
		public MatrixFrame MatrixFrame;

		// Token: 0x040001D6 RID: 470
		public Vec3 ClipRectInfo;

		// Token: 0x040001D7 RID: 471
		public Vec3 Uvs;

		// Token: 0x040001D8 RID: 472
		public Vec2 SpriteSize;

		// Token: 0x040001D9 RID: 473
		public Vec2 ScreenSize;

		// Token: 0x040001DA RID: 474
		public Vec2 ScreenScale;

		// Token: 0x040001DB RID: 475
		public Vec3 NinePatchBorders;

		// Token: 0x040001DC RID: 476
		public Vec2 ClipCircleCenter;

		// Token: 0x040001DD RID: 477
		public float ClipCircleRadius;

		// Token: 0x040001DE RID: 478
		public float ClipCircleSmoothingRadius;

		// Token: 0x040001DF RID: 479
		public uint Color;

		// Token: 0x040001E0 RID: 480
		public float ColorFactor;

		// Token: 0x040001E1 RID: 481
		public float AlphaFactor;

		// Token: 0x040001E2 RID: 482
		public float HueFactor;

		// Token: 0x040001E3 RID: 483
		public float SaturationFactor;

		// Token: 0x040001E4 RID: 484
		public float ValueFactor;

		// Token: 0x040001E5 RID: 485
		public Vec2 OverlayOffset;

		// Token: 0x040001E6 RID: 486
		public Vec2 OverlayScale;

		// Token: 0x040001E7 RID: 487
		public int Layer;
	}
}
