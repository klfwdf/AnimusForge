using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using TaleWorlds.DotNet;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade;

[Serializable]
[EngineStruct("Navigation_data", false, null)]
public struct NavigationData
{
	private const int MaxPathSize = 1024;

	[MarshalAs(UnmanagedType.ByValArray, SizeConst = 1024)]
	public Vec2[] Points;

	public Vec3 StartPoint;

	public Vec3 EndPoint;

	public readonly int PointSize;

	public readonly float AgentRadius;

	public NavigationData(Vec3 startPoint, Vec3 endPoint, float agentRadius)
	{
		Points = new Vec2[1024];
		StartPoint = startPoint;
		EndPoint = endPoint;
		Points[0] = startPoint.AsVec2;
		Points[1] = endPoint.AsVec2;
		PointSize = 2;
		AgentRadius = agentRadius;
	}

	[Conditional("DEBUG")]
	public void TickDebug()
	{
		for (int i = 0; i < PointSize - 1; i++)
		{
		}
	}
}
