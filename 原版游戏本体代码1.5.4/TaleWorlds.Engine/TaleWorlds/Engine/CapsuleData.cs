using TaleWorlds.DotNet;
using TaleWorlds.Library;

namespace TaleWorlds.Engine;

[EngineStruct("rglCapsule_data", false, null)]
public struct CapsuleData
{
	private FtlCapsuleData _globalData;

	private FtlCapsuleData _localData;

	public Vec3 P1
	{
		get
		{
			return _globalData.P1;
		}
		set
		{
			_globalData.P1 = value;
		}
	}

	public Vec3 P2
	{
		get
		{
			return _globalData.P2;
		}
		set
		{
			_globalData.P2 = value;
		}
	}

	public float Radius
	{
		get
		{
			return _globalData.Radius;
		}
		set
		{
			_globalData.Radius = value;
		}
	}

	internal float LocalRadius
	{
		get
		{
			return _localData.Radius;
		}
		set
		{
			_localData.Radius = value;
		}
	}

	internal Vec3 LocalP1
	{
		get
		{
			return _localData.P1;
		}
		set
		{
			_localData.P1 = value;
		}
	}

	internal Vec3 LocalP2
	{
		get
		{
			return _localData.P2;
		}
		set
		{
			_localData.P2 = value;
		}
	}

	public CapsuleData(float radius, Vec3 p1, Vec3 p2)
	{
		_globalData = new FtlCapsuleData(radius, p1, p2);
		_localData = new FtlCapsuleData(radius, p1, p2);
	}

	public (Vec3, Vec3) GetBoxMinMax()
	{
		(float, float) tuple = MathF.MinMax(P1.x, P2.x);
		float item = tuple.Item1;
		float item2 = tuple.Item2;
		(float, float) tuple2 = MathF.MinMax(P1.y, P2.y);
		float item3 = tuple2.Item1;
		float item4 = tuple2.Item2;
		(float, float) tuple3 = MathF.MinMax(P1.z, P2.z);
		float item5 = tuple3.Item1;
		float item6 = tuple3.Item2;
		Vec3 item7 = new Vec3(item - Radius, item3 - Radius, item5 - Radius);
		Vec3 item8 = new Vec3(item2 + Radius, item4 + Radius, item6 + Radius);
		return (item7, item8);
	}
}
