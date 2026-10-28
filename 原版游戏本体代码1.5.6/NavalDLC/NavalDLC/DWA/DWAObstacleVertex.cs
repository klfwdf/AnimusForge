using TaleWorlds.Library;

namespace NavalDLC.DWA;

public class DWAObstacleVertex : IDWAObstacleVertex
{
	private Vec2 _direction;

	int IDWAObstacleVertex.Id => Id;

	Vec2 IDWAObstacleVertex.Point => Point;

	float IDWAObstacleVertex.PointZ => PointZ;

	public int Id { get; }

	public Vec2 Point { get; internal set; }

	public Vec3 Point3D => Point.ToVec3(PointZ);

	public float PointZ { get; internal set; }

	public Vec2 Direction
	{
		get
		{
			return _direction;
		}
		internal set
		{
			_direction = value;
		}
	}

	public DWAObstacleVertex Previous { get; internal set; }

	public DWAObstacleVertex Next { get; internal set; }

	public bool IsConvex { get; internal set; }

	internal DWAObstacleVertex(int id)
	{
		Id = id;
		Point = Vec2.Invalid;
		PointZ = 0f;
		Direction = Vec2.Forward;
		Previous = null;
		Next = null;
		IsConvex = false;
	}
}
