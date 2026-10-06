using System;

namespace TaleWorlds.Library;

[Serializable]
public struct Vec3i
{
	public int X;

	public int Y;

	public int Z;

	public static readonly Vec3i Zero = new Vec3i(0, 0, 0);

	public int this[int index]
	{
		get
		{
			return index switch
			{
				1 => Y, 
				0 => X, 
				_ => Z, 
			};
		}
		set
		{
			switch (index)
			{
			case 0:
				X = value;
				break;
			case 1:
				Y = value;
				break;
			default:
				Z = value;
				break;
			}
		}
	}

	public Vec3i(int x = 0, int y = 0, int z = 0)
	{
		X = x;
		Y = y;
		Z = z;
	}

	public static bool operator ==(Vec3i v1, Vec3i v2)
	{
		if (v1.X == v2.X && v1.Y == v2.Y)
		{
			return v1.Z == v2.Z;
		}
		return false;
	}

	public static bool operator !=(Vec3i v1, Vec3i v2)
	{
		if (v1.X == v2.X && v1.Y == v2.Y)
		{
			return v1.Z != v2.Z;
		}
		return true;
	}

	public Vec3 ToVec3()
	{
		return new Vec3(X, Y, Z);
	}

	public static Vec3i operator *(Vec3i v, int mult)
	{
		return new Vec3i(v.X * mult, v.Y * mult, v.Z * mult);
	}

	public static Vec3i operator +(Vec3i v1, Vec3i v2)
	{
		return new Vec3i(v1.X + v2.X, v1.Y + v2.Y, v1.Z + v2.Z);
	}

	public static Vec3i operator -(Vec3i v1, Vec3i v2)
	{
		return new Vec3i(v1.X - v2.X, v1.Y - v2.Y, v1.Z - v2.Z);
	}

	public override bool Equals(object obj)
	{
		if (obj == null || GetType() != obj.GetType())
		{
			return false;
		}
		if (((Vec3i)obj).X == X && ((Vec3i)obj).Y == Y)
		{
			return ((Vec3i)obj).Z == Z;
		}
		return false;
	}

	public override int GetHashCode()
	{
		return (((X * 397) ^ Y) * 397) ^ Z;
	}

	public override string ToString()
	{
		return string.Format("{0}: {1}, {2}: {3}, {4}: {5}", "X", X, "Y", Y, "Z", Z);
	}
}
