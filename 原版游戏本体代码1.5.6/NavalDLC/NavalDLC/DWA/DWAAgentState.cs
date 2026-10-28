using TaleWorlds.Library;

namespace NavalDLC.DWA;

public struct DWAAgentState
{
	public Vec2 Position;

	public float PositionZ;

	public Vec2 Direction;

	public Vec2 LinearVelocity;

	public float AngularVelocity;

	public float LinearAcceleration;

	public float AngularAcceleration;

	public Vec2 ShapeOffset;

	public Vec2 ShapeHalfSize;

	public Vec2 ShapeCenter => Position + Direction * ShapeOffset.Y - Direction.LeftVec() * ShapeOffset.X * ShapeHalfSize.X;

	public float MaxExtent => MathF.Max(ShapeHalfSize.x, ShapeHalfSize.y);

	public float MinExtent => MathF.Min(ShapeHalfSize.x, ShapeHalfSize.y);

	public Vec3 Position3D => Position.ToVec3(PositionZ);
}
