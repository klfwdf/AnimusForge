using System.Diagnostics;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade;

public class MovementPath
{
	private float[] _lineLengthAccumulations;

	private NavigationData _navigationData;

	private int LineCount => _navigationData.PointSize - 1;

	public Vec2 InitialDirection { get; }

	public Vec2 FinalDirection { get; }

	public Vec3 Destination => _navigationData.EndPoint;

	public MovementPath(NavigationData navigationData, Vec2 initialDirection, Vec2 finalDirection)
	{
		_navigationData = navigationData;
		InitialDirection = initialDirection;
		FinalDirection = finalDirection;
	}

	public MovementPath(Vec3 currentPosition, Vec3 orderPosition, float agentRadius, Vec2 previousDirection, Vec2 finalDirection)
		: this(new NavigationData(currentPosition, orderPosition, agentRadius), previousDirection, finalDirection)
	{
	}

	private void UpdateLineLengths()
	{
		if (_lineLengthAccumulations != null)
		{
			return;
		}
		_lineLengthAccumulations = new float[LineCount];
		for (int i = 0; i < LineCount; i++)
		{
			_lineLengthAccumulations[i] = (_navigationData.Points[i + 1] - _navigationData.Points[i]).Length;
			if (i > 0)
			{
				_lineLengthAccumulations[i] += _lineLengthAccumulations[i - 1];
			}
		}
	}

	private float GetPathProggress(Vec2 point, int lineIndex)
	{
		UpdateLineLengths();
		float num = _lineLengthAccumulations[LineCount - 1];
		if (num == 0f)
		{
			return 1f;
		}
		return (((lineIndex > 0) ? _lineLengthAccumulations[lineIndex - 1] : 0f) + (point - _navigationData.Points[lineIndex]).Length) / num;
	}

	private void GetClosestPointTo(Vec2 point, out Vec2 closest, out int lineIndex)
	{
		closest = Vec2.Invalid;
		lineIndex = -1;
		float num = float.MaxValue;
		for (int i = 0; i < LineCount; i++)
		{
			Vec2 closestPointOnLineSegmentToPoint = MBMath.GetClosestPointOnLineSegmentToPoint(in _navigationData.Points[i], in _navigationData.Points[i + 1], in point);
			float num2 = closestPointOnLineSegmentToPoint.DistanceSquared(point);
			if (num2 < num)
			{
				num = num2;
				closest = closestPointOnLineSegmentToPoint;
				lineIndex = i;
			}
		}
	}

	[Conditional("DEBUG")]
	public void TickDebug(Vec2 position)
	{
		GetClosestPointTo(position, out var closest, out var lineIndex);
		float pathProggress = GetPathProggress(closest, lineIndex);
		Vec2.Slerp(InitialDirection, FinalDirection, pathProggress).Normalize();
	}
}
