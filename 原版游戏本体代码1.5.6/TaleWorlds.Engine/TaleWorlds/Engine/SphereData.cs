using TaleWorlds.DotNet;
using TaleWorlds.Library;

namespace TaleWorlds.Engine;

[EngineStruct("ftlSphere_data", false, null)]
public struct SphereData(float radius, Vec3 origin)
{
	public Vec3 Origin = origin;

	public float Radius = radius;
}
