using System;

namespace TaleWorlds.Library;

[Serializable]
public struct Vec2i : IEquatable<Vec2i>
{
	public int X;

	public int Y;

	public static readonly Vec2i Side = new Vec2i(1);

	public static readonly Vec2i Forward = new Vec2i(0, 1);

	public static readonly Vec2i One = new Vec2i(1, 1);

	public static readonly Vec2i Zero = new Vec2i(0, 0);

	public int Item1 => X;

	public int Item2 => Y;

	public Vec2i(int x = 0, int y = 0)
	{
		X = x;
		Y = y;
	}

	public static bool operator ==(Vec2i a, Vec2i b)
	{
		if (a.X == b.X)
		{
			return a.Y == b.Y;
		}
		return false;
	}

	public static bool operator !=(Vec2i a, Vec2i b)
	{
		if (a.X == b.X)
		{
			return a.Y != b.Y;
		}
		return true;
	}

	public override bool Equals(object obj)
	{
		if (obj == null || GetType() != obj.GetType())
		{
			return false;
		}
		if (((Vec2i)obj).X == X)
		{
			return ((Vec2i)obj).Y == Y;
		}
		return false;
	}

	public bool Equals(Vec2i value)
	{
		if (value.X == X)
		{
			return value.Y == Y;
		}
		return false;
	}

	public override int GetHashCode()
	{
		return (23 * 31 + X.GetHashCode()) * 31 + Y.GetHashCode();
	}
}
