using TaleWorlds.DotNet;
using TaleWorlds.Library;

namespace TaleWorlds.Engine;

[EngineStruct("rglTwo_dimension_mesh_draw_data", false, null)]
public struct TwoDimensionMeshDrawData
{
	public MatrixFrame MatrixFrame;

	public Vec3 ClipRectInfo;

	public Vec3 Uvs;

	public Vec2 SpriteSize;

	public Vec2 ScreenSize;

	public Vec2 ScreenScale;

	public Vec3 NinePatchBorders;

	public Vec2 ClipCircleCenter;

	public float ClipCircleRadius;

	public float ClipCircleSmoothingRadius;

	public uint Color;

	public float ColorFactor;

	public float AlphaFactor;

	public float HueFactor;

	public float SaturationFactor;

	public float ValueFactor;

	public Vec2 OverlayOffset;

	public Vec2 OverlayScale;

	public int Layer;
}
