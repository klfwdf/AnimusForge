using System;
using TaleWorlds.DotNet;
using TaleWorlds.Library;

namespace TaleWorlds.Engine;

[EngineStruct("rglIntersection", false, null)]
public struct Intersection
{
	[CustomEngineStructMemberData("part")]
	internal UIntPtr doNotUse;

	[CustomEngineStructMemberData("collided_material")]
	internal UIntPtr doNotUse2;

	public float Penetration;

	[CustomEngineStructMemberData("intersection_type")]
	public IntersectionType Type;

	[CustomEngineStructMemberData("intersection_details")]
	public IntersectionDetails Details;

	public Vec3 IntersectionPoint;

	public Vec3 IntersectionNormal;

	public static bool DoSegmentsIntersect(Vec2 line1Start, Vec2 line1Direction, Vec2 line2Start, Vec2 line2Direction, ref Vec2 intersectionPoint)
	{
		return EngineApplicationInterface.IBodyPart.DoSegmentsIntersect(line1Start, line1Direction, line2Start, line2Direction, ref intersectionPoint);
	}
}
