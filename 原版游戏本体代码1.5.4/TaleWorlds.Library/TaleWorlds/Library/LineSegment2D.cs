namespace TaleWorlds.Library;

public struct LineSegment2D
{
	public Vec2 Point1;

	public Vec2 Point2;

	public Vec2 Normal;

	public Vec2 this[int index]
	{
		get
		{
			switch (index)
			{
			case 0:
				return Point1;
			case 1:
				return Point2;
			default:
				Debug.FailedAssert("Invalid index", "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\Base\\TaleWorlds.Library\\LineSegment2D.cs", "Item", 30);
				return Vec2.Invalid;
			}
		}
	}

	public LineSegment2D(Vec2 point1, Vec2 point2)
	{
		Point1 = point1;
		Point2 = point2;
		Normal = (point1 - point2).Normalized().RightVec();
	}
}
