using TaleWorlds.DotNet;
using TaleWorlds.Library;

namespace TaleWorlds.Engine;

[EngineStruct("ftlCapsule_data", false, null)]
internal struct FtlCapsuleData
{
	public Vec3 P1;

	public Vec3 P2;

	public float Radius;

	public Vec3 GetBoxMin()
	{
		return new Vec3(MathF.Min(P1.x, P2.x) - Radius, MathF.Min(P1.y, P2.y) - Radius, MathF.Min(P1.z, P2.z) - Radius);
	}

	public Vec3 GetBoxMax()
	{
		return new Vec3(MathF.Max(P1.x, P2.x) + Radius, MathF.Max(P1.y, P2.y) + Radius, MathF.Max(P1.z, P2.z) + Radius);
	}

	public FtlCapsuleData(float radius, Vec3 p1, Vec3 p2)
	{
		P1 = p1;
		P2 = p2;
		Radius = radius;
	}
}
