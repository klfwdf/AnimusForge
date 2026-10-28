using TaleWorlds.DotNet;

namespace TaleWorlds.Engine;

[EngineStruct("rglIntersection_details", false, null)]
public enum IntersectionDetails : uint
{
	None,
	Sphere,
	Plane,
	Capsule,
	Box,
	Convexmesh,
	Trianglemesh,
	Heightfield
}
