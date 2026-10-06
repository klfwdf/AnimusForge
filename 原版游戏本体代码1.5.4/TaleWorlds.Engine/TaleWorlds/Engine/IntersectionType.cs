using TaleWorlds.DotNet;

namespace TaleWorlds.Engine;

[EngineStruct("rglIntersection::Intersection_type", false, null)]
public enum IntersectionType : uint
{
	Body,
	Terrain,
	Invalid
}
