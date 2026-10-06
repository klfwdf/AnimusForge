using TaleWorlds.DotNet;
using TaleWorlds.Library;

namespace TaleWorlds.Engine;

[EngineStruct("ftlSphere_data", false, null)]
public struct SphereData
{
	public Vec3 Origin;

	public float Radius;

	public SphereData(float radius, Vec3 origin)
	{
		Radius = radius;
		Origin = origin;
	}
}
