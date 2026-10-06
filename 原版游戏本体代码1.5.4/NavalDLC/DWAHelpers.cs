using System.Runtime.CompilerServices;
using NavalDLC.DWA;
using TaleWorlds.Library;

public static class DWAHelpers
{
	private const float Epsilon = 1E-06f;

	public static float AgentToAgentSignedClearance(in Vec2 center1, in Vec2 dir1, in Vec2 halfSize1, in Vec2 center2, in Vec2 dir2, in Vec2 halfSize2)
	{
		OBBAxes(in dir1, out var xSide, out var yFwd);
		OBBAxes(in dir2, out var xSide2, out var yFwd2);
		Vec2 centerDiff = center2 - center1;
		bool separated = false;
		float maxGap = 0f;
		float minOverlap = float.MaxValue;
		CheckAxisSeparationBetweenOBBs(in xSide, in centerDiff, in xSide, in yFwd, in halfSize1, in xSide2, in yFwd2, in halfSize2, ref separated, ref maxGap, ref minOverlap);
		CheckAxisSeparationBetweenOBBs(in yFwd, in centerDiff, in xSide, in yFwd, in halfSize1, in xSide2, in yFwd2, in halfSize2, ref separated, ref maxGap, ref minOverlap);
		CheckAxisSeparationBetweenOBBs(in xSide2, in centerDiff, in xSide, in yFwd, in halfSize1, in xSide2, in yFwd2, in halfSize2, ref separated, ref maxGap, ref minOverlap);
		CheckAxisSeparationBetweenOBBs(in yFwd2, in centerDiff, in xSide, in yFwd, in halfSize1, in xSide2, in yFwd2, in halfSize2, ref separated, ref maxGap, ref minOverlap);
		if (!separated)
		{
			return 0f - minOverlap;
		}
		return maxGap;
	}

	public static float AgentToConvexPolySignedClearance(in Vec2 center, in Vec2 dir, in Vec2 half, Vec2[] verts, int count, out bool overlap)
	{
		OBBAxes(in dir, out var xSide, out var yFwd);
		bool separated = false;
		float maxGap = 0f;
		float minOverlap = float.MaxValue;
		for (int i = 0; i < count; i++)
		{
			Vec2 vec = verts[i];
			Vec2 vec2 = verts[(i + 1) % count] - vec;
			if (vec2.Normalize() <= 1E-06f)
			{
				continue;
			}
			Vec2 axis = vec2.RightVec();
			ProjectPolyOnAxis(in axis, verts, count, out var dMin, out var dMax);
			float num = Vec2.DotProduct(center, axis);
			float num2 = ProjectOBBOnAxis(in axis, in xSide, in yFwd, in half);
			float num3 = num - num2;
			float num4 = num + num2;
			if (num4 < dMin)
			{
				separated = true;
				float num5 = dMin - num4;
				if (num5 > maxGap)
				{
					maxGap = num5;
				}
			}
			else if (dMax < num3)
			{
				separated = true;
				float num6 = num3 - dMax;
				if (num6 > maxGap)
				{
					maxGap = num6;
				}
			}
			else
			{
				float num7 = MathF.Min(num4, dMax) - MathF.Max(num3, dMin);
				if (num7 < minOverlap)
				{
					minOverlap = num7;
				}
			}
		}
		CheckAxisSeparationBetweenOBBAndPoly(in xSide, in center, in xSide, in yFwd, in half, verts, count, ref separated, ref maxGap, ref minOverlap);
		CheckAxisSeparationBetweenOBBAndPoly(in yFwd, in center, in xSide, in yFwd, in half, verts, count, ref separated, ref maxGap, ref minOverlap);
		overlap = !separated;
		if (!separated)
		{
			return 0f - minOverlap;
		}
		return maxGap;
	}

	private static void CheckAxisSeparationBetweenOBBs(in Vec2 axis, in Vec2 centerDiff, in Vec2 side1, in Vec2 fwd1, in Vec2 half1, in Vec2 side2, in Vec2 fwd2, in Vec2 half2, ref bool separated, ref float maxGap, ref float minOverlap)
	{
		float num = ProjectOBBOnAxis(in axis, in side1, in fwd1, in half1);
		float num2 = ProjectOBBOnAxis(in axis, in side2, in fwd2, in half2);
		float num3 = MathF.Abs(Vec2.DotProduct(centerDiff, axis)) - (num + num2);
		if (num3 > 0f)
		{
			separated = true;
			if (num3 > maxGap)
			{
				maxGap = num3;
			}
		}
		else
		{
			float num4 = 0f - num3;
			if (num4 < minOverlap)
			{
				minOverlap = num4;
			}
		}
	}

	private static void CheckAxisSeparationBetweenOBBAndPoly(in Vec2 axis, in Vec2 center, in Vec2 side, in Vec2 fwd, in Vec2 half, Vec2[] verts, int count, ref bool separated, ref float maxGap, ref float minOverlap)
	{
		ProjectPolyOnAxis(in axis, verts, count, out var dMin, out var dMax);
		float num = ProjectOBBOnAxis(in axis, in side, in fwd, in half);
		float num2 = Vec2.DotProduct(center, axis);
		float num3 = num2 - num;
		float num4 = num2 + num;
		if (num4 < dMin)
		{
			separated = true;
			float num5 = dMin - num4;
			if (num5 > maxGap)
			{
				maxGap = num5;
			}
		}
		else if (dMax < num3)
		{
			separated = true;
			float num6 = num3 - dMax;
			if (num6 > maxGap)
			{
				maxGap = num6;
			}
		}
		else
		{
			float num7 = MathF.Min(num4, dMax) - MathF.Max(num3, dMin);
			if (num7 < minOverlap)
			{
				minOverlap = num7;
			}
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static float ProjectOBBOnAxis(in Vec2 axis, in Vec2 side, in Vec2 fwd, in Vec2 half)
	{
		return MathF.Abs(Vec2.DotProduct(side, axis)) * half.x + MathF.Abs(Vec2.DotProduct(fwd, axis)) * half.y;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void ProjectPolyOnAxis(in Vec2 axis, Vec2[] verts, int vertexCount, out float dMin, out float dMax)
	{
		dMax = (dMin = Vec2.DotProduct(verts[0], axis));
		for (int i = 1; i < vertexCount; i++)
		{
			float num = Vec2.DotProduct(verts[i], axis);
			if (num < dMin)
			{
				dMin = num;
			}
			if (num > dMax)
			{
				dMax = num;
			}
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void OBBAxes(in Vec2 forward, out Vec2 xSide, out Vec2 yFwd)
	{
		yFwd = forward;
		xSide = -forward.LeftVec();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static void ReadStaticObstacle(DWAObstacleVertex obstacleVertex, Vec2[] obsVertices, out int obsVertexCount)
	{
		DWAObstacleVertex dWAObstacleVertex = obstacleVertex;
		int num = 0;
		do
		{
			obsVertices[num] = dWAObstacleVertex.Point;
			num++;
			dWAObstacleVertex = dWAObstacleVertex.Next;
		}
		while (dWAObstacleVertex != obstacleVertex && num < obsVertices.Length);
		obsVertexCount = num;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static float GateNear(float distance, float gateLength, float gateStart = 0f)
	{
		float num = gateLength;
		if (num < 1E-06f)
		{
			num = 1E-06f;
		}
		float edge = gateStart + num;
		float num2 = MBMath.SmoothStep(gateStart, edge, distance);
		return 1f - num2;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static float GateFar(float distance, float gateLength, float gateStart = 0f)
	{
		float num = gateLength;
		if (num < 1E-06f)
		{
			num = 1E-06f;
		}
		float edge = gateStart + num;
		return MBMath.SmoothStep(gateStart, edge, distance);
	}
}
