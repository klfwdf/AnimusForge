namespace TaleWorlds.Library;

public struct Ray
{
	private Vec3 _origin;

	private Vec3 _direction;

	private float _maxDistance;

	public Vec3 Origin
	{
		get
		{
			return _origin;
		}
		private set
		{
			_origin = value;
		}
	}

	public Vec3 Direction
	{
		get
		{
			return _direction;
		}
		private set
		{
			_direction = value;
		}
	}

	public float MaxDistance
	{
		get
		{
			return _maxDistance;
		}
		private set
		{
			_maxDistance = value;
		}
	}

	public Vec3 EndPoint => Origin + Direction * MaxDistance;

	public Ray(Vec3 origin, Vec3 direction, float maxDistance = float.MaxValue)
	{
		this = default(Ray);
		Reset(origin, direction, maxDistance);
	}

	public Ray(Vec3 origin, Vec3 direction, bool useDirectionLenForMaxDistance)
	{
		_origin = origin;
		_direction = direction;
		float maxDistance = _direction.Normalize();
		if (useDirectionLenForMaxDistance)
		{
			_maxDistance = maxDistance;
		}
		else
		{
			_maxDistance = float.MaxValue;
		}
	}

	public void Reset(Vec3 origin, Vec3 direction, float maxDistance = float.MaxValue)
	{
		_origin = origin;
		_direction = direction;
		_maxDistance = maxDistance;
		_direction.Normalize();
	}
}
